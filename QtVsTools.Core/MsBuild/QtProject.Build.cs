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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;

using Task = System.Threading.Tasks.Task;
using Thread = System.Threading.Thread;

namespace QtVsTools.Core.MsBuild
{
    using Common;
    using Options;
    using VisualStudio;
    using static Common.EnumExt;

    public partial class QtProject
    {
        private enum Target
        {
            // Mark project as dirty, but do not request a build
            [String("QtVsTools.QtMsBuild.QtProjectBuild.Target.SetOutdated")] SetOutdated
        }

        private static PunisherQueue<QtProject> BuildQueue => StaticLazy.Get(() =>
            BuildQueue, () => new PunisherQueue<QtProject>(
                getItemKey: build => build.ConfiguredProject));

        private static ConcurrentStopwatch RequestTimer => StaticLazy.Get(() =>
            RequestTimer, () => new ConcurrentStopwatch());

        private ConfiguredProject ConfiguredProject { get; set; }
        private Dictionary<string, string> Properties { get; set; }
        private List<string> Targets { get; set; }
        private LoggerVerbosity LoggerVerbosity { get; set; }

        private static Task BuildDispatcher { get; set; }

        private QtProject()
        {}

        public void StartBuild(
            string configName,
            Dictionary<string, string> properties,
            IEnumerable<string> targets,
            LoggerVerbosity verbosity = LoggerVerbosity.Quiet)
        {
            _ = Task.Run(() => StartBuildAsync(configName, properties, targets, verbosity));
        }

        public async Task StartBuildAsync(
            string configName,
            Dictionary<string, string> properties,
            IEnumerable<string> targets,
            LoggerVerbosity verbosity)
        {
            if (configName == null)
                throw new ArgumentException("Configuration name cannot be null.");

            RequestTimer.Restart();
            await Initialized;

            if (Options.Get() is { BuildDebugInformation: true }) {
                Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                    + $"QtProjectBuild({Thread.CurrentThread.ManagedThreadId}): "
                    + $"Request [{configName}] {UnconfiguredProject.FullPath}");
            }

            var knownConfigs = await UnconfiguredProject.Services
                .ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();

            ConfiguredProject configuredProject = null;
            foreach (var config in knownConfigs) {
                var configProject = await UnconfiguredProject
                    .LoadConfiguredProjectAsync(config);
                if (configProject.ProjectConfiguration.Name != configName)
                    continue;
                configuredProject = configProject;
                break;
            }
            if (configuredProject == null)
                throw new ArgumentException($"Unknown configuration '{configName}'.");

            BuildQueue.Enqueue(new QtProject
            {
                ConfiguredProject = configuredProject,
                Properties = properties?.ToDictionary(x => x.Key, x => x.Value),
                Targets = targets?.ToList(),
                LoggerVerbosity = verbosity
            });
            StaticThreadSafeInit(() => BuildDispatcher,
                () => BuildDispatcher = Task.Run(BuildDispatcherLoopAsync))
                .Forget();
        }

        public async Task SetOutdatedAsync(
            string configName,
            LoggerVerbosity verbosity = LoggerVerbosity.Quiet)
        {
            await StartBuildAsync(
                configName,
                null,
                new[] { Target.SetOutdated.Cast<string>() },
                verbosity);
        }

        private static async Task BuildDispatcherLoopAsync()
        {
            ITaskHandler2 dispatchStatus = null;
            if (VsServiceProvider.Instance is not AsyncPackage package)
                return;
            while (!package.Zombied) {
                while (BuildQueue.IsEmpty || RequestTimer.ElapsedMilliseconds < 1000) {
                    if (BuildQueue.IsEmpty && dispatchStatus != null) {
                        dispatchStatus.Dismiss();
                        dispatchStatus = null;
                    }
                    await Task.Delay(100);
                }
                if (BuildQueue.TryDequeue(out var buildRequest)) {
                    var progressData = new TaskProgressData
                    {
                        ProgressText = "Refreshing IntelliSense data, "
                            + $"{BuildQueue.Count} project(s) remaining...",
                        CanBeCanceled = true
                    };
                    if (dispatchStatus == null) {
                        dispatchStatus = StatusCenter.PreRegister(
                            new TaskHandlerOptions
                            {
                                Title = "Qt VS Tools"
                            },
                            progressData
                        ) as ITaskHandler2;
                        dispatchStatus.RegisterTask(new Task(() =>
                            throw new InvalidOperationException()));
                    } else {
                        dispatchStatus.Progress.Report(progressData);
                    }
                    await buildRequest.BuildAsync();
                }
                if (BuildQueue.IsEmpty
                    || dispatchStatus?.UserCancellation.IsCancellationRequested == true) {
                    if (dispatchStatus != null) {
                        dispatchStatus.Dismiss();
                        dispatchStatus = null;
                    }
                    BuildQueue.Clear();
                }
            }
        }

        private async Task<bool> BuildProjectAsync(ProjectWriteLockReleaser writeAccess)
        {
            var msBuildProject = await writeAccess.GetProjectAsync(ConfiguredProject);

            if (Targets.Any(t => t == Target.SetOutdated.Cast<string>())) {
                msBuildProject.MarkDirty();
                await writeAccess.ReleaseAsync();
                return true;
            }

            var configProps = new Dictionary<string, string>(
                ConfiguredProject.ProjectConfiguration.Dimensions.ToImmutableDictionary())
                {
                    { "SolutionPath", SolutionPath },
                    { "SolutionFileName", Path.GetFileName(SolutionPath) },
                    { "SolutionName", Path.GetFileNameWithoutExtension(SolutionPath) },
                    { "SolutionExt", Path.GetExtension(SolutionPath) },
                    { "SolutionDir", Path.GetDirectoryName(SolutionPath).TrimEnd(Path.
                                        DirectorySeparatorChar) + Path.DirectorySeparatorChar }
                };

            foreach (var property in Properties)
                configProps[property.Key] = property.Value;

            var projectInstance = new ProjectInstance(msBuildProject.Xml,
                configProps, null, new ProjectCollection());

            var loggerVerbosity = LoggerVerbosity;
            if (Options.Get() is { BuildDebugInformation: true }) {
                if (Options.Get() is {} options)
                    loggerVerbosity = options.BuildLoggerVerbosity;
            }

            var buildParams = new BuildParameters
            {
                Loggers = loggerVerbosity != LoggerVerbosity.Quiet
                        ? new[] { new QtProjectLogger { Verbosity = loggerVerbosity } }
                        : null
            };

            var buildRequest = new BuildRequestData(projectInstance,
                Targets.ToArray(),
                hostServices: null,
                flags: BuildRequestDataFlags.ProvideProjectStateAfterBuild);

            if (Options.Get() is { BuildDebugInformation: true }) {
                Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                    + $"QtProjectBuild({Thread.CurrentThread.ManagedThreadId}): "
                    + $"Build [{ConfiguredProject.ProjectConfiguration.Name}] "
                    + $"{UnconfiguredProject.FullPath}");
                Messages.Print("=== Targets");
                foreach (var target in buildRequest.TargetNames)
                    Messages.Print($"    {target}");
                Messages.Print("=== Properties");
                foreach (var property in Properties)
                    Messages.Print($"    {property.Key}={property.Value}");
            }

            BuildResult result = null;
            while (result == null) {
                try {
                    result = BuildManager.DefaultBuildManager.Build(buildParams, buildRequest);
                } catch (InvalidOperationException) {
                    if (Options.Get() is { BuildDebugInformation: true }) {
                        Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                        + $"QtProjectBuild({Thread.CurrentThread.ManagedThreadId}): "
                        + $"[{ConfiguredProject.ProjectConfiguration.Name}] "
                        + "Warning: Another build is in progress; waiting...");
                    }
                    await Task.Delay(3000);
                }
            }

            if (Options.Get() is { BuildDebugInformation: true }) {
                string resMsg;
                var resInfo = new StringBuilder();
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
                Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                    + $"QtProjectBuild({Thread.CurrentThread.ManagedThreadId}): "
                    + $"[{ConfiguredProject.ProjectConfiguration.Name}] {resMsg}\r\n{resInfo}");
            }

            var ok = false;
            if (result is { ResultsByTarget: null, OverallResult: BuildResultCode.Success }) {
                Messages.Print($"== {Path.GetFileName(UnconfiguredProject.FullPath)}: "
                    + "background build FAILED!");
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

        private async Task BuildAsync()
        {
            var path = Path.GetFileNameWithoutExtension(UnconfiguredProject.FullPath);

            if (LoggerVerbosity != LoggerVerbosity.Quiet) {
                var properties = string.Join("", Properties.Select(property =>
                    $"{Environment.NewLine}        {property.Key} = {property.Value}"));
                Messages.Print(
                      $"== {path}: starting build...{Environment.NewLine}"
                    + $"  * Properties: {properties}{Environment.NewLine}"
                    + $"  * Targets: {string.Join(";", Targets)}{Environment.NewLine}",
                    clear: Options.Get() is not { BuildDebugInformation: true },
                    activate: true);
            }

            var lockService = UnconfiguredProject.ProjectService.Services.ProjectLockService;

            var ok = false;
            try {
                var timer = ConcurrentStopwatch.StartNew();
                while (timer.IsRunning) {
                    try {
                        await lockService.WriteLockAsync(
                            async writeAccess =>
                            {
                                ok = await BuildProjectAsync(writeAccess);
                            });
                        timer.Stop();
                    } catch (InvalidOperationException) {
                        if (timer.ElapsedMilliseconds >= 5000)
                            throw;
                        await lockService.ReadLockAsync(
                            async readAccess =>
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
                Messages.Print($"{Path.GetFileName(UnconfiguredProject.FullPath)}: "
                    + $"background build ERROR: {e.Message}");
            }

            if (LoggerVerbosity != LoggerVerbosity.Quiet) {
                Messages.Print(
                    $"{Environment.NewLine}== {path}: build {(ok ? "successful" : "ERROR")}"
                );
            }
        }
    }
}
