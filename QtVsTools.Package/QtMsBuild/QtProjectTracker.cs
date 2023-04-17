/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.QtMsBuild
{
    using Common;
    using Core;
    using VisualStudio;

    using SubscriberAction = ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>;

    class QtProjectTracker : Concurrent<QtProjectTracker>
    {
        static LazyFactory StaticLazy { get; } = new();

        static ConcurrentDictionary<string, QtProjectTracker> Instances => StaticLazy.Get(() =>
            Instances, () => new ConcurrentDictionary<string, QtProjectTracker>());

        static PunisherQueue<QtProjectTracker> InitQueue => StaticLazy.Get(() =>
            InitQueue, () => new PunisherQueue<QtProjectTracker>());

        static IVsTaskStatusCenterService StatusCenter => StaticLazy.Get(() =>
            StatusCenter, VsServiceProvider
                .GetService<SVsTaskStatusCenterService, IVsTaskStatusCenterService>);

        static Task InitDispatcher { get; set; }
        static ITaskHandler2 InitStatus { get; set; }

        public static string SolutionPath { get; set; } = string.Empty;

        private QtProjectTracker()
        {
            Initialized = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public EnvDTE.Project Project { get; private set; }
        public string ProjectPath { get; private set; }
        public VCProject VcProject { get; private set; }
        public UnconfiguredProject UnconfiguredProject { get; private set; }
        public EventWaitHandle Initialized { get; }

        public static bool IsTracked(string projectPath)
        {
            return Instances.ContainsKey(projectPath);
        }

        public static void Add(EnvDTE.Project project)
        {
            if (!QtVsToolsPackage.Instance.Options.ProjectTracking)
                return;

            ThreadHelper.ThrowIfNotOnUIThread();
            Get(project, project.FullName);
        }

        public static QtProjectTracker Get(EnvDTE.Project project, string projectPath)
        {
            lock (StaticCriticalSection) {
                if (Instances.TryGetValue(projectPath, out QtProjectTracker tracker))
                    return tracker;
                tracker = new QtProjectTracker
                {
                    Project = project,
                    ProjectPath = projectPath
                };
                Instances[projectPath] = tracker;
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

        static async Task InitDispatcherLoopAsync()
        {
            while (!QtVsToolsPackage.Instance.Zombied) {
                while (InitQueue.IsEmpty)
                    await Task.Delay(100);
                if (InitQueue.TryDequeue(out QtProjectTracker tracker)) {
                    if (InitStatus == null) {
                        await QtVsToolsPackage.Instance.JoinableTaskFactory
                            .SwitchToMainThreadAsync();
                        tracker.BeginInitStatus();
                        await TaskScheduler.Default;
                    } else {
                        await QtVsToolsPackage.Instance.JoinableTaskFactory
                            .SwitchToMainThreadAsync();
                        tracker.UpdateInitStatus(0);
                        await TaskScheduler.Default;
                    }
                    await tracker.InitializeAsync();
                }
                if (InitStatus != null
                    && (InitQueue.IsEmpty
                    || InitStatus.UserCancellation.IsCancellationRequested)) {
                    if (InitStatus.UserCancellation.IsCancellationRequested) {
                        InitQueue.Clear();
                    }
                    tracker.EndInitStatus();
                }
            }
        }

        async Task InitializeAsync()
        {
            int p = 0;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            UpdateInitStatus(p += 10);

            VcProject = Project.Object as VCProject;
            if (VcProject == null)
                return;
            UpdateInitStatus(p += 10);

            if (Project.Object is not IVsBrowseObjectContext context)
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

            int n = configs.Count;
            int d = (100 - p) / (n * 2);
            foreach (var config in configs) {
                var configProject = await UnconfiguredProject.LoadConfiguredProjectAsync(config);
                UpdateInitStatus(p += d);
                configProject.ProjectUnloading += OnProjectUnloadingAsync;
                if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                    Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                        + $"QtProjectTracker({Thread.CurrentThread.ManagedThreadId}): "
                        + $"Started tracking [{config.Name}] {ProjectPath}");
                }
                UpdateInitStatus(p += d);
            }
        }

        async Task OnProjectUnloadingAsync(object sender, EventArgs args)
        {
            if (sender is ConfiguredProject project) {
                if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                    Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} QtProjectTracker: "
                        + $"Stopped tracking [{project.ProjectConfiguration.Name}] "
                        + $"{project.UnconfiguredProject.FullPath}");
                }

                lock (CriticalSection) {
                    project.ProjectUnloading -= OnProjectUnloadingAsync;
                    Instances.TryRemove(project.UnconfiguredProject.FullPath, out var _);
                }

                await Task.Yield();
            }
        }

        void BeginInitStatus()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                            ProgressText = $"{Project.Name} ({InitQueue.Count} projects remaining)",
                            CanBeCanceled = true,
                            PercentComplete = 0
                        })
                        as ITaskHandler2;
                } catch (Exception exception) {
                    exception.Log();
                }
                InitStatus.RegisterTask(new Task(() => throw new InvalidOperationException()));
            }
        }

        void UpdateInitStatus(int percentComplete)
        {
            lock (StaticCriticalSection) {
                if (InitStatus == null)
                    return;
                try {
                    InitStatus.Progress.Report(new TaskProgressData
                    {
                        ProgressText = $"{Path.GetFileNameWithoutExtension(ProjectPath)} "
                            + $"({InitQueue.Count} project(s) remaining)",
                        CanBeCanceled = true,
                        PercentComplete = percentComplete
                    });
                } catch (Exception exception) {
                    exception.Log();
                }
            }
        }

        void EndInitStatus()
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
