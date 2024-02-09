/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
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

namespace QtVsTools
{
    using Qml.Language;
    public static partial class Instances
    {
        public static QmlLspClient QmlLspClient => QmlLspClient.Instance;
    }
}

namespace QtVsTools.Qml.Language
{
    using Core;
    using static Instances;
    using static Core.Common.Utils;

    [Export(typeof(ILanguageClient))]
    [Export(typeof(IBraceCompletionSessionProvider))]
    [BracePair('{', '}')]
    [ContentType(QmlContentType.Name)]
    public class QmlLspClient : Concurrent<QmlLspClient>,
        ILanguageClient,
        IBraceCompletionSessionProvider,
        IDisposable
    {
        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;
        public string Name => "QML LSP Client";

        private static string LogFilePath { get; } = @$"{Path.GetTempPath()}\qmllsp.log.txt";
        private LogFile Log { get; set; }

        private Process Server { get; set; }

        private StreamMonitor StdIn { get; } = new();
        private StreamMonitor StdOut { get; } = new();
        private StreamMonitor StdErr { get; } = new();
        private Connection Connection { get; set; }

        public static QmlLspClient Instance { get; private set; }

        public QmlLspClient()
        {
            Instance = this;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public async Task OnLoadedAsync()
        {
            if (!Package.Options.QmlLspEnable)
                Disconnect();
            else
                await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            SetupLog();

            StdIn.StreamData += OnStdInData;
            StdIn.Disconnected += OnDisconnected;
            StdOut.StreamData += OnStdOutData;
            StdOut.Disconnected += OnDisconnected;
            StdErr.StreamData += OnStdErrData;
            StdErr.Disconnected += OnDisconnected;

            if (Server is { HasExited: false })
                Disconnect();

            var qtVersionName = Package.Options.QmlLspVersion switch
            {
                { Length: > 0 } x when !x.Equals("$(DefaultQtVersion)", IgnoreCase) => x,
                _ => QtVersionManager.GetDefaultVersion()
            };
            if (VersionInformation.GetOrAddByName(qtVersionName) is not { } qtVersion)
                return Disconnect();

            var qtPath = qtVersion.InstallPrefix.Replace('/', '\\').TrimEnd('\\');
            if (qtPath is not { Length: > 0 } || !Directory.Exists(qtPath))
                return Disconnect();

            var qmLlsPath = $"{qtVersion.LibExecs.Replace('/', '\\').TrimEnd('\\')}\\qmlls.exe";
            if (qmLlsPath is not { Length: > 0 } || !File.Exists(qmLlsPath))
                return Disconnect();

            Server = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = qmLlsPath,
                    Arguments = $"-b \"{qtPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try {
                if (!Server.Start())
                    return Disconnect();
            } catch (Exception e) {
                e.Log();
                return Disconnect();
            }

            if (!Package.Options.QmlLspLog) {
                StdIn.SetStream(Server.StandardInput);
                StdOut.SetStream(Server.StandardOutput);
                StdErr.SetStream(Server.StandardError);
            } else {
                await Task.WhenAll(
                    StdIn.ConnectAsync(Server.StandardInput),
                    StdOut.ConnectAsync(Server.StandardOutput),
                    StdErr.ConnectAsync(Server.StandardError));
                if (!StdIn.IsConnected || !StdOut.IsConnected || !StdErr.IsConnected)
                    return Disconnect();
            }

            return Connect();
        }

        private void SetupLog()
        {
            if (!Package.Options.QmlLspLog) {
                Log = null;
                return;
            }

            var logMaxSize = 1000 * (Package.Options.QmlLspLogSize switch
            {
                >= 10 and <= 10000 => Package.Options.QmlLspLogSize,
                _ => 2500
            });
            var logTruncSize = 2 * logMaxSize / 3;

            Log = new LogFile(LogFilePath, logMaxSize, logTruncSize, "===");
        }

        private string Timestamp => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff}";

        private Connection Connect()
        {
            Log?.Write(@$"
CLIENT CONNECTED [{Timestamp}]
===".Trim(' ', '\r', '\n') + "\r\n");

            return Connection = new(StdOut, StdIn);
        }

        public Connection Disconnect()
        {
            if (Server is { HasExited: false })
                Server.Kill();
            Server = null;

            StdIn.StreamData -= OnStdInData;
            StdIn.Disconnected -= OnDisconnected;
            StdOut.StreamData -= OnStdOutData;
            StdOut.Disconnected -= OnDisconnected;
            StdErr.StreamData -= OnStdErrData;
            StdErr.Disconnected -= OnDisconnected;

            StdIn.Dispose();
            StdOut.Dispose();
            StdErr.Dispose();

            _ = Task.Run(async () => await StopAsync.InvokeAsync(this, EventArgs.Empty));

            Log?.Write(@$"
CLIENT DISCONNECTED [{Timestamp}]
===".Trim(' ', '\r', '\n') + "\r\n");

            Connection?.Dispose();
            return Connection = null;
        }

        private void OnDisconnected(object sender, EventArgs args)
        {
            Disconnect();
        }

        private void OnStdInData(object sender, StreamDataEventArgs args)
        {
            Log?.Write(@$"
CLIENT --> SERVER [{Timestamp}]
{Encoding.UTF8.GetString(args.Data, 0, args.Data.Length)}
===".Trim(' ', '\r', '\n') + "\r\n");
        }

        private void OnStdOutData(object sender, StreamDataEventArgs args)
        {
            Log?.Write(@$"
SERVER --> CLIENT [{Timestamp}]
{Encoding.UTF8.GetString(args.Data, 0, args.Data.Length)}
===".Trim(' ', '\r', '\n') + "\r\n");
        }

        private void OnStdErrData(object sender, StreamDataEventArgs args)
        {
            Log?.Write(@$"
SERVER ERROR [{Timestamp}]
{Encoding.UTF8.GetString(args.Data, 0, args.Data.Length)}
===".Trim(' ', '\r', '\n') + "\r\n");
        }

        #region ### BOILERPLATE ###################################################################

#if VS2019
        public async Task OnServerInitializeFailedAsync(Exception e)
        {
            await Task.Yield();
        }
#else
        public async Task<InitializationFailureContext> OnServerInitializeFailedAsync(
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

        public async Task OnServerInitializedAsync() => await Task.Yield();

        public bool TryCreateSession(ITextView textView, SnapshotPoint openingPoint,
            char openingBrace, char closingBrace, out IBraceCompletionSession session)
        {
            session = null;
            return true;
        }

        #endregion
    }
}
