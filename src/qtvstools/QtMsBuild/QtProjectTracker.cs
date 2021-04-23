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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell.Interop;
#if VS2015
using Microsoft.VisualStudio.ProjectSystem.Designers;
#endif
using EnvDTE;
using QtVsTools.Core;
using QtVsTools.VisualStudio;
using Microsoft.Build.Framework;

namespace QtVsTools.QtMsBuild
{
    class QtProjectTracker
    {
        static readonly object criticalSection = new object();
        static ConcurrentDictionary<string, QtProjectTracker> _Instances;
        static ConcurrentDictionary<string, QtProjectTracker> Instances {
            get
            {
                lock (criticalSection) {
                    if (_Instances == null)
                        _Instances = new ConcurrentDictionary<string, QtProjectTracker>();
                    return _Instances;
                }
            }
        }

        EnvDTE.Project Project { get; set; }

        UnconfiguredProject UnconfiguredProject { get; set; }
        IEnumerable<ConfiguredProject> ConfiguredProjects { get; set; }
        IProjectLockService LockService { get; set; }

        IVsStatusbar StatusBar { get; set; }
        IVsThreadedWaitDialogFactory WaitDialogFactory { get; set; }

        public static void AddProject(EnvDTE.Project project, bool updateVars, bool runQtTools)
        {
            QtProjectTracker instance = null;
            if (!Instances.TryGetValue(project.FullName, out instance) || instance == null) {
                Instances[project.FullName] = new QtProjectTracker(project, updateVars, runQtTools);
            }
        }

        private QtProjectTracker(EnvDTE.Project project, bool updateVars, bool runQtTools)
        {
            Project = project;
            Task.Run(async () => await InitializeAsync(updateVars, runQtTools));
        }

        bool initialized = false;

        private async Task InitializeAsync(bool updateVars, bool runQtTools)
        {
            var context = Project.Object as IVsBrowseObjectContext;
            if (context == null)
                return;

            UnconfiguredProject = context.UnconfiguredProject;
            if (UnconfiguredProject == null
                || UnconfiguredProject.ProjectService == null
                || UnconfiguredProject.ProjectService.Services == null)
                return;

            LockService = UnconfiguredProject.ProjectService.Services.ProjectLockService;
            if (LockService == null)
                return;

            var configs = await UnconfiguredProject.Services
                .ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();

            StatusBar = await VsServiceProvider
                .GetServiceAsync<SVsStatusbar, IVsStatusbar>();
            WaitDialogFactory = await VsServiceProvider
                .GetServiceAsync<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();

            initialized = true;

            foreach (var config in configs) {
                var configProject = await UnconfiguredProject.LoadConfiguredProjectAsync(config);
                configProject.ProjectChanged += OnProjectChanged;
                configProject.ProjectUnloading += OnProjectUnloading;
                if (Vsix.Instance.Options.BuildDebugInformation) {
                    Messages.Print(string.Format(
                        "{0:HH:mm:ss.FFF} QtProjectTracker: Started tracking [{1}] {2}",
                        DateTime.Now,
                        config.Name,
                        UnconfiguredProject.FullPath));
                }
                if (updateVars)
                    await DesignTimeUpdateQtVarsAsync(configProject, runQtTools, null);
            }
        }

        public static void RefreshIntelliSense(
            EnvDTE.Project project, string configId = null,
            bool runQtTools = false, IEnumerable<string> selectedFiles = null)
        {
            if (project == null)
                return;

            QtProjectTracker instance = null;
            if (!Instances.TryGetValue(project.FullName, out instance) || instance == null)
                return;
            if (!instance.initialized)
                return;
            if (Vsix.Instance.Options.BuildDebugInformation) {
                Messages.Print(string.Format(
                    "{0:HH:mm:ss.FFF} QtProjectTracker: Refreshing: [{1}] {2}",
                    DateTime.Now,
                    (configId != null) ? configId : "(all configs)",
                    project.FullName));
            }

            Task.Run(async () => await instance
                .RefreshIntelliSenseAsync(configId, runQtTools, selectedFiles));
        }

        private async Task RefreshIntelliSenseAsync(string configId,
            bool runQtTools, IEnumerable<string> selectedFiles)
        {
            WaitDialog waitDialog = null;

            if (runQtTools) {
                waitDialog = WaitDialog.Start(
                    "Qt Visual Studio Tools", "Updating IntelliSense...",
                    delay: 1, dialogFactory: WaitDialogFactory);
                StatusBar?.SetText("Qt Visual Studio Tools: Updating IntelliSense...");
            }

            var configs = await UnconfiguredProject.Services
                .ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
            foreach (var config in configs) {
                if (!string.IsNullOrEmpty(configId) && config.Name != configId)
                    continue;
                var configProject = await UnconfiguredProject.LoadConfiguredProjectAsync(config);
                await DesignTimeUpdateQtVarsAsync(configProject, runQtTools, selectedFiles);
            }

            if (runQtTools) {
                try {
                    Vsix.Instance.Dte.ExecuteCommand("Project.RescanSolution");
                } catch (Exception e) {
                    Messages.Print(
                        e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
                }
                waitDialog?.Stop();
                StatusBar?.Clear();
            }
        }

        private void OnProjectChanged(object sender, EventArgs e)
        {
            if (!initialized)
                return;

            var project = sender as ConfiguredProject;
            if (project == null || project.Services == null)
                return;

            if (Vsix.Instance.Options.BuildDebugInformation) {
                Messages.Print(string.Format(
                    "{0:HH:mm:ss.FFF} QtProjectTracker: Changed [{1}] {2}",
                    DateTime.Now,
                    project.ProjectConfiguration.Name,
                    project.UnconfiguredProject.FullPath));
            }
            Task.Run(async () => await DesignTimeUpdateQtVarsAsync(project, false, null));
        }

        public static KeyValuePair<TKey, TValue> PROPERTY<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        private async Task DesignTimeUpdateQtVarsAsync(
            ConfiguredProject project,
            bool runQtTools,
            IEnumerable<string> selectedFiles)
        {
            await BuildAsync(
                project,
                new[] { PROPERTY("QtVSToolsBuild", "true") },
                new[] { "QtVars" });
            if (runQtTools) {
                await BuildAsync(
                    project,
                    (selectedFiles == null) ? new KeyValuePair<string, string>[0]
                        : new[] { PROPERTY("SelectedFiles", string.Join(";", selectedFiles)) },
                    new[] { "Qt" });
            }
        }

        public static void Build(
            EnvDTE.Project project, string configId,
            KeyValuePair<string, string>[] properties,
            params string[] targets)
        {
            if (project == null)
                return;

            QtProjectTracker instance = null;
            if (!Instances.TryGetValue(project.FullName, out instance) || instance == null)
                return;
            if (!instance.initialized)
                return;

            Task.Run(async () => await instance
                .BuildAsync(configId, properties, targets));
        }

        private async Task BuildAsync(
            string configId,
            KeyValuePair<string, string>[] properties,
            string[] targets)
        {
            var configs = await UnconfiguredProject.Services
                .ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
            foreach (var config in configs) {
                if (!string.IsNullOrEmpty(configId) && config.Name != configId)
                    continue;
                var configProject = await UnconfiguredProject.LoadConfiguredProjectAsync(config);
                await BuildAsync(configProject, properties, targets, true, LoggerVerbosity.Minimal);
            }
        }

        private async Task BuildAsync(
            ConfiguredProject project,
            KeyValuePair<string, string>[] properties,
            string[] targets,
            bool checkTargets,
            LoggerVerbosity verbosity = LoggerVerbosity.Quiet)
        {
            var targetsToCheck = checkTargets ? targets : null;
            await BuildAsync(project, properties, targets, targetsToCheck, verbosity);
        }

        private async Task BuildAsync(
            ConfiguredProject project,
            KeyValuePair<string, string>[] properties,
            string[] targets,
            string[] targetsToCheck = null,
            LoggerVerbosity verbosity = LoggerVerbosity.Quiet)
        {
            if (project == null)
                return;

            if (verbosity != LoggerVerbosity.Quiet) {
                Messages.Print(clear: !Vsix.Instance.Options.BuildDebugInformation, activate: true,
                    text: string.Format(
@"== {0}: starting build...
  * Properties: {1}
  * Targets: {2}
",
                    /*{0}*/ Project.Name,
                    /*{1}*/ string.Join("", properties
                        .Select(property => string.Format(@"
        {0} = {1}",     /*{0}*/ property.Key, /*{1}*/ property.Value))),
                    /*{2}*/ string.Join(";", targets)));
            }

            bool ok = false;
            try {
                ProjectWriteLockReleaser writeAccess;
                var timer = Stopwatch.StartNew();
                while (timer.IsRunning) {
                    try {
                        writeAccess = await LockService.WriteLockAsync();
                        timer.Stop();
                    } catch (InvalidOperationException) {
                        if (timer.ElapsedMilliseconds >= 5000)
                            throw;
                        using (var readAccess = await LockService.ReadLockAsync())
                            await readAccess.ReleaseAsync();
                    }
                }

                using (writeAccess)
                using (var buildManager = new BuildManager()) {
                    var msBuildProject = await writeAccess.GetProjectAsync(project);

                    var configProps = new Dictionary<string, string>(
                        project.ProjectConfiguration.Dimensions.ToImmutableDictionary());

                    foreach (var property in properties)
                        configProps[property.Key] = property.Value;

                    var projectInstance = new ProjectInstance(msBuildProject.Xml,
                        configProps, null, new ProjectCollection());

                    var buildParams = new BuildParameters()
                    {
                        Loggers = (verbosity != LoggerVerbosity.Quiet)
                                ? new[] { new Logger() { Verbosity = verbosity } }
                                : null
                    };

                    var buildRequest = new BuildRequestData(projectInstance,
                        targets,
                        hostServices: null,
                        flags: BuildRequestDataFlags.ProvideProjectStateAfterBuild);

                    if (Vsix.Instance.Options.BuildDebugInformation) {
                        Messages.Print(string.Format(
                            "{0:HH:mm:ss.FFF} QtProjectTracker: Build [{1}] {2}",
                            DateTime.Now,
                            project.ProjectConfiguration.Name,
                            project.UnconfiguredProject.FullPath));
                        Messages.Print("=== Targets");
                        foreach (var target in buildRequest.TargetNames)
                            Messages.Print(string.Format("    {0}", target));
                        Messages.Print("=== Properties");
                        foreach (var property in properties) {
                            Messages.Print(string.Format("    {0}={1}",
                                property.Key, property.Value));
                        }
                    }

                    var result = buildManager.Build(buildParams, buildRequest);

                    if (result == null
                        || result.ResultsByTarget == null
                        || result.OverallResult != BuildResultCode.Success) {
                        Messages.Print(string.Format("{0}: background build FAILED!",
                                Path.GetFileName(UnconfiguredProject.FullPath)));
                    } else {
                        bool buildSuccess = true;
                        if (targetsToCheck != null) {
                            var checkResults = result.ResultsByTarget
                                .Where(x => targetsToCheck.Contains(x.Key))
                                .Select(x => x.Value);
                            buildSuccess = checkResults.Any()
                                && checkResults.All(x => x.ResultCode == TargetResultCode.Success);
                        }
                        if (buildSuccess)
                            msBuildProject.MarkDirty();
                        ok = buildSuccess;

                        if (Vsix.Instance.Options.BuildDebugInformation) {
                            Messages.Print(string.Format(
                                "{0:HH:mm:ss.FFF} QtProjectTracker: Build {1}",
                                DateTime.Now, ok ? "successful" : "ERROR"));
                        }
                    }
                    await writeAccess.ReleaseAsync();
                }
            } catch (Exception e) {
                Messages.Print(string.Format("{0}: background build ERROR: {1}",
                        Path.GetFileName(UnconfiguredProject.FullPath), e.Message));
            }

            if (verbosity != LoggerVerbosity.Quiet) {
                Messages.Print(string.Format(
@"
== {0}: build {1}",
                    Project.Name, ok ? "successful" : "ERROR"));
            }
        }

        private async Task OnProjectUnloading(object sender, EventArgs args)
        {
            var project = sender as ConfiguredProject;
            if (project == null || project.Services == null)
                return;
            if (Vsix.Instance.Options.BuildDebugInformation) {
                Messages.Print(string.Format(
                    "{0:HH:mm:ss.FFF} QtProjectTracker: Stopped tracking [{1}] {2}",
                    DateTime.Now,
                    project.ProjectConfiguration.Name,
                    project.UnconfiguredProject.FullPath));
            }
            project.ProjectChanged -= OnProjectChanged;
            project.ProjectUnloading -= OnProjectUnloading;
            Instances[Project.FullName] = null;
            await Task.Yield();
        }

        class Logger : ILogger
        {
            public LoggerVerbosity Verbosity { get; set; }
            public string Parameters { get; set; }

            public void Initialize(IEventSource eventSource)
            {
                eventSource.MessageRaised += new BuildMessageEventHandler(MessageRaised);
                eventSource.WarningRaised += new BuildWarningEventHandler(WarningRaised);
                eventSource.ErrorRaised += new BuildErrorEventHandler(ErrorRaised);
            }

            private void ErrorRaised(object sender, BuildErrorEventArgs e)
            {
                Messages.Print(e.Message);
            }

            private void WarningRaised(object sender, BuildWarningEventArgs e)
            {
                Messages.Print(e.Message);
            }

            private void MessageRaised(object sender, BuildMessageEventArgs e)
            {
                if (e is TaskCommandLineEventArgs)
                    return;
                if (e.Importance == MessageImportance.High)
                    Messages.Print(e.Message);
            }

            public void Shutdown()
            {
            }
        }
    }
}
