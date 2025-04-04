// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using QtVsTools.Core.MsBuild;

namespace QtVsTools.Package.QML.Language
{
    using Core;
    using Core.CMake;
    using Core.Options;
    using Lsp;
    using QtVsTools.Core.Common;

    using static Core.Common.Utils;
    using static Instances;

    public partial class QmlLanguageClient : Concurrent<QmlLanguageClient>
    {
        private LogFile Log { get; set; }
        private static string Timestamp => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff}";
        private static string LogFilePath { get; } = @$"{Path.GetTempPath()}\qmlls.log.txt";

        internal bool InitializeMessageSent { get; private set; }
        internal List<WorkspaceFolder> WorkspaceFolders { get; set; }

        private async Task LoadServerAsync()
        {
            await QtVsToolsPackage.WaitUntilInitializedAsync();

            if (!QtOptionsPage.QmlLanguageServerEnable)
                return;

            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        private async Task<Connection> ActivateServerAsync(CancellationToken token)
        {
            SetupLog();

            var qmlLanguageServerPath = QtOptionsPage.QmlLanguageServerPath;
            if (string.IsNullOrWhiteSpace(qmlLanguageServerPath))
                qmlLanguageServerPath = await GetLocalQmlLanguageServerPathAsync();

            qmlLanguageServerPath = HelperFunctions.ToNativeSeparator(qmlLanguageServerPath);
            if (string.IsNullOrEmpty(qmlLanguageServerPath) || !File.Exists(qmlLanguageServerPath))
                return null;

            var buildDir = await GetBuildDirAsync();
            var qmlDir = await GetQmlDirAsync();
            var docDir = await GetDocDirAsync();

            var arguments = BuildArguments(QtOptionsPage.QmlLanguageServerPath, buildDir, qmlDir,
                docDir);

            var server = new Process
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
                server.ErrorDataReceived += OnErrorDataReceived;

                if (!server.Start())
                    return null;

                server.BeginErrorReadLine();
                if (server.HasExited)
                    return null;

                var stdIn = new StreamMonitor(server.StandardInput.BaseStream);
                stdIn.StreamModifier += StreamModifier;
                stdIn.DataReceived += OnStdInDataReceived;
                stdIn.LoggingEnabled = QtOptionsPage.QmlLanguageServerLog;

                var stdOut = new StreamMonitor(server.StandardOutput.BaseStream);
                stdOut.DataReceived += OnStdOutDataReceived;
                stdOut.LoggingEnabled = QtOptionsPage.QmlLanguageServerLog;

                if (stdIn.IsConnected && stdOut.IsConnected) {
                    Log?.Write($"CLIENT CONNECTED [{Timestamp}]\r\n");
                    return new Connection(stdOut, stdIn);
                }

                stdIn.Dispose();
                stdOut.Dispose();
            } catch (Exception e) {
                e.Log();
            }

            return null;
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

            if (CMakeProject.ActiveProject is { } cMakeProject) {
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

            if (CMakeProject.ActiveProject is { } cMakeProject)
                return Path.Combine(cMakeProject["cmake", "CMAKE_PREFIX_PATH"] ?? "", "qml");
            if (HelperFunctions.GetSelectedQtProject(Package.Dte) is { } msBuildProject)
                return Path.Combine(msBuildProject.InstallPath, "qml");
            return "";
        }

        private static async Task<string> GetDocDirAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (CMakeProject.ActiveProject is { } cMakeProject)
                return Path.Combine(cMakeProject["cmake", "CMAKE_PREFIX_PATH"] ?? "", "doc");
            if (HelperFunctions.GetSelectedQtProject(Package.Dte) is { } msBuildProject)
                return msBuildProject.VersionInfo.QtInstallDocs;
            return "";
        }

        private static async Task<string> GetLocalQmlLanguageServerPathAsync()
        {
            // Check if the project's Qt version is supported by the latest QML Language Server.
            var projectsQtVersion = await GetProjectQtVersionAsync();
            if (projectsQtVersion >= System.Version.Parse("6.5.0"))
                return QmlLanguageServerManager.QmlLanguageServerExePath;

            Messages.Print("Qt version not supported by the latest QML Language Server.");
            return Path.Combine(projectsQtVersion.LibExecs, "qmlls.exe");
        }

        private static string BuildArguments(string qmlLsPath, string buildDir, string qmlDir,
            string docDir)
        {
            var arguments = $@"-b ""{buildDir}""";

            // Build up the options based on the QML Language Server version. If using the
            // GitHub-provided QML Language Server, assume it supports all possible options.
            if (string.IsNullOrWhiteSpace(qmlLsPath))
                return $@"{arguments} -I ""{qmlDir}"" -d ""{docDir}""";

            var qtLibExecs = Path.GetDirectoryName(qmlLsPath);
            if (VersionInformation.GetOrAddByPath(qtLibExecs) is not { } qtVersion)
                return arguments;

            // The supported options for QML Language Server vary depending on the Qt version it
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

            var logMaxSize = 1000 * QtOptionsPage.QmlLanguageServerLogSize switch
            {
                >= 10 and <= 10000 => QtOptionsPage.QmlLanguageServerLogSize,
                _ => 2500
            };
            var logTruncSize = 2 * logMaxSize / 3;

            Log = new LogFile(LogFilePath, logMaxSize, logTruncSize, "===");
        }

        private void OnStdInDataReceived(StreamDataEventArgs args)
        {
            Log?.Write($"CLIENT --> SERVER [{Timestamp}], "
                + $"{Encoding.UTF8.GetString(args.Data, 0, args.Data.Length)}\r\n===\r\n\r\n");
        }

        private void OnStdOutDataReceived(StreamDataEventArgs args)
        {
            Log?.Write($"SERVER --> CLIENT [{Timestamp}], "
                + $"{Encoding.UTF8.GetString(args.Data, 0, args.Data.Length)}\r\n===\r\n\r\n");
        }

        private static void OnErrorDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (!string.IsNullOrEmpty(eventArgs.Data))
                Messages.Print($">>> qmlls({((Process)sender).Id}): {eventArgs.Data}");
        }

        private (StreamData Data, StreamAction action) StreamModifier(StreamData streamData)
        {
            try {
                var message = MessageParser.Deserialize(streamData);
                if (message.Value<string>("method") != "initialize")
                    return (streamData, StreamAction.KeepStreaming);

                if (!message.TryGetValue("params", out var messageParams))
                    return (streamData, StreamAction.StopStreaming);

                var capabilities = messageParams["capabilities"];
                if (capabilities == null)
                    return (streamData, StreamAction.StopStreaming);

                capabilities["workspace"] ??= new JObject();
                capabilities["workspace"]["workspaceFolders"] = true;
                capabilities["workspace"]["didChangeWatchedFiles"] ??= new JObject();
                capabilities["workspace"]["didChangeWatchedFiles"]["dynamicRegistration"] = true;
                capabilities["workspace"]["configuration"] = true;

                WorkspaceFolders = GetWorkspaceFolders();
                messageParams["workspaceFolders"] = JToken.FromObject(WorkspaceFolders.ToArray());

                InitializeMessageSent = true;

                // Return the modified message and stop further modifications.
                return (MessageParser.Serialize(message), StreamAction.StopStreaming);
            } catch (Exception exception) {
                exception.Log();
            }
            return (streamData, StreamAction.KeepStreaming);
        }

        // The workspace folders configured in the client when the server starts.
        // This property is only available if the client supports workspace folders.
        // It can be null if the client supports workspace folders but none are configured.
        internal static List<WorkspaceFolder> GetWorkspaceFolders()
        {
            if (CMakeProject.ActiveProject is { } cMakeProject) {
                return new List<WorkspaceFolder>
                {
                    new()
                    {
                        Uri = new Uri(cMakeProject.Project.Location, UriKind.Absolute).AbsoluteUri,
                        Name = GetLastPathSegment(cMakeProject.Project.Location)
                    }
                };
            }

            return MsBuildProject.GetProjects()
                .Select(qtProject => new WorkspaceFolder
                    {
                        Uri = new Uri(qtProject.VcProjectDirectory, UriKind.Absolute).AbsoluteUri,
                        Name = qtProject.VcProject.Name
                    }
                )
                .ToList();

            static string GetLastPathSegment(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return path;
                path = HelperFunctions.ToNativeSeparator(path);
                return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
            }
        }
    }
}
