/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;
using Thread = System.Threading.Thread;

namespace QtVsTools.QtMsBuild
{
    using Core;
    using Core.MsBuild;

    static class QtProjectIntellisense
    {
        public static void Refresh(
            QtProject qtProject,
            string configId = null,
            IEnumerable<string> selectedFiles = null)
        {
            _ = Task.Run(() => RefreshAsync(qtProject, configId, selectedFiles));
        }

        public static async Task RefreshAsync(
            QtProject qtProject,
            string configId = null,
            IEnumerable<string> selectedFiles = null,
            bool refreshQtVars = false)
        {
            if (QtProjectTracker.GetOrAdd(qtProject) is not {} tracker)
                return;

            if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                    + $"QtProjectIntellisense({Thread.CurrentThread.ManagedThreadId}): "
                    + $"Refreshing: [{configId ?? "(all configs)"}] {qtProject.VcProjectPath}");
            }

            await tracker.Initialized;

            var properties = new Dictionary<string, string>();
            properties["QtVSToolsBuild"] = "true";
            if (selectedFiles != null)
                properties["SelectedFiles"] = string.Join(";", selectedFiles);
            var targets = new List<string> { "QtVars" };
            if (QtVsToolsPackage.Instance.Options.BuildRunQtTools)
                targets.Add("Qt");

            IEnumerable<string> configs;
            if (configId != null) {
                configs = new[] { configId };
            } else {
                var knownConfigs = await tracker.UnconfiguredProject.Services
                    .ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
                configs = knownConfigs.Select(x => x.Name);
            }

            foreach (var config in configs) {
                if (refreshQtVars) {
                    await QtProjectBuild.StartBuildAsync(qtProject, config, properties, targets,
                        LoggerVerbosity.Quiet);
                } else {
                    await QtProjectBuild.SetOutdatedAsync(qtProject, config);
                }
            }
        }
    }
}
