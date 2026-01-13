// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

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

    public static partial class Messages
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
            MsgQueue.Enqueue(new Msg
            {
                Clear = clear,
                Text = trim ? text.Trim(' ', '\t', '\r', '\n') : text,
                Activate = activate
            });
            FlushMessages();
        }

        /// <summary>
        /// Activates the message pane of the Qt VS Tools extension.
        /// </summary>
        public static void ActivateMessagePane()
        {
            MsgQueue.Enqueue(new Msg
            {
                Activate = true
            });
            FlushMessages();
        }

        private static async Task OutputWindowPane_ActivateAsync()
        {
            await OutputWindowPane_InitAsync();
            await Pane.ActivateAsync();
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
            MsgQueue.Enqueue(new Msg
            {
                Clear = true
            });
            FlushMessages();
        }

        private static async Task OutputWindowPane_ClearAsync()
        {
            await OutputWindowPane_InitAsync();
            await Pane.ClearAsync();
        }

        private class Msg
        {
            public bool Clear { get; set; }
            public string Text { get; set; }
            public bool Activate { get; set; }
        }

        private static readonly ConcurrentQueue<Msg> MsgQueue = new();

        private static async Task OutputWindowPane_InitAsync()
        {
            try {
                Pane ??= await OutputWindowPane.CreateAsync(Name, PaneGuid);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static JoinableTaskFactory JoinableTaskFactory { get; set; }

        private static readonly object StaticCriticalSection = new();
        private static Task FlushTask { get; set; }
        private static EventWaitHandle MessageReady { get; set; }

        private static void FlushMessages()
        {
            lock (StaticCriticalSection) {
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
                            var clear = false;
                            var activate = false;
                            var msgText = new StringBuilder();
                            while (!MsgQueue.IsEmpty) {
                                if (!MsgQueue.TryDequeue(out var msg)) {
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

        private static async Task OutputWindowPane_PrintAsync(string text)
        {
            await OutputWindowPane_InitAsync();
            await Pane.PrintAsync(text);
        }
    }
}
