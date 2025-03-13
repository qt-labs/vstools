// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools
{
    using Qml.Language;
    public static partial class Instances
    {
        public static QmlLanguageClient QmlLanguageClient => QmlLanguageClient.Instance;
    }
}

namespace QtVsTools.Qml.Language
{
    using Core;
    using Core.CMake;
    using Core.Options;
    using static Core.Common.Utils;
    using static Instances;

    [Export(typeof(ILanguageClient))]
    [Export(typeof(IBraceCompletionSessionProvider))]
    [BracePair('{', '}')]
    [ContentType(QmlContentType.Name)]
    public class QmlLanguageClient : Concurrent<QmlLanguageClient>,
        ILanguageClient,
        IBraceCompletionSessionProvider,
        IDisposable
    {
        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;
        public string Name => "QML LSP Client";

        private static string LogFilePath { get; } = @$"{Path.GetTempPath()}\qmlls.log.txt";
        private LogFile Log { get; set; }

        private Process Server { get; set; }

        private StreamMonitor StdIn { get; } = new();
        private StreamMonitor StdOut { get; } = new();
        private StreamMonitor StdErr { get; } = new();
        private Connection Connection { get; set; }

        public static QmlLanguageClient Instance { get; private set; }

        public QmlLanguageClient()
        {
            Instance = this;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public async Task OnLoadedAsync()
        {
            await QtVsToolsPackage.WaitUntilInitializedAsync();

            if (!QtOptionsPage.QmlLanguageServerEnable)
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

            var qmlLanguageServerVersion = QtOptionsPage.QmlLanguageServerVersion switch
            {
                { Length: > 0 } x when !string.Equals(x, "$(DefaultQtVersion)", IgnoreCase) => x,
                _ => QtVersionManager.GetDefaultVersion()
            };

            var qmlLanguageServerPath = await DeterminePathAsync(qmlLanguageServerVersion);

            qmlLanguageServerPath = HelperFunctions.ToNativeSeparator(qmlLanguageServerPath);
            if (string.IsNullOrEmpty(qmlLanguageServerPath) || !File.Exists(qmlLanguageServerPath))
                return Disconnect();

            var buildDir = await GetBuildDirAsync();
            var qmlDir = await GetQmlDirAsync();
            var docDir = await GetDocDirAsync();

            var arguments = BuildArguments(qmlLanguageServerVersion, buildDir, qmlDir, docDir);

            Server = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = qmlLanguageServerPath,
                    Arguments = arguments,
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

            if (!QtOptionsPage.QmlLanguageServerLog) {
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

        private static async Task<string> GetBuildDirAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (Package.Dte.Solution?.FullName is { Length: > 0 } slnPath)
                return Directory.Exists(slnPath) ? slnPath : Path.GetDirectoryName(slnPath);

            if (CMakeProject.ActiveProject?.RootPath is { Length: > 0 } cmakePath)
                return cmakePath;

            if (Package.Dte.ActiveDocument?.FullName is { Length: > 0 } docPath)
                return Path.GetDirectoryName(docPath);

            return Environment.CurrentDirectory;
        }

        private static async Task<VersionInformation> GetProjectQtVersionAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (CMakeProject.ActiveProject is {} cMakeProject) {
                var prefixPath = cMakeProject["cmake", "CMAKE_PREFIX_PATH"];
                return VersionInformation.GetOrAddByPath(prefixPath);
            }
            if (HelperFunctions.GetSelectedQtProject(Package.Dte) is { } msBuildProject)
                return msBuildProject.VersionInfo;
            return null;
        }

        private static async Task<string> GetQmlDirAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (CMakeProject.ActiveProject is {} cMakeProject)
                return Path.Combine(cMakeProject["cmake", "CMAKE_PREFIX_PATH"], "qml");
            if (HelperFunctions.GetSelectedQtProject(Package.Dte) is {} msBuildProject)
                return Path.Combine(msBuildProject.InstallPath, "qml");
            return "";
        }

        private static async Task<string> GetDocDirAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (CMakeProject.ActiveProject is { } cMakeProject)
                return Path.Combine(cMakeProject["cmake", "CMAKE_PREFIX_PATH"], "doc");
            if (HelperFunctions.GetSelectedQtProject(Package.Dte) is {} msBuildProject)
                return msBuildProject.VersionInfo.QtInstallDocs;
            return "";
        }

        private static async Task<string> DeterminePathAsync(string version)
        {
            // Determine the QML language server path based on the version.
            if (string.Equals(version, "$(Local LS)", StringComparison.OrdinalIgnoreCase))
                return await GetLocalQmlLanguageServerPathAsync();
            return await GetCustomQmlLanguageServerPathAsync(version);
        }

        private static async Task<string> GetLocalQmlLanguageServerPathAsync()
        {
            // Check if the project's Qt version is supported by the latest QML language server.
            var projectsQtVersion = await GetProjectQtVersionAsync();
            if (projectsQtVersion >= System.Version.Parse("6.5.0"))
                return QmlLanguageServerManager.QmlLanguageServerExePath;

            Messages.Print("Qt version not supported by the latest QML language server.");
            return Path.Combine(projectsQtVersion.LibExecs, "qmlls.exe");
        }

        private static async Task<string> GetCustomQmlLanguageServerPathAsync(string version)
        {
            if (VersionInformation.GetOrAddByName(version) is { } qtVersion)
                return Path.Combine(qtVersion.LibExecs, "qmlls.exe");
            return await Task.FromResult("");
        }

        private static string BuildArguments(string version, string buildDir, string qmlDir,
            string docDir)
        {
            var arguments = $@"-b ""{buildDir}""";

            // Build up the options based on the QML language server version./ If using the
            // GitHub-provided QML language server, assume it supports all possible options.
            if (string.Equals(version, "$(Local LS)", StringComparison.OrdinalIgnoreCase))
                return $@" -I ""{qmlDir}"" -d ""{docDir}""";

            if (VersionInformation.GetOrAddByName(version) is not {} qtVersion)
                return "";

            // The supported options for QML language server vary depending on the Qt version it
            // comes from.
            System.Version qtVersionValue = qtVersion;
            if (qtVersionValue >= System.Version.Parse("6.8.0"))
                arguments += $@" -I ""{qmlDir}""";

            if (qtVersionValue >= System.Version.Parse("6.8.1"))
                arguments += $@" -d ""{docDir}""";

            return arguments;
        }

        private void SetupLog()
        {
            if (!QtOptionsPage.QmlLanguageServerLog) {
                Log = null;
                return;
            }

            var logMaxSize = 1000 * (QtOptionsPage.QmlLanguageServerLogSize switch
            {
                >= 10 and <= 10000 => QtOptionsPage.QmlLanguageServerLogSize,
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
