/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
                    QtVsToolsLegacyPackage.Instance.ShowOptionPage(typeof(Options.QtVersionsPage))
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
            $"LEGACY version {Version.USER_VERSION} was recently installed."
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
                    QtVsToolsLegacyPackage.Instance.Options.NotifyInstalled = false;
                    QtVsToolsLegacyPackage.Instance.Options.SaveSettingsToStorage();
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
                    QtVsToolsLegacyPackage.Instance.Options.UpdateProjectFormat = false;
                    QtVsToolsLegacyPackage.Instance.Options.SaveSettingsToStorage();
                }
            }
        };
    }
}
