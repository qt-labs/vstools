/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools
{
    using Core;
    using QtMsBuild;

    /// <summary>
    /// Run Qt translation tools by invoking the corresponding Qt/MSBuild targets
    /// </summary>
    public static class Translation
    {
        public static void RunlRelease(VCFile[] vcFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vcProj = vcFiles.FirstOrDefault()?.project as VCProject;
            var project = vcProj?.Object as EnvDTE.Project;
            RunTranslationTarget(BuildAction.Release,
                project, vcFiles.Select(vcFile => vcFile?.RelativePath));
        }

        public static void RunlRelease(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunTranslationTarget(BuildAction.Release, project);
        }

        public static void RunlRelease(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution == null)
                return;

            foreach (var project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunlRelease(project);
        }

        public static void RunlUpdate(VCFile vcFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vcProj = vcFile.project as VCProject;
            var project = vcProj?.Object as EnvDTE.Project;
            RunTranslationTarget(BuildAction.Update,
                project, new[] { vcFile.RelativePath });
        }

        public static void RunlUpdate(VCFile[] vcFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vcProj = vcFiles.FirstOrDefault()?.project as VCProject;
            var project = vcProj?.Object as EnvDTE.Project;
            RunTranslationTarget(BuildAction.Update,
                project, vcFiles.Select(vcFile => vcFile?.RelativePath));
        }

        public static void RunlUpdate(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunTranslationTarget(BuildAction.Update, project);
        }

        internal enum BuildAction { Update, Release }

        static void RunTranslationTarget(
            BuildAction buildAction,
            EnvDTE.Project project,
            IEnumerable<string> selectedFiles = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            using (WaitDialog.Start(
                "Qt Visual Studio Tools", "Running translation tool...")) {

                var qtPro = QtProject.Create(project);
                if (project == null || qtPro == null) {
                    Messages.Print(
                        "translation: Error accessing project interface");
                    return;
                }

                if (qtPro.FormatVersion < Resources.qtMinFormatVersion_Settings) {
                    if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                        Notifications.UpdateProjectFormat.Show();
                    return;
                }

                var activeConfig = project.ConfigurationManager?.ActiveConfiguration;
                if (activeConfig == null) {
                    Messages.Print(
                        "translation: Error accessing build interface");
                    return;
                }
                var activeConfigId = string.Format("{0}|{1}",
                    activeConfig.ConfigurationName, activeConfig.PlatformName);

                var target = "QtTranslation";
                var properties = new Dictionary<string, string>();
                switch (buildAction) {
                case BuildAction.Update:
                    properties["QtTranslationForceUpdate"] = "true";
                    break;
                case BuildAction.Release:
                    properties["QtTranslationForceRelease"] = "true";
                    break;
                }
                if (selectedFiles != null)
                    properties["SelectedFiles"] = string.Join(";", selectedFiles);

                QtProjectBuild.StartBuild(
                    project, project.FullName, activeConfigId, properties, new[] { target });
            }
        }

        public static void RunlUpdate(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution == null)
                return;

            foreach (var project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunlUpdate(project);
        }

        public static bool ToolsAvailable(EnvDTE.Project project)
        {
            if (project == null)
                return false;
            if (QtProject.GetPropertyValue(project, "ApplicationType") == "Linux")
                return true;

            var qtToolsPath = QtProject.GetPropertyValue(project, "QtToolsPath");
            if (string.IsNullOrEmpty(qtToolsPath)) {
                var qtVersion = QtVersionManager.The().GetProjectQtVersion(project);
                var qtInstallPath = QtVersionManager.The().GetInstallPath(qtVersion);
                if (string.IsNullOrEmpty(qtInstallPath))
                    return false;
                qtToolsPath = Path.Combine(qtInstallPath, "bin");
            }
            return File.Exists(Path.Combine(qtToolsPath, "lupdate.exe"))
                && File.Exists(Path.Combine(qtToolsPath, "lrelease.exe"));
        }
    }
}
