/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using QtVsTools.Core;
using QtVsTools.Core.QtMsBuild;
using QtVsTools.QtMsBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace QtVsTools
{
    class DteEventsHandler
    {
        private DTE dte;
        private SolutionEvents solutionEvents;
        private BuildEvents buildEvents;
        private DocumentEvents documentEvents;
        private ProjectItemsEvents projectItemsEvents;
        private vsBuildAction currentBuildAction = vsBuildAction.vsBuildActionBuild;
        private VCProjectEngineEvents vcProjectEngineEvents;
        private CommandEvents debugStartEvents;
        private CommandEvents debugStartWithoutDebuggingEvents;
        private CommandEvents f1HelpEvents;
        private int dispId_VCFileConfiguration_ExcludedFromBuild;
        private int dispId_VCCLCompilerTool_UsePrecompiledHeader;
        private int dispId_VCCLCompilerTool_PrecompiledHeaderThrough;
        private int dispId_VCCLCompilerTool_PreprocessorDefinitions;
        private int dispId_VCCLCompilerTool_AdditionalIncludeDirectories;

        public DteEventsHandler(DTE _dte)
        {
            dte = _dte;
            var events = dte.Events as Events2;

            buildEvents = events.BuildEvents;
            buildEvents.OnBuildBegin += buildEvents_OnBuildBegin;
            buildEvents.OnBuildProjConfigBegin += OnBuildProjConfigBegin;
            buildEvents.OnBuildProjConfigDone += OnBuildProjConfigDone;
            buildEvents.OnBuildDone += buildEvents_OnBuildDone;

            documentEvents = events.get_DocumentEvents(null);
            documentEvents.DocumentSaved += DocumentSaved;

            projectItemsEvents = events.ProjectItemsEvents;
            projectItemsEvents.ItemAdded += ProjectItemsEvents_ItemAdded;
            projectItemsEvents.ItemRemoved += ProjectItemsEvents_ItemRemoved;
            projectItemsEvents.ItemRenamed += ProjectItemsEvents_ItemRenamed;

            solutionEvents = events.SolutionEvents;
            solutionEvents.ProjectAdded += SolutionEvents_ProjectAdded;
            solutionEvents.ProjectRemoved += SolutionEvents_ProjectRemoved;
            solutionEvents.Opened += SolutionEvents_Opened;
            solutionEvents.AfterClosing += SolutionEvents_AfterClosing;

            var debugCommandsGUID = "{5EFC7975-14BC-11CF-9B2B-00AA00573819}";
            debugStartEvents = events.get_CommandEvents(debugCommandsGUID, 295);
            debugStartEvents.BeforeExecute += debugStartEvents_BeforeExecute;

            debugStartWithoutDebuggingEvents = events.get_CommandEvents(debugCommandsGUID, 368);
            debugStartWithoutDebuggingEvents.BeforeExecute += debugStartWithoutDebuggingEvents_BeforeExecute;

            f1HelpEvents = events.get_CommandEvents(
                typeof(VSConstants.VSStd97CmdID).GUID.ToString("B"),
                (int)VSConstants.VSStd97CmdID.F1Help);
            f1HelpEvents.BeforeExecute += F1HelpEvents_BeforeExecute;

            dispId_VCFileConfiguration_ExcludedFromBuild = GetPropertyDispId(typeof(VCFileConfiguration), "ExcludedFromBuild");
            dispId_VCCLCompilerTool_UsePrecompiledHeader = GetPropertyDispId(typeof(VCCLCompilerTool), "UsePrecompiledHeader");
            dispId_VCCLCompilerTool_PrecompiledHeaderThrough = GetPropertyDispId(typeof(VCCLCompilerTool), "PrecompiledHeaderThrough");
            dispId_VCCLCompilerTool_PreprocessorDefinitions = GetPropertyDispId(typeof(VCCLCompilerTool), "PreprocessorDefinitions");
            dispId_VCCLCompilerTool_AdditionalIncludeDirectories = GetPropertyDispId(typeof(VCCLCompilerTool), "AdditionalIncludeDirectories");
            InitializeVCProjects();
        }

        private void F1HelpEvents_BeforeExecute(
            string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            if (Vsix.Instance.Options.TryQtHelpOnF1Pressed && QtHelp.QueryEditorContextHelp())
                CancelDefault = true;
        }

        void debugStartEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            var debugger = dte.Debugger;
            if (debugger != null && debugger.CurrentMode != dbgDebugMode.dbgDesignMode)
                return;
            var selectedProject = HelperFunctions.GetSelectedQtProject(dte);
            if (selectedProject != null) {
                if (QtProject.GetFormatVersion(selectedProject) >= Resources.qtMinFormatVersion_Settings)
                    return;
                var qtProject = QtProject.Create(selectedProject);
                if (qtProject != null) {
                    qtProject.SetQtEnvironment();

                    var qtVersion = qtProject.GetQtVersion();
                    var versionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
                    if (!string.IsNullOrEmpty(versionInfo.Namespace()))
                        Vsix.Instance.CopyNatvisFile(versionInfo.Namespace());
                }
            }
        }

        void debugStartWithoutDebuggingEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            var selectedProject = HelperFunctions.GetSelectedQtProject(dte);
            if (selectedProject != null) {
                if (QtProject.GetFormatVersion(selectedProject) >= Resources.qtMinFormatVersion_Settings)
                    return;
                var qtProject = QtProject.Create(selectedProject);
                if (qtProject != null)
                    qtProject.SetQtEnvironment();
            }
        }

#if DEBUG
        public void setDirectory(string dir, string value)
        {
            foreach (EnvDTE.Project project in HelperFunctions.ProjectsInSolution(dte)) {
                var vcProject = project.Object as VCProject;
                if (vcProject == null || vcProject.Files == null)
                    continue;
                var qtProject = QtProject.Create(project);
                if (qtProject == null)
                    continue;

                if (dir == "MocDir") {
                    var oldMocDir = QtVSIPSettings.GetMocDirectory(project);
                    QtVSIPSettings.SaveMocDirectory(project, value);
                    qtProject.UpdateMocSteps(oldMocDir);
                } else if (dir == "RccDir") {
                    var oldRccDir = QtVSIPSettings.GetRccDirectory(project);
                    QtVSIPSettings.SaveRccDirectory(project, value);
                    qtProject.RefreshRccSteps(oldRccDir);
                } else if (dir == "UicDir") {
                    var oldUicDir = QtVSIPSettings.GetUicDirectory(project);
                    QtVSIPSettings.SaveUicDirectory(project, value);
                    qtProject.UpdateUicSteps(oldUicDir, true);
                }
            }
        }
#endif

        public void OnQRCFileSaved(string fileName)
        {
            foreach (var project in HelperFunctions.ProjectsInSolution(dte)) {
                var vcProject = project.Object as VCProject;
                if (vcProject == null || vcProject.Files == null)
                    continue;

                var vcFile = (VCFile) ((IVCCollection) vcProject.Files).Item(fileName);
                if (vcFile == null)
                    continue;

                var qtProject = QtProject.Create(project);
                qtProject.UpdateRccStep(vcFile, null);
            }
        }

        public void Disconnect()
        {
            if (buildEvents != null) {
                buildEvents.OnBuildBegin -= buildEvents_OnBuildBegin;
                buildEvents.OnBuildProjConfigBegin -= OnBuildProjConfigBegin;
                buildEvents.OnBuildProjConfigDone -= OnBuildProjConfigDone;
                buildEvents.OnBuildDone -= buildEvents_OnBuildDone;
            }

            if (documentEvents != null)
                documentEvents.DocumentSaved -= DocumentSaved;

            if (projectItemsEvents != null) {
                projectItemsEvents.ItemAdded -= ProjectItemsEvents_ItemAdded;
                projectItemsEvents.ItemRemoved -= ProjectItemsEvents_ItemRemoved;
                projectItemsEvents.ItemRenamed -= ProjectItemsEvents_ItemRenamed;
            }

            if (solutionEvents != null) {
                solutionEvents.ProjectAdded -= SolutionEvents_ProjectAdded;
                solutionEvents.ProjectRemoved -= SolutionEvents_ProjectRemoved;
                solutionEvents.Opened -= SolutionEvents_Opened;
                solutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
            }

            if (debugStartEvents != null)
                debugStartEvents.BeforeExecute -= debugStartEvents_BeforeExecute;

            if (debugStartWithoutDebuggingEvents != null)
                debugStartWithoutDebuggingEvents.BeforeExecute -= debugStartWithoutDebuggingEvents_BeforeExecute;

            if (vcProjectEngineEvents != null)
                vcProjectEngineEvents.ItemPropertyChange -= OnVCProjectEngineItemPropertyChange;
        }

        public void OnBuildProjConfigBegin(string projectName, string projectConfig, string platform, string solutionConfig)
        {
            if (currentBuildAction != vsBuildAction.vsBuildActionBuild &&
                currentBuildAction != vsBuildAction.vsBuildActionRebuildAll) {
                return;     // Don't do anything, if we're not building.
            }

            Project project = null;
            foreach (var p in HelperFunctions.ProjectsInSolution(dte)) {
                if (p.UniqueName == projectName) {
                    project = p;
                    break;
                }
            }
            if (project == null || !HelperFunctions.IsQtProject(project))
                return;

            if (QtProject.GetFormatVersion(project) >= Resources.qtMinFormatVersion_Settings)
                return;

            var qtpro = QtProject.Create(project);
            var versionManager = QtVersionManager.The();
            var qtVersion = versionManager.GetProjectQtVersion(project, platform);
            if (qtVersion == null) {
                Messages.DisplayCriticalErrorMessage(SR.GetString("ProjectQtVersionNotFoundError", projectName, projectConfig, platform));
                dte.ExecuteCommand("Build.Cancel", "");
                return;
            }

            if (!QtVSIPSettings.GetDisableAutoMocStepsUpdate()) {
                if (qtpro.ConfigurationRowNamesChanged)
                    qtpro.UpdateMocSteps(QtVSIPSettings.GetMocDirectory(project));
            }

            // Solution config is given to function to get QTDIR property
            // set correctly also during batch build
            qtpro.SetQtEnvironment(qtVersion, solutionConfig, true);
            if (QtVSIPSettings.GetLUpdateOnBuild(project))
                Translation.RunlUpdate(project);
        }

        List<string[]> buildDoneEvents = new List<string[]>();

        public void OnBuildProjConfigDone(
            string projectName,
            string configName,
            string platformName,
            string configId,
            bool success)
        {
            if (currentBuildAction != vsBuildAction.vsBuildActionBuild &&
                currentBuildAction != vsBuildAction.vsBuildActionRebuildAll) {
                return;     // Don't do anything, if we're not building.
            }

            if (!success)
                return;

            buildDoneEvents.Add(new[] { projectName, configId });
        }

        void buildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            currentBuildAction = Action;
        }

        public void buildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            foreach (var buildDoneEvent in buildDoneEvents) {
                string projectName = buildDoneEvent[0];
                string configId = buildDoneEvent[1];

                Project project = null;
                foreach (var p in HelperFunctions.ProjectsInSolution(dte)) {
                    if (p.UniqueName == projectName) {
                        project = p;
                        break;
                    }
                }
                if (project == null || !HelperFunctions.IsQtProject(project))
                    continue;

                if (Vsix.Instance.Options.RefreshIntelliSenseOnBuild)
                    QtProjectTracker.RefreshIntelliSense(project, configId);
            }
            buildDoneEvents.Clear();
        }

        public void DocumentSaved(Document document)
        {
            var qtPro = QtProject.Create(document.ProjectItem.ContainingProject);

            if (!HelperFunctions.IsQtProject(qtPro.VCProject))
                return;

            var file = (VCFile) ((IVCCollection) qtPro.VCProject.Files).Item(document.FullName);

            if (HelperFunctions.IsUicFile(file.Name)) {
                if (QtVSIPSettings.AutoUpdateUicSteps() && !QtProject.HasUicStep(file))
                    qtPro.AddUic4BuildStep(file);
                return;
            }

            if (!HelperFunctions.IsSourceFile(file.Name) && !HelperFunctions.IsHeaderFile(file.Name))
                return;

            if (HelperFunctions.HasQObjectDeclaration(file)) {
                if (!qtPro.HasMocStep(file))
                    qtPro.AddMocStep(file);
            } else {
                if (qtPro.HasMocStep(file))
                    qtPro.RemoveMocStep(file);
            }

            if (HelperFunctions.IsSourceFile(file.Name)) {
                var moccedFileName = "moc_" + file.Name;

                if (qtPro.IsMoccedFileIncluded(file)) {
                    foreach (var moccedFile in qtPro.GetFilesFromProject(moccedFileName))
                        QtProject.ExcludeFromAllBuilds(moccedFile);
                } else {
                    var moccedFiles = qtPro.GetFilesFromProject(moccedFileName);
                    if (moccedFiles.Any()) {
                        var hasDifferentMocFilesPerConfig = QtVSIPSettings.HasDifferentMocFilePerConfig(qtPro.Project);
                        var hasDifferentMocFilesPerPlatform = QtVSIPSettings.HasDifferentMocFilePerPlatform(qtPro.Project);
                        var generatedFiles = qtPro.FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
                        foreach (VCFile fileInFilter in (IVCCollection) generatedFiles.Files) {
                            if (fileInFilter.Name == moccedFileName) {
                                foreach (VCFileConfiguration config in (IVCCollection) fileInFilter.FileConfigurations) {
                                    var exclude = true;
                                    var vcConfig = config.ProjectConfiguration as VCConfiguration;
                                    if (hasDifferentMocFilesPerConfig && hasDifferentMocFilesPerPlatform) {
                                        var platform = vcConfig.Platform as VCPlatform;
                                        if (fileInFilter.RelativePath.ToLower().Contains(vcConfig.ConfigurationName.ToLower())
                                            && fileInFilter.RelativePath.ToLower().Contains(platform.Name.ToLower()))
                                            exclude = false;
                                    } else if (hasDifferentMocFilesPerConfig) {
                                        if (fileInFilter.RelativePath.ToLower().Contains(vcConfig.ConfigurationName.ToLower()))
                                            exclude = false;
                                    } else if (hasDifferentMocFilesPerPlatform) {
                                        var platform = vcConfig.Platform as VCPlatform;
                                        var platformName = platform.Name;
                                        if (fileInFilter.RelativePath.ToLower().Contains(platformName.ToLower()))
                                            exclude = false;
                                    } else {
                                        exclude = false;
                                    }
                                    if (config.ExcludedFromBuild != exclude)
                                        config.ExcludedFromBuild = exclude;
                                }
                            }
                        }
                        foreach (VCFilter filt in (IVCCollection) generatedFiles.Filters) {
                            foreach (VCFile f in (IVCCollection) filt.Files) {
                                if (f.Name == moccedFileName) {
                                    foreach (VCFileConfiguration config in (IVCCollection) f.FileConfigurations) {
                                        var vcConfig = config.ProjectConfiguration as VCConfiguration;
                                        var filterToLookFor = string.Empty;
                                        if (hasDifferentMocFilesPerConfig)
                                            filterToLookFor = vcConfig.ConfigurationName;
                                        if (hasDifferentMocFilesPerPlatform) {
                                            var platform = vcConfig.Platform as VCPlatform;
                                            if (!string.IsNullOrEmpty(filterToLookFor))
                                                filterToLookFor += '_';
                                            filterToLookFor += platform.Name;
                                        }
                                        if (filt.Name == filterToLookFor) {
                                            if (config.ExcludedFromBuild)
                                                config.ExcludedFromBuild = false;
                                        } else {
                                            if (!config.ExcludedFromBuild)
                                                config.ExcludedFromBuild = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ProjectItemsEvents_ItemAdded(ProjectItem projectItem)
        {
            var project = HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte);
            var qtPro = QtProject.Create(project);
            if (!HelperFunctions.IsQtProject(project))
                return;
            var vcFile = GetVCFileFromProject(projectItem.Name, qtPro.VCProject);
            if (vcFile == null)
                return;

            try {
                if (HelperFunctions.IsSourceFile(vcFile.Name)) {
                    if (vcFile.Name.StartsWith("moc_", StringComparison.OrdinalIgnoreCase))
                        return;
                    if (vcFile.Name.StartsWith("qrc_", StringComparison.OrdinalIgnoreCase)) {
                        // Do not use precompiled headers with these files
                        QtProject.SetPCHOption(vcFile, pchOption.pchNone);
                        return;
                    }
                    var pcHeaderThrough = qtPro.GetPrecompiledHeaderThrough();
                    if (pcHeaderThrough != null) {
                        var pcHeaderCreator = pcHeaderThrough.Remove(pcHeaderThrough.LastIndexOf('.')) + ".cpp";
                        if (vcFile.Name.EndsWith(pcHeaderCreator, StringComparison.OrdinalIgnoreCase)
                            && HelperFunctions.CxxFileContainsNotCommented(vcFile, "#include \"" + pcHeaderThrough + "\"", StringComparison.OrdinalIgnoreCase, false)) {
                            //File is used to create precompiled headers
                            QtProject.SetPCHOption(vcFile, pchOption.pchCreateUsingSpecific);
                            return;
                        }
                    }
                    if (HelperFunctions.HasQObjectDeclaration(vcFile)) {
                        if (!qtPro.IsQtMsBuildEnabled())
                            HelperFunctions.EnsureCustomBuildToolAvailable(projectItem);
                        qtPro.AddMocStep(vcFile);
                    }
                } else if (HelperFunctions.IsHeaderFile(vcFile.Name)) {
                    if (vcFile.Name.StartsWith("ui_", StringComparison.OrdinalIgnoreCase))
                        return;
                    if (HelperFunctions.HasQObjectDeclaration(vcFile)) {
                        if (!qtPro.IsQtMsBuildEnabled())
                            HelperFunctions.EnsureCustomBuildToolAvailable(projectItem);
                        qtPro.AddMocStep(vcFile);
                    }
                } else if (HelperFunctions.IsUicFile(vcFile.Name)) {
                    if (!qtPro.IsQtMsBuildEnabled())
                        HelperFunctions.EnsureCustomBuildToolAvailable(projectItem);
                    qtPro.AddUic4BuildStep(vcFile);
                    if (Vsix.Instance.Options.RefreshIntelliSenseOnUiFile)
                        QtProjectTracker.RefreshIntelliSense(project, runQtTools: true);
                } else if (HelperFunctions.IsQrcFile(vcFile.Name)) {
                    if (!qtPro.IsQtMsBuildEnabled())
                        HelperFunctions.EnsureCustomBuildToolAvailable(projectItem);
                    qtPro.UpdateRccStep(vcFile, null);
                } else if (HelperFunctions.IsTranslationFile(vcFile.Name)) {
                }
            } catch { }
        }

        void ProjectItemsEvents_ItemRemoved(ProjectItem ProjectItem)
        {
            var pro = HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte);
            if (pro == null)
                return;

            var qtPro = QtProject.Create(pro);
            qtPro.RemoveGeneratedFiles(ProjectItem.Name);
        }

        void ProjectItemsEvents_ItemRenamed(ProjectItem ProjectItem, string OldName)
        {
            if (OldName == null)
                return;
            var pro = HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte);
            if (pro == null)
                return;

            var qtPro = QtProject.Create(pro);
            qtPro.RemoveGeneratedFiles(OldName);
            ProjectItemsEvents_ItemAdded(ProjectItem);
        }

        void SolutionEvents_ProjectAdded(Project project)
        {
            if (HelperFunctions.IsQMakeProject(project)) {
                InitializeVCProject(project);
                QtProjectTracker.AddProject(project, runQtTools: true);
                var vcpro = project.Object as VCProject;
                VCFilter filter = null;
                foreach (VCFilter f in vcpro.Filters as IVCCollection) {
                    if (f.Name == Filters.HeaderFiles().Name) {
                        filter = f;
                        break;
                    }
                }
                if (filter != null) {
                    foreach (VCFile file in filter.Files as IVCCollection) {
                        foreach (VCFileConfiguration config in file.FileConfigurations as IVCCollection) {
                            var tool = new QtCustomBuildTool(config);
                            var commandLine = tool.CommandLine;
                            if (!string.IsNullOrEmpty(commandLine) && commandLine.Contains("moc.exe")) {
                                var matches = Regex.Matches(commandLine, "[^ ^\n]+moc\\.(exe\"|exe)");
                                string qtDir;
                                if (matches.Count != 1) {
                                    var vm = QtVersionManager.The();
                                    qtDir = vm.GetInstallPath(vm.GetDefaultVersion());
                                } else {
                                    qtDir = matches[0].ToString().Trim('"');
                                    qtDir = qtDir.Remove(qtDir.LastIndexOf('\\'));
                                    qtDir = qtDir.Remove(qtDir.LastIndexOf('\\'));
                                }
                                qtDir = qtDir.Replace("_(QTDIR)", "$(QTDIR)");
                                HelperFunctions.SetDebuggingEnvironment(project, "PATH="
                                    + Path.Combine(qtDir, "bin") + ";$(PATH)", false, config.Name);
                            }
                        }
                    }
                }
            }
        }

        void SolutionEvents_ProjectRemoved(Project project)
        {
        }

        public void SolutionEvents_Opened()
        {
            foreach (var p in HelperFunctions.ProjectsInSolution(Vsix.Instance.Dte)) {
                if (HelperFunctions.IsQtProject(p)) {
                    InitializeVCProject(p);
                    QtProjectTracker.AddProject(p, runQtTools: false);
                }
            }
        }

        void SolutionEvents_AfterClosing()
        {
            QtProject.ClearInstances();
        }

        void InitializeVCProjects()
        {
            foreach (var project in HelperFunctions.ProjectsInSolution(dte)) {
                if (project != null && HelperFunctions.IsQtProject(project))
                    InitializeVCProject(project);
            }
        }

        void InitializeVCProject(Project p)
        {
            if (vcProjectEngineEvents != null)
                return;

            var vcPrj = p.Object as VCProject;
            if (vcPrj == null)
                return;

            // Retrieves the VCProjectEngine from the given project and registers the handlers for VCProjectEngineEvents.
            var prjEngine = vcPrj.VCProjectEngine as VCProjectEngine;
            if (prjEngine != null) {
                vcProjectEngineEvents = prjEngine.Events as VCProjectEngineEvents;
                if (vcProjectEngineEvents != null) {
                    try {
                        vcProjectEngineEvents.ItemPropertyChange += OnVCProjectEngineItemPropertyChange;
                    } catch {
                        Messages.DisplayErrorMessage("VCProjectEngine events could not be registered.");
                    }
                }
            }
        }

        private void OnVCProjectEngineItemPropertyChange(object item, object tool, int dispid)
        {
            //System.Diagnostics.Debug.WriteLine("OnVCProjectEngineItemPropertyChange " + dispid.ToString());
            var vcFileCfg = item as VCFileConfiguration;
            if (vcFileCfg == null) {
                // A global or project specific property has changed.

                var vcCfg = item as VCConfiguration;
                if (vcCfg == null)
                    return;
                var vcPrj = vcCfg.project as VCProject;
                if (vcPrj == null)
                    return;
                if (!HelperFunctions.IsQtProject(vcPrj))
                    return;
                // Ignore property events when using shared compiler properties
                if (QtProject.GetFormatVersion(vcPrj) >= Resources.qtMinFormatVersion_ClProperties)
                    return;

                if (dispid == dispId_VCCLCompilerTool_UsePrecompiledHeader
                    || dispid == dispId_VCCLCompilerTool_PrecompiledHeaderThrough
                    || dispid == dispId_VCCLCompilerTool_AdditionalIncludeDirectories
                    || dispid == dispId_VCCLCompilerTool_PreprocessorDefinitions) {
                    var qtPrj = QtProject.Create(vcPrj);
                    if (qtPrj.IsQtMsBuildEnabled()
                        && dispid == dispId_VCCLCompilerTool_AdditionalIncludeDirectories) {
                        qtPrj.RefreshQtMocIncludePath();

                    } else if (qtPrj.IsQtMsBuildEnabled()
                        && dispid == dispId_VCCLCompilerTool_PreprocessorDefinitions) {
                        qtPrj.RefreshQtMocDefine();

                    } else {
                        qtPrj.RefreshMocSteps();
                    }
                }
            } else {
                // A file specific property has changed.

                var vcFile = vcFileCfg.File as VCFile;
                if (vcFile == null)
                    return;
                var vcPrj = vcFile.project as VCProject;
                if (vcPrj == null)
                    return;
                if (!HelperFunctions.IsQtProject(vcPrj))
                    return;
                // Ignore property events when using shared compiler properties
                if (QtProject.GetFormatVersion(vcPrj) >= Resources.qtMinFormatVersion_ClProperties)
                    return;

                if (dispid == dispId_VCFileConfiguration_ExcludedFromBuild) {
                    var qtPrj = QtProject.Create(vcPrj);
                    qtPrj.OnExcludedFromBuildChanged(vcFile, vcFileCfg);
                } else if (dispid == dispId_VCCLCompilerTool_UsePrecompiledHeader
                      || dispid == dispId_VCCLCompilerTool_PrecompiledHeaderThrough
                      || dispid == dispId_VCCLCompilerTool_AdditionalIncludeDirectories
                      || dispid == dispId_VCCLCompilerTool_PreprocessorDefinitions) {
                    var qtPrj = QtProject.Create(vcPrj);
                    qtPrj.RefreshMocStep(vcFile);
                }
            }
        }

        private static VCFile GetVCFileFromProject(string absFileName, VCProject project)
        {
            foreach (VCFile f in (IVCCollection) project.Files) {
                if (f.Name.ToLower() == absFileName.ToLower())
                    return f;
            }
            return null;
        }

        /// <summary>
        /// Returns the COM DISPID of the given property.
        /// </summary>
        private static int GetPropertyDispId(Type type, string propertyName)
        {
            var pi = type.GetProperty(propertyName);
            if (pi != null) {
                foreach (Attribute attribute in pi.GetCustomAttributes(true)) {
                    var dispIdAttribute = attribute as DispIdAttribute;
                    if (dispIdAttribute != null)
                        return dispIdAttribute.Value;
                }
            }
            return 0;
        }

    }
}
