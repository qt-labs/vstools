/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core
{
    using MsBuild;
    using static Common.Utils;

    public static class ProjectImporter
    {
        private static DTE _dteObject;

        private const string ProjectFileExtension = ".vcxproj";

        public static void ImportProFile(EnvDTE.DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _dteObject = dte;
            if (_dteObject is null || GetQtInstallPath() is not {} qtDir)
                return;

            var vi = VersionInformation.Get(qtDir);
            if (vi.qtMajor < 5) {
                Messages.DisplayErrorMessage("The default Qt version does not support Visual "
                    + "Studio. To import .pro files, specify Qt 5.0 or later as the default.");
                return;
            }

            var toOpen = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "Qt Project files (*.pro)|*.pro|All files (*.*)|*.*",
                FilterIndex = 1,
                Title = "Select a Qt Project to Add to the Solution"
            };

            if (DialogResult.OK != toOpen.ShowDialog())
                return;

            var mainInfo = new FileInfo(toOpen.FileName);
            if (IsSubDirsFile(mainInfo.FullName)) {
                // we use the safe way. Make the user close the existing solution manually
                if (!string.IsNullOrEmpty(_dteObject.Solution.FullName)
                    || HelperFunctions.ProjectsInSolution(_dteObject).Count > 0) {
                    if (MessageBox.Show("This seems to be a SUBDIRS .pro file. To open this file, "
                            + "the existing solution needs to be closed (pending changes will be saved).",
                            "Open Solution", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                        == DialogResult.OK) {
                        _dteObject.Solution.Close(true);
                    } else {
                        return;
                    }
                }

                ImportSolution(mainInfo, QtVersionManager.GetDefaultVersion());
            } else {
                ImportProject(mainInfo, QtVersionManager.GetDefaultVersion());
            }
        }

        public static void ImportPriFile(EnvDTE.DTE dte, string pkgInstallPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _dteObject = dte;
            if (dte is null || HelperFunctions.GetSelectedQtProject(dte) is not {} project)
                return;

            using var fd = new OpenFileDialog
            {
                FileName = $"{project.VcProjectDirectory}{project.VcProject.Name}.pri",
                Filter = "Project Include Files (*.pri)|*.pri",
                Title = "Import from .pri File",
                Multiselect = false
            };

            if (fd.ShowDialog() != DialogResult.OK)
                return;

            if (GetQtInstallPath() is not {} qtDir)
                return;

            var qmake = new QMakeWrapper
            {
                QtDir = qtDir,
                PkgInstallPath = pkgInstallPath
            };
            var priFileInfo = new FileInfo(fd.FileName);
            if (!qmake.ReadFile(priFileInfo.FullName)) {
                Messages.Print($"--- (Importing .pri file) file: {priFileInfo} could not be read.");
                return;
            }

            var tuples = new List<(string[] Files, FilesToList FilesToList, FakeFilter Filter)>
            {
                (qmake.SourceFiles, FilesToList.FL_CppFiles, FakeFilter.SourceFiles()),
                (qmake.HeaderFiles, FilesToList.FL_HFiles, FakeFilter.HeaderFiles()),
                (qmake.FormFiles, FilesToList.FL_UiFiles, FakeFilter.FormFiles()),
                (qmake.ResourceFiles, FilesToList.FL_Resources, FakeFilter.ResourceFiles())
            };

            var directoryName = priFileInfo.DirectoryName;
            foreach (var tuple in tuples) {
                var priFiles = ResolveFilesFromQMake(tuple.Files, project, directoryName);
                var projFiles = HelperFunctions.GetProjectFiles(project, tuple.FilesToList);
                projFiles = ConvertFilesToFullPath(projFiles, project);
                SyncIncludeFiles(project, priFiles, projFiles, qmake.IsFlat, tuple.Filter);
            }
        }

        private static void ImportSolution(FileInfo mainInfo, string qtVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var versionInfo = QtVersionManager.GetVersionInfo(qtVersion);
            var vcInfo = RunQmake(mainInfo, ".sln", true, versionInfo);
            if (null == vcInfo)
                return;
            ImportQMakeSolution(vcInfo, versionInfo);

            try {
                if (CheckQtVersion(versionInfo)) {
                    _dteObject.Solution.Open(vcInfo.FullName);
                    if (qtVersion is not null) {
                        foreach (var vcProject in HelperFunctions.ProjectsInSolution(_dteObject)) {
                            if (MsBuildProject.GetOrAdd(vcProject) is not {} project)
                                continue;
                            QtVersionManager.SaveProjectQtVersion(project, qtVersion);
                            ApplyPostImportSteps(project);
                        }
                    }
                }

                Messages.Print($"--- (Import): Finished opening {vcInfo.Name}");
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
            }
        }

        private static void ImportProject(FileInfo mainInfo, string qtVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var versionInfo = QtVersionManager.GetVersionInfo(qtVersion);
            var vcInfo = RunQmake(mainInfo, ProjectFileExtension, false, versionInfo);
            if (null == vcInfo)
                return;

            if (!ImportQMakeProject(vcInfo, versionInfo)) {
                Messages.Print($"Could not convert project file {vcInfo.Name} to Qt/MSBuild.");
                return;
            }

            try {
                if (!CheckQtVersion(versionInfo))
                    return;
                // no need to add the project again if it's already there...
                var fullName = vcInfo.FullName;
                var vcPro = ProjectFromSolution(fullName);
                if (vcPro is null) {
                    try {
                        vcPro = _dteObject.Solution.AddFromFile(fullName).Object as VCProject;
                    } catch (Exception /*exception*/) {
                        Messages.Print("--- (Import): Generated project could not be loaded.");
                        Messages.Print("--- (Import): Please look in the output above for errors and warnings.");
                        return;
                    }
                    Messages.Print($"--- (Import): Added {vcInfo.Name} to Solution");
                } else {
                    Messages.Print("Project already in Solution");
                }

                if (MsBuildProject.GetOrAdd(vcPro) is not {} project)
                    return;

                if (qtVersion is not null)
                    QtVersionManager.SaveProjectQtVersion(project, qtVersion);

                var platformName = versionInfo.GetVSPlatformName();
                if (!SelectSolutionPlatform(platformName) || !HasPlatform(vcPro, platformName)) {
                    CreatePlatform(vcPro, "Win32", platformName, null, versionInfo);
                    if (!SelectSolutionPlatform(platformName))
                        Messages.Print($"Can't select the platform {platformName}.");
                }

                // figure out if the imported project is a plugin project
                var tmp = vcPro.ActiveConfiguration.ConfigurationName;
                var vcConfig = (vcPro.Configurations as IVCCollection)?.Item(tmp)
                    as VCConfiguration;
                var def = CompilerToolWrapper.Create(vcConfig)?.GetPreprocessorDefinitions();
                if (!string.IsNullOrEmpty(def) && def.IndexOf("QT_PLUGIN", IgnoreCase) > -1)
                    project.MarkAsQtPlugin();

                ApplyPostImportSteps(project);
            } catch (Exception e) {
                Messages.DisplayCriticalErrorMessage($"{e} (Maybe the.vcxproj or.sln file is corrupt?)");
            }
        }

        private static void ImportQMakeSolution(FileInfo solutionFile, VersionInformation vi)
        {
            var projects = ParseProjectsFromSolution(solutionFile);
            foreach (var project in projects.Select(project => new FileInfo(project)))
                if (!ImportQMakeProject(project, vi))
                    Messages.Print($"Could not convert project file {project.Name} to Qt/MSBuild.");
        }

        private static IEnumerable<string> ParseProjectsFromSolution(FileInfo solutionFile)
        {
            string content;
            using (var sr = solutionFile.OpenText()) {
                content = sr.ReadToEnd();
                sr.Close();
            }

            var projects = new List<string>();
            var index = content.IndexOf(ProjectFileExtension, IgnoreCase);
            while (index != -1) {
                var startIndex = content.LastIndexOf('\"', index, index) + 1;
                var endIndex = content.IndexOf('\"', index);
                projects.Add(content.Substring(startIndex, endIndex - startIndex));
                content = content.Substring(endIndex);
                index = content.IndexOf(ProjectFileExtension, IgnoreCase);
            }
            return projects;
        }

        private static bool ImportQMakeProject(FileInfo projectFile, VersionInformation vi)
        {
            var xmlProject = MsBuildProjectReaderWriter.Load(projectFile.FullName);
            if (xmlProject == null)
                return false;
            var oldVersion = xmlProject.GetProjectFormatVersion();
            switch (oldVersion) {
            case MsBuildProjectFormat.Version.Latest:
                return true; // Nothing to do!
            case MsBuildProjectFormat.Version.Unknown or > MsBuildProjectFormat.Version.Latest:
                return false; // Nothing we can do!
            }

            xmlProject.ReplacePath(vi.qtDir, "$(QTDIR)");
            xmlProject.ReplacePath(projectFile.DirectoryName, ".");

            var ok = xmlProject.ConvertCustomBuildToQtMsBuild();
            if (ok)
                ok = xmlProject.EnableMultiProcessorCompilation();
            if (ok)
                ok = xmlProject.SetDefaultWindowsSDKVersion(BuildConfig.WindowsTargetPlatformVersion);
            if (ok)
                ok = xmlProject.UpdateProjectFormatVersion(oldVersion);

            if (ok) {
                xmlProject.Save();
                // Initialize Qt variables
                xmlProject.BuildTarget("QtVarsDesignTime");
            }
            return ok;
        }

        private static void ApplyPostImportSteps(MsBuildProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            RemoveResFilesFromGeneratedFilesFilter(project);
            // collapse the generated files/resources filters afterwards
            CollapseFilter(project.VcProject, FakeFilter.ResourceFiles().Name);
            CollapseFilter(project.VcProject, FakeFilter.GeneratedFiles().Name);

            try {
                // save the project after modification
                project.VcProject.Save();
            } catch { /* ignore */ }
        }

        private static FileInfo RunQmake(FileInfo mainInfo, string ext, bool recursive, VersionInformation vi)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var name = mainInfo.Name.Remove(mainInfo.Name.IndexOf('.'));

            var vcxproj = new FileInfo(mainInfo.DirectoryName + Path.DirectorySeparatorChar
                + name + ext);
            if (vcxproj.Exists) {
                var result = MessageBox.Show($"{vcxproj.Name} already exists. Select 'OK' to "
                    + "regenerate the file or 'Cancel' to quit importing the project.",
                    "Project file already exists.",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                    return null;
            }

            Messages.Print($"--- (Import): Generating new project of {mainInfo.Name} file");

            var waitDialog = WaitDialog.Start("Open Qt Project File",
                "Generating Visual Studio project...", delay: 2);

            var qmake = new QMakeImport(vi, mainInfo.FullName, recursiveRun: recursive, dte: _dteObject);
            var exitCode = qmake.Run(setVCVars: true);

            waitDialog.Stop();

            return exitCode == 0 ? vcxproj : null;
        }

        private static bool CheckQtVersion(VersionInformation vi)
        {
            if (vi.qtMajor >= 5)
                return true;
            Messages.DisplayWarningMessage("QMake has generated a .vcproj file, but it needs "
                + "to be converted. To do this you must open and edit the.vcproj file manually. "
                + "(Reason: QMake in Qt versions prior Qt5 does not support proper generation of "
                + "Visual Studio .vcxproj files)");
            return false;
        }

        #region ProjectExporter

        private static List<string> ConvertFilesToFullPath(IEnumerable<string> files,
            MsBuildProject project)
        {
            return new List<string>(files.Select(file => new FileInfo(file.IndexOf(':') != 1
                    ? Path.Combine(project?.VcProjectDirectory ?? "", file) : file)
                ).Select(fi => fi.FullName)
            );
        }

        private static VCFilter BestMatch(string path, IDictionary pathFilterTable)
        {
            var bestMatch = ".";
            if (path.StartsWith(".\\", IgnoreCase))
                path = path.Substring(2);
            foreach (string p in pathFilterTable.Keys) {
                var best = 0;
                for (var i = 0; i < path.Length; ++i) {
                    if (i < p.Length && path[i] == p[i])
                        ++best;
                    else
                        break;
                }
                if (best > bestMatch.Length && p.Length == best)
                    bestMatch = p;
            }
            return pathFilterTable[bestMatch] as VCFilter;
        }

        private static void CollectFilters(VCFilter filter, string path,
            ref Dictionary<VCFilter, string> filterPathTable,
            ref Dictionary<string, VCFilter> pathFilterTable)
        {
            if (filter == null)
                return;

            path = path is null ? "." : Path.Combine(path, filter.Name);

            path = path.ToUpperInvariant().Trim();
            path = Regex.Replace(path, @"\\+\.?\\+", "\\");
            path = Regex.Replace(path, @"\\\.?$", "");
            if (path.StartsWith(".\\", IgnoreCase))
                path = path.Substring(2);
            filterPathTable.Add(filter, path);
            pathFilterTable.Add(path, filter);
            foreach (VCFilter f in (IVCCollection)filter.Filters)
                CollectFilters(f, path, ref filterPathTable, ref pathFilterTable);
        }

        private static void SyncIncludeFiles(MsBuildProject project, IReadOnlyCollection<string> priFiles,
            IReadOnlyCollection<string> projFiles, bool flat, FakeFilter fakeFilter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var cmpPriFilesDict = new Dictionary<string, string>(priFiles.Count);
            foreach (var priFile in priFiles) {
                var normalized = HelperFunctions.NormalizeFilePath(priFile);
                cmpPriFilesDict.Add(normalized.ToUpperInvariant(), normalized);
            }

            var cmpProjFilesDict = new Dictionary<string, string>(projFiles.Count);
            foreach (var projFile in projFiles) {
                var normalized = HelperFunctions.NormalizeFilePath(projFile);
                cmpProjFilesDict.Add(normalized.ToUpperInvariant(), normalized);
            }

            var filterPathTable = new Dictionary<VCFilter, string>(17);
            var pathFilterTable = new Dictionary<string, VCFilter>(17);
            if (!flat && fakeFilter is not null) {
                var rootFilter = project.VcProject.FilterFromGuid(fakeFilter);
                if (rootFilter is null)
                    AddFilterToProject(project, FakeFilter.SourceFiles());

                CollectFilters(rootFilter, null, ref filterPathTable, ref pathFilterTable);
            }

            // first check for new files
            foreach (var pair in cmpPriFilesDict.Where(pair => !cmpProjFilesDict.ContainsKey(pair.Key))) {
                var file = pair.Value;
                if (flat) {
                    project.VcProject.AddFile(file);
                    continue; // the file is not in the project
                }

                var path = HelperFunctions.GetRelativePath(project.VcProjectDirectory, file);
                if (path.StartsWith(".\\", IgnoreCase))
                    path = path.Substring(2);

                var i = path.LastIndexOf(Path.DirectorySeparatorChar);
                path = i > -1 ? path.Substring(0, i) : ".";

                if (pathFilterTable.ContainsKey(path)) {
                    if (pathFilterTable[path] is {} vcFilter)
                        vcFilter.AddFile(file);
                    continue;
                }

                var filter = BestMatch(path, pathFilterTable);

                var filterDir = filterPathTable[filter];
                var name = path;
                if (!name.StartsWith("..", IgnoreCase) && name.StartsWith(filterDir, IgnoreCase))
                    name = name.Substring(filterDir.Length + 1);

                if (filter.AddFilter(name) is not VCFilter newFilter)
                    continue;

                newFilter.AddFile(file);
                filterPathTable.Add(newFilter, path);
                pathFilterTable.Add(path, newFilter);
            }

            // then check for deleted files
            foreach (var file in cmpProjFilesDict) {
                if (!cmpPriFilesDict.ContainsKey(file.Key))
                    continue;
                // the file is not in the pri file
                // (only removes it from the project, does not del. the file)
                var info = new FileInfo(file.Value);
                RemoveFileInProject(project, file.Value);
                Messages.Print($"--- (Importing .pri file) file: {info.Name} does not exist in "
                    + $".pri file, move to {project.VcProjectDirectory} Deleted");
            }
        }

        #endregion

        #region HelperFunctions

        private static VCProject ProjectFromSolution(string fullName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return HelperFunctions.ProjectsInSolution(_dteObject).FirstOrDefault(
                p => p.ProjectFile.Equals(new FileInfo(fullName).FullName, IgnoreCase));
        }

        /// <summary>
        /// Reads lines from a .pro file that is opened with a StreamReader
        /// and concatenates strings that end with a backslash.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns>the composite string</returns>
        private static string ReadProFileLine(TextReader streamReader)
        {
            var line = streamReader.ReadLine();
            if (line is null)
                return null;

            line = line.TrimEnd(' ', '\t');
            while (line.EndsWith("\\", IgnoreCase)) {
                line = line.Remove(line.Length - 1);
                var appendix = streamReader.ReadLine();
                if (appendix is not null)
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

                while (ReadProFileLine(sr) is {} line) {
                    line = line.Replace(" ", string.Empty).Replace("\t", string.Empty);
                    if (line.StartsWith("TEMPLATE", StringComparison.Ordinal))
                        return line.StartsWith("TEMPLATE=subdirs", StringComparison.Ordinal);
                }
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
            } finally {
                sr?.Dispose();
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
            if (subItems is not null) {
                foreach (UIHierarchyItem innerItem in subItems) {
                    if (innerItem.UIHierarchyItems.Count <= 0)
                        continue;

                    CollapseFilter(innerItem, hierarchy);
                    if (!innerItem.UIHierarchyItems.Expanded)
                        continue;

                    innerItem.UIHierarchyItems.Expanded = false;
                    if (!innerItem.UIHierarchyItems.Expanded)
                        continue;

                    innerItem.Select(vsUISelectionType.vsUISelectionTypeSelect);
                    hierarchy.DoDefaultAction();
                }
            }

            if (!item.UIHierarchyItems.Expanded)
                return;

            item.UIHierarchyItems.Expanded = false;
            if (!item.UIHierarchyItems.Expanded)
                return;

            item.Select(vsUISelectionType.vsUISelectionTypeSelect);
            hierarchy.DoDefaultAction();
        }

        /// <summary>
        /// Removes a file reference from the project and moves the file to the "Deleted" folder.
        /// </summary>
        /// <param name="qtPro"></param>
        /// <param name="fileName"></param>
        private static void RemoveFileInProject(MsBuildProject qtPro, string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            fileName = new FileInfo(fileName).FullName;
            foreach (VCFile vcFile in (IVCCollection)qtPro.VcProject.Files) {
                if (!vcFile.FullPath.Equals(fileName, IgnoreCase))
                    continue;
                qtPro.VcProject.RemoveFile(vcFile);
                MoveFileToDeletedFolder(qtPro.VcProject, fileName);
            }
        }

        /// <summary>
        /// Translates the machine type given as command line argument to the linker
        /// to the internal enum type VCProjectEngine.machineTypeOption.
        /// </summary>
        private static machineTypeOption TranslateMachineType(string cmdLineMachine)
        {
            return cmdLineMachine.ToUpper() switch
            {
                "AM33" => machineTypeOption.machineAM33,
                "X64" => machineTypeOption.machineAMD64,
                "ARM" => machineTypeOption.machineARM,
                "EBC" => machineTypeOption.machineEBC,
                "IA-64" => machineTypeOption.machineIA64,
                "M32R" => machineTypeOption.machineM32R,
                "MIPS" => machineTypeOption.machineMIPS,
                "MIPS16" => machineTypeOption.machineMIPS16,
                "MIPSFPU" => machineTypeOption.machineMIPSFPU,
                "MIPSFPU16" => machineTypeOption.machineMIPSFPU16,
                "MIPS41XX" => machineTypeOption.machineMIPSR41XX,
                "SH3" => machineTypeOption.machineSH3,
                "SH3DSP" => machineTypeOption.machineSH3DSP,
                "SH4" => machineTypeOption.machineSH4,
                "SH5" => machineTypeOption.machineSH5,
                "THUMB" => machineTypeOption.machineTHUMB,
                "X86" => machineTypeOption.machineX86,
                _ => machineTypeOption.machineNotSet
            };
        }

        #endregion

        #region QtProject

        private static void MoveFileToDeletedFolder(VCProject vcPro, string fileName)
        {
            var srcFile = new FileInfo(fileName ?? "");
            if (!srcFile.Exists)
                return;

            var destFolder = vcPro.ProjectDirectory + "\\Deleted\\";
            var destName = destFolder + srcFile.Name.Replace(".", "_") + ".bak";
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
                Messages.DisplayWarningMessage(e, "1. Maybe your deleted folder is full."
                    + Environment.NewLine + "2. Or maybe it's write protected.");
            }
        }

        private static void AddFilterToProject(MsBuildProject project, FakeFilter fakeFilter)
        {
            try {
                var vcFilter = project.VcProject.FilterFromGuid(fakeFilter);
                if (vcFilter is not null)
                    return;

                if (!project.VcProject.CanAddFilter(fakeFilter.Name)) {
                    vcFilter = project.VcProject.FilterFromName(fakeFilter);
                    if (vcFilter is null)
                        throw new QtVSException($"Project cannot add filter {fakeFilter.Name}.");
                } else {
                    vcFilter = (VCFilter)project.VcProject.AddFilter(fakeFilter.Name);
                }

                vcFilter.UniqueIdentifier = fakeFilter.UniqueIdentifier;
                vcFilter.Filter = fakeFilter.Filter;
                vcFilter.ParseFiles = fakeFilter.ParseFiles;
            } catch {
                throw new QtVSException("Cannot add a resource filter.");
            }
        }

        private static void RemoveResFilesFromGeneratedFilesFilter(MsBuildProject pro)
        {
            var generatedFiles = pro.VcProject.FilterFromGuid(FakeFilter.GeneratedFiles());
            if (generatedFiles?.Files is not IVCCollection files)
                return;

            for (var i = files.Count - 1; i >= 0; --i) {
                if (files.Item(i) is VCFile vcFile && vcFile.FullPath.EndsWith(".res", IgnoreCase))
                    vcFile.Remove();
            }
        }

        private static void CollapseFilter(VCProject project, string filterName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionExplorer = (UIHierarchy)_dteObject.Windows.Item(Constants.vsext_wk_SProjectWindow).Object;
            if (solutionExplorer.UIHierarchyItems.Count == 0)
                return;

            _dteObject.SuppressUI = true;
            var projectItem = FindProjectHierarchyItem(project, solutionExplorer);
            if (projectItem is not null)
                CollapseFilter(projectItem, solutionExplorer, filterName);
            _dteObject.SuppressUI = false;
        }

        private static UIHierarchyItem FindProjectHierarchyItem(VCProject project, UIHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy.UIHierarchyItems.Count == 0)
                return null;

            var solution = hierarchy.UIHierarchyItems.Item(1);
            UIHierarchyItem projectItem = null;
            foreach (UIHierarchyItem solutionItem in solution.UIHierarchyItems) {
                projectItem = FindProjectHierarchyItem(project, solutionItem);
                if (projectItem is not null)
                    break;
            }
            return projectItem;
        }

        private static UIHierarchyItem FindProjectHierarchyItem(VCProject project, UIHierarchyItem root)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            UIHierarchyItem projectItem = null;
            try {
                if (root.Name == project.Name)
                    return root;

                foreach (UIHierarchyItem childItem in root.UIHierarchyItems) {
                    projectItem = FindProjectHierarchyItem(project, childItem);
                    if (projectItem is not null)
                        break;
                }
            } catch {}
            return projectItem;
        }

        private static bool HasPlatform(VCProject vcPro, string platformName)
        {
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations) {
                var platform = (VCPlatform)config.Platform;
                if (platform.Name == platformName)
                    return true;
            }
            return false;
        }

        private static bool SelectSolutionPlatform(string platformName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionBuild = _dteObject.Solution.SolutionBuild;
            foreach (SolutionConfiguration solutionCfg in solutionBuild.SolutionConfigurations) {
                if (solutionCfg.Name != solutionBuild.ActiveConfiguration.Name)
                    continue;

                var contexts = solutionCfg.SolutionContexts;
                for (var i = 1; i <= contexts.Count; ++i) {
                    try {
                        if (contexts.Item(i).PlatformName != platformName)
                            continue;
                        solutionCfg.Activate();
                        return true;
                    } catch {
                        // This may happen if we encounter an unloaded project.
                    }
                }
            }
            return false;
        }

        private static void CreatePlatform(VCProject vcPro, string oldPlatform,
            string newPlatform, VersionInformation viOld, VersionInformation viNew)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var envPro = vcPro.Object as Project;
            try {
                var cfgMgr = envPro.ConfigurationManager;
                cfgMgr.AddPlatform(newPlatform, oldPlatform, true);
                vcPro.AddPlatform(newPlatform);
            } catch {
                // That stupid ConfigurationManager can't handle platform names
                // containing dots (e.g. "Windows Mobile 5.0 Pocket PC SDK (ARMV4I)")
                // So we have to do it the nasty way...
                var projectFileName = envPro.FullName;
                envPro.Save(null);
                _dteObject.Solution.Remove(envPro);
                AddPlatformToVcProj(projectFileName, oldPlatform, newPlatform);
                envPro = _dteObject.Solution.AddFromFile(projectFileName);
                vcPro = (VCProject)envPro.Object;
            }

            // update the platform settings
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations) {
                var platform = (VCPlatform)config.Platform;
                if (platform.Name != newPlatform)
                    continue;
                if (viOld != null)
                    RemovePlatformDependencies(config, viOld);
                SetupConfiguration(config, viNew);
            }

            SelectSolutionPlatform(newPlatform);
        }

        private static void RemovePlatformDependencies(VCConfiguration config, VersionInformation viOld)
        {
            if (CompilerToolWrapper.Create(config) is not {} compiler)
                return;

            var defines = new HashSet<string>(compiler.PreprocessorDefinitions);
            defines.ExceptWith(viOld.GetQMakeConfEntry("DEFINES").Split(' ', '\t'));
            compiler.SetPreprocessorDefinitions(string.Join(",", defines));

        }

        private static void SetupConfiguration(VCConfiguration config, VersionInformation viNew)
        {
            if (CompilerToolWrapper.Create(config) is {} compiler) {
                var defines = new HashSet<string>(compiler.PreprocessorDefinitions);
                defines.UnionWith(viNew.GetQMakeConfEntry("DEFINES").Split(' ', '\t'));
                compiler.SetPreprocessorDefinitions(string.Join(",", defines));
            }

            if ((config?.Tools as IVCCollection)?.Item("VCLinkerTool") is not VCLinkerTool linker)
                return;
            linker.SubSystem = subSystemOption.subSystemWindows;
            SetTargetMachine(linker, viNew);
        }

        private static void AddPlatformToVcProj(string projectFileName, string oldPlatformName,
            string newPlatformName)
        {
            var tempFileName = Path.GetTempFileName();
            var fi = new FileInfo(projectFileName);
            fi.CopyTo(tempFileName, true);

            var myXmlDocument = new XmlDocument();
            myXmlDocument.Load(tempFileName);
            AddPlatformToVcProj(myXmlDocument, oldPlatformName, newPlatformName);
            myXmlDocument.Save(projectFileName);

            fi = new FileInfo(tempFileName);
            fi.Delete();
        }

        private static void AddPlatformToVcProj(XmlDocument doc, string oldPlatformName,
            string newPlatformName)
        {
            var vsProj = doc.DocumentElement?.SelectSingleNode("/VisualStudioProject");
            var platforms = vsProj?.SelectSingleNode("Platforms");
            if (platforms == null) {
                platforms = doc.CreateElement("Platforms");
                vsProj?.AppendChild(platforms);
            }
            var platform = platforms.SelectSingleNode("Platform[@Name='" + newPlatformName + "']");
            if (platform == null) {
                platform = doc.CreateElement("Platform");
                ((XmlElement)platform).SetAttribute("Name", newPlatformName);
                platforms.AppendChild(platform);
            }

            var configurations = vsProj?.SelectSingleNode("Configurations");
            var cfgList = configurations?.SelectNodes("Configuration[@Name='Debug|"
                + oldPlatformName + "'] | " + "Configuration[@Name='Release|"
                + oldPlatformName + "']");
            if (cfgList != null) {
                foreach (XmlNode oldCfg in cfgList) {
                    var newCfg = (XmlElement)oldCfg.Clone();
                    newCfg.SetAttribute("Name",
                        oldCfg.Attributes?["Name"].Value.Replace(oldPlatformName, newPlatformName));
                    configurations.AppendChild(newCfg);
                }
            }

            const string fileCfgPath = "Files/Filter/File/FileConfiguration";
            var fileCfgList = vsProj?.SelectNodes(fileCfgPath + "[@Name='Debug|"
                + oldPlatformName + "'] | " + fileCfgPath + "[@Name='Release|"
                + oldPlatformName + "']");
            if (fileCfgList == null)
                return;
            foreach (XmlNode oldCfg in fileCfgList) {
                var newCfg = (XmlElement)oldCfg.Clone();
                newCfg.SetAttribute("Name",
                    oldCfg.Attributes?["Name"].Value.Replace(oldPlatformName, newPlatformName));
                oldCfg.ParentNode?.AppendChild(newCfg);
            }
        }

        private static void SetTargetMachine(VCLinkerTool linker, VersionInformation versionInfo)
        {
            var qMakeLFlagsWindows = versionInfo.GetQMakeConfEntry("QMAKE_LFLAGS_WINDOWS");
            var rex = new Regex("/MACHINE:(\\S+)");
            var match = rex.Match(qMakeLFlagsWindows);
            if (match.Success) {
                linker.TargetMachine = TranslateMachineType(match.Groups[1].Value);
            } else {
                var platformName = versionInfo.GetVSPlatformName();
                linker.TargetMachine = platformName switch
                {
                    "Win32" => machineTypeOption.machineX86,
                    "x64" => machineTypeOption.machineAMD64,
                    _ => machineTypeOption.machineNotSet
                };
            }

            var subsystemOption = string.Empty;
            var linkerOptions = linker.AdditionalOptions ?? string.Empty;

            rex = new Regex("(/SUBSYSTEM:\\S+)");
            match = rex.Match(qMakeLFlagsWindows);
            if (match.Success)
                subsystemOption = match.Groups[1].Value;

            match = rex.Match(linkerOptions);
            if (match.Success) {
                linkerOptions = rex.Replace(linkerOptions, subsystemOption);
            } else {
                if (linkerOptions.Length > 0)
                    linkerOptions += " ";
                linkerOptions += subsystemOption;
            }
            linker.AdditionalOptions = linkerOptions;
        }

        #endregion

        #region ExtLoader

        private static string GetQtInstallPath()
        {
            var qtVersion = QtVersionManager.GetDefaultVersion();
            var path = QtVersionManager.GetInstallPath(qtVersion);

            if (path is null)
                Messages.DisplayErrorMessage("Cannot find qmake. Make sure you have specified a Qt version.");
            return path;
        }

        private static List<string> ResolveFilesFromQMake(IEnumerable<string> files,
            MsBuildProject project, string path)
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

        private static string ResolveEnvironmentVariables(string str, MsBuildProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var reg = new Regex(@"\$\(([^\s\(\)]+)\)");
            var col = reg.Matches(str);
            for (var i = 0; i < col.Count; ++i) {
                var env = col[i].Groups[1].ToString();
                string val;
                if (env == "QTDIR") {
                    val = project?.InstallPath ?? Environment.GetEnvironmentVariable(env);
                } else {
                    val = Environment.GetEnvironmentVariable(env);
                }
                if (val is null)
                    return null;
                str = str.Replace($"$({env})", val);
            }
            return str;
        }

        #endregion
    }
}
