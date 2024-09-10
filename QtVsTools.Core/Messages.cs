/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using static System.Environment;
using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core
{
    using Options;
    using VisualStudio;

    public static class Messages
    {
        public static bool Initialized { get; set; } = false;

        private static OutputWindowPane Pane { get; set; }

        private const string Name = "Qt VS Tools";
        private static readonly Guid PaneGuid = new("8f6a1e44-fa0b-49e5-9934-1c050555350e");

        /// <summary>
        /// Show a message on the output pane.
        /// </summary>
        public static void Print(string text,
            bool clear = false, bool activate = false, bool trim = true)
        {
            msgQueue.Enqueue(new Msg
            {
                Clear = clear,
                Text = trim ? text.Trim(' ', '\t', '\r', '\n') : text,
                Activate = activate
            });
            FlushMessages();
        }

        public static void Log(this Exception exception, bool clear = false, bool activate = false)
        {
            msgQueue.Enqueue(new Msg
            {
                Clear = clear,
                Text = ExceptionToString(exception),
                Activate = activate
            });
            FlushMessages();
        }

        /// <summary>
        /// Activates the message pane of the Qt VS Tools extension.
        /// </summary>
        public static void ActivateMessagePane()
        {
            msgQueue.Enqueue(new Msg
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

        private static string ExceptionToString(Exception exception)
        {
            return $"An exception ({exception.GetType().Name}) occurred.\r\n"
                   + $"Message:\r\n   {exception.Message}\r\n"
                   + $"Stack Trace:\r\n   {exception.StackTrace.Trim()}\r\n";
        }

        private const string ErrorString = "The following error occurred:";
        private static readonly string WarningString = "Warning:" + NewLine;

        public static void DisplayCriticalErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString + NewLine + msg,
                Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void DisplayErrorMessage(Exception e)
        {
            MessageBox.Show(ExceptionToString(e),
                Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void DisplayErrorMessage(string msg)
        {
            MessageBox.Show(ErrorString + NewLine + msg,
                Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void DisplayWarningMessage(Exception e, string solution)
        {
            MessageBox.Show(WarningString
                + ExceptionToString(e)
                + NewLine + NewLine + "To solve this problem:" + NewLine
                + solution,
                Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void DisplayWarningMessage(string msg)
        {
            MessageBox.Show(WarningString +
                msg,
                Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void ClearPane()
        {
            msgQueue.Enqueue(new Msg
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
            public bool Clear { get; set; }
            public string Text { get; set; }
            public bool Activate { get; set; }
        }

        static readonly ConcurrentQueue<Msg> msgQueue = new();

        private static async Task OutputWindowPane_InitAsync()
        {
            try {
                Pane ??= await OutputWindowPane.CreateAsync(Name, PaneGuid);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static JoinableTaskFactory JoinableTaskFactory { get; set; }

        static readonly object staticCriticalSection = new();
        static Task FlushTask { get; set; }
        static EventWaitHandle MessageReady { get; set; }

        static void FlushMessages()
        {
            lock (staticCriticalSection) {
                if (FlushTask == null) {
                    MessageReady = new EventWaitHandle(false, EventResetMode.AutoReset);
                    FlushTask = Task.Run(async () =>
                    {
                        while (VsServiceProvider.Instance == null)
                            await Task.Delay(1000, VsShellUtilities.ShutdownToken);
                        while (!VsShellUtilities.ShutdownToken.IsCancellationRequested) {
                            await Task.Delay(Initialized ? 100 : 1000);
                            if (!await MessageReady.ToTask(3000))
                                continue;
                            bool clear = false;
                            bool activate = false;
                            var msgText = new StringBuilder();
                            while (!msgQueue.IsEmpty) {
                                if (!msgQueue.TryDequeue(out var msg)) {
                                    await Task.Yield();
                                    continue;
                                }
                                if (msg is null)
                                    continue;
                                clear |= msg.Clear;
                                activate |= msg.Activate;
                                if (!string.IsNullOrEmpty(msg.Text))
                                    msgText.AppendLine(msg.Text);
                            }
                            if (clear)
                                await OutputWindowPane_ClearAsync();
                            if (msgText.Length > 0)
                                await OutputWindowPane_PrintAsync(msgText.ToString());
                            if (activate && QtOptionsPage.AutoActivatePane)
                                await OutputWindowPane_ActivateAsync();
                        }
                    });
                }
            }
            MessageReady.Set();
        }

        static async Task OutputWindowPane_PrintAsync(string text)
        {
            await OutputWindowPane_InitAsync();
            await Pane.PrintAsync(text);
        }
    }
}
