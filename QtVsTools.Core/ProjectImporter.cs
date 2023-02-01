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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;

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
            if (IsSubDirsFile(mainInfo.FullName)) {
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
                            ApplyPostImportSteps(dteObject, qtPro);
                        }
                    }
                }

                Messages.Print("--- (Import): Finished opening " + VCInfo.Name);
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
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
                    var pro = ProjectFromSolution(dteObject, fullName);
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

                        // figure out if the imported project is a plugin project
                        var tmp = qtPro.Project.ConfigurationManager.ActiveConfiguration
                            .ConfigurationName;
                        var vcConfig = (qtPro.VCProject.Configurations as IVCCollection).Item(tmp)
                            as VCConfiguration;
                        var def = CompilerToolWrapper.Create(vcConfig)?.GetPreprocessorDefinitions();
                        if (!string.IsNullOrEmpty(def)
                            && def.IndexOf("QT_PLUGIN", StringComparison.Ordinal) > -1) {
                            QtProject.MarkAsQtPlugin(qtPro);
                        }

                        qtPro.SetQtEnvironment();
                        ApplyPostImportSteps(dteObject, qtPro);
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

        private static void ApplyPostImportSteps(DTE dte, QtProject qtProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            RemoveResFilesFromGeneratedFilesFilter(qtProject);
            TranslateFilterNames(qtProject.VCProject);

            // collapse the generated files/resources filters afterwards
            CollapseFilter(dte, qtProject.Project, Filters.ResourceFiles().Name);
            CollapseFilter(dte, qtProject.Project, Filters.GeneratedFiles().Name);

            try {
                // save the project after modification
                qtProject.Project.Save(null);
            } catch { /* ignore */ }
        }

        private FileInfo RunQmake(FileInfo mainInfo, string ext, bool recursive, VersionInformation vi)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var name = mainInfo.Name.Remove(mainInfo.Name.IndexOf('.'));

            var vcxproj = new FileInfo(mainInfo.DirectoryName + Path.DirectorySeparatorChar
                + name + ext);
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

            var qmake = new QMakeImport(vi, mainInfo.FullName, recursiveRun: recursive, dte: dteObject);
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

        #region ProjectExporter

        public static List<string> ConvertFilesToFullPath(List<string> files, string path)
        {
            var ret = new List<string>(files.Count);
            foreach (var file in files) {
                FileInfo fi;
                if (file.IndexOf(':') != 1)
                    fi = new FileInfo(path + Path.DirectorySeparatorChar + file);
                else
                    fi = new FileInfo(file);

                ret.Add(fi.FullName);
            }
            return ret;
        }

        private static VCFilter BestMatch(string path, Hashtable pathFilterTable)
        {
            var bestMatch = ".";
            var inPath = path;
            if (inPath.StartsWith(".\\", StringComparison.Ordinal))
                inPath = inPath.Substring(2);
            foreach (string p in pathFilterTable.Keys) {
                var best = 0;
                for (var i = 0; i < inPath.Length; ++i) {
                    if (i < p.Length && inPath[i] == p[i])
                        ++best;
                    else
                        break;
                }
                if (best > bestMatch.Length && p.Length == best)
                    bestMatch = p;
            }
            return pathFilterTable[bestMatch] as VCFilter;
        }

        private static void CollectFilters(VCFilter filter, string path, ref Hashtable filterPathTable,
            ref Hashtable pathFilterTable)
        {
            var newPath = ".";
            if (path != null)
                newPath = path + Path.DirectorySeparatorChar + filter.Name;
            newPath = newPath.ToLower().Trim();
            newPath = Regex.Replace(newPath, @"\\+\.?\\+", "\\");
            newPath = Regex.Replace(newPath, @"\\\.?$", "");
            if (newPath.StartsWith(".\\", StringComparison.Ordinal))
                newPath = newPath.Substring(2);
            filterPathTable.Add(filter, newPath);
            pathFilterTable.Add(newPath, filter);
            foreach (VCFilter f in (IVCCollection)filter.Filters)
                CollectFilters(f, newPath, ref filterPathTable, ref pathFilterTable);
        }

        public static void SyncIncludeFiles(VCProject vcproj, List<string> priFiles,
            List<string> projFiles, DTE dte, bool flat, FakeFilter fakeFilter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var cmpPriFiles = new List<string>(priFiles.Count);
            foreach (var s in priFiles)
                cmpPriFiles.Add(HelperFunctions.NormalizeFilePath(s).ToLower());
            cmpPriFiles.Sort();

            var cmpProjFiles = new List<string>(projFiles.Count);
            foreach (var s in projFiles)
                cmpProjFiles.Add(HelperFunctions.NormalizeFilePath(s).ToLower());

            var qtPro = QtProject.Create(vcproj);
            var filterPathTable = new Hashtable(17);
            var pathFilterTable = new Hashtable(17);
            if (!flat && fakeFilter != null) {
                var rootFilter = qtPro.FindFilterFromGuid(fakeFilter.UniqueIdentifier);
                if (rootFilter == null)
                    AddFilterToProject(qtPro, Filters.SourceFiles());

                CollectFilters(rootFilter, null, ref filterPathTable, ref pathFilterTable);
            }

            // first check for new files
            foreach (var file in cmpPriFiles) {
                if (cmpProjFiles.IndexOf(file) > -1)
                    continue;

                if (flat) {
                    vcproj.AddFile(file); // the file is not in the project
                } else {
                    var path = HelperFunctions.GetRelativePath(vcproj.ProjectDirectory, file);
                    if (path.StartsWith(".\\", StringComparison.Ordinal))
                        path = path.Substring(2);

                    var i = path.LastIndexOf(Path.DirectorySeparatorChar);
                    if (i > -1)
                        path = path.Substring(0, i);
                    else
                        path = ".";

                    if (pathFilterTable.Contains(path)) {
                        var f = pathFilterTable[path] as VCFilter;
                        f.AddFile(file);
                        continue;
                    }

                    var filter = BestMatch(path, pathFilterTable);

                    var filterDir = filterPathTable[filter] as string;
                    var name = path;
                    if (!name.StartsWith("..", StringComparison.Ordinal) && name.StartsWith(filterDir, StringComparison.Ordinal))
                        name = name.Substring(filterDir.Length + 1);

                    var newFilter = filter.AddFilter(name) as VCFilter;
                    newFilter.AddFile(file);

                    filterPathTable.Add(newFilter, path);
                    pathFilterTable.Add(path, newFilter);
                }
            }

            // then check for deleted files
            foreach (var file in cmpProjFiles) {
                if (cmpPriFiles.IndexOf(file) == -1) {
                    // the file is not in the pri file
                    // (only removes it from the project, does not del. the file)
                    var info = new FileInfo(file);
                    RemoveFileInProject(qtPro, file);
                    Messages.Print("--- (Importing .pri file) file: " + info.Name +
                        " does not exist in .pri file, move to " + vcproj.ProjectDirectory + "Deleted");
                }
            }
        }

        #endregion

        #region HelperFunctions

        private static Project ProjectFromSolution(DTE dteObject, string fullName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            fullName = new FileInfo(fullName).FullName;
            foreach (var p in HelperFunctions.ProjectsInSolution(dteObject)) {
                if (p.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Reads lines from a .pro file that is opened with a StreamReader
        /// and concatenates strings that end with a backslash.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns>the composite string</returns>
        private static string ReadProFileLine(StreamReader streamReader)
        {
            var line = streamReader.ReadLine();
            if (line == null)
                return null;

            line = line.TrimEnd(' ', '\t');
            while (line.EndsWith("\\", StringComparison.OrdinalIgnoreCase)) {
                line = line.Remove(line.Length - 1);
                var appendix = streamReader.ReadLine();
                if (appendix != null)
                    line += appendix.TrimEnd(' ', '\t');
            }
            return line;
        }

        /// <summary>
        /// Reads a .pro file and returns true if it is a subdirs template.
        /// </summary>
        /// <param name="profile">full name of .pro file to read</param>
        /// <returns>true if this is a subdirs file</returns>
        private static bool IsSubDirsFile(string profile)
        {
            StreamReader sr = null;
            try {
                sr = new StreamReader(profile);

                var line = string.Empty;
                while ((line = ReadProFileLine(sr)) != null) {
                    line = line.Replace(" ", string.Empty).Replace("\t", string.Empty);
                    if (line.StartsWith("TEMPLATE", StringComparison.Ordinal))
                        return line.StartsWith("TEMPLATE=subdirs", StringComparison.Ordinal);
                }
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
            } finally {
                if (sr != null)
                    sr.Dispose();
            }
            return false;
        }

        private static void CollapseFilter(UIHierarchyItem item, UIHierarchy hierarchy, string nodeToCollapseFilter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(nodeToCollapseFilter))
                return;

            foreach (UIHierarchyItem innerItem in item.UIHierarchyItems) {
                if (innerItem.Name == nodeToCollapseFilter)
                    CollapseFilter(innerItem, hierarchy);
                else if (innerItem.UIHierarchyItems.Count > 0)
                    CollapseFilter(innerItem, hierarchy, nodeToCollapseFilter);
            }
        }

        private static void CollapseFilter(UIHierarchyItem item, UIHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var subItems = item.UIHierarchyItems;
            if (subItems != null) {
                foreach (UIHierarchyItem innerItem in subItems) {
                    if (innerItem.UIHierarchyItems.Count > 0) {
                        CollapseFilter(innerItem, hierarchy);

                        if (innerItem.UIHierarchyItems.Expanded) {
                            innerItem.UIHierarchyItems.Expanded = false;
                            if (innerItem.UIHierarchyItems.Expanded) {
                                innerItem.Select(vsUISelectionType.vsUISelectionTypeSelect);
                                hierarchy.DoDefaultAction();
                            }
                        }
                    }
                }
            }
            if (item.UIHierarchyItems.Expanded) {
                item.UIHierarchyItems.Expanded = false;
                if (item.UIHierarchyItems.Expanded) {
                    item.Select(vsUISelectionType.vsUISelectionTypeSelect);
                    hierarchy.DoDefaultAction();
                }
            }
        }

        /// <summary>
        /// Removes a file reference from the project and moves the file to the "Deleted" folder.
        /// </summary>
        /// <param name="qtPro"></param>
        /// <param name="fileName"></param>
        private static void RemoveFileInProject(QtProject qtPro, string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            fileName = new FileInfo(fileName).FullName;
            foreach (VCFile vcFile in (IVCCollection)qtPro.VCProject.Files) {
                if (vcFile.FullPath.Equals(fileName, StringComparison.OrdinalIgnoreCase)) {
                    qtPro.VCProject.RemoveFile(vcFile);
                    MoveFileToDeletedFolder(qtPro.VCProject, vcFile);
                }
            }
        }

        #endregion

        #region QtProject

        private static void MoveFileToDeletedFolder(VCProject vcPro, VCFile vcfile)
        {
            var srcFile = new FileInfo(vcfile.FullPath);

            if (!srcFile.Exists)
                return;

            var destFolder = vcPro.ProjectDirectory + "\\Deleted\\";
            var destName = destFolder + vcfile.Name.Replace(".", "_") + ".bak";
            var fileNr = 0;

            try {
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);

                while (File.Exists(destName)) {
                    fileNr++;
                    destName = destName.Substring(0, destName.LastIndexOf('.')) + ".b";
                    destName += fileNr.ToString("00");
                }

                srcFile.MoveTo(destName);
            } catch (Exception e) {
                Messages.DisplayWarningMessage(e, SR.GetString("QtProject_DeletedFolderFullOrProteced"));
            }
        }

        private static VCFilter AddFilterToProject(QtProject project, FakeFilter filter)
        {
            try {
                var vfilt = project.FindFilterFromGuid(filter.UniqueIdentifier);
                if (vfilt == null) {
                    if (!project.VCProject.CanAddFilter(filter.Name)) {
                        vfilt = project.FindFilterFromName(filter.Name);
                        if (vfilt == null)
                            throw new QtVSException(SR.GetString("QtProject_ProjectCannotAddFilter", filter.Name));
                    } else {
                        vfilt = (VCFilter)project.VCProject.AddFilter(filter.Name);
                    }

                    vfilt.UniqueIdentifier = filter.UniqueIdentifier;
                    vfilt.Filter = filter.Filter;
                    vfilt.ParseFiles = filter.ParseFiles;
                }
                return vfilt;
            } catch {
                throw new QtVSException(SR.GetString("QtProject_ProjectCannotAddResourceFilter"));
            }
        }

        private static void TranslateFilterNames(VCProject vcPro)
        {
            var filters = vcPro.Filters as IVCCollection;
            if (filters == null)
                return;

            foreach (VCFilter filter in filters) {
                if (filter.Name == "Form Files")
                    filter.Name = Filters.FormFiles().Name;
                if (filter.Name == "Generated Files")
                    filter.Name = Filters.GeneratedFiles().Name;
                if (filter.Name == "Header Files")
                    filter.Name = Filters.HeaderFiles().Name;
                if (filter.Name == "Resource Files")
                    filter.Name = Filters.ResourceFiles().Name;
                if (filter.Name == "Source Files")
                    filter.Name = Filters.SourceFiles().Name;
            }
        }

        private static void RemoveResFilesFromGeneratedFilesFilter(QtProject pro)
        {
            var generatedFiles = pro.FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (generatedFiles == null)
                return;

            var filesToRemove = new List<VCFile>();
            foreach (VCFile filtFile in (IVCCollection)generatedFiles.Files) {
                if (filtFile.FullPath.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
                    filesToRemove.Add(filtFile);
            }
            foreach (var resFile in filesToRemove)
                resFile.Remove();
        }

        private static void CollapseFilter(DTE dte, Project project, string filterName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionExplorer = (UIHierarchy)dte.Windows.Item(Constants.vsext_wk_SProjectWindow).Object;
            if (solutionExplorer.UIHierarchyItems.Count == 0)
                return;

            dte.SuppressUI = true;
            var projectItem = FindProjectHierarchyItem(project, solutionExplorer);
            if (projectItem != null)
                CollapseFilter(projectItem, solutionExplorer, filterName);
            dte.SuppressUI = false;
        }

        private static UIHierarchyItem FindProjectHierarchyItem(Project project, UIHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy.UIHierarchyItems.Count == 0)
                return null;

            var solution = hierarchy.UIHierarchyItems.Item(1);
            UIHierarchyItem projectItem = null;
            foreach (UIHierarchyItem solutionItem in solution.UIHierarchyItems) {
                projectItem = FindProjectHierarchyItem(project, solutionItem);
                if (projectItem != null)
                    break;
            }
            return projectItem;
        }

        private static UIHierarchyItem FindProjectHierarchyItem(Project project, UIHierarchyItem root)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            UIHierarchyItem projectItem = null;
            try {
                if (root.Name == project.Name)
                    return root;

                foreach (UIHierarchyItem childItem in root.UIHierarchyItems) {
                    projectItem = FindProjectHierarchyItem(project, childItem);
                    if (projectItem != null)
                        break;
                }
            } catch {
            }
            return projectItem;
        }

        #endregion
    }
}
