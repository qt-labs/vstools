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

namespace QtVsTools.Core.MsBuild
{
    using Core;
    using Options;

    public partial class MsBuildProject
    {
        public void Refresh(
            string configId = null,
            IEnumerable<string> selectedFiles = null)
        {
            _ = Task.Run(() => RefreshAsync(configId, selectedFiles));
        }

        public async Task RefreshAsync(
            string configId = null,
            IEnumerable<string> selectedFiles = null,
            bool refreshQtVars = false)
        {
            if (Options.Get() is { BuildDebugInformation: true }) {
                Messages.Print($"{DateTime.Now:HH:mm:ss.FFF} "
                    + $"QtProjectIntellisense({Thread.CurrentThread.ManagedThreadId}): "
                    + $"Refreshing: [{configId ?? "(all configs)"}] {VcProjectPath}");
            }

            await Initialized;

            var properties = new Dictionary<string, string>
            {
                ["QtVSToolsBuild"] = "true"
            };
            if (selectedFiles != null)
                properties["SelectedFiles"] = string.Join(";", selectedFiles);
            var targets = new List<string> { "QtVars" };
            if (Options.Get() is { BuildRunQtTools: true })
                targets.Add("Qt");

            var configs = Enumerable.Empty<string>();
            if (configId == null) {
                if (UnconfiguredProject.Services.ProjectConfigurationsService is {} service)
                    configs = (await service.GetKnownProjectConfigurationsAsync()).Select(
                        x => x.Name);
            } else {
                configs = new[] { configId };
            }

            foreach (var config in configs) {
                if (refreshQtVars) {
                    await StartBuildAsync(config, properties, targets,
                        LoggerVerbosity.Quiet);
                } else {
                    await SetOutdatedAsync(config);
                }
            }
        }
    }
}
