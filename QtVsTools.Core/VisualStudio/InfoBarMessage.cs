/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.VisualStudio
{
    using Common;

    public abstract class InfoBarMessage
    {
        protected abstract ImageMoniker Icon { get; }
        protected abstract TextSpan[] Text { get; }
        protected virtual Hyperlink[] Hyperlinks => Array.Empty<Hyperlink>();

        protected class TextSpan
        {
            public string Text { get; set; }
            public bool Bold { get; set; }
            public bool Italic { get; set; }
            public bool Underline { get; set; }
            public static implicit operator TextSpan(string text) => new() { Text = text };
        }

        protected class TextSpacer : TextSpan
        {
            public TextSpacer(int spaces)
            {
                Text = new string(' ', spaces);
            }
        }

        protected class Hyperlink
        {
            public string Text { get; set; }
            public bool CloseInfoBar { get; set; }
            public Action OnClicked { get; set; }
        }

        private MessageUI UI { get; set; }

        public InfoBarMessage()
        {
            UI = new MessageUI { Message = this };
        }

        public virtual void Show()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UI.Show();
        }

        public virtual void Close()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UI.Close();
        }

        public bool IsOpen => UI.IsOpen;

        protected virtual void OnClosed()
        { }

        private class MessageUI : IVsInfoBarUIEvents
        {
            static LazyFactory StaticLazy { get; } = new();
            static IVsInfoBarUIFactory Factory => StaticLazy.Get(() =>
                Factory, VsServiceProvider.GetService<SVsInfoBarUIFactory, IVsInfoBarUIFactory>);

            private IVsInfoBarUIElement UIElement { get; set; }
            private uint eventNotificationCookie;

            public bool IsOpen => UIElement != null;

            public InfoBarMessage Message { get; set; }

            public void Show()
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (Factory == null)
                    return;
                if (UIElement != null) // Message already shown
                    return;
                var textSpans = Enumerable.Empty<InfoBarTextSpan>();
                if (Message.Text != null) {
                    textSpans = Message.Text
                        .Select(x => new InfoBarTextSpan(x.Text, x.Bold, x.Italic, x.Underline));
                }
                var hyperlinks = Enumerable.Empty<InfoBarHyperlink>();
                if (Message.Hyperlinks != null) {
                    hyperlinks = Message.Hyperlinks
                        .Select(x => new InfoBarHyperlink(x.Text, x));
                }
                var model = new InfoBarModel(textSpans, hyperlinks, Message.Icon, true);
                UIElement = Factory.CreateInfoBar(model);
                if (UIElement != null) {
                    UIElement.Advise(this, out eventNotificationCookie);
                    VsShell.InfoBarHost?.AddInfoBar(UIElement);
                }
            }

            public void Close()
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                UIElement?.Close();
            }

            void IVsInfoBarUIEvents.OnActionItemClicked(
                IVsInfoBarUIElement infoBarUIElement,
                IVsInfoBarActionItem actionItem)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Debug.Assert(infoBarUIElement == UIElement);
                if (actionItem.ActionContext is Hyperlink hyperlink) {
                    if (hyperlink.CloseInfoBar)
                        Close();
                    hyperlink.OnClicked?.Invoke();
                }
            }

            void IVsInfoBarUIEvents.OnClosed(IVsInfoBarUIElement infoBarUIElement)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Debug.Assert(infoBarUIElement == UIElement);
                if (UIElement != null) {
                    UIElement.Unadvise(eventNotificationCookie);
                    UIElement = null;
                    eventNotificationCookie = 0;
                    Message.OnClosed();
                }
            }
        }
    }
}
