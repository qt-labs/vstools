/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

using Tasks = System.Threading.Tasks;

namespace QtVsTools
{
    using Core;
    using Core.CMake;
    using Core.MsBuild;
    using Core.Options;
    using VisualStudio;
    using static Core.Common.Utils;

    internal class DteEventsHandler : IVsDebuggerEvents
    {
        private readonly DTE dte;
        private readonly SolutionEvents solutionEvents;
        private readonly DocumentEvents documentEvents;
        private readonly ProjectItemsEvents projectItemsEvents;
        private VCProjectEngineEvents vcProjectEngineEvents;
        private readonly CommandEvents f1HelpEvents;
        private WindowEvents windowEvents;
        private OutputWindowEvents outputWindowEvents;

        public DteEventsHandler(DTE _dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            dte = _dte;
            var events = dte.Events as Events2;

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
                solutionEvents.ProjectRemoved += SolutionEvents_ProjectRemoved;
                solutionEvents.Opened += SolutionEvents_Opened;
                solutionEvents.AfterClosing += SolutionEvents_AfterClosing;
            }

            if (dte.MainWindow?.Visible == false) {
                windowEvents = events?.WindowEvents;
                if (windowEvents != null)
                    windowEvents.WindowActivated += WindowEvents_WindowActivated;
            } else {
                VsMainWindowActivated(); // might happen without splash screen, direct .sln loading
            }

            outputWindowEvents = events?.OutputWindowEvents;
            if (outputWindowEvents != null)
                outputWindowEvents.PaneUpdated += OutputWindowEvents_PaneUpdated;

            if (VsShell.FolderWorkspace.OnActiveWorkspaceChanged != null)
                VsShell.FolderWorkspace.OnActiveWorkspaceChanged += OnActiveWorkspaceChangedAsync;

            f1HelpEvents = events?.CommandEvents[typeof(VSConstants.VSStd97CmdID).GUID.ToString("B"),
                (int)VSConstants.VSStd97CmdID.F1Help];
            if (f1HelpEvents != null)
                f1HelpEvents.BeforeExecute += F1HelpEvents_BeforeExecute;

            foreach (var vcProject in HelperFunctions.ProjectsInSolution(dte)) {
                if (MsBuildProject.GetOrAdd(vcProject) is {} project)
                    InitializeMsBuildProjectProject(project);
            }

            if (VsServiceProvider.GetService<IVsDebugger, IVsDebugger>() is {} service)
                service.AdviseDebuggerEvents(this, out _);
        }

        private async Tasks.Task OnActiveWorkspaceChangedAsync(object sender, EventArgs args)
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
            Instances.QmlLspClient?.Disconnect();
        }

        private void WindowEvents_WindowActivated(Window gotFocus, Window lostFocus)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (dte.MainWindow?.Visible == true) {
                windowEvents.WindowActivated -= WindowEvents_WindowActivated;
                windowEvents = null;
                VsMainWindowActivated();
            }
        }

        private static void VsMainWindowActivated()
        {
            if (QtVersionManager.GetVersions().Length == 0)
                Notifications.NoQtVersion.Show();
            if (QtOptionsPage.NotifyInstalled && TestVersionInstalled())
                Notifications.NotifyInstall.Show();
            if (QtOptionsPage.NotifySearchDevRelease)
                Notifications.NotifySearchDevRelease.Show();
            if (QtOptionsPage.AutoActivatePane)
                Messages.ActivateMessagePane();
        }

        private static bool TestVersionInstalled()
        {
            var newVersion = true;
            var versionFile = Path.Combine(QtVsToolsPackage.Instance.PkgInstallPath,
                "lastversion.txt");
            if (File.Exists(versionFile)) {
                var lastVersion = File.ReadAllText(versionFile);
                newVersion = lastVersion!= Version.PRODUCT_VERSION;
            }
            if (newVersion)
                File.WriteAllText(versionFile, Version.PRODUCT_VERSION);
            return newVersion;
        }

        private void OutputWindowEvents_PaneUpdated(EnvDTE.OutputWindowPane pane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pane is not { Name: "Solution" })
                return;
            var dteProjects = dte.Solution.Projects.Cast<Project>().ToList();
            foreach (var dteProject in dteProjects) {
                if (MsBuildProject.GetOrAdd(dteProject.Object as VCProject) is not { } project)
                    continue;
                if (project.GetPropertyValue("QT_CMAKE_TEMPLATE") != "true")
                    continue;
                outputWindowEvents.PaneUpdated -= OutputWindowEvents_PaneUpdated;
                pane.Clear();
                outputWindowEvents.PaneUpdated += OutputWindowEvents_PaneUpdated;
                return;
            }
        }

        private void F1HelpEvents_BeforeExecute(
            string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (QtOptionsPage.TryQtHelpOnF1Pressed) {
                if (!QtHelp.ShowEditorContextHelp()) {
                    Messages.Print("No help match was found. You can still try to search online at "
                        + "https://doc.qt.io" + ".", false, true);
                }
                CancelDefault = true;
            }
        }

        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            if (dbgmodeNew == DBGMODE.DBGMODE_Run
                && HelperFunctions.GetSelectedQtProject(dte) is { } project) {
                var @namespace = project.VersionInfo?.Namespace;
                if (!string.IsNullOrEmpty(@namespace))
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(() =>
                        QtVsToolsPackage.Instance.CopyVisualizersFilesAsync(@namespace)
                    );
            }
            return VSConstants.S_OK;
        }

        public void Disconnect()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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

            if (vcProjectEngineEvents != null)
                vcProjectEngineEvents.ItemPropertyChange2 -= OnVcProjectEngineItemPropertyChange2;

            if (windowEvents != null)
                windowEvents.WindowActivated -= WindowEvents_WindowActivated;

            if (outputWindowEvents != null)
                outputWindowEvents.PaneUpdated -= OutputWindowEvents_PaneUpdated;
        }

        private void DocumentSaved(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (document.ProjectItem?.ContainingProject?.Object is not VCProject vcProject)
                return;

            if (MsBuildProject.GetOrAdd(vcProject) is not {} qtPro)
                return;

            if (qtPro.VcProject.Files is not IVCCollection files)
                return;

            if (files.Item(document.FullName) is not VCFile file)
                return;

            if (HelperFunctions.IsUicFile(file.Name) && !QtUic.HasUicItemType(file)) {
                QtUic.SetUicItemType(file);
                return;
            }

            if (HelperFunctions.IsQrcFile(file.Name) && !QtRcc.HasRccItemType(file)) {
                QtRcc.SetRccItemType(file);
                return;
            }

            if (!HelperFunctions.IsSourceFile(file.Name) && !HelperFunctions.IsHeaderFile(file.Name))
                return;

            if (HelperFunctions.HasQObjectDeclaration(file)) {
                if (!QtMoc.HasMocItemType(file))
                    QtMoc.SetMocItemType(file);
            } else {
                if (QtMoc.HasMocItemType(file))
                    QtMoc.RemoveMocItemType(file);
            }
        }

        private void ProjectItemsEvents_ItemAdded(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (HelperFunctions.GetSelectedQtProject(dte) is not {} qtPro)
                return;

            if (qtPro.VcProject.Files is not IVCCollection projectFiles)
                return;

            var vcFile = projectFiles.Cast<VCFile>().FirstOrDefault(file =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return file.Name.Equals(projectItem.Name, IgnoreCase);
            });

            var vcFileName = vcFile?.Name;
            if (string.IsNullOrEmpty(vcFileName))
                return;

            try {
                if (HelperFunctions.IsSourceFile(vcFileName)) {
                    if (vcFileName.StartsWith("moc_", IgnoreCase))
                        return;
                    if (vcFileName.StartsWith("qrc_", IgnoreCase)) {
                        // Do not use precompiled headers with these files
                        MsBuildProject.SetPCHOption(vcFile, pchOption.pchNone);
                        return;
                    }
                    var pcHeaderThrough = qtPro.GetPrecompiledHeaderThrough();
                    if (pcHeaderThrough != null) {
                        var pcHeaderCreator = pcHeaderThrough.Remove(pcHeaderThrough.LastIndexOf('.')) + ".cpp";
                        if (vcFileName.EndsWith(pcHeaderCreator, IgnoreCase)
                            && CxxStream.ContainsNotCommented(vcFile,
                                $"#include \"{pcHeaderThrough}\"", IgnoreCase, false)) {
                            //File is used to create precompiled headers
                            MsBuildProject.SetPCHOption(vcFile, pchOption.pchCreateUsingSpecific);
                            return;
                        }
                    }
                    if (HelperFunctions.HasQObjectDeclaration(vcFile))
                        QtMoc.SetMocItemType(vcFile);
                } else if (HelperFunctions.IsHeaderFile(vcFileName)) {
                    if (vcFileName.StartsWith("ui_", IgnoreCase))
                        return;
                    if (HelperFunctions.HasQObjectDeclaration(vcFile))
                        QtMoc.SetMocItemType(vcFile);
                } else if (HelperFunctions.IsUicFile(vcFileName)) {
                    QtUic.SetUicItemType(vcFile);
                    qtPro.Refresh();
                } else if (HelperFunctions.IsQrcFile(vcFileName)) {
                    QtRcc.SetRccItemType(vcFile);
                } else if (HelperFunctions.IsTranslationFile(vcFileName)) {
                    Translation.RunlUpdate(vcFile);
                }
            } catch { }
        }

        private void ProjectItemsEvents_ItemRemoved(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (HelperFunctions.GetSelectedQtProject(dte) is not {} project)
                return;
            project.RemoveGeneratedFiles(projectItem.Name);
        }

        private void ProjectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(oldName))
                return;
            if (HelperFunctions.GetSelectedQtProject(dte) is not {} project)
                return;

            project.RemoveGeneratedFiles(oldName);
            ProjectItemsEvents_ItemAdded(projectItem);
        }

        private void SolutionEvents_ProjectAdded(EnvDTE.Project dteProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Ignore non-VC projects and temp projects created by Qt/CMake wizard
            if (MsBuildProject.GetOrAdd(dteProject.Object as VCProject) is not { } project
                || project.GetPropertyValue("QT_CMAKE_TEMPLATE") == "true") {
                return;
            }

            InitializeMsBuildProjectProject(project);
        }

        private static void SolutionEvents_ProjectRemoved(EnvDTE.Project dteProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            MsBuildProject.Remove(dteProject.FullName);
        }

        public void SolutionEvents_Opened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var vcProject in HelperFunctions.ProjectsInSolution(dte)) {
                if (MsBuildProject.GetOrAdd(vcProject) is {} project)
                    InitializeMsBuildProjectProject(project);
            }
        }

        private static void SolutionEvents_AfterClosing()
        {
            MsBuildProject.Reset();
            Instances.QmlLspClient?.Disconnect();
        }

        // Retrieves the VCProjectEngine from the given project and registers a handler for
        // VCProjectEngineEvents.
        private void InitializeMsBuildProjectProject(MsBuildProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            project.SolutionPath = dte.Solution.FullName;
            project.Refresh(); //forcefully update IntelliSense

            if (vcProjectEngineEvents != null)
                return;

            if (project?.VcProject is not { VCProjectEngine: VCProjectEngine vcProjectEngine })
                return;

            vcProjectEngineEvents = vcProjectEngine.Events as VCProjectEngineEvents;
            if (vcProjectEngineEvents == null)
                return;
            try {
                vcProjectEngineEvents.ItemPropertyChange2 += OnVcProjectEngineItemPropertyChange2;
            } catch {
                Messages.DisplayErrorMessage("VCProjectEngine events could not be registered.");
            }
        }

        private static void OnVcProjectEngineItemPropertyChange2(object item, string propertySheet,
            string itemType, string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item is not VCConfiguration vcConfiguration)
                return;

            if (MsBuildProject.GetOrAdd(vcConfiguration.project as VCProject) is not {} project)
                return;

            if (!propertyName.StartsWith("Qt") || propertyName == "QtTouchProperty")
                return;

            project.Refresh(vcConfiguration.Name);
        }
    }
}
