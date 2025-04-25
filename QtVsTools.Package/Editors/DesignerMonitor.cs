// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Package.Editors
{
    using Core;
    using QtVsTools.Core.Common;
    using QtVsTools.Core.MsBuild;

    internal class DesignerMonitor
    {
        private readonly int processId;
        private readonly Process process;
        private readonly MsBuildProject project;
        private readonly ConcurrentDictionary<string, MonitorInfo> monitors = new(Utils.CaseIgnorer);

        private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(500);

        private class MonitorInfo
        {
            public FileSystemWatcher Watcher { get; }
            public DateTime LastHandledEventTime { get; set; }

            public MonitorInfo(FileSystemWatcher watcher)
            {
                Watcher = watcher;
                LastHandledEventTime = DateTime.MinValue;
            }
        }

        public DesignerMonitor(Process process, MsBuildProject project)
        {
            this.process = process;
            processId = process.Id;
            this.project = project;
            this.process.Exited += OnProcessExited;
        }

        public void Watch(string filePath)
        {
            if (monitors.ContainsKey(filePath))
                return;

            string directoryName = null;
            try {
                directoryName = Path.GetDirectoryName(filePath);
            } catch { /* ignored */ }

            if (string.IsNullOrWhiteSpace(directoryName))
                return;

            var watcher = new FileSystemWatcher(directoryName)
            {
                Filter = Path.GetFileName(filePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            var info = new MonitorInfo(watcher);

            watcher.Changed += (s, e) => OnChanged(info);
            watcher.Deleted += (s, e) =>
            {
                OnChanged(info);
                RemoveMonitor(filePath);
            };
            watcher.Renamed += (s, e) =>
            {
                OnChanged(info);
                RemoveMonitor(filePath);
                Watch(e.FullPath);
            };

            watcher.EnableRaisingEvents = true;
            monitors[filePath] = info;
        }

        private void OnChanged(MonitorInfo info)
        {
            var now = DateTime.UtcNow;
            if (now - info.LastHandledEventTime < DebounceInterval)
                return;
            info.LastHandledEventTime = now;
            _ = RefreshFileAsync();
        }

        private async Task RefreshFileAsync()
        {
            try {
                await project.RefreshAsync();
            } catch (Exception ex) {
                ex.Log();
            }
        }

        private void RemoveMonitor(string filePath)
        {
            if (!monitors.TryRemove(filePath, out var info))
                return;
            info.Watcher.EnableRaisingEvents = false;
            info.Watcher.Dispose();
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            foreach (var info in monitors.Values)
                info.Watcher.Dispose();

            monitors.Clear();
            QtDesigner.Monitors.TryRemove(processId, out _);
        }
    }
}
