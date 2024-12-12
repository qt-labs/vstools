/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;

using Tasks = System.Threading.Tasks;
using Interop = Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.VisualStudio
{
    using Core;

    /// <summary>
    /// Marker interface for the Visual Studio service providing access to the idle task manager.
    /// </summary>
    /// <remarks>This service can be queried to retrieve an instance of the idle task manager for
    /// scheduling and executing tasks during Visual Studio idle time. </remarks>
    [Guid("F41B71AE-6FDD-4CE9-A8BC-C513416B5A34")]
    public interface SIdleTaskManager
    {}

    /// <summary>
    /// Interface defining a manager for scheduling and managing tasks to be run during Visual
    /// Studio idle time.
    /// </summary>
    /// <remarks>Implementations of this interface will integrate with Visual Studio's idle
    /// notification system to execute registered tasks during periods of inactivity.</remarks>
    public interface IIdleTaskManager
    {
        /// <summary>
        /// Adds a task to be executed during idle time.
        /// </summary>
        /// <param name="idleTask">The idle task to add.</param>
        void Add(IIdleTask idleTask);

        /// <summary>
        /// Removes a task from the manager, preventing it from being executed during idle time.
        /// </summary>
        /// <param name="idleTask">The idle task to remove.</param>
        void Remove(IIdleTask idleTask);
    }

    /// <summary>
    /// Interface defining a task to be run during idle time.
    /// </summary>
    public interface IIdleTask
    {
        /// <summary>
        /// Runs the idle task asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Token to signal cancellation of the task.</param>
        Tasks.Task RunAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Manages the execution of idle tasks during Visual Studio idle time.
    /// Implements <see cref="Microsoft.VisualStudio.Shell.Interop.IVsLongIdleEvents"/> to receive
    /// idle notifications.
    /// </summary>
    public class IdleTaskManager : SIdleTaskManager, IIdleTaskManager, Interop.IVsLongIdleEvents,
        System.IAsyncDisposable
    {
        private const string OnExitIdleTime = "OnExitIdleTime";

        private readonly JoinableTaskContext taskContext;
        private Interop.IVsLongIdleManager longIdleManager;
        private uint? cookie;

        private JoinableTask currentIdleTasksRunner;
        private JoinableTaskFactory backgroundPriorityFactory;
        private CancellationTokenSource currentIdleTasksRunnerCancellationTokenSource;

        private readonly object criticalSection = new();
        private IIdleTask currentIdleTask;
        private CancellationTokenSource currentIdleTaskCancellationTokenSource;
        private readonly List<IIdleTask> activeIdleTasks = new();
        private readonly List<IIdleTask> processedIdleTasks = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="IdleTaskManager"/> class.
        /// </summary>
        /// <param name="context">
        /// The join-able task context to use for managing tasks.
        /// </param>
        public IdleTaskManager(JoinableTaskContext context)
        {
            taskContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Initializes the <see cref="IdleTaskManager"/> by registering it to receive long idle
        /// notifications from Visual Studio.
        /// </summary>
        /// <param name="provider">The asynchronous service provider used to retrieve Visual Studio
        /// services.</param>
        /// <param name="token">A cancellation token to monitor for operation cancellation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>The idle notifications are triggered after 60 seconds of inactivity.</remarks>
        public async Tasks.Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken token)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);

            longIdleManager = await provider
                .GetServiceAsync<Interop.SVsLongIdleManager, Interop.IVsLongIdleManager>(false);
            cookie = longIdleManager?.AdviseLongIdleEvents(60u, this);
        }

        /// <summary>
        /// Adds an idle task to the manager for processing.
        /// </summary>
        /// <param name="idleTask">The idle task to add.</param>
        /// <remarks>
        /// Adding a task may not result in immediate processing due to the following reasons:
        /// <list type="bullet">
        /// <item><description>Visual Studio is not currently in an idle state.</description></item>
        /// <item><description>All prior tasks have been processed, and the next processing cycle
        /// is scheduled within the next 24 hours.</description></item>
        /// </list>
        /// </remarks>
        public void Add(IIdleTask idleTask)
        {
            if (idleTask == null)
                throw new ArgumentNullException(nameof(idleTask));

            lock (criticalSection)
                activeIdleTasks.Add(idleTask);
        }

        /// <summary>
        /// Removes an idle task from the manager.
        /// </summary>
        /// <param name="idleTask">The idle task to remove.</param>
        /// <remarks>
        /// If the task being removed is the currently running task, it will be immediately canceled.
        /// </remarks>
        public void Remove(IIdleTask idleTask)
        {
            if (idleTask == null)
                throw new ArgumentNullException(nameof(idleTask));

            lock (criticalSection) {
                activeIdleTasks.Remove(idleTask);
                processedIdleTasks.Remove(idleTask);
                if (currentIdleTask != idleTask)
                    return;
                currentIdleTask = null;
                currentIdleTaskCancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Triggered when Visual Studio enters idle mode.
        /// </summary>
        /// <param name="reason">Reason for entering idle.</param>
        public void OnEnterIdle(uint reason)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            using (var key = Registry.CurrentUser.OpenSubKey(Resources.SettingsRegistryPath)) {
                var value = key?.GetValue(OnExitIdleTime);
                if (!DateTime.TryParse(value?.ToString(), out var time))
                    time = DateTime.MinValue;

                // If no valid DateTime was read or if more than 24 hours have passed, swap lists
                if (time == DateTime.MinValue || (DateTime.Now - time).TotalHours >= 24) {
                    lock (criticalSection) {
                        activeIdleTasks.AddRange(processedIdleTasks);
                        processedIdleTasks.Clear();
                    }
                }
            }

            if (!activeIdleTasks.Any())
                return;

            if (currentIdleTasksRunnerCancellationTokenSource?.IsCancellationRequested == false)
                return;

            backgroundPriorityFactory ??= taskContext.Factory
                .WithPriority(VsTaskRunContext.UIThreadBackgroundPriority);

            currentIdleTasksRunnerCancellationTokenSource?.Dispose();
            currentIdleTasksRunnerCancellationTokenSource = new CancellationTokenSource();

            var previousIdleTasksRunner = currentIdleTasksRunner;
            currentIdleTasksRunner = backgroundPriorityFactory.RunAsync(async () =>
            {
                if (previousIdleTasksRunner != null)
                    await previousIdleTasksRunner;
                await ExecuteIdleTasksAsync();
            });
            currentIdleTasksRunner.FileAndForget("QtVsTools/IdleTaskManager/OnEnterIdle");
        }

        /// <summary>
        /// Triggered when Visual Studio exits idle mode.
        /// </summary>
        public void OnExitIdle()
        {
            currentIdleTaskCancellationTokenSource?.Cancel();
            currentIdleTasksRunnerCancellationTokenSource?.Cancel();

            using var registry = Registry.CurrentUser.OpenSubKey(Resources.SettingsRegistryPath,
                writable: true);
            registry?.SetValue(OnExitIdleTime, DateTime.Now);
        }

        /// <summary>
        /// Disposes resources and ends any active idle task asynchronously.
        /// </summary>
        public async Tasks.ValueTask DisposeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (cookie.HasValue)
                longIdleManager?.UnadviseLongIdleEvents(cookie.Value);

            currentIdleTaskCancellationTokenSource?.Cancel();
            currentIdleTaskCancellationTokenSource?.Dispose();

            currentIdleTasksRunnerCancellationTokenSource?.Cancel();
            await currentIdleTasksRunner.JoinAsync();
            currentIdleTasksRunnerCancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// Executes idle tasks asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Tasks.Task ExecuteIdleTasksAsync()
        {
            await Tasks.TaskScheduler.Default;
            while (true) {
                if (currentIdleTasksRunnerCancellationTokenSource?.IsCancellationRequested ?? true)
                    break;
                try {
                    lock (criticalSection) {
                        if (!activeIdleTasks.Any())
                            return;
                        currentIdleTask = activeIdleTasks[0];
                        currentIdleTaskCancellationTokenSource?.Dispose();
                        currentIdleTaskCancellationTokenSource = new CancellationTokenSource();
                    }
                    await currentIdleTask.RunAsync(currentIdleTaskCancellationTokenSource.Token);
                    lock (criticalSection) {
                        if (currentIdleTask == null)
                            continue; // can happen if the task was removed
                        if (currentIdleTaskCancellationTokenSource?.IsCancellationRequested ?? true)
                            continue; // can happen if the task was cancelled in OnExitIdle
                        processedIdleTasks.Add(currentIdleTask);
                        activeIdleTasks.Remove(currentIdleTask);
                    }
                } catch (OperationCanceledException) {
                    // Idle processing preempted for this task
                } catch (Exception exception) {
                    exception.Log();
                }
            }
        }
    }
}
