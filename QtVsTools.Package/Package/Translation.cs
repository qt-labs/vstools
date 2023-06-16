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
    using Core.MsBuild;

    /// <summary>
    /// Run Qt translation tools by invoking the corresponding Qt/MSBuild targets
    /// </summary>
    public static class Translation
    {
        public static void RunlRelease(VCFile[] vcFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var qtProject = QtProject.Create(vcFiles.FirstOrDefault()?.project as VCProject);
            RunTranslationTarget(BuildAction.Release,
                qtProject, vcFiles.Select(vcFile => vcFile?.RelativePath));
        }

        public static void RunlRelease(QtProject qtProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunTranslationTarget(BuildAction.Release, qtProject);
        }

        public static void RunlRelease(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution == null)
                return;

            foreach (var project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunlRelease(QtProject.Create(project));
        }

        public static void RunlUpdate(VCFile vcFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var qtProject = QtProject.Create(vcFile.project as VCProject);
            RunTranslationTarget(BuildAction.Update,
                qtProject, new[] { vcFile.RelativePath });
        }

        public static void RunlUpdate(VCFile[] vcFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var qtProject = QtProject.Create(vcFiles.FirstOrDefault()?.project as VCProject);
            RunTranslationTarget(BuildAction.Update,
                qtProject, vcFiles.Select(vcFile => vcFile?.RelativePath));
        }

        public static void RunlUpdate(QtProject qtProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RunTranslationTarget(BuildAction.Update, qtProject);
        }

        private enum BuildAction { Update, Release }

        private static void RunTranslationTarget(BuildAction buildAction, QtProject qtProject,
            IEnumerable<string> selectedFiles = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (qtProject == null) {
                Messages.Print("translation: Error accessing project interface");
                return;
            }

            if (qtProject.FormatVersion < ProjectFormat.Version.V3) {
                if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                    QtProject.ShowUpdateFormatMessage();
                return;
            }

            if (qtProject.VcProject.ActiveConfiguration is not {} activeConfig) {
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

            var activeConfigId = $"{activeConfig.ConfigurationName}|{activeConfig.Platform}";
            QtProjectBuild.StartBuild(
                    qtProject, activeConfigId, properties, new[] { "QtTranslation" });
        }

        public static void RunlUpdate(EnvDTE.Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var project in HelperFunctions.ProjectsInSolution(solution?.DTE))
                RunlUpdate(QtProject.Create(project));
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
