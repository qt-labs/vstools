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

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace QtVsTools.Core
{
    public class ProjectImporter
    {
        private readonly DTE dteObject;
        const string projectFileExtension = ".vcxproj";

        public ProjectImporter(DTE dte)
        {
            dteObject = dte;
        }

        public void ImportProFile(string qtVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            FileDialog toOpen = new OpenFileDialog();
            toOpen.FilterIndex = 1;
            toOpen.CheckFileExists = true;
            toOpen.Title = SR.GetString("ExportProject_SelectQtProjectToAdd");
            toOpen.Filter = "Qt Project files (*.pro)|*.pro|All files (*.*)|*.*";

            if (DialogResult.OK != toOpen.ShowDialog())
                return;

            var mainInfo = new FileInfo(toOpen.FileName);
            if (HelperFunctions.IsSubDirsFile(mainInfo.FullName)) {
                // we use the safe way. Make the user close the existing solution manually
                if ((!string.IsNullOrEmpty(dteObject.Solution.FullName))
                    || (HelperFunctions.ProjectsInSolution(dteObject).Count > 0)) {
                    if (MessageBox.Show(SR.GetString("ExportProject_SubdirsProfileSolutionClose"),
                        SR.GetString("OpenSolution"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                        == DialogResult.OK) {
                        dteObject.Solution.Close(true);
                    } else {
                        return;
                    }
                }

                ImportSolution(mainInfo, qtVersion);
            } else {
                ImportProject(mainInfo, qtVersion);
            }
        }

        private void ImportSolution(FileInfo mainInfo, string qtVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var versionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
            var VCInfo = RunQmake(mainInfo, ".sln", true, versionInfo);
            if (null == VCInfo)
                return;
            ImportQMakeSolution(VCInfo, versionInfo);

            try {
                if (CheckQtVersion(versionInfo)) {
                    dteObject.Solution.Open(VCInfo.FullName);
                    if (qtVersion != null) {
                        Legacy.QtVersionManager.SaveSolutionQtVersion(dteObject.Solution, qtVersion);
                        foreach (var prj in HelperFunctions.ProjectsInSolution(dteObject)) {
                            QtVersionManager.The().SaveProjectQtVersion(prj, qtVersion);
                            var qtPro = QtProject.Create(prj);
                            qtPro.SetQtEnvironment();
                            ApplyPostImportSteps(qtPro);
                        }
                    }
                }

                Messages.Print("--- (Import): Finished opening " + VCInfo.Name);
            } catch (Exception e) {
                Messages.DisplayCriticalErrorMessage(e);
            }
        }

        public void ImportProject(FileInfo mainInfo, string qtVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var versionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
            var VCInfo = RunQmake(mainInfo, projectFileExtension, false, versionInfo);
            if (null == VCInfo)
                return;

            ImportQMakeProject(VCInfo, versionInfo);

            try {
                if (CheckQtVersion(versionInfo)) {
                    // no need to add the project again if it's already there...
                    var fullName = VCInfo.FullName;
                    var pro = HelperFunctions.ProjectFromSolution(dteObject, fullName);
                    if (pro == null) {
                        try {
                            pro = dteObject.Solution.AddFromFile(fullName, false);
                        } catch (Exception /*exception*/) {
                            Messages.Print("--- (Import): Generated project could not be loaded.");
                            Messages.Print("--- (Import): Please look in the output above for errors and warnings.");
                            return;
                        }
                        Messages.Print("--- (Import): Added " + VCInfo.Name + " to Solution");
                    } else {
                        Messages.Print("Project already in Solution");
                    }

                    if (pro != null) {
                        var qtPro = QtProject.Create(pro);
                        qtPro.SetQtEnvironment();
                        var platformName = versionInfo.GetVSPlatformName();

                        if (qtVersion != null)
                            QtVersionManager.The().SaveProjectQtVersion(pro, qtVersion, platformName);

                        if (!qtPro.SelectSolutionPlatform(platformName) || !qtPro.HasPlatform(platformName)) {
                            var newProject = false;
                            qtPro.CreatePlatform("Win32", platformName, null, versionInfo, ref newProject);
                            if (!qtPro.SelectSolutionPlatform(platformName))
                                Messages.Print("Can't select the platform " + platformName + ".");
                        }

                        if (HelperFunctions.IsDesignerPlugin(qtPro))
                            qtPro.MarkAsDesignerPluginProject();

                        qtPro.SetQtEnvironment();
                        ApplyPostImportSteps(qtPro);
                    }
                }
            } catch (Exception e) {
                Messages.DisplayCriticalErrorMessage(SR.GetString("ExportProject_ProjectOrSolutionCorrupt", e.ToString()));
            }
        }

        private void ImportQMakeSolution(FileInfo solutionFile, VersionInformation vi)
        {
            var projects = ParseProjectsFromSolution(solutionFile);
            foreach (var project in projects) {
                var projectInfo = new FileInfo(project);
                ImportQMakeProject(projectInfo, vi);
            }
        }

        private static List<string> ParseProjectsFromSolution(FileInfo solutionFile)
        {
            var sr = solutionFile.OpenText();
            var content = sr.ReadToEnd();
            sr.Close();

            var projects = new List<string>();
            var index = content.IndexOf(projectFileExtension, StringComparison.Ordinal);
            while (index != -1) {
                var startIndex = content.LastIndexOf('\"', index, index) + 1;
                var endIndex = content.IndexOf('\"', index);
                projects.Add(content.Substring(startIndex, endIndex - startIndex));
                content = content.Substring(endIndex);
                index = content.IndexOf(projectFileExtension, StringComparison.Ordinal);
            }
            return projects;
        }

        private void ImportQMakeProject(FileInfo projectFile, VersionInformation vi)
        {
            var xmlProject = MsBuildProject.Load(projectFile.FullName);
            xmlProject.ReplacePath(vi.qtDir, "$(QTDIR)");
            xmlProject.ReplacePath(projectFile.DirectoryName, ".");

            bool ok = xmlProject.AddQtMsBuildReferences();
            if (ok)
                ok = xmlProject.ConvertCustomBuildToQtMsBuild();
            if (ok)
                ok = xmlProject.EnableMultiProcessorCompilation();
            if (ok) {
                string versionWin10SDK = HelperFunctions.GetWindows10SDKVersion();
                if (!string.IsNullOrEmpty(versionWin10SDK))
                    ok = xmlProject.SetDefaultWindowsSDKVersion(versionWin10SDK);
            }
            if (ok)
                ok = xmlProject.UpdateProjectFormatVersion();

            if (ok) {
                xmlProject.Save();
                // Initialize Qt variables
                xmlProject.BuildTarget("QtVarsDesignTime");
            } else {
                Messages.Print($"Could not convert project file {projectFile.Name} to Qt/MSBuild.");
            }
        }

        private static void ApplyPostImportSteps(QtProject qtProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            qtProject.RemoveResFilesFromGeneratedFilesFilter();
            qtProject.TranslateFilterNames();

            // collapse the generated files/resources filters afterwards
            qtProject.CollapseFilter(Filters.ResourceFiles().Name);
            qtProject.CollapseFilter(Filters.GeneratedFiles().Name);

            try {
                // save the project after modification
                qtProject.Project.Save(null);
            } catch { /* ignore */ }
        }

        private FileInfo RunQmake(FileInfo mainInfo, string ext, bool recursive, VersionInformation vi)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var name = mainInfo.Name.Remove(mainInfo.Name.IndexOf('.'));

            var vcxproj = new FileInfo(mainInfo.DirectoryName + "\\" + name + ext);
            if (vcxproj.Exists) {
                var result = MessageBox.Show($@"{vcxproj.Name} already exists. Select 'OK' to " +
                    "regenerate the file or 'Cancel' to quit importing the project.",
                    "Project file already exists.",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                    return null;
            }

            Messages.Print("--- (Import): Generating new project of " + mainInfo.Name + " file");

            var waitDialog = WaitDialog.Start("Open Qt Project File",
                "Generating Visual Studio project...", delay: 2);

            var qmake = new QMakeImport(vi, mainInfo.FullName, recursive, dteObject);
            int exitCode = qmake.Run(setVCVars: true);

            waitDialog.Stop();

            if (exitCode == 0)
                return vcxproj;
            return null;
        }

        private static bool CheckQtVersion(VersionInformation vi)
        {
            if (vi.qtMajor < 5) {
                Messages.DisplayWarningMessage(SR.GetString("ExportProject_EditProjectFileManually"));
                return false;
            }
            return true;
        }

    }
}
