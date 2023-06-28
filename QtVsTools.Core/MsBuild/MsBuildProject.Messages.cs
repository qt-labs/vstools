// ************************************************************************************************
// Copyright (C) 2023 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
// ************************************************************************************************

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.MsBuild
{
    using Options;
    using VisualStudio;

    public partial class MsBuildProject
    {
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
    }
}
