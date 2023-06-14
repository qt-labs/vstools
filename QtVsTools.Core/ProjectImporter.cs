/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
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
    using static Utils;

    public class ProjectImporter
    {
        private readonly DTE dteObject;

        private const string ProjectFileExtension = ".vcxproj";

        public ProjectImporter(DTE dte)
        {
            dteObject = dte;
        }

        public void ImportProFile(string qtVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                if (!string.IsNullOrEmpty(dteObject.Solution.FullName)
                    || HelperFunctions.ProjectsInSolution(dteObject).Count > 0) {
                    if (MessageBox.Show("This seems to be a SUBDIRS .pro file. To open this file, "
                        + "the existing solution needs to be closed (pending changes will be saved).",
                        "Open Solution", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
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
            var vcInfo = RunQmake(mainInfo, ".sln", true, versionInfo);
            if (null == vcInfo)
                return;
            ImportQMakeSolution(vcInfo, versionInfo);

            try {
                if (CheckQtVersion(versionInfo)) {
                    dteObject.Solution.Open(vcInfo.FullName);
                    if (qtVersion is not null) {
                        foreach (var prj in HelperFunctions.ProjectsInSolution(dteObject)) {
                            QtVersionManager.The().SaveProjectQtVersion(prj, qtVersion);
                            ApplyPostImportSteps(dteObject, QtProject.Create(prj));
                        }
                    }
                }

                Messages.Print($"--- (Import): Finished opening {vcInfo.Name}");
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
            }
        }

        private void ImportProject(FileInfo mainInfo, string qtVersion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var versionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
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
                var pro = ProjectFromSolution(dteObject, fullName);
                if (pro is null) {
                    try {
                        pro = dteObject.Solution.AddFromFile(fullName);
                    } catch (Exception /*exception*/) {
                        Messages.Print("--- (Import): Generated project could not be loaded.");
                        Messages.Print("--- (Import): Please look in the output above for errors and warnings.");
                        return;
                    }
                    Messages.Print($"--- (Import): Added {vcInfo.Name} to Solution");
                } else {
                    Messages.Print("Project already in Solution");
                }

                if (QtProject.Create(pro) is not {} qtPro)
                    return;
                var platformName = versionInfo.GetVSPlatformName();

                if (qtVersion is not null)
                    QtVersionManager.The().SaveProjectQtVersion(pro, qtVersion, platformName);

                var vcPro = qtPro.VcProject;
                if (!SelectSolutionPlatform(dteObject, platformName)
                    || !HasPlatform(vcPro, platformName)) {
                    CreatePlatform(pro, vcPro, dteObject, "Win32", platformName, null, versionInfo);
                    if (!SelectSolutionPlatform(dteObject, platformName))
                        Messages.Print($"Can't select the platform {platformName}.");
                }

                // figure out if the imported project is a plugin project
                var tmp = pro.ConfigurationManager.ActiveConfiguration.ConfigurationName;
                var vcConfig = (vcPro.Configurations as IVCCollection)?.Item(tmp)
                    as VCConfiguration;
                var def = CompilerToolWrapper.Create(vcConfig)?.GetPreprocessorDefinitions();
                if (!string.IsNullOrEmpty(def)
                    && def.IndexOf("QT_PLUGIN", IgnoreCase) > -1) {
                    QtProject.MarkAsQtPlugin(qtPro);
                }

                ApplyPostImportSteps(dteObject, qtPro);
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
            var xmlProject = MsBuildProject.Load(projectFile.FullName);
            if (xmlProject == null)
                return false;
            var oldVersion = xmlProject.GetProjectFormatVersion();
            switch (oldVersion) {
            case ProjectFormat.Version.Latest:
                return true; // Nothing to do!
            case ProjectFormat.Version.Unknown or > ProjectFormat.Version.Latest:
                return false; // Nothing we can do!
            }

            xmlProject.ReplacePath(vi.qtDir, "$(QTDIR)");
            xmlProject.ReplacePath(projectFile.DirectoryName, ".");

            var ok = xmlProject.AddQtMsBuildReferences();
            if (ok)
                ok = xmlProject.ConvertCustomBuildToQtMsBuild();
            if (ok)
                ok = xmlProject.EnableMultiProcessorCompilation();
            if (ok) {
                // Since Visual Studio 2019: WindowsTargetPlatformVersion=10.0
                // will be treated as "use latest installed Windows 10 SDK".
                // https://developercommunity.visualstudio.com/comments/407752/view.html
                const string versionWin10Sdk = "10.0";
                ok = xmlProject.SetDefaultWindowsSDKVersion(versionWin10Sdk);
            }
            if (ok)
                ok = xmlProject.UpdateProjectFormatVersion(oldVersion);

            if (ok) {
                xmlProject.Save();
                // Initialize Qt variables
                xmlProject.BuildTarget("QtVarsDesignTime");
            }
            return ok;
        }

        private static void ApplyPostImportSteps(DTE dte, QtProject qtProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            RemoveResFilesFromGeneratedFilesFilter(qtProject);
            TranslateFilterNames(qtProject.VcProject);

            // collapse the generated files/resources filters afterwards
            CollapseFilter(dte, qtProject.VcProject, Filters.ResourceFiles().Name);
            CollapseFilter(dte, qtProject.VcProject, Filters.GeneratedFiles().Name);

            try {
                // save the project after modification
                qtProject.VcProject.Save();
            } catch { /* ignore */ }
        }

        private FileInfo RunQmake(FileInfo mainInfo, string ext, bool recursive, VersionInformation vi)
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

            var qmake = new QMakeImport(vi, mainInfo.FullName, recursiveRun: recursive, dte: dteObject);
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

        public static List<string> ConvertFilesToFullPath(IEnumerable<string> files, QtProject qtProject)
        {
            return new List<string>(files.Select(file => new FileInfo(file.IndexOf(':') != 1
                    ? Path.Combine(qtProject?.VcProjectDirectory ?? "", file) : file)
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

        public static void SyncIncludeFiles(QtProject qtProject, List<string> priFiles,
            List<string> projFiles, bool flat, FakeFilter fakeFilter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var cmpPriFiles = new List<string>(priFiles.Count);
            cmpPriFiles.AddRange(priFiles.Select(s => HelperFunctions.NormalizeFilePath(s).ToUpperInvariant()));
            cmpPriFiles.Sort();

            var cmpProjFiles = new List<string>(projFiles.Count);
            cmpProjFiles.AddRange(projFiles.Select(s => HelperFunctions.NormalizeFilePath(s).ToUpperInvariant()));

            var filterPathTable = new Dictionary<VCFilter, string>(17);
            var pathFilterTable = new Dictionary<string, VCFilter>(17);
            if (!flat && fakeFilter is not null) {
                var rootFilter = qtProject.FindFilterFromGuid(fakeFilter.UniqueIdentifier);
                if (rootFilter is null)
                    AddFilterToProject(qtProject, Filters.SourceFiles());

                CollectFilters(rootFilter, null, ref filterPathTable, ref pathFilterTable);
            }

            // first check for new files
            foreach (var file in cmpPriFiles.Where(file => cmpProjFiles.IndexOf(file) <= -1)) {
                if (flat) {
                    qtProject.VcProject.AddFile(file);
                    continue; // the file is not in the project
                }

                var path = HelperFunctions.GetRelativePath(qtProject.VcProjectDirectory, file);
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
            foreach (var file in cmpProjFiles) {
                if (cmpPriFiles.IndexOf(file) != -1)
                    continue;
                // the file is not in the pri file
                // (only removes it from the project, does not del. the file)
                var info = new FileInfo(file);
                RemoveFileInProject(qtProject, file);
                Messages.Print($"--- (Importing .pri file) file: {info.Name} does not exist in "
                    + $".pri file, move to {qtProject.VcProjectDirectory} Deleted");
            }
        }

        #endregion

        #region HelperFunctions

        private static Project ProjectFromSolution(DTE dte, string fullName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool Func(Project p) => p.FullName.Equals(new FileInfo(fullName).FullName, IgnoreCase);
            return HelperFunctions.ProjectsInSolution(dte).FirstOrDefault(Func);
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
        private static void RemoveFileInProject(QtProject qtPro, string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            fileName = new FileInfo(fileName).FullName;
            foreach (VCFile vcFile in (IVCCollection)qtPro.VcProject.Files) {
                if (!vcFile.FullPath.Equals(fileName, IgnoreCase))
                    continue;
                qtPro.VcProject.RemoveFile(vcFile);
                MoveFileToDeletedFolder(qtPro.VcProject, vcFile);
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

        private static void MoveFileToDeletedFolder(VCProject vcPro, VCFile vcFile)
        {
            var srcFile = new FileInfo(vcFile.FullPath);

            if (!srcFile.Exists)
                return;

            var destFolder = vcPro.ProjectDirectory + "\\Deleted\\";
            var destName = destFolder + vcFile.Name.Replace(".", "_") + ".bak";
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

        private static void AddFilterToProject(QtProject project, FakeFilter fakeFilter)
        {
            try {
                var vcFilter = project.FindFilterFromGuid(fakeFilter.UniqueIdentifier);
                if (vcFilter is not null)
                    return;

                if (!project.VcProject.CanAddFilter(fakeFilter.Name)) {
                    vcFilter = project.FindFilterFromName(fakeFilter.Name);
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

        private static void TranslateFilterNames(VCProject vcProject)
        {
            if (vcProject.Filters is not IVCCollection filters)
                return;

            foreach (VCFilter filter in filters) {
                filter.Name = filter.Name switch
                {
                    "Form Files" => Filters.FormFiles().Name,
                    "Generated Files" => Filters.GeneratedFiles().Name,
                    "Header Files" => Filters.HeaderFiles().Name,
                    "Resource Files" => Filters.ResourceFiles().Name,
                    "Source Files" => Filters.SourceFiles().Name, _ => filter.Name
                };
            }
        }

        private static void RemoveResFilesFromGeneratedFilesFilter(QtProject pro)
        {
            var generatedFiles = pro.FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (generatedFiles?.Files is not IVCCollection files)
                return;

            for (var i = files.Count - 1; i >= 0; --i) {
                if (files.Item(i) is VCFile vcFile && vcFile.FullPath.EndsWith(".res", IgnoreCase))
                    vcFile.Remove();
            }
        }

        private static void CollapseFilter(DTE dte, VCProject project, string filterName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionExplorer = (UIHierarchy)dte.Windows.Item(Constants.vsext_wk_SProjectWindow).Object;
            if (solutionExplorer.UIHierarchyItems.Count == 0)
                return;

            dte.SuppressUI = true;
            var projectItem = FindProjectHierarchyItem(project, solutionExplorer);
            if (projectItem is not null)
                CollapseFilter(projectItem, solutionExplorer, filterName);
            dte.SuppressUI = false;
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

        private static bool SelectSolutionPlatform(DTE dte, string platformName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionBuild = dte.Solution.SolutionBuild;
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

        private static void CreatePlatform(Project envPro, VCProject vcPro, DTE dte,
            string oldPlatform, string newPlatform, VersionInformation viOld, VersionInformation viNew)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                dte.Solution.Remove(envPro);
                AddPlatformToVcProj(projectFileName, oldPlatform, newPlatform);
                envPro = dte.Solution.AddFromFile(projectFileName);
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

            SelectSolutionPlatform(dte, newPlatform);
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
    }
}
