/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools
{
    using Common;
    using Microsoft.VisualStudio.Imaging.Interop;
    using VisualStudio;

    public static class Notifications
    {
        static LazyFactory StaticLazy { get; } = new LazyFactory();

        public static NoQtVersion NoQtVersion
            => StaticLazy.Get(() => NoQtVersion, () => new NoQtVersion());

        public static NotifyInstall NotifyInstall
            => StaticLazy.Get(() => NotifyInstall, () => new NotifyInstall());

        public static UpdateProjectFormat UpdateProjectFormat
            => StaticLazy.Get(() => UpdateProjectFormat, () => new UpdateProjectFormat());
    }

    public class NoQtVersion : InfoBarMessage
    {
        protected override ImageMoniker Icon => KnownMonikers.StatusWarning;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new TextSpan { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            "\u2014", // Em dash
            new TextSpacer(2),
            "You must select a Qt version to use for development."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new Hyperlink
            {
                Text = "Select Qt version...",
                CloseInfoBar = false,
                OnClicked = () =>
                    QtVsToolsPackage.Instance.ShowOptionPage(typeof(Options.QtVersionsPage))
            }
        };
    }

    public class NotifyInstall : InfoBarMessage
    {
        protected override ImageMoniker Icon => KnownMonikers.StatusInformation;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new TextSpan { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            "\u2014", // Em dash
            new TextSpacer(2),
            $"Version {Version.USER_VERSION} was recently installed."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new Hyperlink
            {
                Text = "Release Notes",
                CloseInfoBar = false,
                OnClicked= () =>
                {
                    VsShellUtilities.OpenSystemBrowser(
                        "https://code.qt.io/cgit/qt-labs/vstools.git/tree/Changelog");
                }
            },
            new Hyperlink
            {
                Text = "Don't show again",
                CloseInfoBar = true,
                OnClicked = () =>
                {
                    QtVsToolsPackage.Instance.Options.NotifyInstalled = false;
                    QtVsToolsPackage.Instance.Options.SaveSettingsToStorage();
                }
            }
        };
    }

    public class UpdateProjectFormat : InfoBarMessage
    {
        protected override ImageMoniker Icon => KnownMonikers.StatusWarning;

        protected override TextSpan[] Text => new TextSpan[]
        {
                new TextSpan { Bold = true, Text = "Qt Visual Studio Tools" },
                new TextSpacer(2),
                "\u2014", // Em dash
                new TextSpacer(2),
                "You are using some legacy code path of the Qt Visual Studio Tools. We strongly "
                    + "recommend updating your code base to use our latest development version."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new Hyperlink
            {
                Text = "Update",
                CloseInfoBar = true,
                OnClicked = () => {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    QtMsBuildConverter.SolutionToQtMsBuild();
                }
            },
            new Hyperlink
            {
                Text = "Don't show again",
                CloseInfoBar = true,
                OnClicked = () =>
                {
                    QtVsToolsPackage.Instance.Options.UpdateProjectFormat = false;
                    QtVsToolsPackage.Instance.Options.SaveSettingsToStorage();
                }
            }
        };
    }
}
