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
        private readonly DocumentEvents documentEvents;
        private readonly ProjectItemsEvents projectItemsEvents;
        private VCProjectEngineEvents vcProjectEngineEvents;
        private readonly CommandEvents debugStartEvents;
        private readonly CommandEvents f1HelpEvents;
        private WindowEvents windowEvents;

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

            f1HelpEvents = events?.CommandEvents[typeof(VSConstants.VSStd97CmdID).GUID.ToString("B"),
                (int)VSConstants.VSStd97CmdID.F1Help];
            if (f1HelpEvents != null)
                f1HelpEvents.BeforeExecute += F1HelpEvents_BeforeExecute;

            foreach (var vcProject in HelperFunctions.ProjectsInSolution(dte)) {
                if (MsBuildProject.GetOrAdd(vcProject) is {} project)
                    InitializeMsBuildProjectProject(project);
            }
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

            if (HelperFunctions.GetSelectedQtProject(dte) is not {} project)
                return;

            var versionInfo = project.VersionInfo;
            if (!string.IsNullOrEmpty(versionInfo?.Namespace()))
                QtVsToolsPackage.Instance.CopyVisualizersFiles(versionInfo.Namespace());
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

            if (debugStartEvents != null)
                debugStartEvents.BeforeExecute -= DebugStartEvents_BeforeExecute;

            if (vcProjectEngineEvents != null)
                vcProjectEngineEvents.ItemPropertyChange2 -= OnVcProjectEngineItemPropertyChange2;

            if (windowEvents != null)
                windowEvents.WindowActivated -= WindowEvents_WindowActivated;
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

            // Ignore temp projects created by Qt/CMake wizard
            if (MsBuildProject.GetOrAdd(dteProject.Object as VCProject) is not {} project)
                return;

            // ignore temporary projects created by Qt/CMake wizard
            if (project.GetPropertyValue("QT_CMAKE_TEMPLATE") == "true")
                return;

            InitializeMsBuildProjectProject(project);
            project.Refresh();
        }

        public void SolutionEvents_Opened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var vcProject in HelperFunctions.ProjectsInSolution(dte)) {
                if (MsBuildProject.GetOrAdd(vcProject) is not {} project)
                    continue;

                InitializeMsBuildProjectProject(project);
                project.SolutionPath = dte.Solution.FullName;
            }
        }

        private static void SolutionEvents_AfterClosing()
        {
            MsBuildProject.Reset();
        }

        // Retrieves the VCProjectEngine from the given project and registers a handler for
        // VCProjectEngineEvents.
        private void InitializeMsBuildProjectProject(MsBuildProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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

            if (item is not VCConfiguration vcConfig)
                return;

            if (MsBuildProject.GetOrAdd(vcConfig.project as VCProject) is not {} project)
                return;

            if (!propertyName.StartsWith("Qt") || propertyName == "QtLastBackgroundBuild")
                return;

            project.Refresh(vcConfig.Name);
        }
    }
}
