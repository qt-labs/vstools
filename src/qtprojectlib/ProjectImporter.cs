/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace QtProjectLib
{
    public class ProjectImporter
    {
        private EnvDTE.DTE dteObject = null;
        const string projectFileExtension = ".vcxproj";

        public ProjectImporter(EnvDTE.DTE dte)
        {
            dteObject = dte;
        }

        public void ImportProFile(string qtVersion)
        {
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
            var versionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
            var VCInfo = RunQmake(mainInfo, ".sln", true, versionInfo);
            if (null == VCInfo)
                return;
            ReplaceAbsoluteQtDirInSolution(VCInfo);

            try {
                if (CheckQtVersion(versionInfo)) {
                    dteObject.Solution.Open(VCInfo.FullName);
                    if (qtVersion != null) {
                        QtVersionManager.The().SaveSolutionQtVersion(dteObject.Solution, qtVersion);
                        foreach (Project prj in HelperFunctions.ProjectsInSolution(dteObject)) {
                            QtVersionManager.The().SaveProjectQtVersion(prj, qtVersion);
                            var qtPro = QtProject.Create(prj);
                            qtPro.SetQtEnvironment();
                            ApplyPostImportSteps(qtPro);
                        }
                    }
                }

                Messages.PaneMessage(dteObject, "--- (Import): Finished opening " + VCInfo.Name);
            } catch (Exception e) {
                Messages.DisplayCriticalErrorMessage(e);
            }
        }

        public void ImportProject(FileInfo mainInfo, string qtVersion)
        {
            var versionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
            var VCInfo = RunQmake(mainInfo, projectFileExtension, false, versionInfo);
            if (null == VCInfo)
                return;

            ReplaceAbsoluteQtDirInProject(VCInfo);

            try {
                if (CheckQtVersion(versionInfo)) {
                    // no need to add the project again if it's already there...
                    if (!HelperFunctions.IsProjectInSolution(dteObject, VCInfo.FullName)) {
                        try {
                            dteObject.Solution.AddFromFile(VCInfo.FullName, false);
                        } catch (Exception /*exception*/) {
                            Messages.PaneMessage(dteObject, "--- (Import): Generated project could not be loaded.");
                            Messages.PaneMessage(dteObject, "--- (Import): Please look in the output above for errors and warnings.");
                            return;
                        }
                        Messages.PaneMessage(dteObject, "--- (Import): Added " + VCInfo.Name + " to Solution");
                    } else {
                        Messages.PaneMessage(dteObject, "Project already in Solution");
                    }

                    EnvDTE.Project pro = null;
                    foreach (EnvDTE.Project p in HelperFunctions.ProjectsInSolution(dteObject)) {
                        if (p.FullName.ToLower() == VCInfo.FullName.ToLower()) {
                            pro = p;
                            break;
                        }
                    }
                    if (pro != null) {
                        var qtPro = QtProject.Create(pro);
                        qtPro.SetQtEnvironment();
                        var platformName = versionInfo.GetVSPlatformName();

                        if (qtVersion != null) {
                            QtVersionManager.The().SaveProjectQtVersion(pro, qtVersion, platformName);
                        }
                        if (!qtPro.SelectSolutionPlatform(platformName) || !qtPro.HasPlatform(platformName)) {
                            bool newProject = false;
                            qtPro.CreatePlatform("Win32", platformName, null, versionInfo, ref newProject);
                            if (!qtPro.SelectSolutionPlatform(platformName)) {
                                Messages.PaneMessage(dteObject, "Can't select the platform " + platformName + ".");
                            }
                        }

                        // try to figure out if the project is a plugin project
                        try {
                            string activeConfig = pro.ConfigurationManager.ActiveConfiguration.ConfigurationName;
                            var config = (VCConfiguration) ((IVCCollection) qtPro.VCProject.Configurations).Item(activeConfig);
                            if (config.ConfigurationType == ConfigurationTypes.typeDynamicLibrary) {
                                var compiler = CompilerToolWrapper.Create(config);
                                var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");
                                if (compiler.GetPreprocessorDefinitions().IndexOf("QT_PLUGIN", StringComparison.Ordinal) > -1
                                    && compiler.GetPreprocessorDefinitions().IndexOf("QDESIGNER_EXPORT_WIDGETS", StringComparison.Ordinal) > -1
                                    && compiler.GetAdditionalIncludeDirectories().IndexOf("QtDesigner", StringComparison.Ordinal) > -1
                                    && linker.AdditionalDependencies.IndexOf("QtDesigner", StringComparison.Ordinal) > -1) {
                                    qtPro.MarkAsDesignerPluginProject();
                                }
                            }
                        } catch (Exception) { }

                        ApplyPostImportSteps(qtPro);
                    }
                }
            } catch (Exception e) {
                Messages.DisplayCriticalErrorMessage(SR.GetString("ExportProject_ProjectOrSolutionCorrupt", e.ToString()));
            }
        }

        private void ReplaceAbsoluteQtDirInSolution(FileInfo solutionFile)
        {
            var projects = ParseProjectsFromSolution(solutionFile);
            foreach (string project in projects) {
                var projectInfo = new FileInfo(project);
                ReplaceAbsoluteQtDirInProject(projectInfo);
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
                int startIndex = content.LastIndexOf('\"', index, index) + 1;
                var endIndex = content.IndexOf('\"', index);
                projects.Add(content.Substring(startIndex, endIndex - startIndex));
                content = content.Substring(endIndex);
                index = content.IndexOf(projectFileExtension, StringComparison.Ordinal);
            }
            return projects;
        }

        private void ReplaceAbsoluteQtDirInProject(FileInfo projectFile)
        {
            var sr = projectFile.OpenText();
            var content = sr.ReadToEnd();
            sr.Close();

            var qtDir = ParseQtDirFromFileContent(content);
            if (!string.IsNullOrEmpty(qtDir)) {
                content = HelperFunctions.ReplaceCaseInsensitive(content, qtDir, "$(QTDIR)\\");
                var sw = projectFile.CreateText();
                sw.Write(content);
                sw.Flush();
                sw.Close();
            } else {
                Messages.PaneMessage(dteObject, SR.GetString("ImportProject_CannotFindQtDirectory", projectFile.Name));
            }
        }

        private static string ParseQtDirFromFileContent(string vcFileContent)
        {
            var uicQtDir = FindQtDirFromExtension(vcFileContent, "bin\\uic.exe");
            var rccQtDir = FindQtDirFromExtension(vcFileContent, "bin\\rcc.exe");
            var mkspecQtDir = FindQtDirFromExtension(vcFileContent, "mkspecs\\default");
            if (!string.IsNullOrEmpty(mkspecQtDir)) {
                if (!string.IsNullOrEmpty(uicQtDir) && uicQtDir.ToLower() != mkspecQtDir.ToLower()) {
                    return "";
                }
                if (!string.IsNullOrEmpty(rccQtDir) && rccQtDir.ToLower() != mkspecQtDir.ToLower()) {
                    return "";
                }
                return mkspecQtDir;
            }
            if (!string.IsNullOrEmpty(uicQtDir)) {
                if (!string.IsNullOrEmpty(rccQtDir) && rccQtDir.ToLower() != uicQtDir.ToLower()) {
                    return "";
                }
                return uicQtDir;
            }
            if (!string.IsNullOrEmpty(rccQtDir))
                return rccQtDir;
            return "";
        }

        private static string FindQtDirFromExtension(string content, string extension)
        {
            string s = "";
            int index = -1;
            index = content.IndexOf(extension.ToLower(), StringComparison.OrdinalIgnoreCase);
            if (index != -1) {
                s = content.Remove(index);
                index = s.LastIndexOf("CommandLine=", StringComparison.Ordinal);
                if (s.LastIndexOf("AdditionalDependencies=", StringComparison.Ordinal) > index)
                    index = s.LastIndexOf("AdditionalDependencies=", StringComparison.Ordinal);
                if (index != -1) {
                    s = s.Substring(index);
                    s = s.Substring(s.IndexOf('=') + 1);
                }

                index = s.LastIndexOf(';');
                if (index != -1)
                    s = s.Substring(index + 1);
            }
            if (!string.IsNullOrEmpty(s)) {
                s = s.Trim(new char[] { ' ', '\"', ',' });
                if (s.StartsWith(">", StringComparison.Ordinal))
                    s = s.Substring(1);
                if (!Path.IsPathRooted(s))
                    s = null;
            }
            return s;
        }

        private static void ApplyPostImportSteps(QtProject qtProject)
        {
            foreach (VCConfiguration cfg in (IVCCollection) qtProject.VCProject.Configurations) {
                cfg.IntermediateDirectory = @"$(Platform)\$(Configuration)\";
                var compilerTool = CompilerToolWrapper.Create(cfg);
                if (compilerTool != null) {
                    compilerTool.ObjectFile = @"$(IntDir)";
                    compilerTool.ProgramDataBaseFileName = @"$(IntDir)vc$(PlatformToolsetVersion).pdb";
                }
            }

            qtProject.RemoveResFilesFromGeneratedFilesFilter();
            qtProject.RepairGeneratedFilesStructure();
            qtProject.TranslateFilterNames();

            QtVSIPSettings.SaveUicDirectory(qtProject.Project, QtVSIPSettings.GetUicDirectory());
            qtProject.UpdateUicSteps(".", false); // false is to not remove given path from includes
            QtVSIPSettings.SaveRccDirectory(qtProject.Project, QtVSIPSettings.GetRccDirectory());
            qtProject.RefreshRccSteps();

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
            var name = mainInfo.Name.Remove(mainInfo.Name.IndexOf('.'));

            var VCInfo = new FileInfo(mainInfo.DirectoryName + "\\" + name + ext);

            if (!VCInfo.Exists || DialogResult.Yes == MessageBox.Show(SR.GetString("ExportProject_ProjectExistsRegenerateOrReuse", VCInfo.Name),
                SR.GetString("ProjectExists"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)) {
                Messages.PaneMessage(dteObject, "--- (Import): Generating new project of " + mainInfo.Name + " file");

                var dialog = new InfoDialog(mainInfo.Name);
                var qmake = new QMake(dteObject, mainInfo.FullName, recursive, vi);

                qmake.CloseEvent += dialog.CloseEventHandler;
                qmake.PaneMessageDataEvent += PaneMessageDataReceived;

                var qmakeThread = new System.Threading.Thread(new ThreadStart(qmake.RunQMake));
                qmakeThread.Start();
                dialog.ShowDialog();
                qmakeThread.Join();

                if (qmake.ErrorValue == 0)
                    return VCInfo;
            }

            return null;
        }

        private void PaneMessageDataReceived(string data)
        {
            Messages.PaneMessage(dteObject, data);
        }

        private static bool CheckQtVersion(VersionInformation vi)
        {
            if (!vi.qt5Version) {
                Messages.DisplayWarningMessage(SR.GetString("ExportProject_EditProjectFileManually"));
                return false;
            }
            return true;
        }

    }
}
