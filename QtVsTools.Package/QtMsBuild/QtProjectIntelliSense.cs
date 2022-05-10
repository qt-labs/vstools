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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;
using Thread = System.Threading.Thread;

namespace QtVsTools.QtMsBuild
{
    using Core;

    static class QtProjectIntellisense
    {
        public static void Refresh(
            EnvDTE.Project project,
            string configId = null,
            IEnumerable<string> selectedFiles = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null || !QtProjectTracker.IsTracked(project.FullName))
                return;

            if (QtVsToolsPackage.Instance.Options.BuildDebugInformation) {
                Messages.Print(string.Format(
                    "{0:HH:mm:ss.FFF} QtProjectIntellisense({1}): Refreshing: [{2}] {3}",
                    DateTime.Now, Thread.CurrentThread.ManagedThreadId,
                    (configId != null) ? configId : "(all configs)", project.FullName));
            }
            string projectPath = project.FullName;
            _ = Task.Run(() => RefreshAsync(project, projectPath, configId, selectedFiles, false));
        }

        public static async Task RefreshAsync(
            EnvDTE.Project project,
            string projectPath,
            string configId = null,
            IEnumerable<string> selectedFiles = null,
            bool refreshQtVars = false)
        {
            if (project == null || !QtProjectTracker.IsTracked(projectPath))
                return;
            var tracker = QtProjectTracker.Get(project, projectPath);
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
                    await QtProjectBuild.StartBuildAsync(
                        project, projectPath, config, properties, targets,
                        LoggerVerbosity.Quiet);
                } else {
                    await QtProjectBuild.SetOutdatedAsync(
                        project, projectPath, config, LoggerVerbosity.Quiet);
                }
            }
        }
    }
}
