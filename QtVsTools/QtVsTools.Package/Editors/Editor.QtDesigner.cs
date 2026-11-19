// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Package.Editors
{
    using Core;
    using Core.MsBuild;
    using Core.Options;
    using QtVsTools.Core.Common;
    using VisualStudio;

    [Guid(GuidString)]
    public class QtDesigner : Editor
    {
        public const string GuidString = "96FE523D-6182-49F5-8992-3BEA5F7E6FF6";
        public const string Title = "Qt Widgets Designer";
        public const string LegacyTitle = "Qt Designer";

        private static readonly ConcurrentDictionary<string, DesignerSession> Sessions =
            new(Utils.CaseIgnorer);

        internal static readonly ConcurrentDictionary<int, DesignerMonitor> Monitors = new();

        public QtDesigner()
            : base(new QtDesignerFileSniffer())
        { }

        private Guid? guid;
        public override Guid Guid => guid ??= new Guid(GuidString);

        public override string ExecutableName => "designer.exe";

        public override Func<string, bool> WindowFilter =>
            caption => caption.StartsWith(Title) || caption.StartsWith(LegacyTitle);

        protected override string GetTitle(Process editorProcess)
        {
            return Title;
        }

        protected override Dictionary<string, (string, bool)> GetArguments(string filePath)
        {
            Dictionary<string, (string, bool WithOption)> arguments = base.GetArguments(filePath);

            if (!Detached)
                return arguments;

            ThreadHelper.ThrowIfNotOnUIThread();
            var designerToolPath = GetToolsPath();
            if (string.IsNullOrEmpty(designerToolPath))
                throw new InvalidOperationException("Designer path cannot be null or empty.");

            var session = new DesignerSession();
            Sessions[designerToolPath] = session;

            session.Listener.Start();
            var port = ((IPEndPoint)session.Listener.LocalEndpoint).Port;
            arguments["client"] = ($"{port}", WithOption: true);

            return arguments;
        }

        public override Process Start(string filePath = "", string toolPath = null,
            bool hideWindow = true)
        {
            if (!Detached)
                return base.Start(filePath, toolPath, hideWindow);

            ThreadHelper.ThrowIfNotOnUIThread();

            toolPath = GetToolsPath();
            if (string.IsNullOrEmpty(toolPath))
                throw new InvalidOperationException("Designer path cannot be null or empty.");

            if (!Sessions.TryGetValue(toolPath, out var session)) {
                var process = base.Start(filePath, toolPath, hideWindow);
                if (!Sessions.TryGetValue(toolPath, out session))
                    return process;

                session.Process = process;
                session.Process.Exited += (s, e) =>
                {
                    // Clean up when process exits
                    session.Dispose();
                    Sessions.TryRemove(toolPath, out _);
                };

                var iar = session.Listener.BeginAcceptTcpClient(null, null);
                if (!iar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), exitContext: false))
                    throw new TimeoutException("Timed out waiting for Designer to connect.");

                var client = session.Listener.EndAcceptTcpClient(iar);
                session.Stream = client.GetStream();

                // Return immediately as the initial file was already sent
                return session.Process; // with the process startup arguments.
            }

            if (string.IsNullOrEmpty(filePath))
                return session.Process;

            // Send the file path using the network stream for subsequent calls
            var messageBytes = Encoding.UTF8.GetBytes(filePath + Environment.NewLine);
            session.Stream.Write(messageBytes, 0, messageBytes.Length);
            session.Stream.Flush();

            return session.Process;
        }

        protected override void OnStart(Process process)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.OnStart(process);
            var document = VsShell.GetDocument(Context, ItemId);

            if (document?.ProjectItem?.ContainingProject?.Object is not VCProject vcProject)
                return;

            if (MsBuildProject.GetOrAdd(vcProject) is not { IsTracked: true } project)
                return;

            if (Monitors.GetOrAdd(process.Id, _ => new DesignerMonitor(process, project)) is { } m)
                m.Watch(document.FullName);
        }

        protected override bool Detached => QtOptionsPage.DesignerDetached;

        protected override bool ShowDetachNotification => QtOptionsPage.NotifyDesignerDetachable;

        protected override void DisableDetachNotification()
        {
            try {
                QtOptionsPage.NotifyDesignerDetachable = false;
                QtOptionsPage.SaveSettingsToStorageStatic();
            } catch (Exception ex) {
                ex.Log();
            }
        }
    }
}
