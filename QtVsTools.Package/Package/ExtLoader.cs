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

        public static void ImportPriFile(QtProject qtProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (qtProject == null || !HelperFunctions.IsVsToolsProject(qtProject.VcProject))
                return;

            using var fd = new OpenFileDialog
            {
                FileName = $"{qtProject.VcProjectDirectory}{qtProject.VcProject.Name}.pri",
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

            var directoryName = priFileInfo.DirectoryName;
            var priFiles = ResolveFilesFromQMake(qmake.SourceFiles, qtProject, directoryName);
            var projFiles = HelperFunctions.GetProjectFiles(qtProject, FilesToList.FL_CppFiles);
            projFiles = ProjectImporter.ConvertFilesToFullPath(projFiles, qtProject);
            ProjectImporter.SyncIncludeFiles(qtProject, priFiles, projFiles, qmake.IsFlat,
                Filters.SourceFiles());

            priFiles = ResolveFilesFromQMake(qmake.HeaderFiles, qtProject, directoryName);
            projFiles = HelperFunctions.GetProjectFiles(qtProject, FilesToList.FL_HFiles);
            projFiles = ProjectImporter.ConvertFilesToFullPath(projFiles, qtProject);
            ProjectImporter.SyncIncludeFiles(qtProject, priFiles, projFiles, qmake.IsFlat,
                Filters.HeaderFiles());

            priFiles = ResolveFilesFromQMake(qmake.FormFiles, qtProject, directoryName);
            projFiles = HelperFunctions.GetProjectFiles(qtProject, FilesToList.FL_UiFiles);
            projFiles = ProjectImporter.ConvertFilesToFullPath(projFiles, qtProject);
            ProjectImporter.SyncIncludeFiles(qtProject, priFiles, projFiles, qmake.IsFlat,
                Filters.FormFiles());

            priFiles = ResolveFilesFromQMake(qmake.ResourceFiles, qtProject, directoryName);
            projFiles = HelperFunctions.GetProjectFiles(qtProject, FilesToList.FL_Resources);
            projFiles = ProjectImporter.ConvertFilesToFullPath(projFiles, qtProject);
            ProjectImporter.SyncIncludeFiles(qtProject, priFiles, projFiles, qmake.IsFlat,
                Filters.ResourceFiles());
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
            QtProject qtProject, string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var lst = new List<string>();
            foreach (var file in files) {
                var s = ResolveEnvironmentVariables(file, qtProject);
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

        private static string ResolveEnvironmentVariables(string str, QtProject qtProject)
        {
            var reg = new Regex(@"\$\(([^\s\(\)]+)\)");
            var col = reg.Matches(str);
            for (var i = 0; i < col.Count; ++i) {
                var env = col[i].Groups[1].ToString();
                string val;
                if (env == "QTDIR") {
                    val = qtProject?.InstallPath ?? Environment.GetEnvironmentVariable(env);
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
