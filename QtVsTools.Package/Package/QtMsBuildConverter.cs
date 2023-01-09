/****************************************************************************
**
** Copyright (C) 2017 The Qt Company Ltd.
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
        public static bool SolutionToQtMsBuild()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var allProjects = HelperFunctions.ProjectsInSolution(QtVsToolsPackage.Instance.Dte);
            if (allProjects.Count == 0)
                return WarningMessage(SR.GetString("NoProjectsToConvert"));

            var projects = new List<EnvDTE.Project>();
            foreach (var project in allProjects.Where(HelperFunctions.IsQtProject)) {
                if (QtProject.IsQtMsBuildEnabled(project)) {
                    if (QtProject.GetFormatVersion(project) >= Resources.qtProjectFormatVersion)
                        continue;
                }
                projects.Add(project);
            }
            if (projects.Count == 0)
                return WarningMessage(SR.GetString("NoProjectsToConvert"));

            if (MessageBox.Show(
                SR.GetString("ConvertAllConfirmation"),
                SR.GetString("ConvertTitle"),
                MessageBoxButtons.YesNo) != DialogResult.Yes)
                return WarningMessage(SR.GetString("CancelConvertingProject"));

            bool hasDirtyProjects = projects
                .Where(project =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return project.IsDirty;
                })
                .Any();
            if (hasDirtyProjects) {
                if (MessageBox.Show(
                    SR.GetString("ConvertSaveConfirmation"),
                    SR.GetString("ConvertTitle"),
                    MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return WarningMessage(SR.GetString("CancelConvertingProject"));
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

            var waitDialog = WaitDialog.StartWithProgress(
                SR.GetString("Resources_QtVsTools"), SR.GetString("ConvertWait"),
                projectPaths.Count, isCancelable: true);

            int projCount = 0;
            bool canceled = false;
            foreach (var projectPath in projectPaths) {
                if (waitDialog != null) {
                    waitDialog.Update(string.Format(SR.GetString("ConvertProgress"),
                        projCount + 1, projectPaths.Count,
                        Path.GetFileNameWithoutExtension(projectPath)),
                        projectPaths.Count, projCount);
                    if (waitDialog.Canceled) {
                        canceled = true;
                        break;
                    }
                }
                if (!ConvertProject(projectPath)) {
                    waitDialog?.Stop();
                    QtVsToolsPackage.Instance.Dte.Solution.Open(solutionPath);
                    return ErrorMessage(string.Format(SR.GetString("ErrorConvertingProject"),
                        Path.GetFileName(projectPath)));
                }
                ++projCount;
            }

            waitDialog?.Stop();

            QtVsToolsPackage.Instance.Dte.Solution.Open(solutionPath);
            if (canceled && projCount < projectPaths.Count) {
                MessageBox.Show(string.Format(SR.GetString("ConvertCanceled"),
                    projectPaths.Count - projCount), SR.GetString("Resources_QtVsTools"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return true;
        }

        static bool ConvertProject(string pathToProject)
        {
            var xmlProject = MsBuildProject.Load(pathToProject);
            bool ok = (xmlProject != null);
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
                return ErrorMessage(string.Format(SR.GetString("ErrorConvertingProject"), ""));
            var pathToProject = project.FullName;

            if (askConfirmation
                && MessageBox.Show(
                    SR.GetString("ConvertConfirmation"),
                    SR.GetString("ConvertTitle"),
                    MessageBoxButtons.YesNo) != DialogResult.Yes)
                return WarningMessage(SR.GetString("CancelConvertingProject"));
            if (project.IsDirty) {
                if (askConfirmation
                    && MessageBox.Show(SR.GetString("ConvertSaveConfirmation"), project.Name,
                    MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return WarningMessage(SR.GetString("CancelConvertingProject"));
                try {
                    project.Save();
                } catch (Exception e) {
                    return ErrorMessage(string.Format(SR.GetString("ErrorConvertingProject"),
                        string.Format("{0}\r\n{1}", project.Name, e.Message)));
                }
            }

            var vcProject = project.Object as VCProject;
            if (vcProject == null)
                return ErrorMessage(
                    string.Format(SR.GetString("ErrorConvertingProject"), project.Name));
            var solution = VsServiceProvider.GetService<SVsSolution, IVsSolution4>();
            if (solution == null)
                return ErrorMessage(
                    string.Format(SR.GetString("ErrorConvertingProject"), project.Name));
            var projectGuid = new Guid(vcProject.ProjectGUID);
            var projectName = project.Name;
            try {
                if (solution.UnloadProject(
                    ref projectGuid,
                    (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser)
                    != VSConstants.S_OK)
                    return ErrorMessage(
                        string.Format(SR.GetString("ErrorConvertingProject"), projectName));
            } catch (Exception e) {
                return ErrorMessage(string.Format(SR.GetString("ErrorConvertingProject"),
                    string.Format("{0}\r\n{1}", projectName, e.Message)));
            }

            bool ok = ConvertProject(pathToProject);
            try {
                solution.ReloadProject(ref projectGuid);
            } catch (Exception e) {
                return ErrorMessage(
                    string.Format(SR.GetString("ErrorConvertingProject"),
                    string.Format("{0}\r\n{1}", projectName, e.Message)));
            }
            if (!ok) {
                return ErrorMessage(
                    string.Format(SR.GetString("ErrorConvertingProject"), projectName));
            }
            return true;
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
