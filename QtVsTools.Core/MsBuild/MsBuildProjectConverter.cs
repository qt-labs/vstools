/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Core.MsBuild
{
    using Core;
    using VisualStudio;

    public static class MsBuildProjectConverter
    {
        private const string ErrorConversion = "Error converting project {0}";

        public static bool SolutionToQtMsBuild()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = VsServiceProvider.GetService<SDTE, DTE>();

            var projects = (from project in HelperFunctions.ProjectsInSolution(dte)
                let version = MsBuildProjectFormat.GetVersion(project)
                where version is >= MsBuildProjectFormat.Version.V1 and < MsBuildProjectFormat.Version.Latest
                select project).ToList();
            if (projects.Count == 0) {
                Messages.DisplayWarningMessage("No projects to convert.");
                return true;
            }

            if (MessageBox.Show("Do you really want to convert all projects?",
                    "Project Conversion", MessageBoxButtons.YesNo) != DialogResult.Yes) {
                return true;
            }

            var hasDirtyProjects = projects
                .Where(project =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return project.IsDirty;
                })
                .Any();
            if (hasDirtyProjects
                && MessageBox.Show("Projects must be saved before conversion. Save and Continue?",
                    "Project Conversion", MessageBoxButtons.YesNo) == DialogResult.Cancel) {
                return true;
            }

            var projectPaths = projects
                .Select(x =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return x.ProjectFile;
                })
                .ToList();

            var solution = dte.Solution;
            var solutionPath = solution.FileName;
            solution.Close(true);

            var waitDialog = WaitDialog.StartWithProgress("Qt VS Tools",
                "Converting solution to Qt/MSBuild...", projectPaths.Count, isCancelable: true);

            var projCount = 0;
            var canceled = false;
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
                    solution.Open(solutionPath);
                    return ErrorMessage(string.Format(ErrorConversion,
                        Path.GetFileName(projectPath)));
                }
                ++projCount;
            }

            waitDialog?.Stop();

            try {
                solution.Open(solutionPath);
            } catch (Exception exception) {
                // This can happen if one opens a .vcxproj instead of an already existing
                // solution, or with no solution at all. The solution.Close(true) forces
                // saving the solution, but we have no means to get the actual solution path.
                // Using solution.Open(solutionPath) with an empty path throws an exception.
                Messages.DisplayWarningMessage("There was a problem reopening the Solution. "
                    + "Please try to open the Solution from the File menu.");
                exception.Log();
            }

            if (canceled && projCount < projectPaths.Count) {
                MessageBox.Show($"Conversion canceled. {projectPaths.Count - projCount} "
                    + "projects were not converted.", "Qt VS Tools",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return true;
        }

        private static bool ConvertProject(string pathToProject)
        {
            var xmlProject = MsBuildProjectReaderWriter.Load(pathToProject);
            if (xmlProject == null)
                return false;
            var oldVersion = xmlProject.GetProjectFormatVersion();
            switch (oldVersion) {
            case MsBuildProjectFormat.Version.Latest:
                return true; // Nothing to do!
            case > MsBuildProjectFormat.Version.Latest:
                return false; // Nothing we can do!
            }

            var ok = xmlProject.ConvertCustomBuildToQtMsBuild();
            if (ok)
                ok = xmlProject.EnableMultiProcessorCompilation();
            if (ok)
                ok = xmlProject.UpdateProjectFormatVersion(oldVersion);
            if (ok)
                ok = xmlProject.Save();

            // Initialize Qt variables
            if (ok)
                xmlProject.BuildTarget("QtVarsDesignTime");
            return ok;
        }

        public static bool ProjectToQtMsBuild()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = VsServiceProvider.GetService<SDTE, DTE>();

            var project = HelperFunctions.GetSelectedProject(dte);
            if (project == null)
                return ErrorMessage("No project to convert.");

            if (MessageBox.Show(
                "Do you really want to convert the selected project?",
                "Project Conversion", MessageBoxButtons.YesNo) != DialogResult.Yes) {
                return true;
            }

            var projectName = project.Name;
            if (project.IsDirty
                && MessageBox.Show("Project must be saved before conversion. Save and Continue?",
                    projectName, MessageBoxButtons.OKCancel) == DialogResult.Cancel) {
                return true;
            }

            try {
                project.Save();
            } catch (Exception e) {
                return ErrorMessage(string.Format(ErrorConversion, $"{projectName}\r\n{e.Message}"));
            }

            var solution = VsServiceProvider.GetService<SVsSolution, IVsSolution4>();
            if (solution == null)
                return ErrorMessage(string.Format(ErrorConversion, projectName));

            var projectFile = project.ProjectFile;
            var projectGuid = new Guid(project.ProjectGUID);
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

            var ok = ConvertProject(projectFile);
            try {
                solution.ReloadProject(ref projectGuid);
            } catch (Exception e) {
                return ErrorMessage(
                    string.Format(ErrorConversion, $"{projectName}\r\n{e.Message}"));
            }
            return ok || ErrorMessage(string.Format(ErrorConversion, projectName));
        }

        private static bool ErrorMessage(string msg)
        {
            Messages.DisplayErrorMessage(msg);
            return false;
        }
    }
}
