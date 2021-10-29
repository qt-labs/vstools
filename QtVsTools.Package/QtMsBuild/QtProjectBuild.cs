/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem;
#if VS2015
using Microsoft.VisualStudio.ProjectSystem.Designers;
#endif
#if !VS2015
using Microsoft.VisualStudio.TaskStatusCenter;
#endif
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;

namespace QtVsTools.QtMsBuild
{
    using Core;
    using VisualStudio;
    using Thread = System.Threading.Thread;

    class QtProjectBuild : Concurrent<QtProjectBuild>
    {
        static PunisherQueue<QtProjectBuild> _BuildQueue;
        static PunisherQueue<QtProjectBuild> BuildQueue =>
            StaticThreadSafeInit(() => _BuildQueue,
                () => _BuildQueue = new PunisherQueue<QtProjectBuild>(
                    getItemKey: (QtProjectBuild build) =>
                    {
                        return build.ConfiguredProject;
                    })
                );

        static ConcurrentStopwatch _RequestTimer;
        static ConcurrentStopwatch RequestTimer =>
            StaticThreadSafeInit(() => _RequestTimer, () => _RequestTimer = new ConcurrentStopwatch());

#if !VS2015
        static IVsTaskStatusCenterService _StatusCenter;
        static IVsTaskStatusCenterService StatusCenter => StaticThreadSafeInit(() => _StatusCenter,
                () => _StatusCenter = VsServiceProvider
                    .GetService<SVsTaskStatusCenterService, IVsTaskStatusCenterService>());
#endif

        EnvDTE.Project Project { get; set; }
        UnconfiguredProject UnconfiguredProject { get; set; }
        ConfiguredProject ConfiguredProject { get; set; }
        Dictionary<string, string> Properties { get; set; }
        List<string> Targets { get; set; }
        LoggerVerbosity LoggerVerbosity { get; set; }

        static Task BuildDispatcher { get; set; }

        public static void StartBuild(
            EnvDTE.Project project,
            string configName,
            Dictionary<string, string> properties,
            IEnumerable<string> targets,
            LoggerVerbosity verbosity = LoggerVerbosity.Quiet)
        {
            if (project == null)
                throw new ArgumentException("Project cannot be null.");
            if (configName == null)
                throw new ArgumentException("Configuration name cannot be null.");

            Task.Run(() => StartBuildAsync(project, configName, properties, targets, verbosity));
        }

        public static async Task StartBuildAsync(
            EnvDTE.Project project,
            string configName,
            Dictionary<string, string> properties,
            IEnumerable<string> targets,
            LoggerVerbosity verbosity)
        {
            if (project == null)
                throw new ArgumentException("Project cannot be null.");
            if (configName == null)
                throw new ArgumentException("Configuration name cannot be null.");

            RequestTimer.Restart();
            if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                Messages.Print(string.Format(
                "{0:HH:mm:ss.FFF} QtProjectBuild({1}): Request [{2}] {3}",
                DateTime.Now, Thread.CurrentThread.ManagedThreadId,
                configName, project.FullName));
            }

            var tracker = QtProjectTracker.Get(project);
            await tracker.Initialized;

            var knownConfigs = await tracker.UnconfiguredProject.Services
                .ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();

            ConfiguredProject configuredProject = null;
            foreach (var config in knownConfigs) {
                var configProject = await tracker.UnconfiguredProject
                    .LoadConfiguredProjectAsync(config);
                if (configProject.ProjectConfiguration.Name == configName) {
                    configuredProject = configProject;
                    break;
                }
            }
            if (configuredProject == null)
                throw new ArgumentException(string.Format("Unknown configuration '{0}'.", configName));

            BuildQueue.Enqueue(new QtProjectBuild()
            {
                Project = project,
                UnconfiguredProject = tracker.UnconfiguredProject,
                ConfiguredProject = configuredProject,
                Properties = properties?.ToDictionary(x => x.Key, x => x.Value),
                Targets = targets?.ToList(),
                LoggerVerbosity = verbosity
            });
            StaticThreadSafeInit(() => BuildDispatcher,
                () => BuildDispatcher = Task.Run(BuildDispatcherLoopAsync))
                .Forget();
        }

        public static void Reset()
        {
            BuildQueue.Clear();
        }

        static async Task BuildDispatcherLoopAsync()
        {
#if !VS2015
            ITaskHandler2 dispatchStatus = null;
#endif
            while (!QtVsToolsPackage.Instance.Zombied) {
                while (BuildQueue.IsEmpty || RequestTimer.ElapsedMilliseconds < 1000) {
#if !VS2015
                    if (BuildQueue.IsEmpty && dispatchStatus != null) {
                        dispatchStatus.Dismiss();
                        dispatchStatus = null;
                    }
#endif
                    await Task.Delay(100);
                }
                QtProjectBuild buildRequest;
                if (BuildQueue.TryDequeue(out buildRequest)) {
#if !VS2015
                    if (dispatchStatus == null) {
                        dispatchStatus = StatusCenter.PreRegister(
                            new TaskHandlerOptions
                            {
                                Title = "Qt VS Tools",
                            },
                            new TaskProgressData
                            {
                                ProgressText = string.Format(
                                    "Refreshing IntelliSense data, {0} project(s) remaining...",
                                    BuildQueue.Count),
                                CanBeCanceled = true
                            })
                            as ITaskHandler2;
                        dispatchStatus.RegisterTask(new Task(() =>
                            throw new InvalidOperationException()));
                    } else {
                        dispatchStatus.Progress.Report(
                            new TaskProgressData
                            {
                                ProgressText = string.Format(
                                    "Refreshing IntelliSense data, {0} project(s) remaining...",
                                    BuildQueue.Count),
                                CanBeCanceled = true,
                            });
                    }
#endif
                    await buildRequest.BuildAsync();
                }
                if (BuildQueue.IsEmpty
#if !VS2015
                    || dispatchStatus?.UserCancellation.IsCancellationRequested == true
#endif
                    ) {
#if !VS2015
                    if (dispatchStatus != null) {
                        dispatchStatus.Dismiss();
                        dispatchStatus = null;
                    }
#endif
                    Reset();
                }
            }
        }

        async Task BuildAsync()
        {
            if (LoggerVerbosity != LoggerVerbosity.Quiet) {
                Messages.Print(clear: !QtVsToolsPackage.Instance.Options.BuildDebugInformation, activate: true,
                    text: string.Format(
@"== {0}: starting build...
  * Properties: {1}
  * Targets: {2}
",
                    /*{0}*/ Project.Name,
                    /*{1}*/ string.Join("", Properties
                        .Select(property => string.Format(@"
        {0} = {1}",     /*{0}*/ property.Key, /*{1}*/ property.Value))),
                    /*{2}*/ string.Join(";", Targets)));
            }

            var lockService = UnconfiguredProject.ProjectService.Services.ProjectLockService;

            bool ok = false;
            try {
                ProjectWriteLockReleaser writeAccess;
                var timer = ConcurrentStopwatch.StartNew();
                while (timer.IsRunning) {
                    try {
                        writeAccess = await lockService.WriteLockAsync();
                        timer.Stop();
                    } catch (InvalidOperationException) {
                        if (timer.ElapsedMilliseconds >= 5000)
                            throw;
                        using (var readAccess = await lockService.ReadLockAsync())
                            await readAccess.ReleaseAsync();
                    }
                }

                using (writeAccess) {
                    var msBuildProject = await writeAccess.GetProjectAsync(ConfiguredProject);

                    var configProps = new Dictionary<string, string>(
                        ConfiguredProject.ProjectConfiguration.Dimensions.ToImmutableDictionary());

                    foreach (var property in Properties)
                        configProps[property.Key] = property.Value;

                    var projectInstance = new ProjectInstance(msBuildProject.Xml,
                        configProps, null, new ProjectCollection());

                    var loggerVerbosity = LoggerVerbosity;
                    if (QtVsToolsPackage.Instance.Options.BuildDebugInformation)
                        loggerVerbosity = QtVsToolsPackage.Instance.Options.BuildLoggerVerbosity;
                    var buildParams = new BuildParameters()
                    {
                        Loggers = (loggerVerbosity != LoggerVerbosity.Quiet)
                                ? new[] { new QtProjectLogger() { Verbosity = loggerVerbosity } }
                                : null
                    };

                    var buildRequest = new BuildRequestData(projectInstance,
                        Targets.ToArray(),
                        hostServices: null,
                        flags: BuildRequestDataFlags.ProvideProjectStateAfterBuild);

                    if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                        Messages.Print(string.Format(
                            "{0:HH:mm:ss.FFF} QtProjectBuild({1}): Build [{2}] {3}",
                            DateTime.Now, Thread.CurrentThread.ManagedThreadId,
                            ConfiguredProject.ProjectConfiguration.Name,
                            UnconfiguredProject.FullPath));
                        Messages.Print("=== Targets");
                        foreach (var target in buildRequest.TargetNames)
                            Messages.Print(string.Format("    {0}", target));
                        Messages.Print("=== Properties");
                        foreach (var property in Properties) {
                            Messages.Print(string.Format("    {0}={1}",
                                property.Key, property.Value));
                        }
                    }

                    BuildResult result = null;
                    while (result == null) {
                        try {
                            result = BuildManager.DefaultBuildManager.Build(
                                buildParams, buildRequest);
                        } catch (InvalidOperationException) {
                            if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                                Messages.Print(string.Format(
                                    "{0:HH:mm:ss.FFF} QtProjectBuild({1}): [{2}] "
                                    + "Warning: Another build is in progress; waiting...",
                                    DateTime.Now,
                                    Thread.CurrentThread.ManagedThreadId,
                                    ConfiguredProject.ProjectConfiguration.Name));
                            }
                            await Task.Delay(3000);
                        }
                    }

                    if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                        string resMsg;
                        StringBuilder resInfo = new StringBuilder();
                        if (result?.OverallResult == BuildResultCode.Success) {
                            resMsg = "Build ok";
                        } else {
                            resMsg = "Build FAIL";
                            if (result == null) {
                                resInfo.AppendLine("####### Build returned 'null'");
                            } else {
                                resInfo.AppendLine("####### Build returned 'Failure' code");
                                if (result.ResultsByTarget != null) {
                                    foreach (var tr in result.ResultsByTarget) {
                                        var res = tr.Value;
                                        if (res.ResultCode != TargetResultCode.Failure)
                                            continue;
                                        resInfo.AppendFormat("### Target '{0}' FAIL\r\n", tr.Key);
                                        if (res.Items != null && res.Items.Length > 0) {
                                            resInfo.AppendFormat(
                                                "Items: {0}\r\n", string.Join(", ", res.Items
                                                    .Select(it => it.ItemSpec)));
                                        }
                                        var e = tr.Value?.Exception;
                                        if (e != null) {
                                            resInfo.AppendFormat(
                                                "Exception: {0}\r\nStacktrace:\r\n{1}\r\n",
                                                e.Message, e.StackTrace);
                                        }
                                    }
                                }
                            }
                        }
                        Messages.Print(string.Format(
                            "{0:HH:mm:ss.FFF} QtProjectBuild({1}): [{2}] {3}\r\n{4}",
                            DateTime.Now, Thread.CurrentThread.ManagedThreadId,
                            ConfiguredProject.ProjectConfiguration.Name,
                            resMsg, resInfo.ToString()));
                    }

                    if (result == null
                        || result.ResultsByTarget == null
                        || result.OverallResult != BuildResultCode.Success) {
                        Messages.Print(string.Format("{0}: background build FAILED!",
                                Path.GetFileName(UnconfiguredProject.FullPath)));
                    } else {
                        var checkResults = result.ResultsByTarget
                            .Where(x => Targets.Contains(x.Key))
                            .Select(x => x.Value);
                        ok = checkResults.Any()
                            && checkResults.All(x => x.ResultCode == TargetResultCode.Success);
                        if (ok)
                            msBuildProject.MarkDirty();
                    }
                    await writeAccess.ReleaseAsync();
                }

                if (ok) {
                    var vcProject = Project.Object as VCProject;
                    var vcConfigs = vcProject.Configurations as IVCCollection;
                    var vcConfig = vcConfigs.Item(ConfiguredProject.ProjectConfiguration.Name) as VCConfiguration;
                    var props = vcConfig.Rules.Item("QtRule10_Settings") as IVCRulePropertyStorage;
                    props.SetPropertyValue("QtLastBackgroundBuild", DateTime.UtcNow.ToString("o"));
                }
            } catch (Exception e) {
                Messages.Print(string.Format("{0}: background build ERROR: {1}",
                        Path.GetFileName(UnconfiguredProject.FullPath), e.Message));
            }

            if (LoggerVerbosity != LoggerVerbosity.Quiet) {
                Messages.Print(string.Format(
@"
== {0}: build {1}",
                    Project.Name, ok ? "successful" : "ERROR"));
            }
        }
    }
}
