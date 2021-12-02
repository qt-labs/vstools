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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;
using EnvDTE;

namespace QtVsTools.QtMsBuild
{
    using Core;
    using VisualStudio;
    using Thread = System.Threading.Thread;
    using SubscriberAction = ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>;

    class QtProjectTracker : Concurrent<QtProjectTracker>
    {
        static ConcurrentDictionary<string, QtProjectTracker> _Instances;
        static ConcurrentDictionary<string, QtProjectTracker> Instances =>
            StaticThreadSafeInit(() => _Instances, () =>
                _Instances = new ConcurrentDictionary<string, QtProjectTracker>());

        static PunisherQueue<QtProjectTracker> _InitQueue;
        static PunisherQueue<QtProjectTracker> InitQueue =>
            StaticThreadSafeInit(() => _InitQueue, () =>
                _InitQueue = new PunisherQueue<QtProjectTracker>());

        static IVsTaskStatusCenterService _StatusCenter;
        static IVsTaskStatusCenterService StatusCenter => StaticThreadSafeInit(() => _StatusCenter,
                () => _StatusCenter = VsServiceProvider
                    .GetService<SVsTaskStatusCenterService, IVsTaskStatusCenterService>());

        static Task InitDispatcher { get; set; }
        static ITaskHandler2 InitStatus { get; set; }

        public static string SolutionPath { get; set; } = string.Empty;

        private QtProjectTracker()
        {
            Initialized = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        class Subscriber : IDisposable
        {
            public Subscriber(QtProjectTracker tracker, ConfiguredProject config)
            {
                Tracker = tracker;
                Config = config;
                Subscription = Config.Services.ProjectSubscription.JointRuleSource.SourceBlock
                    .LinkTo(new SubscriberAction(ProjectUpdateAsync),
                        ruleNames: new[]
                        {
                            "ClCompile",
                            "QtRule10_Settings",
                            "QtRule30_Moc",
                            "QtRule40_Rcc",
                            "QtRule60_Repc",
                            "QtRule50_Uic",
                            "QtRule_Translation",
                            "QtRule70_Deploy",
                        },
                        initialDataAsNew: false
                    );
            }

            QtProjectTracker Tracker { get; set; }
            ConfiguredProject Config { get; set; }
            IDisposable Subscription { get; set; }

            public void Dispose()
            {
                Subscription?.Dispose();
                Subscription = null;
            }

            async Task ProjectUpdateAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
            {
                await Tracker.OnProjectUpdateAsync(Config, update.Value);
            }
        }

        public EnvDTE.Project Project { get; private set; }
        public UnconfiguredProject UnconfiguredProject { get; private set; }
        public EventWaitHandle Initialized { get; private set; }
        List<Subscriber> Subscribers { get; set; }

        public static bool IsTracked(EnvDTE.Project project)
        {
            return Instances.ContainsKey(project.FullName);
        }

        public static void Add(EnvDTE.Project project)
        {
            if (!QtVsToolsPackage.Instance.Options.ProjectTracking)
                return;
            Get(project);
        }

        public static QtProjectTracker Get(EnvDTE.Project project)
        {
            lock (StaticCriticalSection) {
                QtProjectTracker tracker = null;
                if (Instances.TryGetValue(project.FullName, out tracker))
                    return tracker;
                tracker = new QtProjectTracker
                {
                    Project = project,
                };
                Instances[project.FullName] = tracker;
                InitQueue.Enqueue(tracker);
                if (InitDispatcher == null)
                    InitDispatcher = Task.Run(InitDispatcherLoopAsync);
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
                QtProjectTracker tracker;
                if (InitQueue.TryDequeue(out tracker)) {
                    if (InitStatus == null) {
                        await QtVsToolsPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
                        tracker.BeginInitStatus();
                        await TaskScheduler.Default;
                    } else {
                        tracker.UpdateInitStatus(0);
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
            UpdateInitStatus(p += 10);

            await QtVsToolsPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            UpdateInitStatus(p += 10);

            var context = Project.Object as IVsBrowseObjectContext;
            if (context == null)
                return;
            UpdateInitStatus(p += 10);

            UnconfiguredProject = context.UnconfiguredProject;
            if (UnconfiguredProject == null
                || UnconfiguredProject.ProjectService == null
                || UnconfiguredProject.ProjectService.Services == null)
                return;
            await TaskScheduler.Default;
            UpdateInitStatus(p += 10);

            var configs = await UnconfiguredProject.Services
                .ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
            UpdateInitStatus(p += 10);

            Initialized.Set();

            Subscribers = new List<Subscriber>();
            int n = configs.Count;
            int d = (100 - p) / (n * 2);
            foreach (var config in configs) {
                var configProject = await UnconfiguredProject.LoadConfiguredProjectAsync(config);
                UpdateInitStatus(p += d);
                Subscribers.Add(new Subscriber(this, configProject));
                configProject.ProjectUnloading += OnProjectUnloading;
                if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                    Messages.Print(string.Format(
                        "{0:HH:mm:ss.FFF} QtProjectTracker({1}): Started tracking [{2}] {3}",
                        DateTime.Now, Thread.CurrentThread.ManagedThreadId,
                        config.Name,
                        UnconfiguredProject.FullPath));
                }
                UpdateInitStatus(p += d);
            }
        }


        async Task OnProjectUpdateAsync(ConfiguredProject config, IProjectSubscriptionUpdate update)
        {
            var changes = update.ProjectChanges.Values
                .Where(x => x.Difference.AnyChanges)
                .Select(x => x.Difference);
            var changesCount = changes
                .Select(x => x.AddedItems.Count
                    + x.ChangedItems.Count
                    + x.ChangedProperties.Count
                    + x.RemovedItems.Count
                    + x.RenamedItems.Count)
                .Sum();
            var changedProps = changes.SelectMany(x => x.ChangedProperties);
            if (changesCount == 0
                || (changesCount == 1
                    && changedProps.Count() == 1
                    && changedProps.First() == "QtLastBackgroundBuild")) {
                return;
            }

            if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                Messages.Print(string.Format(
                    "{0:HH:mm:ss.FFF} QtProjectTracker({1}): Changed [{2}] {3}",
                    DateTime.Now, Thread.CurrentThread.ManagedThreadId,
                    config.ProjectConfiguration.Name,
                    config.UnconfiguredProject.FullPath));
            }
            await QtProjectIntellisense.RefreshAsync(Project, config.ProjectConfiguration.Name);
        }

        async Task OnProjectUnloading(object sender, EventArgs args)
        {
            var project = sender as ConfiguredProject;
            if (project == null || project.Services == null)
                return;
            if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                Messages.Print(string.Format(
                    "{0:HH:mm:ss.FFF} QtProjectTracker: Stopped tracking [{1}] {2}",
                    DateTime.Now,
                    project.ProjectConfiguration.Name,
                    project.UnconfiguredProject.FullPath));
            }
            lock (CriticalSection) {
                if (Subscribers != null) {
                    Subscribers.ForEach(s => s.Dispose());
                    Subscribers.Clear();
                    Subscribers = null;
                }
                project.ProjectUnloading -= OnProjectUnloading;
                Instances[Project.FullName] = null;
            }
        }

        void BeginInitStatus()
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
                            ProgressText = string.Format("{0} ({1} projects remaining)",
                                Project.Name, InitQueue.Count),
                            CanBeCanceled = true,
                            PercentComplete = 0
                        })
                        as ITaskHandler2;
                } catch (Exception e) {
                    Messages.Print(
                        e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
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
                        ProgressText = string.Format("{0} ({1} project(s) remaining)",
                            Project.Name, InitQueue.Count),
                        CanBeCanceled = true,
                        PercentComplete = percentComplete
                    });
                } catch (Exception e) {
                    Messages.Print(
                        e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
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
