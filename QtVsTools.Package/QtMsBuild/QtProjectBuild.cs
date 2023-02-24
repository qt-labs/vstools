/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;

using Thread = System.Threading.Thread;

namespace QtVsTools.QtMsBuild
{
    using Common;
    using Core;
    using VisualStudio;
    using static Common.EnumExt;

    class QtProjectBuild : Concurrent<QtProjectBuild>
    {
        static LazyFactory StaticLazy { get; } = new LazyFactory();

        public enum Target
        {
            // Mark project as dirty, but do not request a build
            [String("QtVsTools.QtMsBuild.QtProjectBuild.Target.SetOutdated")] SetOutdated
        }

        static PunisherQueue<QtProjectBuild> BuildQueue => StaticLazy.Get(() =>
            BuildQueue, () => new PunisherQueue<QtProjectBuild>(
                getItemKey: (QtProjectBuild build) =>
                {
                    return build.ConfiguredProject;
                }));

        static ConcurrentStopwatch RequestTimer => StaticLazy.Get(() =>
            RequestTimer, () => new ConcurrentStopwatch());

        static IVsTaskStatusCenterService StatusCenter => StaticLazy.Get(() =>
            StatusCenter, () => VsServiceProvider
                .GetService<SVsTaskStatusCenterService, IVsTaskStatusCenterService>());

        EnvDTE.Project Project { get; set; }
        VCProject VcProject { get; set; }
        UnconfiguredProject UnconfiguredProject { get; set; }
        ConfiguredProject ConfiguredProject { get; set; }
        Dictionary<string, string> Properties { get; set; }
        List<string> Targets { get; set; }
        LoggerVerbosity LoggerVerbosity { get; set; }

        static Task BuildDispatcher { get; set; }

        public static void StartBuild(
            EnvDTE.Project project,
            string projectPath,
            string configName,
            Dictionary<string, string> properties,
            IEnumerable<string> targets,
            LoggerVerbosity verbosity = LoggerVerbosity.Quiet)
        {
            if (project == null)
                throw new ArgumentException("Project cannot be null.");
            if (configName == null)
                throw new ArgumentException("Configuration name cannot be null.");

            _ = Task.Run(() => StartBuildAsync(
                project, projectPath, configName, properties, targets, verbosity));
        }

        public static async Task StartBuildAsync(
            EnvDTE.Project project,
            string projectPath,
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
            var tracker = QtProjectTracker.Get(project, projectPath);
            await tracker.Initialized;

            if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                Messages.Print(string.Format(
                "{0:HH:mm:ss.FFF} QtProjectBuild({1}): Request [{2}] {3}",
                DateTime.Now, Thread.CurrentThread.ManagedThreadId,
                configName, tracker.UnconfiguredProject.FullPath));
            }

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
                VcProject = tracker.VcProject,
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

        public static async Task SetOutdatedAsync(
            EnvDTE.Project project,
            string projectPath,
            string configName,
            LoggerVerbosity verbosity = LoggerVerbosity.Quiet)
        {
            await StartBuildAsync(
                project,
                projectPath,
                configName,
                null,
                new[] { Target.SetOutdated.Cast<string>() },
                verbosity);
        }

        public static void Reset()
        {
            BuildQueue.Clear();
        }

        static async Task BuildDispatcherLoopAsync()
        {
            ITaskHandler2 dispatchStatus = null;
            while (!QtVsToolsPackage.Instance.Zombied) {
                while (BuildQueue.IsEmpty || RequestTimer.ElapsedMilliseconds < 1000) {
                    if (BuildQueue.IsEmpty && dispatchStatus != null) {
                        dispatchStatus.Dismiss();
                        dispatchStatus = null;
                    }
                    await Task.Delay(100);
                }
                if (BuildQueue.TryDequeue(out QtProjectBuild buildRequest)) {
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
                    await buildRequest.BuildAsync();
                }
                if (BuildQueue.IsEmpty
                    || dispatchStatus?.UserCancellation.IsCancellationRequested == true) {
                    if (dispatchStatus != null) {
                        dispatchStatus.Dismiss();
                        dispatchStatus = null;
                    }
                    Reset();
                }
            }
        }

        async Task<bool> BuildProjectAsync(ProjectWriteLockReleaser writeAccess)
        {
            var msBuildProject = await writeAccess.GetProjectAsync(ConfiguredProject);

            if (Targets.Any(t => t == Target.SetOutdated.Cast<string>())) {
                msBuildProject.MarkDirty();
                await writeAccess.ReleaseAsync();
                return true;
            }

            var solutionPath = QtProjectTracker.SolutionPath;
            var configProps = new Dictionary<string, string>(
                ConfiguredProject.ProjectConfiguration.Dimensions.ToImmutableDictionary())
                {
                    { "SolutionPath", solutionPath },
                    { "SolutionFileName", Path.GetFileName(solutionPath) },
                    { "SolutionName", Path.GetFileNameWithoutExtension(solutionPath) },
                    { "SolutionExt", Path.GetExtension(solutionPath) },
                    { "SolutionDir", Path.GetDirectoryName(solutionPath).TrimEnd(Path.
                                        DirectorySeparatorChar) + Path.DirectorySeparatorChar }
                };

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
                if (result.OverallResult == BuildResultCode.Success) {
                    resMsg = "Build ok";
                } else {
                    resMsg = "Build FAIL";
                    resInfo.AppendLine("####### Build returned 'Failure' code");
                    if (result.ResultsByTarget != null) {
                        foreach (var tr in result.ResultsByTarget) {
                            var res = tr.Value;
                            if (res.ResultCode != TargetResultCode.Failure)
                                continue;
                            resInfo.AppendFormat("### Target '{0}' FAIL\r\n", tr.Key);
                            if (res.Items is {Length: > 0}) {
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
                Messages.Print(string.Format(
                        "{0:HH:mm:ss.FFF} QtProjectBuild({1}): [{2}] {3}\r\n{4}",
                        DateTime.Now, Thread.CurrentThread.ManagedThreadId,
                        ConfiguredProject.ProjectConfiguration.Name,
                        resMsg, resInfo.ToString()));
            }

            bool ok = false;
            if (result is { ResultsByTarget: null, OverallResult: BuildResultCode.Success }) {
                Messages.Print(string.Format("{0}: background build FAILED!",
                        Path.GetFileName(UnconfiguredProject.FullPath)));
            } else {
                var checkResults = result.ResultsByTarget
                    .Where(x => Targets.Contains(x.Key))
                    .Select(x => x.Value).ToList();
                ok = checkResults.Any()
                    && checkResults.All(x => x.ResultCode == TargetResultCode.Success);
                if (ok)
                    msBuildProject.MarkDirty();
            }
            await writeAccess.ReleaseAsync();
            return ok;
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
                    /*{0}*/ Path.GetFileNameWithoutExtension(UnconfiguredProject.FullPath),
                    /*{1}*/ string.Join("", Properties
                        .Select(property => string.Format(@"
        {0} = {1}",     /*{0}*/ property.Key, /*{1}*/ property.Value))),
                    /*{2}*/ string.Join(";", Targets)));
            }

            var lockService = UnconfiguredProject.ProjectService.Services.ProjectLockService;

            bool ok = false;
            try {
                var timer = ConcurrentStopwatch.StartNew();
                while (timer.IsRunning) {
                    try {
                        await lockService.WriteLockAsync(
                            async (ProjectWriteLockReleaser writeAccess) =>
                            {
                                ok = await BuildProjectAsync(writeAccess);
                            });
                        timer.Stop();
                    } catch (InvalidOperationException) {
                        if (timer.ElapsedMilliseconds >= 5000)
                            throw;
                        await lockService.ReadLockAsync(
                            async (ProjectLockReleaser readAccess) =>
                            {
                                await readAccess.ReleaseAsync();
                            });
                    }
                }

                if (ok) {
                    var vcConfigs = VcProject.Configurations as IVCCollection;
                    var vcConfig = vcConfigs.Item(ConfiguredProject.ProjectConfiguration.Name) as VCConfiguration;
                    var props = vcConfig.Rules.Item("QtRule10_Settings") as IVCRulePropertyStorage;
                    props?.SetPropertyValue("QtLastBackgroundBuild", DateTime.UtcNow.ToString("o"));
                }
            } catch (Exception e) {
                Messages.Print(string.Format("{0}: background build ERROR: {1}",
                        Path.GetFileName(UnconfiguredProject.FullPath), e.Message));
            }

            if (LoggerVerbosity != LoggerVerbosity.Quiet) {
                Messages.Print(string.Format(
@"
== {0}: build {1}",
                    Path.GetFileNameWithoutExtension(UnconfiguredProject.FullPath),
                    ok ? "successful" : "ERROR"));
            }
        }
    }
}
