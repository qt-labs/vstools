/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
#if VS2017
using Microsoft.Win32;
#endif
using EnvDTE;

using Process = System.Diagnostics.Process;

namespace QtVsTools.Core
{
    using Common;
    using static Utils;
    using static SyntaxAnalysis.RegExpr;

    public static class HelperFunctions
    {
        static LazyFactory StaticLazy { get; } = new LazyFactory();

        static readonly HashSet<string> _sources = new(new[] { ".c", ".cpp", ".cxx" }, CaseIgnorer);
        public static bool IsSourceFile(string fileName)
        {
            return _sources.Contains(Path.GetExtension(fileName));
        }

        static readonly HashSet<string> _headers = new(new[] { ".h", ".hpp", ".hxx" }, CaseIgnorer);
        public static bool IsHeaderFile(string fileName)
        {
            return _headers.Contains(Path.GetExtension(fileName));
        }

        public static bool IsUicFile(string fileName)
        {
            return ".ui".Equals(Path.GetExtension(fileName), IgnoreCase);
        }

        public static bool IsMocFile(string fileName)
        {
            return ".moc".Equals(Path.GetExtension(fileName), IgnoreCase);
        }

        public static bool IsQrcFile(string fileName)
        {
            return ".qrc".Equals(Path.GetExtension(fileName), IgnoreCase);
        }

        public static bool IsWinRCFile(string fileName)
        {
            return ".rc".Equals(Path.GetExtension(fileName), IgnoreCase);
        }

        public static bool IsTranslationFile(string fileName)
        {
            return ".ts".Equals(Path.GetExtension(fileName), IgnoreCase);
        }

        public static bool IsQmlFile(string fileName)
        {
            return ".qml".Equals(Path.GetExtension(fileName), IgnoreCase);
        }

        /// <summary>
        /// Returns the normalized file path of a given file.
        /// </summary>
        /// <param name="name">file name</param>
        public static string NormalizeFilePath(string name)
        {
            var fi = new FileInfo(name);
            return fi.FullName;
        }

        public static string NormalizeRelativeFilePath(string path)
        {
            if (path == null)
                return ".\\";

            path = path.Trim();
            path = HelperFunctions.ToNativeSeparator(path);

            var tmp = string.Empty;
            while (tmp != path) {
                tmp = path;
                path = path.Replace("\\\\", "\\");
            }

            path = path.Replace("\"", "");

            if (path != "." && !IsAbsoluteFilePath(path)
                && !path.StartsWith(".\\", IgnoreCase)
                && !path.StartsWith("$", IgnoreCase)) {
                path = ".\\" + path;
            }
            if (path.EndsWith("\\", IgnoreCase))
                path = path.Substring(0, path.Length - 1);

            return path;
        }

        public static bool IsAbsoluteFilePath(string path)
        {
            path = path.Trim();
            if (path.Length >= 2 && path[1] == ':')
                return true;
            return path.StartsWith("\\", IgnoreCase)
                || path.StartsWith("/", IgnoreCase);
        }

        /// <summary>
        /// Returns the relative path between a given file and a path.
        /// </summary>
        /// <param name="path">absolute path</param>
        /// <param name="file">absolute file name</param>
        public static string GetRelativePath(string path, string file)
        {
            if (file == null || path == null)
                return "";
            var fi = new FileInfo(file);
            var di = new DirectoryInfo(path);

            var fiArray = fi.FullName.Split(Path.DirectorySeparatorChar);
            var dir = di.FullName;
            while (dir.EndsWith("\\", IgnoreCase))
                dir = dir.Remove(dir.Length - 1, 1);
            var diArray = dir.Split(Path.DirectorySeparatorChar);

            var minLen = fiArray.Length < diArray.Length ? fiArray.Length : diArray.Length;
            int i = 0, j = 0, commonParts = 0;

            while (i < minLen && fiArray[i].ToLower() == diArray[i].ToLower()) {
                commonParts++;
                i++;
            }

            if (commonParts < 1)
                return fi.FullName;

            var result = string.Empty;

            for (j = i; j < fiArray.Length; j++) {
                if (j == i)
                    result = fiArray[j];
                else
                    result += Path.DirectorySeparatorChar + fiArray[j];
            }
            while (i < diArray.Length) {
                result = "..\\" + result;
                i++;
            }
            //MessageBox.Show(path + "\n" + file + "\n" + result);
            if (result.StartsWith("..\\", StringComparison.Ordinal))
                return result;
            return ".\\" + result;
        }

        /// <summary>
        /// Since VS2010 it is possible to have VCCustomBuildTools without commandlines
        /// for certain filetypes. We are not interested in them and thus try to read the
        /// tool's commandline. If this causes an exception, we ignore it.
        /// There does not seem to be another way for checking which kind of tool it is.
        /// </summary>
        /// <param name="config">File configuration</param>
        /// <returns></returns>
        public static VCCustomBuildTool GetCustomBuildTool(VCFileConfiguration config)
        {
            if (config.File is VCFile file
                && file.ItemType == "CustomBuild"
                && config.Tool is VCCustomBuildTool tool) {
                    try {
                        _ = tool.CommandLine;
                    } catch {
                        return null;
                    }
                    return tool;
            }
            return null;
        }

        /// <summary>
        /// Since VS2010 we have to ensure, that a custom build tool is present
        /// if we want to use it. In order to do so, the ProjectItem's ItemType
        /// has to be "CustomBuild"
        /// </summary>
        /// <param name="projectItem">Project Item which needs to have custom build tool</param>
        public static void EnsureCustomBuildToolAvailable(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Property prop in projectItem.Properties) {
                if (prop.Name == "ItemType") {
                    if ((string)prop.Value != "CustomBuild")
                        prop.Value = "CustomBuild";
                    break;
                }
            }
        }

        /// <summary>
        /// Return true if the project is a VS tools project; false otherwise.
        /// </summary>
        /// <param name="proj">project</param>
        public static bool IsVsToolsProject(Project proj)
        {
            ThreadHelper.ThrowIfNotOnUIThread(); // C++ Project Type GUID
            if (proj == null || proj.Kind != "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                return false;
            return IsVsToolsProject(proj.Object as VCProject);
        }

        /// <summary>
        /// Return true if the project is a VS tools project; false otherwise.
        /// </summary>
        /// <param name="proj">project</param>
        public static bool IsVsToolsProject(VCProject proj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!IsQtProject(proj))
                return false;

            if (QtProject.GetFormatVersion(proj) >= Resources.qtMinFormatVersion_Settings)
                return true;

            var envPro = proj.Object as Project;
            if (envPro.Globals == null || envPro.Globals.VariableNames == null)
                return false;

            foreach (var global in envPro.Globals.VariableNames as string[]) {
                if (global.StartsWith("Qt5Version", StringComparison.Ordinal)
                    && envPro.Globals.VariablePersists[global]) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return true if the project is a Qt project; false otherwise.
        /// </summary>
        /// <param name="proj">project</param>
        public static bool IsQtProject(Project proj)
        {
            ThreadHelper.ThrowIfNotOnUIThread(); //C++ Project Type GUID
            if (proj == null || proj.Kind != "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                return false;
            return IsQtProject(proj.Object as VCProject);
        }

        /// <summary>
        /// Return true if the project is a Qt project; false otherwise.
        /// </summary>
        /// <param name="proj">project</param>
        public static bool IsQtProject(VCProject proj)
        {
            if (proj == null)
                return false;
            var keyword = proj.keyword;
            if (string.IsNullOrEmpty(keyword))
                return false;
            return keyword.StartsWith(Resources.qtProjectKeyword, StringComparison.Ordinal)
                || keyword.StartsWith(Resources.qtProjectV2Keyword, StringComparison.Ordinal);
        }

        public static bool HasQObjectDeclaration(VCFile file)
        {
            return CxxFileContainsNotCommented(file,
                new[]
                {
                    "Q_OBJECT",
                    "Q_GADGET",
                    "Q_NAMESPACE"
                },
                StringComparison.Ordinal, true);
        }

        public static bool CxxFileContainsNotCommented(VCFile file, string str,
            StringComparison comparisonType, bool suppressStrings)
        {
            return CxxFileContainsNotCommented(file, new[] { str }, comparisonType, suppressStrings);
        }

        public static bool CxxFileContainsNotCommented(VCFile file, string[] searchStrings,
            StringComparison comparisonType, bool suppressStrings)
        {
            // Small optimization, we first read the whole content as a string and look for the
            // search strings. Once we found at least one, ...
            bool found = false;
            var content = string.Empty;
            try {
                using (StreamReader sr = new StreamReader(file.FullPath))
                    content = sr.ReadToEnd();

                foreach (var key in searchStrings) {
                    if (content.IndexOf(key, comparisonType) >= 0) {
                        found = true;
                        break;
                    }
                }
            } catch { }

            if (!found)
                return false;

            // ... we will start parsing the file again to see if the actual string is commented
            // or not. The combination of string.IndexOf(...) and string.Split(...) seems to be
            // way faster then reading the file line by line.
            found = false;
            CxxStreamReader cxxSr = null;
            try {
                cxxSr = new CxxStreamReader(content.Split(new[] { "\n", "\r\n" },
                    StringSplitOptions.RemoveEmptyEntries));
                string strLine;
                while (!found && (strLine = cxxSr.ReadLine(suppressStrings)) != null) {
                    foreach (var str in searchStrings) {
                        if (strLine.IndexOf(str, comparisonType) != -1) {
                            found = true;
                            break;
                        }
                    }
                }
                cxxSr.Close();
            } catch (Exception) {
                if (cxxSr != null)
                    cxxSr.Close();
            }
            return found;
        }

        /// <summary>
        /// Converts all directory separators of the path to the alternate character
        /// directory separator. For instance, FromNativeSeparators("c:\\winnt\\system32")
        /// returns "c:/winnt/system32".
        /// </summary>
        /// <param name="path">The path to convert.</param>
        /// <returns>Returns path using '/' as file separator.</returns>
        public static string FromNativeSeparators(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Converts all alternate directory separators characters of the path to the native
        /// directory separator. For instance, ToNativeSeparators("c:/winnt/system32")
        /// returns "c:\\winnt\\system32".
        /// </summary>
        /// <param name="path">The path to convert.</param>
        /// <returns>Returns path using '\' as file separator.</returns>
        public static string ToNativeSeparator(string path)
        {
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public static bool IsInFilter(VCFile vcfile, FakeFilter filter)
        {
            var item = (VCProjectItem)vcfile;

            while ((item.Parent != null) && (item.Kind != "VCProject")) {
                item = (VCProjectItem)item.Parent;

                if (item.Kind == "VCFilter") {
                    var f = (VCFilter)item;
                    if (f.UniqueIdentifier != null
                        && f.UniqueIdentifier.ToLower() == filter.UniqueIdentifier.ToLower())
                        return true;
                }
            }
            return false;
        }

        // returns true if some exception occurs
        public static bool IsGenerated(VCFile vcfile)
        {
            try {
                return IsInFilter(vcfile, Filters.GeneratedFiles());
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
                return true;
            }
        }

        // returns false if some exception occurs
        public static bool IsResource(VCFile vcfile)
        {
            try {
                return IsInFilter(vcfile, Filters.ResourceFiles());
            } catch (Exception) {
                return false;
            }
        }

        public static List<string> GetProjectFiles(Project pro, FilesToList filter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VCProject vcpro;
            try {
                vcpro = (VCProject)pro.Object;
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
                return null;
            }

            var fileList = new List<string>();
            var configurationName = pro.ConfigurationManager.ActiveConfiguration.ConfigurationName;

            foreach (VCFile vcfile in (IVCCollection)vcpro.Files) {
                // Why project files are also returned?
                if (vcfile.ItemName.EndsWith(".vcxproj.filters", StringComparison.Ordinal))
                    continue;
                var excluded = false;
                var fileConfigurations = (IVCCollection)vcfile.FileConfigurations;
                foreach (VCFileConfiguration config in fileConfigurations) {
                    if (config.ExcludedFromBuild && config.MatchName(configurationName, false)) {
                        excluded = true;
                        break;
                    }
                }

                if (excluded)
                    continue;

                // can be in any filter
                if (IsTranslationFile(vcfile.Name) && (filter == FilesToList.FL_Translation))
                    fileList.Add(FromNativeSeparators(vcfile.RelativePath));

                // can also be in any filter
                if (IsWinRCFile(vcfile.Name) && (filter == FilesToList.FL_WinResource))
                    fileList.Add(FromNativeSeparators(vcfile.RelativePath));

                if (IsGenerated(vcfile)) {
                    if (filter == FilesToList.FL_Generated)
                        fileList.Add(FromNativeSeparators(vcfile.RelativePath));
                    continue;
                }

                if (IsResource(vcfile)) {
                    if (filter == FilesToList.FL_Resources)
                        fileList.Add(FromNativeSeparators(vcfile.RelativePath));
                    continue;
                }

                switch (filter) {
                case FilesToList.FL_UiFiles: // form files
                    if (IsUicFile(vcfile.Name))
                        fileList.Add(FromNativeSeparators(vcfile.RelativePath));
                    break;
                case FilesToList.FL_HFiles:
                    if (IsHeaderFile(vcfile.Name))
                        fileList.Add(FromNativeSeparators(vcfile.RelativePath));
                    break;
                case FilesToList.FL_CppFiles:
                    if (IsSourceFile(vcfile.Name))
                        fileList.Add(FromNativeSeparators(vcfile.RelativePath));
                    break;
                case FilesToList.FL_QmlFiles:
                    if (IsQmlFile(vcfile.Name))
                        fileList.Add(FromNativeSeparators(vcfile.RelativePath));
                    break;
                }
            }

            return fileList;
        }

        public static Project GetSelectedProject(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dteObject == null)
                return null;

            Array prjs = null;
            try {
                prjs = (Array)dteObject.ActiveSolutionProjects;
            } catch {
                // When VS2010 is started from the command line,
                // we may catch a "Unspecified error" here.
            }
            if (prjs == null || prjs.Length < 1)
                return null;

            // don't handle multiple selection... use the first one
            if (prjs.GetValue(0) is Project project)
                return project;
            return null;
        }

        public static Project GetActiveDocumentProject(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return dteObject?.ActiveDocument?.ProjectItem?.ContainingProject;
        }

        public static Project GetSingleProjectInSolution(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projectList = ProjectsInSolution(dteObject);
            if (projectList.Count != 1)
                return null; // no way to know which one to select

            return projectList[0];
        }

        /// <summary>
        /// Returns the the current selected Qt Project. If not project
        /// is selected or if the selected project is not a Qt project
        /// this function returns null.
        /// </summary>
        public static Project GetSelectedQtProject(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // can happen sometimes shortly after starting VS
            if (ProjectsInSolution(dteObject).Count == 0)
                return null;

            var pro = GetSelectedProject(dteObject);
            if (pro == null) {
                if ((pro = GetSingleProjectInSolution(dteObject)) == null)
                    pro = GetActiveDocumentProject(dteObject);
            }
            return IsVsToolsProject(pro) ? pro : null;
        }

        public static VCFile[] GetSelectedFiles(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (GetSelectedQtProject(dteObject) == null)
                return null;

            if (dteObject.SelectedItems.Count <= 0)
                return null;

            var items = dteObject.SelectedItems;

            var files = new VCFile[items.Count + 1];
            for (var i = 1; i <= items.Count; ++i) {
                var item = items.Item(i);
                if (item.ProjectItem == null)
                    continue;

                VCProjectItem vcitem;
                try {
                    vcitem = (VCProjectItem)item.ProjectItem.Object;
                } catch (Exception) {
                    return null;
                }

                if (vcitem.Kind == "VCFile")
                    files[i - 1] = (VCFile)vcitem;
            }
            files[items.Count] = null;
            return files;
        }

        public static List<Project> ProjectsInSolution(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dteObject == null)
                return new List<Project>();

            var projects = new List<Project>();
            var solution = dteObject.Solution;
            if (solution != null) {
                var c = solution.Count;
                for (var i = 1; i <= c; ++i) {
                    try {
                        var prj = solution.Projects.Item(i);
                        if (prj == null)
                            continue;
                        addSubProjects(prj, ref projects);
                    } catch {
                        // Ignore this exception... maybe the next project is ok.
                        // This happens for example for Intel VTune projects.
                    }
                }
            }
            return projects;
        }

        private static void addSubProjects(Project prj, ref List<Project> projects)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If the actual object of the project is null then the project was probably unloaded.
            if (prj.Object == null)
                return;

            if (prj.ConfigurationManager != null &&
                // Is this a Visual C++ project?
                prj.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") {
                projects.Add(prj);
            } else {
                // In this case, prj is a solution folder
                addSubProjects(prj.ProjectItems, ref projects);
            }
        }

        private static void addSubProjects(ProjectItems subItems, ref List<Project> projects)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (subItems == null)
                return;

            foreach (ProjectItem item in subItems) {
                Project subprj = null;
                try {
                    subprj = item.SubProject;
                } catch {
                    // The property "SubProject" might not be implemented.
                    // This is the case for Intel Fortran projects. (QTBUG-11567)
                }
                if (subprj != null)
                    addSubProjects(subprj, ref projects);
            }
        }

        /// <summary>
        /// Translates the machine type given as command line argument to the linker
        /// to the internal enum type VCProjectEngine.machineTypeOption.
        /// </summary>
        public static machineTypeOption TranslateMachineType(string cmdLineMachine)
        {
            switch (cmdLineMachine.ToUpper()) {
            case "AM33":
                return machineTypeOption.machineAM33;
            case "X64":
                return machineTypeOption.machineAMD64;
            case "ARM":
                return machineTypeOption.machineARM;
            case "EBC":
                return machineTypeOption.machineEBC;
            case "IA-64":
                return machineTypeOption.machineIA64;
            case "M32R":
                return machineTypeOption.machineM32R;
            case "MIPS":
                return machineTypeOption.machineMIPS;
            case "MIPS16":
                return machineTypeOption.machineMIPS16;
            case "MIPSFPU":
                return machineTypeOption.machineMIPSFPU;
            case "MIPSFPU16":
                return machineTypeOption.machineMIPSFPU16;
            case "MIPS41XX":
                return machineTypeOption.machineMIPSR41XX;
            case "SH3":
                return machineTypeOption.machineSH3;
            case "SH3DSP":
                return machineTypeOption.machineSH3DSP;
            case "SH4":
                return machineTypeOption.machineSH4;
            case "SH5":
                return machineTypeOption.machineSH5;
            case "THUMB":
                return machineTypeOption.machineTHUMB;
            case "X86":
                return machineTypeOption.machineX86;
            default:
                return machineTypeOption.machineNotSet;
            }
        }

        /// <summary>
        /// This method copies the specified directory and all its child directories and files to
        /// the specified destination. The destination directory is created if it does not exist.
        /// </summary>
        public static void CopyDirectory(string directory, string targetPath)
        {
            var sourceDir = new DirectoryInfo(directory);
            if (!sourceDir.Exists)
                return;

            try {
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);

                var files = sourceDir.GetFiles();
                foreach (var file in files) {
                    try {
                        file.CopyTo(Path.Combine(targetPath, file.Name), true);
                    } catch { }
                }
            } catch { }

            var subDirs = sourceDir.GetDirectories();
            foreach (var subDir in subDirs)
                CopyDirectory(subDir.FullName, Path.Combine(targetPath, subDir.Name));
        }

        /// <summary>
        /// Performs an in-place expansion of MS Build properties in the form $(PropertyName)
        /// and project item metadata in the form %(MetadataName).<para/>
        /// Returns: 'true' if expansion was successful, 'false' otherwise<para/>
        /// <paramref name="stringToExpand"/>: The string containing properties and/or metadata to
        /// expand. This string is passed by ref and expansion is performed in-place.<para/>
        /// <paramref name="config"/>: Either a VCConfiguration or VCFileConfiguration object to
        /// use as provider of property expansion (through Evaluate()). Cannot be null.<para/>
        /// </summary>
        public static bool ExpandString(
            ref string stringToExpand,
            object config)
        {
            if (config == null)
                return false;

            /* try property expansion through VCConfiguration.Evaluate()
             * or VCFileConfiguration.Evaluate() */
            string expanded = stringToExpand;
            VCProject vcProj = null;
            VCFile vcFile = null;
            string configName = "", platformName = "";
            if (config is VCConfiguration vcConfig) {
                vcProj = vcConfig.project as VCProject;
                configName = vcConfig.ConfigurationName;
                if (vcConfig.Platform is VCPlatform vcPlatform)
                    platformName = vcPlatform.Name;
                try {
                    expanded = vcConfig.Evaluate(expanded);
                } catch { }
            } else {
                var vcFileConfig = config as VCFileConfiguration;
                if (vcFileConfig == null)
                    return false;
                vcFile = vcFileConfig.File as VCFile;
                if (vcFile != null)
                    vcProj = vcFile.project as VCProject;
                if (vcFileConfig.ProjectConfiguration is VCConfiguration vcProjConfig) {
                    configName = vcProjConfig.ConfigurationName;
                    if (vcProjConfig.Platform is VCPlatform vcPlatform)
                        platformName = vcPlatform.Name;
                }
                try {
                    expanded = vcFileConfig.Evaluate(expanded);
                } catch { }
            }

            /* fail-safe */
            foreach (Match propNameMatch in Regex.Matches(expanded, @"\$\(([^\)]+)\)")) {
                string propName = propNameMatch.Groups[1].Value;
                string propValue = "";
                switch (propName) {
                case "Configuration":
                case "ConfigurationName":
                    if (string.IsNullOrEmpty(configName))
                        return false;
                    propValue = configName;
                    break;
                case "Platform":
                case "PlatformName":
                    if (string.IsNullOrEmpty(platformName))
                        return false;
                    propValue = platformName;
                    break;
                default:
                    return false;
                }
                expanded = expanded.Replace($"$({propName})", propValue);
            }

            /* because item metadata is not expanded in Evaluate() */
            foreach (Match metaNameMatch in Regex.Matches(expanded, @"\%\(([^\)]+)\)")) {
                string metaName = metaNameMatch.Groups[1].Value;
                string metaValue = "";
                switch (metaName) {
                case "FullPath":
                    if (vcFile == null)
                        return false;
                    metaValue = vcFile.FullPath;
                    break;
                case "RootDir":
                    if (vcFile == null)
                        return false;
                    metaValue = Path.GetPathRoot(vcFile.FullPath);
                    break;
                case "Filename":
                    if (vcFile == null)
                        return false;
                    metaValue = Path.GetFileNameWithoutExtension(vcFile.FullPath);
                    break;
                case "Extension":
                    if (vcFile == null)
                        return false;
                    metaValue = Path.GetExtension(vcFile.FullPath);
                    break;
                case "RelativeDir":
                    if (vcProj == null || vcFile == null)
                        return false;
                    metaValue = Path.GetDirectoryName(GetRelativePath(
                        Path.GetDirectoryName(vcProj.ProjectFile),
                        vcFile.FullPath));
                    if (!metaValue.EndsWith("\\"))
                        metaValue += "\\";
                    if (metaValue.StartsWith(".\\"))
                        metaValue = metaValue.Substring(2);
                    break;
                case "Directory":
                    if (vcFile == null)
                        return false;
                    metaValue = Path.GetDirectoryName(GetRelativePath(
                        Path.GetPathRoot(vcFile.FullPath),
                        vcFile.FullPath));
                    if (!metaValue.EndsWith("\\"))
                        metaValue += "\\";
                    if (metaValue.StartsWith(".\\"))
                        metaValue = metaValue.Substring(2);
                    break;
                case "Identity":
                    if (vcProj == null || vcFile == null)
                        return false;
                    metaValue = GetRelativePath(
                        Path.GetDirectoryName(vcProj.ProjectFile),
                        vcFile.FullPath);
                    if (metaValue.StartsWith(".\\"))
                        metaValue = metaValue.Substring(2);
                    break;
                case "RecursiveDir":
                case "ModifiedTime":
                case "CreatedTime":
                case "AccessedTime":
                    return false;
                default:
                    var vcFileConfig = config as VCFileConfiguration;
                    if (vcFileConfig == null)
                        return false;
                    var propStoreTool = vcFileConfig.Tool as IVCRulePropertyStorage;
                    if (propStoreTool == null)
                        return false;
                    try {
                        metaValue = propStoreTool.GetEvaluatedPropertyValue(metaName);
                    } catch {
                        return false;
                    }
                    break;
                }
                expanded = expanded.Replace($"%({metaName})", metaValue);
            }

            stringToExpand = expanded;
            return true;
        }

#if VS2017
        public static string GetRegistrySoftwareString(string subKeyName, string valueName)
        {
            var keyName = new StringBuilder();
            keyName.Append(@"SOFTWARE\");
            if (System.Environment.Is64BitOperatingSystem && IntPtr.Size == 4)
                keyName.Append(@"WOW6432Node\");
            keyName.Append(subKeyName);
            try {
                using (var key = Registry.LocalMachine.OpenSubKey(keyName.ToString(), false)) {
                    if (key == null)
                        return ""; //key not found
                    RegistryValueKind valueKind = key.GetValueKind(valueName);
                    if (valueKind != RegistryValueKind.String
                        && valueKind != RegistryValueKind.ExpandString) {
                        return ""; //wrong value kind
                    }
                    Object objValue = key.GetValue(valueName);
                    if (objValue == null)
                        return ""; //error getting value
                    return objValue.ToString();
                }
            } catch {
                return "";
            }
        }
#endif

        static string _VCPath;
        public static string VCPath
        {
            set => _VCPath = value;
            get
            {
                if (!string.IsNullOrEmpty(_VCPath))
                    return _VCPath;
                else
                    return GetVCPathFromRegistry();
            }
        }

        private static string GetVCPathFromRegistry()
        {
#if VS2022
            Debug.Assert(false, "VCPath for VS2022 is not available through the registry");
            string vcPath = string.Empty;
#elif VS2019
            Debug.Assert(false, "VCPath for VS2019 is not available through the registry");
            string vcPath = string.Empty;
#elif VS2017
            string vsPath = GetRegistrySoftwareString(@"Microsoft\VisualStudio\SxS\VS7", "15.0");
            if (string.IsNullOrEmpty(vsPath))
                return "";
            string vcPath = Path.Combine(vsPath, "VC");
#endif
            return vcPath;
        }

        static Parser EnvVarParser => StaticLazy.Get(() => EnvVarParser, () =>
        {
            Token tokenName = new Token("name", (~Chars["=\r\n"]).Repeat(atLeast: 1));
            Token tokenValuePart = new Token("value_part", (~Chars[";\r\n"]).Repeat(atLeast: 1));
            Token tokenValue = new Token("value", (tokenValuePart | Chars[';']).Repeat())
            {
                new Rule<List<string>>
                {
                    Capture(_ => new List<string>()),
                    Update("value_part", (List<string> parts, string part) => parts.Add(part))
                }
            };
            Token tokenEnvVar = new Token("env_var", tokenName & "=" & tokenValue & LineBreak)
            {
                new Rule<KeyValuePair<string, List<string>>>
                {
                    Create("name", (string name)
                        => new KeyValuePair<string, List<string>>(name, null)),
                    Transform("value", (KeyValuePair<string, List<string>> prop, List<string> value)
                        => new KeyValuePair<string, List<string>>(prop.Key, value))
                }
            };
            return tokenEnvVar.Render();
        });

        public static bool SetVCVars(VersionInformation VersionInfo, ProcessStartInfo startInfo)
        {
            var vm = QtVersionManager.The();
            VersionInfo ??= vm.GetVersionInfo(vm.GetDefaultVersion());

            if (string.IsNullOrEmpty(VCPath))
                return false;

            // Select vcvars script according to host and target platforms
            bool osIs64Bit = System.Environment.Is64BitOperatingSystem;
            string comspecPath = Environment.GetEnvironmentVariable("COMSPEC");
            string vcVarsCmd = "";
            switch (VersionInfo.platform()) {
            case Platform.x86:
                vcVarsCmd = Path.Combine(VCPath, osIs64Bit
                        ? @"Auxiliary\Build\vcvarsamd64_x86.bat"
                        : @"Auxiliary\Build\vcvars32.bat");
                break;
            case Platform.x64:
                vcVarsCmd = Path.Combine(VCPath, osIs64Bit
                        ? @"Auxiliary\Build\vcvars64.bat"
                        : @"Auxiliary\Build\vcvarsx86_amd64.bat");
                break;
            case Platform.arm64:
                vcVarsCmd = Path.Combine(VCPath, osIs64Bit
                        ? @"Auxiliary\Build\vcvarsamd64_arm64.bat"
                        : @"Auxiliary\Build\vcvarsx86_arm64.bat");
                if (!File.Exists(vcVarsCmd)) {
                    vcVarsCmd = Path.Combine(VCPath, osIs64Bit
                            ? @"Auxiliary\Build\vcvars64.bat"
                            : @"Auxiliary\Build\vcvarsx86_amd64.bat");
                }
                break;
            }

            Messages.Print($"vcvars: {vcVarsCmd}");
            if (!File.Exists(vcVarsCmd)) {
                Messages.Print("vcvars: NOT FOUND");
                return false;
            }

            // Run vcvars and print environment variables
            StringBuilder stdOut = new StringBuilder();
            string command = $"/c \"{vcVarsCmd}\" && set";
            var vcVarsStartInfo = new ProcessStartInfo(comspecPath, command);
            vcVarsStartInfo.CreateNoWindow = true;
            vcVarsStartInfo.UseShellExecute = false;
            vcVarsStartInfo.RedirectStandardError = true;
            vcVarsStartInfo.RedirectStandardOutput = true;
            var process = Process.Start(vcVarsStartInfo);
            process.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                e.Data.TrimEnd('\r', '\n');
                if (!string.IsNullOrEmpty(e.Data))
                    stdOut.Append($"{e.Data}\r\n");
            };
            process.BeginOutputReadLine();
            process.WaitForExit();
            bool ok = (process.ExitCode == 0);
            process.Close();
            if (!ok)
                return false;

            // Parse command output: copy environment variables to startInfo
            var envVars = EnvVarParser.Parse(stdOut.ToString())
                .GetValues<KeyValuePair<string, List<string>>>("env_var")
                .ToDictionary(envVar => envVar.Key, envVar => envVar.Value, CaseIgnorer);
            foreach (var vcVar in envVars)
                startInfo.Environment[vcVar.Key] = string.Join(";", vcVar.Value);

            // Warn if cl.exe is not in PATH
            string clPath = envVars["PATH"]
                .Select(path => Path.Combine(path, "cl.exe"))
                .FirstOrDefault(File.Exists);
            Messages.Print($"cl: {clPath ?? "NOT FOUND"}");

            return true;
        }

        /// <summary>
        /// Rooted canonical path is the absolute path for the specified path string
        /// (cf. Path.GetFullPath()) without a trailing path separator.
        /// </summary>
        static string RootedCanonicalPath(string path)
        {
            try {
                return Path
                .GetFullPath(path)
                .TrimEnd(new char[] {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                });
            } catch {
                return "";
            }
        }

        /// <summary>
        /// If the given path is relative and a sub-path of the current directory, returns
        /// a "relative canonical path", containing only the steps beyond the current directory.
        /// Otherwise, returns the absolute ("rooted") canonical path.
        /// </summary>
        public static string CanonicalPath(string path)
        {
            string canonicalPath = RootedCanonicalPath(path);
            if (!Path.IsPathRooted(path)) {
                string currentCanonical = RootedCanonicalPath(".");
                if (canonicalPath.StartsWith(currentCanonical, IgnoreCase)) {
                    return canonicalPath
                    .Substring(currentCanonical.Length)
                    .TrimStart(new char[] {
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar
                    });
                } else {
                    return canonicalPath;
                }
            } else {
                return canonicalPath;
            }
        }

        public static bool PathIsRelativeTo(string path, string subPath)
        {
            return CanonicalPath(path).EndsWith(CanonicalPath(subPath), IgnoreCase);
        }

        public static string Unquote(string text)
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text)
                || text.Length < 3
                || !text.StartsWith("\"")
                || !text.EndsWith("\"")) {
                return text;
            }
            return text.Substring(1, text.Length - 2);
        }

        public static string NewProjectGuid()
        {
            return $"{{{Guid.NewGuid().ToString().ToUpper()}}}";
        }

        public static string SafePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            path = path.Replace("\"", "");
            if (!path.Contains(' '))
                return path;
            if (path.EndsWith("\\"))
                path += Path.DirectorySeparatorChar;
            return $"\"{path}\"";
        }
    }
}
