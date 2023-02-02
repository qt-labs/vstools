/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools
{
    using Core;

    public static class ExtLoader
    {
        public static void ImportProFile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (GetQtInstallPath() is not {} qtDir)
                return;

            var vi = VersionInformation.Get(qtDir);
            if (vi.qtMajor < 5) {
                Messages.DisplayErrorMessage("The default Qt version does not support Visual "
                    + "Studio. To import .pro files, specify Qt 5.0 or later as the default.");
                return;
            }

            if (QtVsToolsPackage.Instance.Dte is null)
                return;
            var proFileImporter = new ProjectImporter(QtVsToolsPackage.Instance.Dte);
            proFileImporter.ImportProFile(QtVersionManager.The().GetDefaultVersion());
        }

        public static void ImportPriFile(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project?.Object is not VCProject vcProject)
                return;

            if (!HelperFunctions.IsVsToolsProject(vcProject))
                return;

            using var fd = new OpenFileDialog
            {
                FileName = $"{vcProject.ProjectDirectory}{vcProject.Name}.pri",
                Filter = "Project Include Files (*.pri)|*.pri",
                Title = "Import from .pri File",
                Multiselect = false
            };

            if (fd.ShowDialog() != DialogResult.OK)
                return;

            if (GetQtInstallPath() is not {} qtDir)
                return;

            var qmake = new QMakeWrapper { QtDir = qtDir };
            var priFileInfo = new FileInfo(fd.FileName);
            if (qmake.ReadFile(priFileInfo.FullName)) {
                Messages.Print($"--- (Importing .pri file) file: {priFileInfo} could not be read.");
                return;
            }

            var priFiles = ResolveFilesFromQMake(qmake.SourceFiles, project, priFileInfo.DirectoryName);
            var projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_CppFiles);
            projFiles = ProjectImporter.ConvertFilesToFullPath(projFiles, vcProject.ProjectDirectory);
            ProjectImporter.SyncIncludeFiles(vcProject, priFiles, projFiles, qmake.IsFlat, Filters.SourceFiles());

            priFiles = ResolveFilesFromQMake(qmake.HeaderFiles, project, priFileInfo.DirectoryName);
            projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_HFiles);
            projFiles = ProjectImporter.ConvertFilesToFullPath(projFiles, vcProject.ProjectDirectory);
            ProjectImporter.SyncIncludeFiles(vcProject, priFiles, projFiles, qmake.IsFlat, Filters.HeaderFiles());

            priFiles = ResolveFilesFromQMake(qmake.FormFiles, project, priFileInfo.DirectoryName);
            projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_UiFiles);
            projFiles = ProjectImporter.ConvertFilesToFullPath(projFiles, vcProject.ProjectDirectory);
            ProjectImporter.SyncIncludeFiles(vcProject, priFiles, projFiles, qmake.IsFlat, Filters.FormFiles());

            priFiles = ResolveFilesFromQMake(qmake.ResourceFiles, project, priFileInfo.DirectoryName);
            projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_Resources);
            projFiles = ProjectImporter.ConvertFilesToFullPath(projFiles, vcProject.ProjectDirectory);
            ProjectImporter.SyncIncludeFiles(vcProject, priFiles, projFiles, qmake.IsFlat, Filters.ResourceFiles());
        }

        private static string GetQtInstallPath()
        {
            var vm = QtVersionManager.The();
            var qtVersion = vm.GetDefaultVersion();
            var path = vm.GetInstallPath(qtVersion);

            if (path is null)
                Messages.DisplayErrorMessage("Cannot find qmake. Make sure you have specified a Qt version.");
            return path;
        }

        private static List<string> ResolveFilesFromQMake(IEnumerable<string> files,
            EnvDTE.Project project, string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var lst = new List<string>();
            foreach (var file in files) {
                var s = ResolveEnvironmentVariables(file, project);
                if (s is null) {
                    Messages.Print($"--- (importing .pri file) file: {file} cannot be resolved. "
                        + "Skipping file.");
                } else {
                    if (!HelperFunctions.IsAbsoluteFilePath(s))
                        s = path + Path.DirectorySeparatorChar + s;
                    lst.Add(s);
                }
            }
            return lst;
        }

        private static string ResolveEnvironmentVariables(string str, EnvDTE.Project project)
        {
            var reg = new Regex(@"\$\(([^\s\(\)]+)\)");
            var col = reg.Matches(str);
            for (var i = 0; i < col.Count; ++i) {
                var env = col[i].Groups[1].ToString();
                string val;
                if (env == "QTDIR") {
                    var vm = QtVersionManager.The();
                    val = vm.GetInstallPath(project) ?? Environment.GetEnvironmentVariable(env);
                } else {
                    val = Environment.GetEnvironmentVariable(env);
                }
                if (val is null)
                    return null;
                str = str.Replace($"$({env})", val);
            }
            return str;
        }
    }
}
