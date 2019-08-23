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

#if VS2017 || VS2019
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using EnvDTE;
using QtProjectLib;
using System.Diagnostics;

namespace QtVsTools.QtMsBuild
{
    class QtVarsDesignTime
    {
        static ConcurrentDictionary<EnvDTE.Project, QtVarsDesignTime> Instances { get; set; }

        EnvDTE.Project Project { get; set; }

        UnconfiguredProject UnconfiguredProject { get; set; }
        IEnumerable<ConfiguredProject> ConfiguredProjects { get; set; }
        IProjectLockService LockService { get; set; }

        static readonly object criticalSection = new object();
        public static void AddProject(EnvDTE.Project project)
        {
            lock (criticalSection) {
                if (Instances == null)
                    Instances = new ConcurrentDictionary<EnvDTE.Project, QtVarsDesignTime>();
            }
            Instances[project] = new QtVarsDesignTime(project);
        }

        private QtVarsDesignTime(EnvDTE.Project project)
        {
            Project = project;
            Task.Run(Initialize).Wait(5000);
        }

        bool initialized = false;

        private async Task Initialize()
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

            initialized = true;

            foreach (var config in configs) {
                var configProject = await UnconfiguredProject.LoadConfiguredProjectAsync(config);
                configProject.ProjectChanged += OnProjectChanged;
                configProject.ProjectUnloading += OnProjectUnloading;
                await DesignTimeUpdateQtVarsAsync(configProject);
            }
        }

        private void OnProjectChanged(object sender, EventArgs e)
        {
            if (!initialized)
                return;

            var project = sender as ConfiguredProject;
            if (project == null || project.Services == null)
                return;

            Task.Run(async () => await DesignTimeUpdateQtVarsAsync(project));
        }

        private async Task DesignTimeUpdateQtVarsAsync(ConfiguredProject project)
        {
            if (project == null)
                return;

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

                    var configProps = project.ProjectConfiguration
                        .Dimensions.ToImmutableDictionary();

                    var projectInstance = new ProjectInstance(msBuildProject.Xml,
                        new Dictionary<string, string>(configProps)
                        { { "DesignTimeBuild", "true" } },
                        null, new ProjectCollection());

                    var buildRequest = new BuildRequestData(projectInstance,
                        targetsToBuild: new[] { "QtVars" },
                        hostServices: null,
                        flags: BuildRequestDataFlags.ProvideProjectStateAfterBuild);

                    var result = buildManager.Build(new BuildParameters(), buildRequest);

                    if (result == null
                        || result.ResultsByTarget == null
                        || result.OverallResult != BuildResultCode.Success) {
                        Messages.PaneMessageSafe(Vsix.Instance.Dte, timeout: 5000,
                            str: string.Format("{0}: design-time pre-build FAILED!",
                                Path.GetFileName(UnconfiguredProject.FullPath)));
                    } else {
                        var resultQtVars = result.ResultsByTarget["QtVars"];
                        if (resultQtVars.ResultCode == TargetResultCode.Success) {
                            msBuildProject.MarkDirty();
                        }
                    }
                }
            } catch (Exception e) {
                Messages.PaneMessageSafe(Vsix.Instance.Dte, timeout: 5000,
                    str: string.Format("{0}: design-time pre-build ERROR: {1}",
                        Path.GetFileName(UnconfiguredProject.FullPath), e.Message));
            }
        }

        private async Task OnProjectUnloading(object sender, EventArgs args)
        {
            var project = sender as ConfiguredProject;
            if (project == null || project.Services == null)
                return;
            project.ProjectChanged -= OnProjectChanged;
            project.ProjectUnloading -= OnProjectUnloading;
            Instances[Project] = null;
            await Task.Yield();
        }
    }
}
#endif
