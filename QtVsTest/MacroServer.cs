/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using EnvDTE;
using EnvDTE80;

using Task = System.Threading.Tasks.Task;
using Process = System.Diagnostics.Process;

namespace QtVsTest.Macros
{
    /// <summary>
    /// Provides test clients with macro compilation and execution services
    /// </summary>
    class MacroServer
    {
        public CancellationTokenSource Loop { get; }

        AsyncPackage Package { get; }
        JoinableTaskFactory JoinableTaskFactory { get; }

        /// <summary>
        /// Macro server constructor
        /// </summary>
        /// <param name="package">QtVSTest extension package</param>
        /// <param name="joinableTaskFactory">Task factory, enables joining with UI thread</param>
        public MacroServer(AsyncPackage package, JoinableTaskFactory joinableTaskFactory)
        {
            Package = package;
            JoinableTaskFactory = joinableTaskFactory;
            Loop = new CancellationTokenSource();
        }

        /// <summary>
        /// Server loop
        /// </summary>
        public async Task LoopAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(Loop.Token);
            var dte = await Package.GetServiceAsync(typeof(DTE)) as DTE2;
            var mainWindowHWnd = new IntPtr((long)dte.MainWindow.HWnd);
            await TaskScheduler.Default;

            var pipeName = $"QtVSTest_{Process.GetCurrentProcess().Id}";
            while (!Loop.Token.IsCancellationRequested) {
                using (var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut)) {

                    // Clean-up previous macro session
                    Macro.Reset();

                    await pipe.WaitForConnectionAsync(Loop.Token);
                    if (Loop.Token.IsCancellationRequested)
                        break;

                    while (!Loop.Token.IsCancellationRequested && pipe.IsConnected) {
                        byte[] data = new byte[4];
                        await pipe.ReadAsync(data, 0, 4, Loop.Token);
                        if (Loop.Token.IsCancellationRequested)
                            break;

                        if (pipe.IsConnected) {
                            int size = BitConverter.ToInt32(data, 0);
                            data = new byte[size];

                            await pipe.ReadAsync(data, 0, size, Loop.Token);
                            if (Loop.Token.IsCancellationRequested)
                                break;

                            var macro = new Macro(
                                Package, dte, mainWindowHWnd, JoinableTaskFactory, Loop.Token);
                            await macro.CompileAsync(Encoding.UTF8.GetString(data));
                            if (macro.AutoRun)
                                await macro.RunAsync();

                            data = Encoding.UTF8.GetBytes(macro.Result);
                            size = data.Length;

                            await pipe.WriteAsync(BitConverter.GetBytes(size), 0, 4, Loop.Token);
                            if (Loop.Token.IsCancellationRequested)
                                break;

                            await pipe.WriteAsync(data, 0, size, Loop.Token);
                            if (Loop.Token.IsCancellationRequested)
                                break;

                            await pipe.FlushAsync(Loop.Token);
                            if (Loop.Token.IsCancellationRequested)
                                break;

                            pipe.WaitForPipeDrain();

                            if (macro is { Ok: true, AutoRun: true, QuitWhenDone: true }) {
                                await JoinableTaskFactory.SwitchToMainThreadAsync(Loop.Token);
                                dte.Solution.Close(false);
                                dte.Quit();
                                await TaskScheduler.Default;
                                Loop.Cancel();
                            }
                        }
                    }
                }
            }
        }
    }
}
