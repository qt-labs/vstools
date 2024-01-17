/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;

using Task = System.Threading.Tasks.Task;
namespace QtVsTools.Qml.Language
{
    using Core;
    using static Core.Common.Utils;
    using static Instances;
    using static Core.Instances;

    [Export(typeof(ILanguageClient))]
    [Export(typeof(IBraceCompletionSessionProvider))]
    [BracePair('{', '}')]
    [ContentType(QmlContentType.Name)]
    public class QmlLspClient :
        ILanguageClient,
        IBraceCompletionSessionProvider
    {
        public bool TryCreateSession(ITextView textView, SnapshotPoint openingPoint,
            char openingBrace, char closingBrace, out IBraceCompletionSession session)
        {
            session = null;
            return true;
        }

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public QmlLspClient()
        {
        }

        public string Name => "QML Language Client";

        private string PathToQt { get; set; }
        private string PathToServer { get; set; }

        private static string LogFilePath { get; } = @$"{Path.GetTempPath()}\qmllsp.log.txt";
        private LogFile Log { get; set; }

        private NamedPipeClientStream StdIn { get; set; }
        private NamedPipeClientStream StdOut { get; set; }
        private NamedPipeClientStream StdErr { get; set; }

        private Task StdInListner { get; set; }
        private Task StdOutListner { get; set; }
        private Task StdErrListner { get; set; }
        private Task ServerAwaiter { get; set; }

        private Process Server { get; set; }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            if (!Package.Options.QmlLspEnable)
                return null;

            var logMaxSize = 1000 * (Package.Options.QmlLspLogSize switch
            {
                >= 10 and <= 10000 => Package.Options.QmlLspLogSize,
                _ => 2500
            });
            var logTruncSize = 2 * logMaxSize / 3;

            if (PathToServer is not { Length: > 0 })
                return null;

            if (PathToQt is not { Length: > 0 })
                return null;

            if (Server is { HasExited: false }) {
                try {
                    Server.Kill();
                    Server.WaitForExit();
                } catch (Exception ex) {
                    ex.Log();
                }
                Server = null;
            }

            var server = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = PathToServer,
                    Arguments = $"-b \"{PathToQt}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            try {
                if (!server.Start())
                    return null;
                if (token.IsCancellationRequested) {
                    await StopAsync.InvokeAsync(this, EventArgs.Empty);
                    await server.WaitForExitAsync();
                    return null;
                }
            } catch (Exception e) {
                e.Log();
                return null;
            }

            Server = server;
            if (Package.Options.QmlLspLog)
                return await LogConnectionAsync(server, logMaxSize, logTruncSize);

            return new(server.StandardOutput.BaseStream, server.StandardInput.BaseStream);
        }

        public async Task OnLoadedAsync()
        {
            if (!Package.Options.QmlLspEnable)
                return;
            var qtVersionName = Package.Options.QmlLspVersion switch
            {
                { Length: > 0} x when !x.Equals("$(DefaultQtVersion)", IgnoreCase) => x,
                _ => QtVersionManager.GetDefaultVersion()
            };

            if (VersionInformation.GetOrAddByName(qtVersionName) is not { } qtVersion)
                return;

            var qtPath = qtVersion.InstallPrefix.Replace('/', '\\').TrimEnd('\\');
            if (!Directory.Exists(qtPath))
                return;

            var qmlLsPath = $"{qtVersion.LibExecs.Replace('/', '\\').TrimEnd('\\')}\\qmlls.exe";
            if (!File.Exists(qmlLsPath))
                return;

            PathToQt = qtPath;
            PathToServer = qmlLsPath;

            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public async Task OnServerInitializedAsync()
        {
            await Task.Yield();
        }


#if VS2019
        public async Task OnServerInitializeFailedAsync(Exception e)
        {
            await Task.Yield();
        }
#else
        public async Task<InitializationFailureContext>OnServerInitializeFailedAsync(
            ILanguageClientInitializationInfo initializationState)
        {
            await Task.Yield();
            return new InitializationFailureContext
            {
                FailureMessage = initializationState.StatusMessage
            };
        }
#endif

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => false;

        private async Task<Connection> LogConnectionAsync(Process server, int size, int truncSize)
        {
            Log = new(LogFilePath, size, truncSize, "===");
            StdInListner = Task.Run(async () =>
            {
                var data = new byte[4096];
                using var stdIn = new NamedPipeServerStream("qmllsp_stdin", PipeDirection.In);
                await stdIn.WaitForConnectionAsync();
                while (!server.HasExited) {
                    try {
                        var taskRead = stdIn.ReadAsync(data, 0, data.Length);
                        var taskWaitForExit = server.WaitForExitAsync();
                        await Task.WhenAny(taskRead, taskWaitForExit);
                        if (!server.HasExited && taskRead.IsCompleted) {
                            var size = await taskRead;
                            Log.Write(@$"
CLI <<< SRV [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]
{Encoding.UTF8.GetString(data, 0, size)}
===".Trim(' ', '\r', '\n') + "\r\n");
                            await server.StandardInput.BaseStream.WriteAsync(data, 0, size);
                            await server.StandardInput.BaseStream.FlushAsync();
                        }
                    } catch (Exception ex) {
                        ex.Log();
                        return;
                    }
                }
            });

            StdOutListner = Task.Run(async () =>
            {
                var data = new byte[4096];
                using var stdOut = new NamedPipeServerStream("qmllsp_stdout", PipeDirection.Out);
                await stdOut.WaitForConnectionAsync();
                while (!server.HasExited) {
                    try {
                        var taskRead = server.StandardOutput.BaseStream.ReadAsync(data, 0, data.Length);
                        var taskWaitForExit = server.WaitForExitAsync();
                        var completedTask = await Task.WhenAny(taskRead, taskWaitForExit);
                        if (!server.HasExited && taskRead.IsCompleted) {
                            var size = await taskRead;
                            Log.Write(@$"
CLI >>> SRV [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]
{Encoding.UTF8.GetString(data, 0, size)}
===".Trim(' ', '\r', '\n') + "\r\n");
                            await stdOut.WriteAsync(data, 0, size);
                            await stdOut.FlushAsync();
                        }
                    } catch (Exception ex) {
                        ex.Log();
                        return;
                    }
                }
            });

            StdErrListner = Task.Run(async () =>
            {
                var data = new byte[4096];
                using var stdErr = new NamedPipeServerStream("qmllsp_stderr", PipeDirection.Out);
                await stdErr.WaitForConnectionAsync();
                while (!server.HasExited) {
                    try {
                        var taskRead = server.StandardError.BaseStream.ReadAsync(data, 0, data.Length);
                        var taskWaitForExit = server.WaitForExitAsync();
                        var completedTask = await Task.WhenAny(taskRead, taskWaitForExit);
                        if (!server.HasExited && taskRead.IsCompleted) {
                            var size = await taskRead;
                            Log.Write(@$"
CLI !!! [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]
{Encoding.UTF8.GetString(data, 0, size)}
===".Trim(' ', '\r', '\n') + "\r\n");
                            await stdErr.WriteAsync(data, 0, size);
                            await stdErr.FlushAsync();
                        }
                    } catch (Exception ex) {
                        ex.Log();
                        return;
                    }
                }
            });

            StdIn = new NamedPipeClientStream(".", "qmllsp_stdin", PipeDirection.Out);
            while (!StdIn.IsConnected) {
                try {
                    await StdIn.ConnectAsync();
                } catch (Exception ex) {
                    ex.Log();
                    await Task.Delay(100);
                }
            }

            StdOut = new NamedPipeClientStream(".", "qmllsp_stdout", PipeDirection.In);
            while (!StdOut.IsConnected) {
                try {
                    await StdOut.ConnectAsync();
                } catch (Exception ex) {
                    ex.Log();
                    await Task.Delay(100);
                }
            }

            StdErr = new NamedPipeClientStream(".", "qmllsp_stderr", PipeDirection.In);
            while (!StdErr.IsConnected) {
                try {
                    await StdErr.ConnectAsync();
                } catch (Exception ex) {
                    ex.Log();
                    await Task.Delay(100);
                }
            }

            ServerAwaiter = Task.Run(async () =>
            {
                server.WaitForExit();
                await server.WaitForExitAsync();
                StdIn.Dispose();
                StdOut.Dispose();
                StdErr.Dispose();
            });

            return new(StdOut, StdIn);
        }
    }
}
