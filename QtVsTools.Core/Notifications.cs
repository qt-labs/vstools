/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace QtVsTools.Core
{
    using Common;
    using QtVsTools.Common;
    using VisualStudio;

    using static Options.QtOptionsPage;

    public static class Notifications
    {
        static LazyFactory StaticLazy { get; } = new();

        public static NoQtVersion NoQtVersion
            => StaticLazy.Get(() => NoQtVersion, () => new NoQtVersion());

        public static NotifyInstall NotifyInstall
            => StaticLazy.Get(() => NotifyInstall, () => new NotifyInstall());

        public static SearchDevRelease NotifySearchDevRelease
            => StaticLazy.Get(() => NotifySearchDevRelease, () => new SearchDevRelease());

        public static NotifyMessage NotifyMessage
            => StaticLazy.Get(() => NotifyMessage, () => new NotifyMessage());
    }

    public class NoQtVersion : InfoBarMessage
    {
        protected override ImageMoniker Icon => KnownMonikers.StatusWarning;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new() { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            Utils.EmDash,
            new TextSpacer(2),
            "You must select a Qt version to use for development."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new()
            {
                Text = "Select Qt version...",
                CloseInfoBar = false,
                OnClicked = () =>
                {
                    if (VsServiceProvider.Instance is AsyncPackage package)
                        package.ShowOptionPage(typeof(Options.QtVersionsPage));
                }
            }
        };
    }

    public class NotifyInstall : InfoBarMessage
    {
        protected override ImageMoniker Icon => KnownMonikers.StatusInformation;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new() { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            Utils.EmDash,
            new TextSpacer(2),
            $"Version {Version.USER_VERSION} was recently installed."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new()
            {
                Text = "Release Notes",
                CloseInfoBar = false,
                OnClicked= () =>
                {
                    VsShellUtilities.OpenSystemBrowser(
                        "https://code.qt.io/cgit/qt-labs/vstools.git/tree/Changelog");
                }
            },
            new()
            {
                Text = "Don't show again",
                CloseInfoBar = true,
                OnClicked = () =>
                {
                    if (Options.Options.Get() is not {} options)
                        return;
                    options.NotifyInstalled = false;
                    options.SaveSettingsToStorage();
                }
            }
        };
    }

    public class SearchDevRelease : InfoBarMessage
    {
        private readonly bool isSearchEnabled;
        private readonly string keyName = DevelopmentReleases.SearchDevRelease.ToString();

        public SearchDevRelease()
        {
            using var key = Registry.CurrentUser
                .OpenSubKey(Resources.PackageSettingsPath, writable: false);
            isSearchEnabled = key?.GetBoolValue(keyName) ?? false;
        }

        protected override ImageMoniker Icon => KnownMonikers.StatusInformation;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new() { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            Utils.EmDash,
            new TextSpacer(2),
            "can automatically search for development releases during startup."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new()
            {
                Text = isSearchEnabled ? "Disable" : "Enable",
                CloseInfoBar = false,
                OnClicked= () =>
                {
                    try {
                        using var key =
                            Registry.CurrentUser.CreateSubKey(Resources.PackageSettingsPath);
                        key?.SetValue(keyName, Convert.ToInt32(!isSearchEnabled));
                    } catch (Exception ex) {
                        ex.Log();
                    }
                }
            },
            new()
            {
                Text = "Don't show again",
                CloseInfoBar = true,
                OnClicked = () =>
                {
                    if (Options.Options.Get() is not {} options)
                        return;
                    options.NotifySearchDevRelease = false;
                    options.SaveSettingsToStorage();
                }
            }
        };
    }

    public class NotifyMessage : InfoBarMessage
    {
        public void Show(Message message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            message.Setup(this);
            Close();
            base.Show();
        }

        public void Show(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Show(new Message { text });
        }

        public new void Show()
        {
            throw new NotSupportedException();
        }

        public class Message : IEnumerable
        {
            private ImageMoniker MessageIcon { get; set; } = KnownMonikers.StatusInformation;

            public void Add(ImageMoniker icon)
            {
                MessageIcon = icon;
            }

            public void Add(TextSpan text)
            {
                MessageText.Add(text);
            }

            public void Add(string text)
            {
                MessageText.Add(text);
            }

            public void Add(Hyperlink hyperlink)
            {
                MessageHyperlinks.Add(hyperlink);
            }

            public void Setup(NotifyMessage notifyMessage)
            {
                notifyMessage.MessageIcon = MessageIcon;
                notifyMessage.MessageText = MessageText;
                notifyMessage.MessageHyperlinks = MessageHyperlinks;
            }

            private List<TextSpan> MessageText { get; set; } = new()
            {
                new TextSpan { Bold = true, Text = "Qt Visual Studio Tools" },
                new TextSpacer(2),
                Utils.EmDash,
                new TextSpacer(2),
            };
            private List<Hyperlink> MessageHyperlinks { get; set; } = new();

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotSupportedException();
            }
        }

        private ImageMoniker MessageIcon { get; set; } = KnownMonikers.StatusInformation;
        protected override ImageMoniker Icon => MessageIcon;

        private List<TextSpan> MessageText { get; set; } = new();
        protected override TextSpan[] Text => MessageText.ToArray();

        private List<Hyperlink> MessageHyperlinks { get; set; } = new();
        protected override Hyperlink[] Hyperlinks => MessageHyperlinks.ToArray();
    }

    public class NotifyDetach : InfoBarMessage
    {
        private Action DetachAction { get; }

        public NotifyDetach(Action detachAction, IVsInfoBarHost host = null) : base(host)
        {
            DetachAction = detachAction;
        }

        protected override ImageMoniker Icon => KnownMonikers.StatusInformation;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new() { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            Utils.EmDash,
            new TextSpacer(2),
            "This window is detachable. Click detach to launch in a separate window."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new()
            {
                Text = "Detach",
                CloseInfoBar = false,
                OnClicked = DetachAction
            }
        };
    }
}
