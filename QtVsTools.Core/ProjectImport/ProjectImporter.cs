// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

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
    using Common;
    using MsBuild;
    using static Common.Utils;

    public static class ProjectImporter
    {
        private static DTE _dteObject;
        private const string ProjectFileExtension = ".vcxproj";

        private static bool Setup(EnvDTE.DTE dte)
        {
            _dteObject ??= dte;
            return _dteObject != null;
        }

        private static FileInfo OpenFileDialog(string title, string filter, string file, string ext)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = file,
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return null;

            var fileInfo = new FileInfo(openFileDialog.FileName);
            if (string.Equals(fileInfo.Extension, ext, IgnoreCase))
                return fileInfo;
            Messages.Print($"The selected file was not a {ext} file.");
            return null;
        }

        public static void ImportProFile(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            const string noQt = "Cannot find qmake. Make sure you have specified a "
                + "default Qt version.";
            const string wrongVersion = "The default Qt version to import the .pro file is no "
                + "longer supported. To import the file, please use Qt 5.0 or later as default.";
            if (!Setup(dte) || GetQtInstallPath("$(DefaultQtVersion)", noQt, wrongVersion) == null)
                return;

            var proFile = OpenFileDialog("Select a Qt Project to Add to the Solution",
                "Qt Project files (*.pro)|*.pro", "", ".pro");
            if (proFile == null)
                return;

            const string subDirsMessage = "You selected a SUBDIRS .pro file. To open this file, "
                + "the existing solution needs to be closed (pending changes will be saved).";

            switch (GetTemplateType(File.ReadLines(proFile.FullName))) {
            case TemplateType.SubDirs:
                var result = DialogResult.None;
                if (!string.IsNullOrEmpty(_dteObject.Solution.FullName)) {
                    result = MessageBox.Show(subDirsMessage, "Open Solution",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                } else if (HelperFunctions.ProjectsInSolution(_dteObject).Count > 0) {
                    result = MessageBox.Show(subDirsMessage, "Open Solution",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                }

                if (result != DialogResult.OK)
                    return;
                _dteObject.Solution.Close(true);
                ImportSolution(proFile, QtVersionManager.GetDefaultVersion());
                break;
            case TemplateType.App:
            case TemplateType.Lib:
            case TemplateType.VcApp:
            case TemplateType.VcLib:
            case TemplateType.Unknown: // no TEMPLATE type defaults to 'app'
                ImportProject(proFile, QtVersionManager.GetDefaultVersion());
                break;
            case TemplateType.Aux:
                Messages.Print("Unsupported TEMPLATE type 'aux' in .pro file.");
                return;
            }
        }

        public static void ImportPriFile(DTE dte, string pkgInstallPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Setup(dte) || HelperFunctions.GetSelectedQtProject(dte) is not {} project)
                return;

            const string notQt = "Cannot find qmake. Make sure you have specified installed the "
                + "project's Qt version.";
            const string wrongVersion = "The project's Qt version is no longer supported. To "
                + "import the .pri file, please use Qt 5.0 or later for your project.";
            if (GetQtInstallPath(project.QtVersion, notQt, wrongVersion) is not {} qtDir)
                return;

            var priFile = OpenFileDialog("Import from .pri File",
                "Qt Project Include Files (*.pri)|*.pri",
                $"{project.VcProjectDirectory}{project.VcProject.Name}.pri", ".pri");
            if (priFile == null)
                return;

            var qmake = new QMakeWrapper
            {
                QtDir = qtDir,
                PkgInstallPath = pkgInstallPath
            };
            if (!qmake.ReadFile(priFile.FullName)) {
                Messages.Print($"--- (Importing .pri file) file: {priFile} could not be read.");
                return;
            }

            var tuples = new List<(string[] Files, FilesToList FilesToList, FakeFilter Filter)>
            {
                (qmake.SourceFiles, FilesToList.FL_CppFiles, FakeFilter.SourceFiles()),
                (qmake.HeaderFiles, FilesToList.FL_HFiles, FakeFilter.HeaderFiles()),
                (qmake.FormFiles, FilesToList.FL_UiFiles, FakeFilter.FormFiles()),
                (qmake.ResourceFiles, FilesToList.FL_Resources, FakeFilter.ResourceFiles())
            };

            var directoryName = priFile.DirectoryName;
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

            var versionInfo = VersionInformation.GetOrAddByName(qtVersion);
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
                var properties = new Dictionary<string, string>()
                {
                    {"Operation", typeof(ProjectImporter).FullName + ".ImportSolution"}
                };
                Telemetry.TrackException(e, properties);
                Messages.DisplayErrorMessage(e);
            }
        }

        private static void ImportProject(FileInfo mainInfo, string qtVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var versionInfo = VersionInformation.GetOrAddByName(qtVersion);
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

                var platformName = versionInfo.VsPlatformName;
                if (!SelectSolutionPlatform(platformName) || !HasPlatform(vcPro, platformName)) {
                    CreatePlatform(vcPro, "Win32", platformName, versionInfo);
                    if (!SelectSolutionPlatform(platformName))
                        Messages.Print($"Can't select the platform {platformName}.");
                }

                // figure out if the imported project is a plugin project
                var tmp = vcPro?.ActiveConfiguration.ConfigurationName;
                var vcConfig = (vcPro?.Configurations as IVCCollection)?.Item(tmp)
                    as VCConfiguration;
                var def = CompilerToolWrapper.Create(vcConfig)?.GetPreprocessorDefinitions();
                if (!string.IsNullOrEmpty(def) && def.IndexOf("QT_PLUGIN", IgnoreCase) > -1)
                    project.MarkAsQtPlugin();

                ApplyPostImportSteps(project);
            } catch (Exception e) {
                var properties = new Dictionary<string, string>()
                {
                    {"Operation", typeof(ProjectImporter).FullName + ".ImportProject"}
                };
                Telemetry.TrackException(e, properties);
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
            if (vi == null)
                return false;
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

            xmlProject.ReplacePath(vi.QtDir, "$(QTDIR)");
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
            // collapse the generated files/resources filters afterward
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

            var exitCode = QMakeImport.Run(vi, mainInfo.FullName, recursiveRun: recursive);

            waitDialog.Stop();

            return exitCode == 0 ? vcxproj : null;
        }

        private static bool CheckQtVersion(VersionInformation vi)
        {
            if (vi.Major >= 5)
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

                if (pathFilterTable.TryGetValue(path, out var vcFilter)) {
                    vcFilter?.AddFile(file);
                    continue;
                }

                if (BestMatch(path, pathFilterTable) is not {} filter)
                    continue;
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
                p => string.Equals(p.ProjectFile, new FileInfo(fullName).FullName, IgnoreCase));
        }

        private enum TemplateType
        {
            Unknown,
            App,
            Lib,
            SubDirs,
            Aux,
            VcApp,
            VcLib
        }

        /// <summary>
        /// Reads a .pro file content from a string array or list and returns the template type.
        /// </summary>
        /// <param name="fileContent">Strings representing the lines in a .pro file</param>
        /// <returns>The template type based on the TEMPLATE variable</returns>
        private static TemplateType GetTemplateType(IEnumerable<string> fileContent)
        {
            var concatenatedLine = "";
            foreach (var lineContent in fileContent) {
                var line = lineContent.Trim();
                if (line.EndsWith("\\")) {
                    concatenatedLine += line.TrimEnd('\\').Trim() + " ";
                    continue;
                }

                concatenatedLine += line.Trim();

                if (concatenatedLine.StartsWith("TEMPLATE", IgnoreCase)) {
                    var parts = concatenatedLine.Split('=');
                    if (parts.Length == 2) {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();

                        if (string.Equals(key, "TEMPLATE", IgnoreCase)) {
                            return value.ToLower() switch
                            {
                                "app" => TemplateType.App,
                                "lib" => TemplateType.Lib,
                                "subdirs" => TemplateType.SubDirs,
                                _ => TemplateType.Unknown
                            };
                        }
                    }
                }
                concatenatedLine = "";
            }
            return TemplateType.Unknown;
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
                if (!string.Equals(vcFile.FullPath, fileName, IgnoreCase))
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

                var name = fakeFilter.Name;
                if (!project.VcProject.CanAddFilter(name)) {
                    vcFilter = project.VcProject.FilterFromName(fakeFilter);
                    if (vcFilter is null)
                        throw new InvalidOperationException($"Project cannot add filter {name}.");
                } else {
                    vcFilter = (VCFilter)project.VcProject.AddFilter(name);
                }

                vcFilter.UniqueIdentifier = fakeFilter.UniqueIdentifier;
                vcFilter.Filter = fakeFilter.Filter;
                vcFilter.ParseFiles = fakeFilter.ParseFiles;
            } catch {
                throw new InvalidOperationException("Cannot add a resource filter.");
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
            } catch {
                // ignored
            }

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
            string newPlatform, VersionInformation versionInfo)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var envPro = vcPro.Object as Project;
            try {
                var cfgMgr = envPro?.ConfigurationManager;
                cfgMgr?.AddPlatform(newPlatform, oldPlatform, true);
                vcPro.AddPlatform(newPlatform);
            } catch {
                // That stupid ConfigurationManager can't handle platform names
                // containing dots (e.g. "Windows Mobile 5.0 Pocket PC SDK (ARMV4I)")
                // So we have to do it the nasty way...
                var projectFileName = envPro?.FullName;
                envPro?.Save(null);
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
                SetupConfiguration(config, versionInfo);
            }

            SelectSolutionPlatform(newPlatform);
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

            Utils.DeleteFile(tempFileName);
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
                var platformName = versionInfo.VsPlatformName;
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

        private static string GetQtInstallPath(string qtVersion, string noQt, string wrongVersion)
        {
            if (VersionInformation.GetOrAddByName(qtVersion) is {} vi) {
                if (vi.Major >= 5)
                    return vi.InstallPrefix;
                Messages.DisplayErrorMessage(wrongVersion);
            } else {
                Messages.DisplayErrorMessage(noQt);
            }
            return null;
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
