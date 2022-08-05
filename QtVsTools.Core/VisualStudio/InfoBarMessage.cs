/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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
        protected abstract Hyperlink[] Hyperlinks { get; }

        protected class TextSpan
        {
            public string Text { get; set; }
            public bool Bold { get; set; }
            public bool Italic { get; set; }
            public bool Underline { get; set; }
            public static implicit operator TextSpan(string text) => new TextSpan { Text = text };
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
            static LazyFactory StaticLazy { get; } = new LazyFactory();
            static IVsInfoBarUIFactory Factory => StaticLazy.Get(() =>
                Factory, () => VsServiceProvider
                    .GetService<SVsInfoBarUIFactory, IVsInfoBarUIFactory>());

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
                var hyperlink = actionItem.ActionContext as Hyperlink;
                if (hyperlink == null)
                    return;
                if (hyperlink.CloseInfoBar)
                    Close();
                hyperlink.OnClicked();
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
