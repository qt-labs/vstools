/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.CMake
{
    using VisualStudio;
    using static Utils;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private class AlertIncompatibleProject : InfoBarMessage
        {
            protected override ImageMoniker Icon => KnownMonikers.StatusWarning;

            protected override TextSpan[] Text => new []
            {
                new TextSpan { Bold = true, Text = "Qt Visual Studio Tools" },
                new TextSpacer(2), EmDash, new TextSpacer(2),
                "Found a reference to Qt. Projects that use a CMakeSettings.json configuration "
                + "file are not supported. Please convert to a CMakePresets.json configuration."
            };
        }

        private static AlertIncompatibleProject IncompatibleProjectMessage => StaticLazy.Get(
            () => IncompatibleProjectMessage, () => new AlertIncompatibleProject());

        private async Task ShowIncompatibleProjectAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IncompatibleProjectMessage.Show();
        }

        private class AskConversionConfirmation : InfoBarMessage
        {
            public AskConversionConfirmation(CMakeProject project)
            {
                Project = project;
            }

            private CMakeProject Project { get; }

            protected override ImageMoniker Icon => KnownMonikers.StatusAlert;

            protected override TextSpan[] Text => new []
            {
                new TextSpan { Bold = true, Text = "Qt Visual Studio Tools" },
                new TextSpacer(2), EmDash, new TextSpacer(2),
                "Found a reference to Qt."
            };

            protected override Hyperlink[] Hyperlinks => new Hyperlink[]
            {
                new()
                {
                    Text = "Convert project to Qt/CMake",
                    CloseInfoBar = true,
                    OnClicked = Project.ConfirmConversion
                }
            };
        }

        private AskConversionConfirmation ConversionConfirmationMessage => Lazy.Get(
            () => ConversionConfirmationMessage, () => new AskConversionConfirmation(this));

        private async Task ShowConversionConfirmationAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ConversionConfirmationMessage.Show();
        }

        private void ConfirmConversion()
        {
            if (Status != QtStatus.ConversionPending)
                return;
            Status = QtStatus.True;
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(RefreshAsync);
        }

        private async Task CloseMessagesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IncompatibleProjectMessage.Close();
            ConversionConfirmationMessage.Close();
        }
    }
}
