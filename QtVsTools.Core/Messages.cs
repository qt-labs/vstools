/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using EnvDTE;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core
{
    using VisualStudio;

    public static class Messages
    {
        private static OutputWindowPane Pane { get; set; }

        private static readonly string PaneName = "Qt VS Tools";
        private static readonly Guid PaneGuid = new Guid("8f6a1e44-fa0b-49e5-9934-1c050555350e");

        /// <summary>
        /// Show a message on the output pane.
        /// </summary>
        public static void Print(string text, bool clear = false, bool activate = false)
        {
            msgQueue.Enqueue(new Msg()
            {
                Clear = clear,
                Text = text,
                Activate = activate
            });
            FlushMessages();
        }

        /// <summary>
        /// Activates the message pane of the Qt VS Tools extension.
        /// </summary>
        public static void ActivateMessagePane()
        {
            msgQueue.Enqueue(new Msg()
            {
                Activate = true
            });
            FlushMessages();
        }

        static async Task OutputWindowPane_ActivateAsync()
        {
            await OutputWindowPane_InitAsync();
            await Pane?.ActivateAsync();
        }

        private static string ExceptionToString(System.Exception e)
        {
            return e.Message + "\r\n" + "(" + e.StackTrace.Trim() + ")";
        }

        private static readonly string ErrorString = SR.GetString("Messages_ErrorOccured");
        private static readonly string WarningString = SR.GetString("Messages_Warning");
        private static readonly string SolutionString = SR.GetString("Messages_SolveProblem");

        public static void DisplayCriticalErrorMessage(System.Exception e)
        {
            MessageBox.Show(ErrorString +
                ExceptionToString(e),
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void DisplayCriticalErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString +
                msg,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void DisplayErrorMessage(System.Exception e)
        {
            MessageBox.Show(ErrorString +
                ExceptionToString(e),
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void DisplayErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString +
                msg,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void DisplayWarningMessage(System.Exception e, string solution)
        {
            MessageBox.Show(WarningString +
                ExceptionToString(e) +
                SolutionString +
                solution,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void DisplayWarningMessage(string msg)
        {
            MessageBox.Show(WarningString +
                msg,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void ClearPane()
        {
            msgQueue.Enqueue(new Msg()
            {
                Clear = true
            });
            FlushMessages();
        }

        static async Task OutputWindowPane_ClearAsync()
        {
            await OutputWindowPane_InitAsync();
            await Pane?.ClearAsync();
        }

        class Msg
        {
            public bool Clear { get; set; } = false;
            public string Text { get; set; } = null;
            public bool Activate { get; set; } = false;
        }

        static readonly ConcurrentQueue<Msg> msgQueue = new ConcurrentQueue<Msg>();

        private static async Task OutputWindowPane_InitAsync()
        {
            try {
                if (Pane == null)
                    Pane = await OutputWindowPane.CreateAsync(PaneName, PaneGuid);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static JoinableTaskFactory JoinableTaskFactory { get; set; }

        static readonly object staticCriticalSection = new object();
        static Task FlushTask { get; set; }
        static EventWaitHandle MessageReady { get; set; }

        static void FlushMessages()
        {
            lock (staticCriticalSection) {
                if (FlushTask == null) {
                    MessageReady = new EventWaitHandle(false, EventResetMode.AutoReset);
                    FlushTask = Task.Run(async () =>
                    {
                        var package = VsServiceProvider.Instance as Package;
                        while (!package.Zombied) {
                            if (!await MessageReady.ToTask(3000))
                                continue;
                            while (!msgQueue.IsEmpty) {
                                if (!msgQueue.TryDequeue(out Msg msg)) {
                                    await Task.Yield();
                                    continue;
                                }
                                if (msg.Clear)
                                    await OutputWindowPane_ClearAsync();
                                if (msg.Text != null)
                                    await OutputWindowPane_PrintAsync(msg.Text);
                                if (msg.Activate)
                                    await OutputWindowPane_ActivateAsync();
                            }
                        }
                    });
                }
            }
            MessageReady.Set();
        }

        static async Task OutputWindowPane_PrintAsync(string text)
        {
            var active = await OutputWindowPane.GetActiveAsync();

            await OutputWindowPane_InitAsync();
            await Pane.PrintAsync(text);

            (active?.ActivateAsync()).Forget();
        }
    }
}
