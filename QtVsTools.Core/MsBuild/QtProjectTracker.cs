/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.MsBuild
{
    using Common;
    using Core;
    using Options;
    using VisualStudio;

    public class QtProjectTracker : Concurrent<QtProjectTracker>
    {
        private static LazyFactory StaticLazy { get; } = new();

        private static ConcurrentDictionary<string, QtProjectTracker> Instances => StaticLazy.Get(() =>
            Instances, () => new ConcurrentDictionary<string, QtProjectTracker>());

        private static PunisherQueue<QtProjectTracker> InitQueue => StaticLazy.Get(() =>
            InitQueue, () => new PunisherQueue<QtProjectTracker>());

        private static IVsTaskStatusCenterService StatusCenter => StaticLazy.Get(() =>
            StatusCenter, VsServiceProvider
                .GetService<SVsTaskStatusCenterService, IVsTaskStatusCenterService>);

        private static Task InitDispatcher { get; set; }
        private static ITaskHandler2 InitStatus { get; set; }

        public static string SolutionPath { get; set; } = string.Empty;

        private QtProjectTracker()
        {
            Initialized = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public QtProject QtProject { get; private set; }

        public UnconfiguredProject UnconfiguredProject { get; private set; }
        public EventWaitHandle Initialized { get; }

        public static bool IsTracked(string projectPath)
        {
            return !string.IsNullOrEmpty(projectPath) && Instances.ContainsKey(projectPath);
        }

        /// <summary>
        /// Tries to return the Qt project tracker the project belongs to.
        /// If the project is not tracked it gets added to the tracking cache.
        /// </summary>
        /// <param name="project">The tracked project.</param>
        /// <returns><see langword="null" /> if the given project is <see langword="null" /> or
        /// project tracking is disabled by the user (via settings).</returns>
        public static QtProjectTracker GetOrAdd(QtProject project)
        {
            if (project == null || Options.Get() is not { ProjectTracking: true })
                return null;
            lock (StaticCriticalSection) {
                if (Instances.TryGetValue(project.VcProjectPath, out var tracker))
                    return tracker;
                tracker = new QtProjectTracker
                {
                    QtProject = project
                };
                Instances[project.VcProjectPath] = tracker;
                InitQueue.Enqueue(tracker);
                InitDispatcher ??= Task.Run(InitDispatcherLoopAsync);
                return tracker;
            }
        }

        public static void Reset()
        {
            lock (StaticCriticalSection) {
                Instances.Clear();
                InitQueue.Clear();
            }
        }

        private static async Task InitDispatcherLoopAsync()
        {
            if (VsServiceProvider.Instance is not AsyncPackage package)
                return;

            while (!package.Zombied) {
                while (InitQueue.IsEmpty)
                    await Task.Delay(100);
                if (InitQueue.TryDequeue(out var tracker)) {
                    if (InitStatus == null) {
                        await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        tracker.BeginInitStatus();
                        await TaskScheduler.Default;
                    } else {
                        await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        tracker.UpdateInitStatus(0);
                        await TaskScheduler.Default;
                    }
                    await tracker.InitializeAsync();
                }

                if (InitStatus == null)
                    continue;
                var cancellationRequested = InitStatus.UserCancellation.IsCancellationRequested;
                if (!InitQueue.IsEmpty && !cancellationRequested)
                    continue;
                if (cancellationRequested)
                    InitQueue.Clear();
                EndInitStatus();
            }
        }

        private async Task InitializeAsync()
        {
            var p = 0;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            UpdateInitStatus(p += 10);

            if (QtProject == null)
                return;
            UpdateInitStatus(p += 10);

            if (QtProject.VcProject.Object is not IVsBrowseObjectContext context)
                return;
            UpdateInitStatus(p += 10);

            UnconfiguredProject = context.UnconfiguredProject;
            if (UnconfiguredProject?.ProjectService.Services == null)
                return;

            await TaskScheduler.Default;
            UpdateInitStatus(p += 10);

            var configs = await UnconfiguredProject.Services
                .ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
            UpdateInitStatus(p += 10);

            Initialized.Set();

            var n = configs.Count;
            var d = (100 - p) / (n * 2);
            foreach (var config in configs) {
                var configProject = await UnconfiguredProject.LoadConfiguredProjectAsync(config);
                UpdateInitStatus(p += d);
                configProject.ProjectUnloading += OnProjectUnloadingAsync;
                if (Options.Get() is { BuildDebugInformation: true }) {
                    Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                        + $"QtProjectTracker({Thread.CurrentThread.ManagedThreadId}): "
                        + $"Started tracking [{config.Name}] {QtProject.VcProjectPath}");
                }
                UpdateInitStatus(p += d);
            }
        }

        private async Task OnProjectUnloadingAsync(object sender, EventArgs args)
        {
            if (sender is ConfiguredProject project) {
                if (Options.Get() is { BuildDebugInformation: true }) {
                    Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} QtProjectTracker: "
                        + $"Stopped tracking [{project.ProjectConfiguration.Name}] "
                        + $"{project.UnconfiguredProject.FullPath}");
                }

                lock (CriticalSection) {
                    project.ProjectUnloading -= OnProjectUnloadingAsync;
                    Instances.TryRemove(project.UnconfiguredProject.FullPath, out _);
                }

                await Task.Yield();
            }
        }

        private void BeginInitStatus()
        {
            lock (StaticCriticalSection) {
                if (InitStatus != null)
                    return;
                try {
                    InitStatus = StatusCenter.PreRegister(
                        new TaskHandlerOptions
                        {
                            Title = "Qt VS Tools: Setting up project tracking..."
                        },
                        new TaskProgressData
                        {
                            ProgressText = $"{QtProject.VcProjectPath} ({InitQueue.Count} projects remaining)",
                            CanBeCanceled = true,
                            PercentComplete = 0
                        })
                        as ITaskHandler2;
                } catch (Exception exception) {
                    exception.Log();
                }
                InitStatus?.RegisterTask(new Task(() => throw new InvalidOperationException()));
            }
        }

        private void UpdateInitStatus(int percentComplete)
        {
            lock (StaticCriticalSection) {
                if (InitStatus == null)
                    return;
                try {
                    InitStatus.Progress.Report(new TaskProgressData
                    {
                        ProgressText = $"{Path.GetFileNameWithoutExtension(QtProject.VcProjectPath)} "
                            + $"({InitQueue.Count} project(s) remaining)",
                        CanBeCanceled = true,
                        PercentComplete = percentComplete
                    });
                } catch (Exception exception) {
                    exception.Log();
                }
            }
        }

        private static void EndInitStatus()
        {
            lock (StaticCriticalSection) {
                if (InitStatus == null)
                    return;
                InitStatus.Dismiss();
                InitStatus = null;
            }
        }
    }
}
