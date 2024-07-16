/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace QtVsTools.TestAdapter
{
    using Core;

    internal class ProcessMonitor
    {
        private readonly Process process;
        private readonly ManualResetEvent waitHandle = new(false);

        internal ProcessMonitor(Process process, CancellationTokenSource cancellationTokenSource = null)
        {
            this.process = process ?? throw new ArgumentNullException(nameof(process));
            ProcessId = process.Id;
            InitializeProcessMonitoring(cancellationTokenSource);
        }

        internal ProcessMonitor(CancellationTokenSource cancellationTokenSource = null)
        {
            process = new Process();
            InitializeProcessMonitoring(cancellationTokenSource);
        }

        private void InitializeProcessMonitoring(CancellationTokenSource cancellationTokenSource)
        {
            process.EnableRaisingEvents = true;
            process.Exited += OnExited;
            cancellationTokenSource?.Token.Register(CancelProcess);
        }

        internal int ExitCode { get; private set; }
        internal int ProcessId { get; private set; }

        internal List<string> StandardOutput { get; } = new();

        internal static ProcessStartInfo CreateStartInfo(string filePath, string arguments,
            bool redirectStandardOutput, string workingDirectory, QtTestSettings settings,
            Logger log)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments,

                UseShellExecute = false,
                RedirectStandardOutput = redirectStandardOutput,

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,

                WorkingDirectory = workingDirectory ?? ""
            };

            if (VersionInformation.GetOrAddByName(settings.QtInstall) is not { LibExecs: {} bin })
                return startInfo;

            startInfo.Environment["Path"] = HelperFunctions.ToNativeSeparator(bin);
            log.SendMessage($"Added Qt version '{settings.QtInstall}': '{bin}' to path.");

            return startInfo;
        }

        internal void StartProcess(ProcessStartInfo startInfo)
        {
            process.StartInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
            if (startInfo.RedirectStandardOutput) {
                process.OutputDataReceived += (_, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data))
                        return;
                    var data = e.Data.TrimEnd('\r', '\n');
                    if (!string.IsNullOrEmpty(data))
                        StandardOutput.Add(e.Data);
                };
            }

            if (!process.Start())
                throw new InvalidOperationException("Failed to start the process.");

            ProcessId = process.Id;
            if (startInfo.RedirectStandardOutput)
                process.BeginOutputReadLine();
        }

        internal void WaitForExit(int timeoutMilliseconds = Timeout.Infinite)
        {
            if (waitHandle.WaitOne(timeoutMilliseconds))
                return;
            CancelProcess();
            throw new TimeoutException("Process did not exit within the specified timeout.");
        }

        private void OnExited(object sender, EventArgs e)
        {
            if (sender is not Process proc)
                return;
            ExitCode = proc.ExitCode;
            waitHandle.Set();
            proc.Exited -= OnExited;
        }

        private void CancelProcess()
        {
            if (process.HasExited)
                return;
            process.Kill();
            process.WaitForExit();
        }
    }
}
