/****************************************************************************
**
** Copyright (C) 2019 The Qt Company Ltd.
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
            var DTE = await Package.GetServiceAsync(typeof(DTE)) as DTE2;
            await TaskScheduler.Default;

            var pipeName = string.Format("QtVSTest_{0}", Process.GetCurrentProcess().Id);
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

                            var macro = new Macro(Package, DTE, JoinableTaskFactory, Loop.Token);
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

                            if (macro != null && macro.Ok && macro.AutoRun && macro.QuitWhenDone) {
                                await JoinableTaskFactory.SwitchToMainThreadAsync(Loop.Token);
                                if (DTE != null) {
                                    DTE.Solution.Close(false);
                                    DTE.Quit();
                                }
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
