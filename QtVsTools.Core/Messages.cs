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

using EnvDTE;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using QtVsTools.VisualStudio;
using Microsoft.VisualStudio.Shell;

using Thread = System.Threading.Thread;
using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core
{
    public static class Messages
    {
        private static OutputWindow Window { get; set; }
        private static OutputWindowPane Pane { get; set; }

        private static OutputWindowPane _BuildPane;
        private static OutputWindowPane BuildPane
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _BuildPane ?? (_BuildPane = Window.OutputWindowPanes.Cast<OutputWindowPane>()
                    .Where(pane =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        return pane.Guid == "{1BD8A850-02D1-11D1-BEE7-00A0C913D1F8}";
                    })
                    .FirstOrDefault());
            }
        }

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

            ThreadHelper.ThrowIfNotOnUIThread();
            FlushMessages();
        }

        static void OutputWindowPane_Print(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OutputWindowPane_Init();
            Pane.OutputString(text + "\r\n");
            // show buildPane if a build is in progress
            if (Dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress)
                BuildPane?.Activate();
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

            ThreadHelper.ThrowIfNotOnUIThread();
            FlushMessages();
        }

        static void OutputWindowPane_Activate()
        {
            OutputWindowPane_Init();

            ThreadHelper.ThrowIfNotOnUIThread();

            Pane?.Activate();
        }

        private static string ExceptionToString(System.Exception e)
        {
            return e.Message + "\r\n" + "(" + e.StackTrace.Trim() + ")";
        }

        private static readonly string ErrorString = SR.GetString("Messages_ErrorOccured");
        private static readonly string WarningString = SR.GetString("Messages_Warning");
        private static readonly string SolutionString = SR.GetString("Messages_SolveProblem");

        static public void DisplayCriticalErrorMessage(System.Exception e)
        {
            MessageBox.Show(ErrorString +
                ExceptionToString(e),
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayCriticalErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString +
                msg,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayErrorMessage(System.Exception e)
        {
            MessageBox.Show(ErrorString +
                ExceptionToString(e),
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString +
                msg,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static public void DisplayWarningMessage(System.Exception e, string solution)
        {
            MessageBox.Show(WarningString +
                ExceptionToString(e) +
                SolutionString +
                solution,
                SR.GetString("Resources_QtVsTools"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        static public void DisplayWarningMessage(string msg)
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

            ThreadHelper.ThrowIfNotOnUIThread();

            FlushMessages();
        }

        static void OutputWindowPane_Clear()
        {
            OutputWindowPane_Init();

            ThreadHelper.ThrowIfNotOnUIThread();

            Pane?.Clear();
        }

        class Msg
        {
            public bool Clear { get; set; } = false;
            public string Text { get; set; } = null;
            public bool Activate { get; set; } = false;
        }

        static bool shuttingDown = false;
        static readonly ConcurrentQueue<Msg> msgQueue = new ConcurrentQueue<Msg>();
        static DTE Dte { get; set; } = null;

        private static void OnBeginShutdown()
        {
            shuttingDown = true;
        }

        private static void OutputWindowPane_Init()
        {
            if (Dte == null)
                Dte = VsServiceProvider.GetService<DTE>();
            var t = Stopwatch.StartNew();

            ThreadHelper.ThrowIfNotOnUIThread();

            while (Pane == null && t.ElapsedMilliseconds < 5000) {
                try {
                    Window = Dte.Windows.Item(Constants.vsWindowKindOutput).Object as OutputWindow;
                    Pane = Window?.OutputWindowPanes.Add(SR.GetString("Resources_QtVsTools"));
                } catch {
                }
                if (Pane == null)
                    Thread.Yield();
            }
            Dte.Events.DTEEvents.OnBeginShutdown += OnBeginShutdown;
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
                        while (!shuttingDown) {
                            if (!await MessageReady.ToTask(3000))
                                continue;
                            while (!msgQueue.IsEmpty) {
                                Msg msg;
                                if (!msgQueue.TryDequeue(out msg)) {
                                    await Task.Yield();
                                    continue;
                                }
                                ////////////////////////////////////////////////////////////////////
                                // Switch to main (UI) thread
                                await JoinableTaskFactory.SwitchToMainThreadAsync();
                                if (msg.Clear)
                                    OutputWindowPane_Clear();
                                if (msg.Text != null)
                                    OutputWindowPane_Print(msg.Text);
                                if (msg.Activate)
                                    OutputWindowPane_Activate();
                                ////////////////////////////////////////////////////////////////////
                                // Switch to background thread
                                await TaskScheduler.Default;
                            }
                        }
                    });
                }
            }
            MessageReady.Set();
        }
    }
}
