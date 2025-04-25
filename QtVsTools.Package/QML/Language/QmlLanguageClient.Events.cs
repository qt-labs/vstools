// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.ComponentModel.Composition;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Package.QML.Language
{
    using Lsp;
    using QtVsTools.Core.MsBuild;
    using VisualStudio;

    public partial class QmlLanguageClient
    {
        [Import(typeof(SVsServiceProvider))]
        public IServiceProvider ServiceProvider;

        [Import]
        public IVsFolderWorkspaceService WorkspaceService;

        private SolutionEvents solutionEvents;
        private VCProjectEngineEvents vcProjectEngineEvents;

        private const string DidChangeConfiguration = "workspace/didChangeConfiguration";
        private const string DidChangeWorkspaceFolders = "workspace/didChangeWorkspaceFolders";

        private async Task MonitorSolutionOrWorkspaceAsync()
        {
            await VsShell.UiThreadAsync(() =>
            {
#pragma warning disable VSTHRD010
                if (ServiceProvider.GetService(typeof(DTE)) is not EnvDTE80.DTE2 dte)
                    return;

                solutionEvents = dte.Events.SolutionEvents;
                solutionEvents.BeforeClosing += OnSolutionClosing;
                solutionEvents.ProjectAdded += OnProjectAdded;
                solutionEvents.ProjectRemoved += OnProjectRemoved;

                vcProjectEngineEvents =
                    dte.Events.GetObject("VCProjectEngineEventsObject") as VCProjectEngineEvents;
                if (vcProjectEngineEvents != null)
                    vcProjectEngineEvents.ItemPropertyChange2 += OnItemPropertyChange2;
#pragma warning restore VSTHRD010
            });

            WorkspaceService.OnActiveWorkspaceChanged += OnActiveWorkspaceChangedAsync;

            if (InitializeMessageSent)
                return;

            InitializeMessageSent = true;
            WorkspaceFolders = GetWorkspaceFolders();

            await NotifyDidChangeWorkspaceFoldersParamsAsync(WorkspaceFolders.ToArray(),
                Array.Empty<WorkspaceFolder>());
        }

        private void OnSolutionClosing()
        {
            _ = ThreadHelper.JoinableTaskContext.Factory.RunAsync(async () =>
            {
                await VsShell.UiThreadAsync(() =>
                {
                    if (solutionEvents != null) {
#pragma warning disable VSTHRD010
                        solutionEvents.BeforeClosing -= OnSolutionClosing;
                        solutionEvents.ProjectAdded -= OnProjectAdded;
                        solutionEvents.ProjectRemoved -= OnProjectRemoved;
                        solutionEvents = null;
#pragma warning restore VSTHRD010
                    }

                    if (vcProjectEngineEvents != null)
                        vcProjectEngineEvents.ItemPropertyChange2 -= OnItemPropertyChange2;
                    vcProjectEngineEvents = null;
                });

                WorkspaceService.OnActiveWorkspaceChanged -= OnActiveWorkspaceChangedAsync;

                if (!InitializeMessageSent)
                    return;

                await NotifyDidChangeWorkspaceFoldersParamsAsync(Array.Empty<WorkspaceFolder>(),
                    WorkspaceFolders.ToArray());

                WorkspaceFolders.Clear();
                InitializeMessageSent = false;
            });
        }

        private void OnProjectAdded(Project project)
        {
            _ = ThreadHelper.JoinableTaskContext.Factory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (MsBuildProject.GetOrAdd(project.Object as VCProject) is not { } qtProject)
                    return;

                var workspaceFolder = new WorkspaceFolder
                {
                    Uri = new Uri(qtProject.VcProjectDirectory, UriKind.Absolute).AbsoluteUri,
                    Name = qtProject.VcProject.Name
                };
                WorkspaceFolders.Add(workspaceFolder);

                await NotifyDidChangeWorkspaceFoldersParamsAsync(new[] { workspaceFolder },
                    Array.Empty<WorkspaceFolder>());
            });
        }

        private void OnProjectRemoved(Project project)
        {
            _ = ThreadHelper.JoinableTaskContext.Factory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (project?.Object is not VCProject vcProject)
                    return;

                var workspaceFolder = new WorkspaceFolder
                {
                    Uri = new Uri(vcProject.ProjectDirectory, UriKind.Absolute).AbsoluteUri,
                    Name = vcProject.Name
                };
                var entry = WorkspaceFolders.Find(f => string.Equals(f.Uri, workspaceFolder.Uri));
                if (entry != null)
                    WorkspaceFolders.Remove(entry);

                await NotifyDidChangeWorkspaceFoldersParamsAsync(Array.Empty<WorkspaceFolder>(),
                    new[] { workspaceFolder });
            });
        }

        private void OnItemPropertyChange2(object item, string propertySheet, string type,
            string propertyName)
        {
            if (item is not VCConfiguration vcConfig || !string.Equals(propertyName, "QtInstall"))
                return;

            if (MsBuildProject.GetOrAdd(vcConfig.project as VCProject) == null)
                return;

            _ = ThreadHelper.JoinableTaskContext.Factory.RunAsync(async () =>
                await NotifyDidChangeConfigurationAsync());
        }

        private async Task OnActiveWorkspaceChangedAsync(object arg1, EventArgs arg2)
        {
            if (InitializeMessageSent || WorkspaceService.CurrentWorkspace == null)
                return;

            InitializeMessageSent = true;
            WorkspaceFolders = GetWorkspaceFolders();

            await NotifyDidChangeWorkspaceFoldersParamsAsync(WorkspaceFolders.ToArray(),
                Array.Empty<WorkspaceFolder>());
        }


        private async Task NotifyDidChangeConfigurationAsync()
        {
            if (JsonRpc == null)
                return;

            await JsonRpc.NotifyWithParameterObjectAsync(DidChangeConfiguration,
                new DidChangeConfigurationParams
                {
                    Settings = null
                }
            );
        }

        internal async Task NotifyDidChangeWorkspaceFoldersParamsAsync(WorkspaceFolder[] added,
            WorkspaceFolder[] removed)
        {
            if (JsonRpc == null)
                return;

            await JsonRpc.NotifyWithParameterObjectAsync(DidChangeWorkspaceFolders,
                new DidChangeWorkspaceFoldersParams
                {
                    Event = new WorkspaceFoldersChangeEvent
                    {
                        Added = added,
                        Removed = removed,
                    }
                }
            );
        }
    }
}
