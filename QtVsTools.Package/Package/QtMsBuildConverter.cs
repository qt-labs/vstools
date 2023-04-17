/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools
{
    using Core;
    using VisualStudio;

    static class QtMsBuildConverter
    {
        private const string CancelConversion = "Project conversion canceled.";
        private const string ErrorConversion = "Error converting project {0}";

        public static bool SolutionToQtMsBuild()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var allProjects = HelperFunctions.ProjectsInSolution(QtVsToolsPackage.Instance.Dte);
            if (allProjects.Count == 0)
                return WarningMessage("No projects to convert.");

            var projects = new List<EnvDTE.Project>();
            foreach (var project in allProjects.Where(HelperFunctions.IsQtProject)) {
                if (QtProject.IsQtMsBuildEnabled(project)) {
                    if (QtProject.GetFormatVersion(project) >= Resources.qtProjectFormatVersion)
                        continue;
                }
                projects.Add(project);
            }
            if (projects.Count == 0)
                return WarningMessage("No projects to convert.");

            if (MessageBox.Show("Do you really want to convert all projects?",
                    "Project Conversion", MessageBoxButtons.YesNo) != DialogResult.Yes) {
                return WarningMessage(CancelConversion);
            }

            bool hasDirtyProjects = projects
                .Where(project =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return project.IsDirty;
                })
                .Any();
            if (hasDirtyProjects
                && MessageBox.Show("Projects must be saved before conversion. Save projects?",
                    "Project Conversion", MessageBoxButtons.YesNo) != DialogResult.Yes) {
                return WarningMessage(CancelConversion);
            }

            var projectPaths = projects
                .Select(x =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return x.FullName;
                })
                .ToList();

            var solution = QtVsToolsPackage.Instance.Dte.Solution;
            string solutionPath = solution.FileName;
            solution.Close(true);

            var waitDialog = WaitDialog.StartWithProgress("Qt VS Tools",
                "Converting solution to Qt/MSBuild...", projectPaths.Count, isCancelable: true);

            int projCount = 0;
            bool canceled = false;
            foreach (var projectPath in projectPaths) {
                if (waitDialog != null) {
                    waitDialog.Update("Converting solution to Qt/MSBuild..."
                            + Environment.NewLine
                            + $"Converting project {projCount + 1}/{projectPaths.Count}: "
                            + $"{Path.GetFileNameWithoutExtension(projectPath)}...",
                        projectPaths.Count, projCount);
                    if (waitDialog.Canceled) {
                        canceled = true;
                        break;
                    }
                }
                if (!ConvertProject(projectPath)) {
                    waitDialog?.Stop();
                    QtVsToolsPackage.Instance.Dte.Solution.Open(solutionPath);
                    return ErrorMessage(string.Format(ErrorConversion,
                        Path.GetFileName(projectPath)));
                }
                ++projCount;
            }

            waitDialog?.Stop();

            QtVsToolsPackage.Instance.Dte.Solution.Open(solutionPath);
            if (canceled && projCount < projectPaths.Count) {
                MessageBox.Show($"Conversion canceled. {projectPaths.Count - projCount} "
                    + "projects were not converted.", "Qt VS Tools",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return true;
        }

        static bool ConvertProject(string pathToProject)
        {
            var xmlProject = MsBuildProject.Load(pathToProject);
            bool ok = xmlProject != null;
            if (ok)
                ok = xmlProject.AddQtMsBuildReferences();
            if (ok)
                ok = xmlProject.ConvertCustomBuildToQtMsBuild();
            if (ok)
                ok = xmlProject.EnableMultiProcessorCompilation();
            if (ok)
                ok = xmlProject.UpdateProjectFormatVersion();
            if (ok)
                ok = xmlProject.Save();

            // Initialize Qt variables
            if (ok)
                xmlProject.BuildTarget("QtVarsDesignTime");
            return ok;
        }

        public static bool ProjectToQtMsBuild(EnvDTE.Project project, bool askConfirmation = true)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                return ErrorMessage(string.Format(ErrorConversion, ""));
            var pathToProject = project.FullName;

            if (askConfirmation
                && MessageBox.Show("Do you really want to convert the selected project?",
                    "Project Conversion", MessageBoxButtons.YesNo) != DialogResult.Yes) {
                return WarningMessage(CancelConversion);
            }

            if (project.IsDirty) {
                if (askConfirmation
                    && MessageBox.Show("Projects must be saved before conversion. Save projects?",
                        project.Name, MessageBoxButtons.YesNo) != DialogResult.Yes) {
                    return WarningMessage(CancelConversion);
                }

                try {
                    project.Save();
                } catch (Exception e) {
                    return ErrorMessage(string.Format(ErrorConversion, $"{project.Name}\r\n{e.Message}"));
                }
            }

            if (project.Object is not VCProject vcProject)
                return ErrorMessage(string.Format(ErrorConversion, project.Name));

            var solution = VsServiceProvider.GetService<SVsSolution, IVsSolution4>();
            if (solution == null)
                return ErrorMessage(
                    string.Format(ErrorConversion, project.Name));
            var projectGuid = new Guid(vcProject.ProjectGUID);
            var projectName = project.Name;
            try {
                if (solution.UnloadProject(
                    ref projectGuid,
                    (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser)
                    != VSConstants.S_OK)
                    return ErrorMessage(
                        string.Format(ErrorConversion, projectName));
            } catch (Exception e) {
                return ErrorMessage(string.Format(ErrorConversion, $"{projectName}\r\n{e.Message}"));
            }

            bool ok = ConvertProject(pathToProject);
            try {
                solution.ReloadProject(ref projectGuid);
            } catch (Exception e) {
                return ErrorMessage(
                    string.Format(ErrorConversion, $"{projectName}\r\n{e.Message}"));
            }
            return ok || ErrorMessage(string.Format(ErrorConversion, projectName));
        }

        static bool ErrorMessage(string msg)
        {
            Messages.DisplayErrorMessage(msg);
            return false;
        }

        static bool WarningMessage(string msg)
        {
            Messages.DisplayWarningMessage(msg);
            return true;
        }
    }
}
