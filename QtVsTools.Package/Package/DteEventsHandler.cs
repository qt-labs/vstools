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

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;
using EnvDTE80;

namespace QtVsTools
{
    using Core;
    using QtMsBuild;

    class DteEventsHandler
    {
        private readonly DTE dte;
        private readonly SolutionEvents solutionEvents;
        private readonly BuildEvents buildEvents;
        private readonly DocumentEvents documentEvents;
        private readonly ProjectItemsEvents projectItemsEvents;
        private vsBuildAction currentBuildAction = vsBuildAction.vsBuildActionBuild;
        private VCProjectEngineEvents vcProjectEngineEvents;
        private readonly CommandEvents debugStartEvents;
        private readonly CommandEvents debugStartWithoutDebuggingEvents;
        private readonly CommandEvents f1HelpEvents;
        private WindowEvents windowEvents;
        private readonly int dispId_VCFileConfiguration_ExcludedFromBuild;
        private readonly int dispId_VCCLCompilerTool_UsePrecompiledHeader;
        private readonly int dispId_VCCLCompilerTool_PrecompiledHeaderThrough;
        private readonly int dispId_VCCLCompilerTool_PreprocessorDefinitions;
        private readonly int dispId_VCCLCompilerTool_AdditionalIncludeDirectories;

        public DteEventsHandler(DTE _dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            dte = _dte;
            var events = dte.Events as Events2;

            buildEvents = events.BuildEvents;
            buildEvents.OnBuildBegin += buildEvents_OnBuildBegin;
            buildEvents.OnBuildProjConfigBegin += OnBuildProjConfigBegin;

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

            windowEvents = events.WindowEvents;
            windowEvents.WindowActivated += WindowEvents_WindowActivated;

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

        private void WindowEvents_WindowActivated(Window gotFocus, Window lostFocus)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (dte.MainWindow?.Visible == true) {
                windowEvents.WindowActivated -= WindowEvents_WindowActivated;
                windowEvents = null;
                QtVsToolsPackage.Instance.VsMainWindowActivated();
            }
        }

        private void F1HelpEvents_BeforeExecute(
            string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (QtVsToolsPackage.Instance.Options.TryQtHelpOnF1Pressed && QtHelp.QueryEditorContextHelp())
                CancelDefault = true;
        }

        void debugStartEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                        QtVsToolsPackage.Instance.CopyNatvisFiles(versionInfo.Namespace());
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

        public void Disconnect()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (buildEvents != null) {
                buildEvents.OnBuildBegin -= buildEvents_OnBuildBegin;
                buildEvents.OnBuildProjConfigBegin -= OnBuildProjConfigBegin;
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

            if (vcProjectEngineEvents != null) {
                vcProjectEngineEvents.ItemPropertyChange -= OnVCProjectEngineItemPropertyChange;
                vcProjectEngineEvents.ItemPropertyChange2 -= OnVCProjectEngineItemPropertyChange2;
            }

            if (windowEvents != null)
                windowEvents.WindowActivated -= WindowEvents_WindowActivated;
        }

        public void OnBuildProjConfigBegin(string projectName, string projectConfig, string platform, string solutionConfig)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!QtVsToolsPackage.Instance.LegacyOptions.PreBuildSetup)
                return;

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
            if (project == null || !HelperFunctions.IsVsToolsProject(project))
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

        void buildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            currentBuildAction = Action;
        }

        public void DocumentSaved(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var qtPro = QtProject.Create(document.ProjectItem.ContainingProject);

            if (!HelperFunctions.IsVsToolsProject(qtPro.VCProject))
                return;

            var file = (VCFile)((IVCCollection)qtPro.VCProject.Files).Item(document.FullName);

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
                        foreach (VCFile fileInFilter in (IVCCollection)generatedFiles.Files) {
                            if (fileInFilter.Name == moccedFileName) {
                                foreach (VCFileConfiguration config in (IVCCollection)fileInFilter.FileConfigurations) {
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
                        foreach (VCFilter filt in (IVCCollection)generatedFiles.Filters) {
                            foreach (VCFile f in (IVCCollection)filt.Files) {
                                if (f.Name == moccedFileName) {
                                    foreach (VCFileConfiguration config in (IVCCollection)f.FileConfigurations) {
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
            ThreadHelper.ThrowIfNotOnUIThread();

            var project = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
            var qtPro = QtProject.Create(project);
            if (!HelperFunctions.IsVsToolsProject(project))
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
                    QtProjectIntellisense.Refresh(project);
                } else if (HelperFunctions.IsQrcFile(vcFile.Name)) {
                    if (!qtPro.IsQtMsBuildEnabled())
                        HelperFunctions.EnsureCustomBuildToolAvailable(projectItem);
                    qtPro.UpdateRccStep(vcFile, null);
                } else if (HelperFunctions.IsTranslationFile(vcFile.Name)) {
                    Translation.RunlUpdate(vcFile);
                }
            } catch { }
        }

        void ProjectItemsEvents_ItemRemoved(ProjectItem ProjectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
            if (pro == null)
                return;

            var qtPro = QtProject.Create(pro);
            qtPro.RemoveGeneratedFiles(ProjectItem.Name);
        }

        void ProjectItemsEvents_ItemRenamed(ProjectItem ProjectItem, string OldName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (OldName == null)
                return;
            var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
            if (pro == null)
                return;

            var qtPro = QtProject.Create(pro);
            qtPro.RemoveGeneratedFiles(OldName);

            ProjectItemsEvents_ItemAdded(ProjectItem);
        }

        void SolutionEvents_ProjectAdded(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (HelperFunctions.IsQtProject(project)) {
                InitializeVCProject(project);
                QtProjectTracker.Add(project);
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
                QtProjectIntellisense.Refresh(project);
            }
        }

        void SolutionEvents_ProjectRemoved(Project project)
        {
        }

        public void SolutionEvents_Opened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            QtProjectTracker.SolutionPath = QtVsToolsPackage.Instance.Dte.Solution.FullName;
            foreach (var p in HelperFunctions.ProjectsInSolution(QtVsToolsPackage.Instance.Dte)) {
                if (HelperFunctions.IsVsToolsProject(p)) {
                    InitializeVCProject(p);
                    QtProjectTracker.Add(p);
                }
            }
        }

        void SolutionEvents_AfterClosing()
        {
            QtProject.ClearInstances();
            QtProjectTracker.Reset();
            QtProjectTracker.SolutionPath = string.Empty;
        }

        void InitializeVCProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var project in HelperFunctions.ProjectsInSolution(dte)) {
                if (project != null && HelperFunctions.IsVsToolsProject(project))
                    InitializeVCProject(project);
            }
        }

        void InitializeVCProject(Project p)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vcProjectEngineEvents != null)
                return;

            var vcPrj = p.Object as VCProject;
            if (vcPrj == null)
                return;

            // Retrieves the VCProjectEngine from the given project and registers the handlers for VCProjectEngineEvents.
            if (vcPrj.VCProjectEngine is VCProjectEngine prjEngine) {
                vcProjectEngineEvents = prjEngine.Events as VCProjectEngineEvents;
                if (vcProjectEngineEvents != null) {
                    try {
                        vcProjectEngineEvents.ItemPropertyChange += OnVCProjectEngineItemPropertyChange;
                        vcProjectEngineEvents.ItemPropertyChange2 += OnVCProjectEngineItemPropertyChange2;
                    } catch {
                        Messages.DisplayErrorMessage("VCProjectEngine events could not be registered.");
                    }
                }
            }
        }

        private void OnVCProjectEngineItemPropertyChange(object item, object tool, int dispid)
        {
            var vcFileCfg = item as VCFileConfiguration;
            if (vcFileCfg == null) {
                // A global or project specific property has changed.

                var vcCfg = item as VCConfiguration;
                if (vcCfg == null)
                    return;
                var vcPrj = vcCfg.project as VCProject;
                if (vcPrj == null)
                    return;
                if (!HelperFunctions.IsVsToolsProject(vcPrj))
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
                if (!HelperFunctions.IsVsToolsProject(vcPrj))
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

        private void OnVCProjectEngineItemPropertyChange2(
            object item,
            string propertySheet,
            string itemType,
            string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!propertyName.StartsWith("Qt") || propertyName == "QtLastBackgroundBuild")
                return;
            if (item is VCConfiguration vcConfig
                && vcConfig.project is VCProject vcProject
                && vcProject.Object is Project project) {
                QtProjectIntellisense.Refresh(
                    QtProjectTracker.Get(project, project.FullName).Project, vcConfig.Name);
            }
        }

        private static VCFile GetVCFileFromProject(string absFileName, VCProject project)
        {
            foreach (VCFile f in (IVCCollection)project.Files) {
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
                    if (attribute is DispIdAttribute dispIdAttribute)
                        return dispIdAttribute.Value;
                }
            }
            return 0;
        }

    }
}
