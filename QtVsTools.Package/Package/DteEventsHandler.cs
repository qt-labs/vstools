/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools
{
    using Core;
    using Core.CMake;
    using Core.MsBuild;
    using VisualStudio;
    using static Utils;

    internal class DteEventsHandler
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

        public DteEventsHandler(DTE _dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            dte = _dte;
            var events = dte.Events as Events2;

            buildEvents = events?.BuildEvents;
            if (buildEvents != null) {
                buildEvents.OnBuildBegin += buildEvents_OnBuildBegin;
                buildEvents.OnBuildProjConfigBegin += OnBuildProjConfigBegin;
            }

            documentEvents = events?.DocumentEvents;
            if (documentEvents != null)
                documentEvents.DocumentSaved += DocumentSaved;

            projectItemsEvents = events?.ProjectItemsEvents;
            if (projectItemsEvents != null) {
                projectItemsEvents.ItemAdded += ProjectItemsEvents_ItemAdded;
                projectItemsEvents.ItemRemoved += ProjectItemsEvents_ItemRemoved;
                projectItemsEvents.ItemRenamed += ProjectItemsEvents_ItemRenamed;
            }

            solutionEvents = events?.SolutionEvents;
            if (solutionEvents != null) {
                solutionEvents.ProjectAdded += SolutionEvents_ProjectAdded;
                solutionEvents.Opened += SolutionEvents_Opened;
                solutionEvents.AfterClosing += SolutionEvents_AfterClosing;
            }

            windowEvents = events?.WindowEvents;
            if (windowEvents != null)
                windowEvents.WindowActivated += WindowEvents_WindowActivated;

            if (VsShell.FolderWorkspace.OnActiveWorkspaceChanged != null)
                VsShell.FolderWorkspace.OnActiveWorkspaceChanged += OnActiveWorkspaceChangedAsync;

            var debugCommandsGUID = "{5EFC7975-14BC-11CF-9B2B-00AA00573819}";
            debugStartEvents = events?.CommandEvents[debugCommandsGUID, 295];
            if (debugStartEvents != null)
                debugStartEvents.BeforeExecute += DebugStartEvents_BeforeExecute;

            debugStartWithoutDebuggingEvents = events?.CommandEvents[debugCommandsGUID, 368];
            if (debugStartWithoutDebuggingEvents != null)
                debugStartWithoutDebuggingEvents.BeforeExecute +=
                    DebugStartWithoutDebuggingEvents_BeforeExecute;

            f1HelpEvents = events?.CommandEvents[typeof(VSConstants.VSStd97CmdID).GUID.ToString("B"),
                (int)VSConstants.VSStd97CmdID.F1Help];
            if (f1HelpEvents != null)
                f1HelpEvents.BeforeExecute += F1HelpEvents_BeforeExecute;

            InitializeVCProjects();
        }

        private async Task OnActiveWorkspaceChangedAsync(object sender, EventArgs args)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            OnActiveWorkspaceChanged();
        }

        public void OnActiveWorkspaceChanged()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var workspace = VsShell.FolderWorkspace?.CurrentWorkspace;
            if (workspace != null)
                CMakeProject.Load(workspace);
            else
                CMakeProject.Unload();
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
            if (QtVsToolsPackage.Instance.Options.TryQtHelpOnF1Pressed) {
                if (!QtHelp.ShowEditorContextHelp()) {
                    Messages.Print("No help match was found. You can still try to search online at "
                        + "https://doc.qt.io" + ".", false, true);
                }
                CancelDefault = true;
            }
        }

        private void DebugStartEvents_BeforeExecute(string guid, int iD, object customIn,
            object customOut, ref bool cancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dte.Debugger is { CurrentMode: not dbgDebugMode.dbgDesignMode })
                return;
            if (HelperFunctions.GetSelectedQtProject(dte) is not {} qtProject)
                return;

            var versionInfo = qtProject.VersionInfo;
            if (!string.IsNullOrEmpty(versionInfo?.Namespace()))
                QtVsToolsPackage.Instance.CopyVisualizersFiles(versionInfo.Namespace());

            if (qtProject.FormatVersion >= ProjectFormat.Version.V3)
                return;

            // Notify about old project format and offer upgrade option.
            if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                QtProject.ShowUpdateFormatMessage();
        }

        private void DebugStartWithoutDebuggingEvents_BeforeExecute(string guid, int id,
            object customIn, object customOut, ref bool cancelDefault)
        {
            if (HelperFunctions.GetSelectedQtProject(dte) is not {} qtProject)
                return;

            if (qtProject.FormatVersion >= ProjectFormat.Version.V3)
                return;

            // Notify about old project format and offer upgrade option.
            if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                QtProject.ShowUpdateFormatMessage();
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
                solutionEvents.Opened -= SolutionEvents_Opened;
                solutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
            }

            if (debugStartEvents != null)
                debugStartEvents.BeforeExecute -= DebugStartEvents_BeforeExecute;

            if (debugStartWithoutDebuggingEvents != null)
                debugStartWithoutDebuggingEvents.BeforeExecute -= DebugStartWithoutDebuggingEvents_BeforeExecute;

            if (vcProjectEngineEvents != null) {
                vcProjectEngineEvents.ItemPropertyChange -= OnVCProjectEngineItemPropertyChange;
                vcProjectEngineEvents.ItemPropertyChange2 -= OnVCProjectEngineItemPropertyChange2;
            }

            if (windowEvents != null)
                windowEvents.WindowActivated -= WindowEvents_WindowActivated;
        }

        private void OnBuildProjConfigBegin(string projectName, string projectConfig,
            string platform, string solutionConfig)
        {
            if (currentBuildAction is not vsBuildAction.vsBuildActionBuild
                and not vsBuildAction.vsBuildActionRebuildAll) {
                return;     // Don't do anything, if we're not building.
            }

            bool Predicate(Project p)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return p.UniqueName == projectName;
            }
            if (HelperFunctions.ProjectsInSolution(dte).FirstOrDefault(Predicate) is not {} project)
                return;

            if (!HelperFunctions.IsVsToolsProject(project))
                return;

            // Notify about old project format and offer upgrade option.
            if (ProjectFormat.GetVersion(project) >= ProjectFormat.Version.V3)
                return;
            if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                QtProject.ShowUpdateFormatMessage();
        }

        private void buildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            currentBuildAction = action;
        }

        public void DocumentSaved(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var project = document.ProjectItem.ContainingProject;
            if (!HelperFunctions.IsVsToolsProject(project))
                return;

            if (QtProject.GetOrAdd(project) is not {} qtPro)
                return;

            var file = (VCFile)((IVCCollection)qtPro.VcProject.Files).Item(document.FullName);
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

            if (!HelperFunctions.IsSourceFile(file.Name))
                return;

            var moccedFileName = "moc_" + file.Name;
            var moccedFiles = qtPro.GetFilesFromProject(moccedFileName).ToList();
            if (!moccedFiles.Any())
                return;

            if (qtPro.IsMoccedFileIncluded(file)) {
                foreach (var moccedFile in moccedFiles)
                    QtProject.ExcludeFromAllBuilds(moccedFile);
                return;
            }

            var hasDifferentMocFilesPerConfig = QtVSIPSettings.HasDifferentMocFilePerConfig(project);
            var hasDifferentMocFilesPerPlatform = QtVSIPSettings.HasDifferentMocFilePerPlatform(project);
            var generatedFiles = qtPro.FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            foreach (VCFile fileInFilter in (IVCCollection)generatedFiles.Files) {
                if (fileInFilter.Name != moccedFileName)
                    continue;

                foreach (VCFileConfiguration config in (IVCCollection)fileInFilter.FileConfigurations) {
                    var exclude = true;
                    var vcConfig = config.ProjectConfiguration as VCConfiguration;
                    var platform = vcConfig.Platform as VCPlatform;
                    var fileInFilterLowered = fileInFilter.RelativePath.ToLower();

                    switch (hasDifferentMocFilesPerConfig) {
                    case true when hasDifferentMocFilesPerPlatform:
                        if (fileInFilterLowered.Contains(vcConfig.ConfigurationName.ToLower())
                            && fileInFilterLowered.Contains(platform.Name.ToLower()))
                            exclude = false;
                        break;
                    case true:
                        if (fileInFilterLowered.Contains(vcConfig.ConfigurationName.ToLower()))
                            exclude = false;
                        break;
                    default:
                        if (hasDifferentMocFilesPerPlatform) {
                            var platformName = platform.Name;
                            if (fileInFilterLowered.Contains(platformName.ToLower()))
                                exclude = false;
                        } else {
                            exclude = false;
                        }
                        break;
                    }
                    if (config.ExcludedFromBuild != exclude)
                        config.ExcludedFromBuild = exclude;
                }
            }

            foreach (VCFilter filt in (IVCCollection)generatedFiles.Filters) {
                foreach (VCFile f in (IVCCollection)filt.Files) {
                    if (f.Name != moccedFileName)
                        continue;
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

        public void ProjectItemsEvents_ItemAdded(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var qtPro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
            if (qtPro == null)
                return;

            if (GetVCFileFromProject(projectItem.Name, qtPro.VcProject) is not {} vcFile)
                return;

            try {
                if (HelperFunctions.IsSourceFile(vcFile.Name)) {
                    if (vcFile.Name.StartsWith("moc_", IgnoreCase))
                        return;
                    if (vcFile.Name.StartsWith("qrc_", IgnoreCase)) {
                        // Do not use precompiled headers with these files
                        QtProject.SetPCHOption(vcFile, pchOption.pchNone);
                        return;
                    }
                    var pcHeaderThrough = qtPro.GetPrecompiledHeaderThrough();
                    if (pcHeaderThrough != null) {
                        var pcHeaderCreator = pcHeaderThrough.Remove(pcHeaderThrough.LastIndexOf('.')) + ".cpp";
                        if (vcFile.Name.EndsWith(pcHeaderCreator, IgnoreCase)
                            && HelperFunctions.CxxFileContainsNotCommented(vcFile, "#include \""
                            + pcHeaderThrough + "\"", IgnoreCase, false)) {
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
                    if (vcFile.Name.StartsWith("ui_", IgnoreCase))
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
                    QtProjectIntellisense.Refresh(qtPro);
                } else if (HelperFunctions.IsQrcFile(vcFile.Name)) {
                    if (!qtPro.IsQtMsBuildEnabled())
                        HelperFunctions.EnsureCustomBuildToolAvailable(projectItem);
                    qtPro.UpdateRccStep(vcFile);
                } else if (HelperFunctions.IsTranslationFile(vcFile.Name)) {
                    Translation.RunlUpdate(vcFile);
                }
            } catch { }
        }

        private void ProjectItemsEvents_ItemRemoved(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte)
                ?.RemoveGeneratedFiles(projectItem.Name);
        }

        private void ProjectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (oldName == null)
                return;
            var pro = HelperFunctions.GetSelectedQtProject(QtVsToolsPackage.Instance.Dte);
            if (pro == null)
                return;

            pro.RemoveGeneratedFiles(oldName);
            ProjectItemsEvents_ItemAdded(projectItem);
        }

        private void SolutionEvents_ProjectAdded(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Ignore temp projects created by Qt/CMake wizard
            if (QtProject.GetOrAdd(project) is not {} qtProject)
                return;

            var activeConfiguration = qtProject.VcProject.ActiveConfiguration;
            if (QtProject.GetPropertyValue(activeConfiguration, "QT_CMAKE_TEMPLATE") == "true")
                return;

            var formatVersion = ProjectFormat.GetVersion(project);
            if (formatVersion >= ProjectFormat.Version.V3) {
                InitializeVCProject(project);
                QtProjectIntellisense.Refresh(qtProject);
            }

            if (formatVersion is < ProjectFormat.Version.V1 or >= ProjectFormat.Version.Latest)
                return;
            if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                QtProject.ShowUpdateFormatMessage();
        }

        public void SolutionEvents_Opened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            QtProjectTracker.SolutionPath = QtVsToolsPackage.Instance.Dte.Solution.FullName;
            foreach (var p in HelperFunctions.ProjectsInSolution(QtVsToolsPackage.Instance.Dte)) {
                var formatVersion = ProjectFormat.GetVersion(p);
                if (formatVersion >= ProjectFormat.Version.V3) {
                    InitializeVCProject(p);
                    QtProjectTracker.GetOrAdd(QtProject.GetOrAdd(p));
                }

                if (formatVersion is < ProjectFormat.Version.V1 or >= ProjectFormat.Version.Latest)
                    continue;
                if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                    QtProject.ShowUpdateFormatMessage();
            }
        }

        private void SolutionEvents_AfterClosing()
        {
            QtProject.Reset();
            QtProjectTracker.Reset();
            QtProjectTracker.SolutionPath = string.Empty;
        }

        private void InitializeVCProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var project in HelperFunctions.ProjectsInSolution(dte)) {
                if (project != null && HelperFunctions.IsVsToolsProject(project))
                    InitializeVCProject(project);
            }
        }

        private void InitializeVCProject(Project p)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vcProjectEngineEvents != null)
                return;

            if (p.Object is not VCProject {VCProjectEngine: VCProjectEngine prjEngine})
                return;

            // Retrieves the VCProjectEngine from the given project and
            // registers the handlers for VCProjectEngineEvents.
            vcProjectEngineEvents = prjEngine.Events as VCProjectEngineEvents;
            if (vcProjectEngineEvents == null)
                return;
            try {
                vcProjectEngineEvents.ItemPropertyChange += OnVCProjectEngineItemPropertyChange;
                vcProjectEngineEvents.ItemPropertyChange2 += OnVCProjectEngineItemPropertyChange2;
            } catch {
                Messages.DisplayErrorMessage("VCProjectEngine events could not be registered.");
            }
        }

        private void OnVCProjectEngineItemPropertyChange(object item, object tool, int dispId)
        {
            var vcPrj = item switch
            {
                VCFileConfiguration {File: VCFile vcFile} => vcFile.project as VCProject,
                VCConfiguration vcCfg => vcCfg.project as VCProject,
                _ => null
            };

            if (ProjectFormat.GetVersion(vcPrj) >= ProjectFormat.Version.V3ClProperties)
                return; // Ignore property events when using shared compiler properties

            if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                QtProject.ShowUpdateFormatMessage();
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

            if (item is not VCConfiguration {project: VCProject {Object: Project project}} vcConfig)
                return;

            QtProjectIntellisense.Refresh(QtProject.GetOrAdd(project), vcConfig.Name);
        }

        private static VCFile GetVCFileFromProject(string absFileName, VCProject project)
        {
            foreach (VCFile f in (IVCCollection)project.Files) {
                if (f.Name.Equals(absFileName, IgnoreCase))
                    return f;
            }
            return null;
        }
    }
}
