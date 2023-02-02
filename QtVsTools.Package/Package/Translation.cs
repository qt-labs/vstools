/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
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

        private enum BuildAction { Update, Release }

        private static void RunTranslationTarget(BuildAction buildAction, EnvDTE.Project project,
            IEnumerable<string> selectedFiles = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                Messages.Print("translation: Error accessing build interface");
                return;
            }

            using var _ = WaitDialog.Start("Qt Visual Studio Tools", "Running translation tool...");

            var properties = new Dictionary<string, string>();
            switch (buildAction) {
            case BuildAction.Update:
                properties["QtTranslationForceUpdate"] = "true";
                break;
            case BuildAction.Release:
                properties["QtTranslationForceRelease"] = "true";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(buildAction), buildAction, null);
            }
            if (selectedFiles != null)
                properties["SelectedFiles"] = string.Join(";", selectedFiles);

            var activeConfigId = $"{activeConfig.ConfigurationName}|{activeConfig.PlatformName}";
            QtProjectBuild.StartBuild(
                project, project.FullName, activeConfigId, properties, new[] { "QtTranslation" });
        }

        public static void RunlUpdate(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var project in HelperFunctions.ProjectsInSolution(solution?.DTE))
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
