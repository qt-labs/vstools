// ************************************************************************************************
// Copyright (C) 2024 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
// ************************************************************************************************

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.MsBuild
{
    using Common;
    using Options;
    using VisualStudio;

    public partial class MsBuildProject
    {
        private class UpdateQtInstallation : InfoBarMessage
        {
            private UpdateQtInstallation(MsBuildProject project = null) => Project = project;

            private static UpdateQtInstallation MessageImpl =>
                StaticLazy.Get(() => MessageImpl, () => null);

            public static UpdateQtInstallation Message(MsBuildProject project = null) =>
                StaticLazy.Get(() => MessageImpl, () => new UpdateQtInstallation(project));

            protected override ImageMoniker Icon => KnownMonikers.StatusWarning;

            protected override TextSpan[] Text => new TextSpan[]
            {
                new() { Bold = true, Text = "Qt Visual Studio Tools" },
                new TextSpacer(2),
                Utils.EmDash,
                new TextSpacer(2),
                "The project's 'Qt Installation' property is not set correctly. Please specify a "
                    + "valid Qt version or path."
            };

            protected override Hyperlink[] Hyperlinks => new Hyperlink[]
            {
                    new()
                    {
                        Text = "Open project properties",
                        CloseInfoBar = true,
                        OnClicked = () => {
                            if (!Project.SelectInSolutionExplorer())
                                return;
                            if (VsServiceProvider.GetService<SVsUIShell, IVsUIShell>() is not {} shell)
                                return;

                            var guid = typeof(VSConstants.VSStd97CmdID).GUID;
                            const uint id = (uint)VSConstants.VSStd97CmdID.ProjectProperties;
                            ThreadHelper.ThrowIfNotOnUIThread();
                            shell.PostExecCommand(guid, id, 0, null);
                        }
                    },
                    new()
                    {
                        Text = "Don't show again",
                        CloseInfoBar = true,
                        OnClicked = () =>
                        {
                            if (Options.Get() is not {} options)
                                return;
                            options.UpdateQtInstallation = false;
                            options.SaveSettingsToStorage();
                        }
                    }
            };

            private MsBuildProject Project { get; }
        }

        public static void ShowUpdateQtInstallationMessage(MsBuildProject project)
        {
            ThreadHelper.JoinableTaskFactory.Run(() =>
                ShowUpdateQtInstallationMessageAsync(project));
        }

        public static async Task ShowUpdateQtInstallationMessageAsync(MsBuildProject project)
        {
            if (!Options.Get().UpdateQtInstallation)
                return;
            await VsShell.UiThreadAsync(UpdateQtInstallation.Message(project).Show);
        }

        public static void CloseUpdateQtInstallationMessage()
        {
            ThreadHelper.JoinableTaskFactory.Run(CloseUpdateQtInstallationMessageAsync);
        }

        public static async Task CloseUpdateQtInstallationMessageAsync()
        {
            await VsShell.UiThreadAsync(UpdateQtInstallation.Message().Close);
        }

        private class UpdateProjectFormat : InfoBarMessage
        {
            public static UpdateProjectFormat Message =>
                StaticLazy.Get(() => Message, () => new UpdateProjectFormat());

            protected override ImageMoniker Icon => KnownMonikers.StatusWarning;

            protected override TextSpan[] Text => new TextSpan[]
            {
                new() { Bold = true, Text = "Qt Visual Studio Tools" },
                new TextSpacer(2),
                Utils.EmDash,
                new TextSpacer(2),
                "You are using a legacy Qt project with Qt Visual Studio Tools. The project format"
                    + " is no longer supported, please update your project to our current version."
            };

            protected override Hyperlink[] Hyperlinks => new Hyperlink[]
            {
                    new()
                    {
                        Text = "Update",
                        CloseInfoBar = true,
                        OnClicked = () => {
                            ThreadHelper.ThrowIfNotOnUIThread();
                            MsBuildProjectConverter.SolutionToQtMsBuild();
                        }
                    },
                    new()
                    {
                        Text = "Don't show again",
                        CloseInfoBar = true,
                        OnClicked = () =>
                        {
                            if (Options.Get() is not {} options)
                                return;
                            options.UpdateProjectFormat = false;
                            options.SaveSettingsToStorage();
                        }
                    }
            };
        }

        public static void ShowUpdateFormatMessage()
        {
            ThreadHelper.JoinableTaskFactory.Run(ShowUpdateFormatMessageAsync);
        }

        public static async Task ShowUpdateFormatMessageAsync()
        {
            if (!Options.Get().UpdateProjectFormat)
                return;
            await VsShell.UiThreadAsync(UpdateProjectFormat.Message.Show);
        }

        public static void CloseUpdateFormatMessage()
        {
            ThreadHelper.JoinableTaskFactory.Run(CloseUpdateFormatMessageAsync);
        }

        public static async Task CloseUpdateFormatMessageAsync()
        {
            await VsShell.UiThreadAsync(UpdateProjectFormat.Message.Close);
        }

        private class ProjectFormatUpdated : InfoBarMessage
        {
            public static ProjectFormatUpdated Message =>
                StaticLazy.Get(() => Message, () => new ProjectFormatUpdated());

            public string ReportPath { get; set; }

            protected override ImageMoniker Icon => KnownMonikers.StatusInformation;

            protected override TextSpan[] Text => new TextSpan[]
            {
                new() { Bold = true, Text = "Qt Visual Studio Tools" },
                new TextSpacer(2),
                Utils.EmDash,
                new TextSpacer(2),
                "Project format conversion complete."
            };

            protected override Hyperlink[] Hyperlinks => new Hyperlink[]
            {
                    new()
                    {
                        Text = "View report",
                        CloseInfoBar = true,
                        OnClicked = () => {
                            ThreadHelper.ThrowIfNotOnUIThread();
                            VsEditor.Open(ReportPath);
                        }
                    }
            };
        }

        public static void ShowProjectFormatUpdated(string reportPath)
        {
            ThreadHelper.JoinableTaskFactory.Run(() => ShowProjectFormatUpdatedAsync(reportPath));
        }

        public static async Task ShowProjectFormatUpdatedAsync(string reportPath)
        {
            ProjectFormatUpdated.Message.ReportPath = reportPath;
            await VsShell.UiThreadAsync(ProjectFormatUpdated.Message.Show);
        }

        public static void CloseProjectFormatUpdated()
        {
            ThreadHelper.JoinableTaskFactory.Run(() => CloseProjectFormatUpdatedAsync());
        }

        public static async Task CloseProjectFormatUpdatedAsync()
        {
            await VsShell.UiThreadAsync(ProjectFormatUpdated.Message.Close);
        }
    }
}
