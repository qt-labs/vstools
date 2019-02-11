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
using Microsoft.Win32;
using QtProjectLib.QtMsBuild;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Process = System.Diagnostics.Process;

namespace QtProjectLib
{
    public static class HelperFunctions
    {
        public static string FindQtDirWithTools(Project project)
        {
            var versionManager = QtVersionManager.The();
            string projectQtVersion = null;
            if (IsQtProject(project))
                projectQtVersion = versionManager.GetProjectQtVersion(project);
            return FindQtDirWithTools(projectQtVersion);
        }

        public static string FindQtDirWithTools(string projectQtVersion)
        {
            string tool = null;
            return FindQtDirWithTools(tool, projectQtVersion);
        }

        public static string FindQtDirWithTools(string tool, string projectQtVersion)
        {
            if (!string.IsNullOrEmpty(tool)) {
                if (!tool.StartsWith("\\bin\\", StringComparison.OrdinalIgnoreCase))
                    tool = "\\bin\\" + tool;
                if (!tool.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    tool += ".exe";
            }

            var versionManager = QtVersionManager.The();
            string qtDir = null;
            if (projectQtVersion != null)
                qtDir = versionManager.GetInstallPath(projectQtVersion);

            if (qtDir == null)
                qtDir = Environment.GetEnvironmentVariable("QTDIR");

            var found = false;
            if (tool == null) {
                found = File.Exists(qtDir + "\\bin\\designer.exe")
                    && File.Exists(qtDir + "\\bin\\linguist.exe");
            } else {
                found = File.Exists(qtDir + tool);
            }
            if (!found) {
                VersionInformation exactlyMatchingVersion = null;
                VersionInformation matchingVersion = null;
                VersionInformation somehowMatchingVersion = null;
                var viProjectQtVersion = versionManager.GetVersionInfo(projectQtVersion);
                foreach (var qtVersion in versionManager.GetVersions()) {
                    var vi = versionManager.GetVersionInfo(qtVersion);
                    if (tool == null) {
                        found = File.Exists(vi.qtDir + "\\bin\\designer.exe")
                            && File.Exists(vi.qtDir + "\\bin\\linguist.exe");
                    } else {
                        found = File.Exists(vi.qtDir + tool);
                    }
                    if (!found)
                        continue;

                    if (viProjectQtVersion != null
                        && vi.qtMajor == viProjectQtVersion.qtMajor
                        && vi.qtMinor == viProjectQtVersion.qtMinor) {
                        exactlyMatchingVersion = vi;
                        break;
                    }
                    if (matchingVersion == null
                        && viProjectQtVersion != null
                        && vi.qtMajor == viProjectQtVersion.qtMajor) {
                        matchingVersion = vi;
                    }
                    if (somehowMatchingVersion == null)
                        somehowMatchingVersion = vi;
                }

                if (exactlyMatchingVersion != null)
                    qtDir = exactlyMatchingVersion.qtDir;
                else if (matchingVersion != null)
                    qtDir = matchingVersion.qtDir;
                else if (somehowMatchingVersion != null)
                    qtDir = somehowMatchingVersion.qtDir;
                else
                    qtDir = null;
            }
            return qtDir;
        }

        static readonly HashSet<string> _sources = new HashSet<string>(new [] { ".c", ".cpp", ".cxx"},
            StringComparer.OrdinalIgnoreCase);
        static public bool IsSourceFile(string fileName)
        {
            return _sources.Contains(Path.GetExtension(fileName));
        }

        static readonly HashSet<string> _headers = new HashSet<string>(new[] { ".h", ".hpp", ".hxx" },
            StringComparer.OrdinalIgnoreCase);
        static public bool IsHeaderFile(string fileName)
        {
            return _headers.Contains(Path.GetExtension(fileName));
        }

        public static bool IsUicFile(string fileName)
        {
            return ".ui".Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMocFile(string fileName)
        {
            return ".moc".Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsQrcFile(string fileName)
        {
            return ".qrc".Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsWinRCFile(string fileName)
        {
            return ".rc".Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTranslationFile(string fileName)
        {
            return ".ts".Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase);
        }

        static public void SetDebuggingEnvironment(Project prj)
        {
            SetDebuggingEnvironment(prj, string.Empty);
        }

        static public void SetDebuggingEnvironment(Project prj, string solutionConfig)
        {
            SetDebuggingEnvironment(prj, "PATH=$(QTDIR)\\bin;$(PATH)", false, solutionConfig);
        }

        static public void SetDebuggingEnvironment(Project prj, string envpath, bool overwrite)
        {
            SetDebuggingEnvironment(prj, envpath, overwrite, string.Empty);
        }

        static public void SetDebuggingEnvironment(Project prj, string envpath, bool overwrite, string solutionConfig)
        {
            // Get platform name from given solution configuration
            // or if not available take the active configuration
            var activePlatformName = string.Empty;
            if (string.IsNullOrEmpty(solutionConfig)) {
                // First get active configuration cause not given as parameter
                try {
                    var activeConf = prj.ConfigurationManager.ActiveConfiguration;
                    activePlatformName = activeConf.PlatformName;
                } catch {
                    Messages.PaneMessage(prj.DTE, "Could not get the active platform name.");
                }
            } else {
                activePlatformName = solutionConfig.Split('|')[1];
            }

            var vcprj = prj.Object as VCProject;
            foreach (VCConfiguration conf in vcprj.Configurations as IVCCollection) {
                // Set environment only for active (or given) platform
                var currentPlatform = conf.Platform as VCPlatform;
                if (currentPlatform == null || currentPlatform.Name != activePlatformName)
                    continue;

                var de = conf.DebugSettings as VCDebugSettings;
                if (de == null)
                    continue;

                // See: https://connect.microsoft.com/VisualStudio/feedback/details/619702
                // Project | Properties | Configuration Properties | Debugging | Environment
                //
                // Issue: Substitution of ";" to "%3b"
                // Answer: This behavior currently is by design as ';' is a special MSBuild
                // character and needs to be escaped. In the Project Properties we show this
                // escaped value, but it should be the original when we use it.
                envpath = envpath.Replace("%3b", ";");
                de.Environment = de.Environment.Replace("%3b", ";");

                var index = envpath.LastIndexOf(";$(PATH)", StringComparison.Ordinal);
                var withoutPath = (index >= 0 ? envpath.Remove(index) : envpath);

                if (overwrite || string.IsNullOrEmpty(de.Environment))
                    de.Environment = envpath;
                else if (!de.Environment.Contains(envpath) && !de.Environment.Contains(withoutPath)) {
                    var m = Regex.Match(de.Environment, "PATH\\s*=\\s*");
                    if (m.Success) {
                        de.Environment = Regex.Replace(de.Environment, "PATH\\s*=\\s*", withoutPath + ";");
                        if (!de.Environment.Contains("$(PATH)") && !de.Environment.Contains("%PATH%")) {
                            if (!de.Environment.EndsWith(";", StringComparison.Ordinal))
                                de.Environment = de.Environment + ";";
                            de.Environment += "$(PATH)";
                        }
                    } else {
                        if (!string.IsNullOrEmpty(de.Environment))
                            de.Environment += "\n";
                        de.Environment += envpath;
                    }
                }
            }
        }

        public static bool IsProjectInSolution(DTE dteObject, string fullName)
        {
            var fi = new FileInfo(fullName);

            foreach (var p in ProjectsInSolution(dteObject)) {
                if (p.FullName.ToLower() == fi.FullName.ToLower())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the normalized file path of a given file.
        /// </summary>
        /// <param name="name">file name</param>
        static public string NormalizeFilePath(string name)
        {
            var fi = new FileInfo(name);
            return fi.FullName;
        }

        static public string NormalizeRelativeFilePath(string path)
        {
            if (path == null)
                return ".\\";

            path = path.Trim();
            path = path.Replace("/", "\\");

            var tmp = string.Empty;
            while (tmp != path) {
                tmp = path;
                path = path.Replace("\\\\", "\\");
            }

            path = path.Replace("\"", "");

            if (path != "." && !IsAbsoluteFilePath(path) && !path.StartsWith(".\\", StringComparison.Ordinal)
                 && !path.StartsWith("$", StringComparison.Ordinal))
                path = ".\\" + path;

            if (path.EndsWith("\\", StringComparison.Ordinal))
                path = path.Substring(0, path.Length - 1);

            return path;
        }

        static public bool IsAbsoluteFilePath(string path)
        {
            path = path.Trim();
            if (path.Length >= 2 && path[1] == ':')
                return true;
            if (path.StartsWith("\\", StringComparison.Ordinal) || path.StartsWith("/", StringComparison.Ordinal))
                return true;

            return false;
        }

        /// <summary>
        /// Reads lines from a .pro file that is opened with a StreamReader
        /// and concatenates strings that end with a backslash.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns>the composite string</returns>
        static private string ReadProFileLine(StreamReader streamReader)
        {
            var line = streamReader.ReadLine();
            if (line == null)
                return null;

            line = line.TrimEnd(' ', '\t');
            while (line.EndsWith("\\", StringComparison.Ordinal)) {
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
        static public bool IsSubDirsFile(string profile)
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

            char[] separator = { '\\' };
            var fiArray = fi.FullName.Split(separator);
            var dir = di.FullName;
            while (dir.EndsWith("\\", StringComparison.Ordinal))
                dir = dir.Remove(dir.Length - 1, 1);
            var diArray = dir.Split(separator);

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
                    result += "\\" + fiArray[j];
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
        /// Replaces a string in the commandLine, description, outputs and additional dependencies
        /// in all Custom build tools of the project
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="oldString">String, which is going to be replaced</param>
        /// <param name="oldString">String, which is going to replace the other one</param>
        /// <returns></returns>
        public static void ReplaceInCustomBuildTools(Project project, string oldString, string replaceString)
        {
            var vcPro = (VCProject) project.Object;
            if (vcPro == null)
                return;

            var qtMsBuild = new QtMsBuildContainer(new VCPropertyStorageProvider());
            qtMsBuild.BeginSetItemProperties();
            foreach (VCFile vcfile in (IVCCollection) vcPro.Files) {
                foreach (VCFileConfiguration config in (IVCCollection) vcfile.FileConfigurations) {
                    try {
                        if (vcfile.ItemType == "CustomBuild") {
                            var tool = GetCustomBuildTool(config);
                            if (tool == null)
                                continue;

                            tool.CommandLine = tool.CommandLine
                                .Replace(oldString, replaceString,
                                StringComparison.OrdinalIgnoreCase);
                            tool.Description = tool.Description
                                .Replace(oldString, replaceString,
                                StringComparison.OrdinalIgnoreCase);
                            tool.Outputs = tool.Outputs
                                .Replace(oldString, replaceString,
                                StringComparison.OrdinalIgnoreCase);
                            tool.AdditionalDependencies = tool.AdditionalDependencies
                                .Replace(oldString, replaceString,
                                StringComparison.OrdinalIgnoreCase);
                        } else {
                            var tool = new QtCustomBuildTool(config, qtMsBuild);
                            tool.CommandLine = tool.CommandLine
                                .Replace(oldString, replaceString,
                                StringComparison.OrdinalIgnoreCase);
                        }
                    } catch (Exception) {
                    }
                }
            }
            qtMsBuild.EndSetItemProperties();
        }

        /// <summary>
        /// Since VS2010 it is possible to have VCCustomBuildTools without commandlines
        /// for certain filetypes. We are not interested in them and thus try to read the
        /// tool's commandline. If this causes an exception, we ignore it.
        /// There does not seem to be another way for checking which kind of tool it is.
        /// </summary>
        /// <param name="config">File configuration</param>
        /// <returns></returns>
        static public VCCustomBuildTool GetCustomBuildTool(VCFileConfiguration config)
        {
            var file = config.File as VCFile;
            if (file == null || file.ItemType != "CustomBuild")
                return null;

            var tool = config.Tool as VCCustomBuildTool;
            if (tool == null)
                return null;

            try {
                // TODO: The return value is not used at all?
                var cmdLine = tool.CommandLine;
            } catch {
                return null;
            }
            return tool;
        }

        /// <summary>
        /// Since VS2010 we have to ensure, that a custom build tool is present
        /// if we want to use it. In order to do so, the ProjectItem's ItemType
        /// has to be "CustomBuild"
        /// </summary>
        /// <param name="projectItem">Project Item which needs to have custom build tool</param>
        static public void EnsureCustomBuildToolAvailable(ProjectItem projectItem)
        {
            foreach (Property prop in projectItem.Properties) {
                if (prop.Name == "ItemType") {
                    if ((string) prop.Value != "CustomBuild")
                        prop.Value = "CustomBuild";
                    break;
                }
            }
        }

        /// <summary>
        /// As Qmake -tp vc Adds the full path to the additional dependencies
        /// we need to do the same when toggling project kind to qmake generated.
        /// </summary>
        /// <returns></returns>
        private static string AddFullPathToAdditionalDependencies(string qtDir, string additionalDependencies)
        {
            var returnString = additionalDependencies;
            returnString =
                Regex.Replace(returnString, "Qt(\\S+5?)\\.lib", qtDir + "\\lib\\Qt${1}.lib");
            returnString =
                Regex.Replace(returnString, "(qtmaind?5?)\\.lib", qtDir + "\\lib\\${1}.lib");
            returnString =
                Regex.Replace(returnString, "(enginiod?5?)\\.lib", qtDir + "\\lib\\${1}.lib");
            return returnString;
        }

        /// <summary>
        /// Toggles the kind of a project. If the project is a QMake generated project (qmake -tp vc)
        /// it is transformed to an Qt VS Tools project and vice versa.
        /// </summary>
        /// <param name="project">Project</param>
        /// <returns></returns>
        public static void ToggleProjectKind(Project project)
        {
            string qtDir = null;
            var vcPro = (VCProject) project.Object;
            if (!IsQMakeProject(project))
                return;
            if (IsQtProject(project)) {
                // TODO: qtPro is never used.
                var qtPro = QtProject.Create(project);
                var vm = QtVersionManager.The();
                qtDir = vm.GetInstallPath(project);

                foreach (var global in (string[]) project.Globals.VariableNames) {
                    if (global.StartsWith("Qt5Version", StringComparison.Ordinal))
                        project.Globals.set_VariablePersists(global, false);
                }

                foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                    var compiler = CompilerToolWrapper.Create(config);
                    var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");
                    var librarian = (VCLibrarianTool) ((IVCCollection) config.Tools).Item("VCLibrarianTool");
                    if (compiler != null) {
                        var additionalIncludes = compiler.GetAdditionalIncludeDirectories();
                        additionalIncludes = additionalIncludes.Replace("$(QTDIR)", qtDir,
                            StringComparison.OrdinalIgnoreCase);
                        compiler.SetAdditionalIncludeDirectories(additionalIncludes);
                    }
                    if (linker != null) {
                        linker.AdditionalLibraryDirectories = linker.AdditionalLibraryDirectories.
                            Replace("$(QTDIR)", qtDir, StringComparison.OrdinalIgnoreCase);
                        linker.AdditionalDependencies = AddFullPathToAdditionalDependencies(qtDir, linker.AdditionalDependencies);
                    } else {
                        librarian.AdditionalLibraryDirectories = librarian.AdditionalLibraryDirectories
                            .Replace("$(QTDIR)", qtDir, StringComparison.OrdinalIgnoreCase);
                        librarian.AdditionalDependencies = AddFullPathToAdditionalDependencies(qtDir, librarian.AdditionalDependencies);
                    }
                }

                ReplaceInCustomBuildTools(project, "$(QTDIR)", qtDir);
            } else {
                qtDir = GetQtDirFromQMakeProject(project);

                var vm = QtVersionManager.The();
                var qtVersion = vm.GetQtVersionFromInstallDir(qtDir);
                if (qtVersion == null)
                    qtVersion = vm.GetDefaultVersion();
                if (qtDir == null)
                    qtDir = vm.GetInstallPath(qtVersion);
                var vi = vm.GetVersionInfo(qtVersion);
                var platformName = vi.GetVSPlatformName();
                vm.SaveProjectQtVersion(project, qtVersion, platformName);
                var qtPro = QtProject.Create(project);
                if (!qtPro.SelectSolutionPlatform(platformName) || !qtPro.HasPlatform(platformName)) {
                    var newProject = false;
                    qtPro.CreatePlatform("Win32", platformName, null, vi, ref newProject);
                    if (!qtPro.SelectSolutionPlatform(platformName))
                        Messages.PaneMessage(project.DTE, "Can't select the platform " + platformName + ".");
                }

                var activeConfig = project.ConfigurationManager.ActiveConfiguration.ConfigurationName;
                var activeVCConfig = (VCConfiguration) ((IVCCollection) qtPro.VCProject.Configurations).Item(activeConfig);
                if (activeVCConfig.ConfigurationType == ConfigurationTypes.typeDynamicLibrary) {
                    var compiler = CompilerToolWrapper.Create(activeVCConfig);
                    var linker = (VCLinkerTool) ((IVCCollection) activeVCConfig.Tools).Item("VCLinkerTool");
                    var ppdefs = compiler.GetPreprocessorDefinitions();
                    if (ppdefs != null
                        && ppdefs.IndexOf("QT_PLUGIN", StringComparison.Ordinal) > -1
                        && ppdefs.IndexOf("QDESIGNER_EXPORT_WIDGETS", StringComparison.Ordinal) > -1
                        && ppdefs.IndexOf("QtDesigner", StringComparison.Ordinal) > -1
                        && linker.AdditionalDependencies != null
                        && linker.AdditionalDependencies.IndexOf("QtDesigner", StringComparison.Ordinal) > -1) {
                        qtPro.MarkAsDesignerPluginProject();
                    }
                }

                CleanupQMakeDependencies(project);

                foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                    var compiler = CompilerToolWrapper.Create(config);
                    var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");

                    if (compiler != null) {
                        var additionalIncludes = compiler.AdditionalIncludeDirectories;
                        if (additionalIncludes != null) {
                            ReplaceDirectory(ref additionalIncludes, qtDir, "$(QTDIR)", project);
                            compiler.AdditionalIncludeDirectories = additionalIncludes;
                        }
                    }
                    if (linker != null) {
                        var linkerToolWrapper = new LinkerToolWrapper(linker);
                        var paths = linkerToolWrapper.AdditionalLibraryDirectories;
                        if (paths != null) {
                            ReplaceDirectory(ref paths, qtDir, "$(QTDIR)", project);
                            linkerToolWrapper.AdditionalLibraryDirectories = paths;
                        }
                    }
                }

                ReplaceInCustomBuildTools(project, qtDir, "$(QTDIR)");
                qtPro.TranslateFilterNames();
            }
            project.Save(project.FullName);
        }

        /// <summary>
        /// Replaces every occurrence of oldDirectory with replacement in the array of strings.
        /// Parameter oldDirectory must be an absolute path.
        /// This function converts relative directories to absolute paths internally
        /// and replaces them, if necessary. If no replacement is done, the path isn't altered.
        /// </summary>
        /// <param name="project">The project is needed to convert relative paths to absolute paths.</param>
        private static void ReplaceDirectory(ref List<string> paths, string oldDirectory, string replacement, Project project)
        {
            for (var i = 0; i < paths.Count; ++i) {
                var dirName = paths[i];
                if (dirName.StartsWith("\"", StringComparison.Ordinal) && dirName.EndsWith("\"", StringComparison.Ordinal)) {
                    dirName = dirName.Substring(1, dirName.Length - 2);
                }
                if (!Path.IsPathRooted(dirName)) {
                    // convert to absolute path
                    dirName = Path.Combine(Path.GetDirectoryName(project.FullName), dirName);
                    dirName = Path.GetFullPath(dirName);
                    var alteredDirName = dirName.Replace(oldDirectory, replacement, StringComparison
                        .OrdinalIgnoreCase);
                    if (alteredDirName == dirName)
                        continue;
                    dirName = alteredDirName;
                } else {
                    dirName = dirName.Replace(oldDirectory, replacement, StringComparison
                        .OrdinalIgnoreCase);
                }
                paths[i] = dirName;
            }
        }

        public static string GetQtDirFromQMakeProject(Project project)
        {
            var vcProject = project.Object as VCProject;
            if (vcProject == null)
                return null;

            try {
                foreach (VCConfiguration projectConfig in vcProject.Configurations as IVCCollection) {
                    var compiler = CompilerToolWrapper.Create(projectConfig);
                    if (compiler != null) {
                        var additionalIncludeDirectories = compiler.AdditionalIncludeDirectories;
                        if (additionalIncludeDirectories != null) {
                            foreach (var dir in additionalIncludeDirectories) {
                                var subdir = Path.GetFileName(dir);
                                if (subdir != "QtCore" && subdir != "QtGui")    // looking for Qt include directories
                                    continue;
                                var dirName = Path.GetDirectoryName(dir);    // cd ..
                                dirName = Path.GetDirectoryName(dirName);       // cd ..
                                if (!Path.IsPathRooted(dirName)) {
                                    var projectDir = Path.GetDirectoryName(project.FullName);
                                    dirName = Path.Combine(projectDir, dirName);
                                    dirName = Path.GetFullPath(dirName);
                                }
                                return dirName;
                            }
                        }
                    }

                    var linker = (VCLinkerTool) ((IVCCollection) projectConfig.Tools).Item("VCLinkerTool");
                    if (linker != null) {
                        var linkerWrapper = new LinkerToolWrapper(linker);
                        var linkerPaths = linkerWrapper.AdditionalDependencies;
                        if (linkerPaths != null) {
                            foreach (var library in linkerPaths) {
                                var idx = library.IndexOf("\\lib\\qtmain.lib", StringComparison.OrdinalIgnoreCase);
                                if (idx == -1)
                                    idx = library.IndexOf("\\lib\\qtmaind.lib", StringComparison.OrdinalIgnoreCase);
                                if (idx == -1)
                                    idx = library.IndexOf("\\lib\\qtcore5.lib", StringComparison.OrdinalIgnoreCase);
                                if (idx == -1)
                                    idx = library.IndexOf("\\lib\\qtcored5.lib", StringComparison.OrdinalIgnoreCase);
                                if (idx == -1)
                                    continue;

                                var dirName = Path.GetDirectoryName(library);
                                dirName = Path.GetDirectoryName(dirName);   // cd ..
                                if (!Path.IsPathRooted(dirName)) {
                                    var projectDir = Path.GetDirectoryName(project.FullName);
                                    dirName = Path.Combine(projectDir, dirName);
                                    dirName = Path.GetFullPath(dirName);
                                }

                                return dirName;
                            }
                        }

                        linkerPaths = linkerWrapper.AdditionalLibraryDirectories;
                        if (linkerPaths != null) {
                            foreach (var libDir in linkerPaths) {
                                var dirName = libDir;
                                if (!Path.IsPathRooted(dirName)) {
                                    var projectDir = Path.GetDirectoryName(project.FullName);
                                    dirName = Path.Combine(projectDir, dirName);
                                    dirName = Path.GetFullPath(dirName);
                                }

                                if (File.Exists(dirName + "\\qtmain.lib") ||
                                    File.Exists(dirName + "\\qtmaind.lib") ||
                                    File.Exists(dirName + "\\QtCore5.lib") ||
                                    File.Exists(dirName + "\\QtCored5.lib")) {
                                    return Path.GetDirectoryName(dirName);
                                }
                            }
                        }
                    }
                }
            } catch { }

            return null;
        }

        /// <summary>
        /// Return true if the project is a Qt project, otherwise false.
        /// </summary>
        /// <param name="proj">project</param>
        /// <returns></returns>
        public static bool IsQtProject(VCProject proj)
        {
            if (!IsQMakeProject(proj))
                return false;

            var envPro = proj.Object as Project;
            if (envPro.Globals == null || envPro.Globals.VariableNames == null)
                return false;

            foreach (var global in envPro.Globals.VariableNames as string[]) {
                if (global.StartsWith("Qt5Version", StringComparison.Ordinal) && envPro.Globals.get_VariablePersists(global))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the specified project is a Qt Project.
        /// </summary>
        /// <param name="proj">project</param>
        public static bool IsQtProject(Project proj)
        {
            try {
                if (proj != null && proj.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                    return IsQtProject(proj.Object as VCProject);
            } catch { }
            return false;
        }

        /// <summary>
        /// Return true if the project is a QMake -tp vc project, otherwise false.
        /// </summary>
        /// <param name="proj">project</param>
        /// <returns></returns>
        public static bool IsQMakeProject(VCProject proj)
        {
            if (proj == null)
                return false;
            var keyword = proj.keyword;
            if (keyword == null || !keyword.StartsWith(Resources.qtProjectKeyword, StringComparison.Ordinal))
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the specified project is a QMake -tp vc Project.
        /// </summary>
        /// <param name="proj">project</param>
        public static bool IsQMakeProject(Project proj)
        {
            try {
                if (proj != null && proj.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                    return IsQMakeProject(proj.Object as VCProject);
            } catch { }
            return false;
        }

        public static void CleanupQMakeDependencies(Project project)
        {
            var vcPro = (VCProject) project.Object;
            // clean up qmake mess
            var rxp1 = new Regex("\\bQt\\w+d?5?\\.lib\\b");
            var rxp2 = new Regex("\\bQAx\\w+\\.lib\\b");
            var rxp3 = new Regex("\\bqtmaind?.lib\\b");
            var rxp4 = new Regex("\\benginiod?.lib\\b");
            foreach (VCConfiguration cfg in (IVCCollection) vcPro.Configurations) {
                var linker = (VCLinkerTool) ((IVCCollection) cfg.Tools).Item("VCLinkerTool");
                if (linker == null || linker.AdditionalDependencies == null)
                    continue;
                var linkerWrapper = new LinkerToolWrapper(linker);
                var deps = linkerWrapper.AdditionalDependencies;
                var newDeps = new List<string>();
                foreach (var lib in deps) {
                    var m1 = rxp1.Match(lib);
                    var m2 = rxp2.Match(lib);
                    var m3 = rxp3.Match(lib);
                    var m4 = rxp4.Match(lib);
                    if (m1.Success)
                        newDeps.Add(m1.ToString());
                    else if (m2.Success)
                        newDeps.Add(m2.ToString());
                    else if (m3.Success)
                        newDeps.Add(m3.ToString());
                    else if (m4.Success)
                        newDeps.Add(m4.ToString());
                    else
                        newDeps.Add(lib);
                }
                // Remove Duplicates
                var uniques = new Dictionary<string, int>();
                foreach (var dep in newDeps)
                    uniques[dep] = 1;
                var uniqueList = new List<string>(uniques.Keys);
                linkerWrapper.AdditionalDependencies = uniqueList;
            }
        }

        /// <summary>
        /// Deletes the file's directory if it is empty (not deleting the file itself so it must
        /// have been deleted before) and every empty parent directory until the first, non-empty
        /// directory is found.
        /// </summary>
        /// <param term='file'>Start point of the deletion</param>
        public static void DeleteEmptyParentDirs(VCFile file)
        {
            var dir = file.FullPath.Remove(file.FullPath.LastIndexOf(Path.DirectorySeparatorChar));
            DeleteEmptyParentDirs(dir);
        }

        /// <summary>
        /// Deletes the directory if it is empty and every empty parent directory until the first,
        /// non-empty directory is found.
        /// </summary>
        /// <param term='file'>Start point of the deletion</param>
        public static void DeleteEmptyParentDirs(string directory)
        {
            var dirInfo = new DirectoryInfo(directory);
            while (dirInfo.Exists && dirInfo.GetFileSystemInfos().Length == 0) {
                var tmp = dirInfo;
                dirInfo = dirInfo.Parent;
                tmp.Delete();
            }
        }

        public static bool HasQObjectDeclaration(VCFile file)
        {
            return CxxFileContainsNotCommented(file, new[] { "Q_OBJECT", "Q_GADGET" },
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

        public static void SetEnvironmentVariableEx(string environmentVariable, string variableValue)
        {
            try {
                Environment.SetEnvironmentVariable(environmentVariable, variableValue);
            } catch {
                throw new QtVSException(SR.GetString("HelperFunctions_CannotWriteEnvQTDIR"));
            }
        }

        public static string ChangePathFormat(string path)
        {
            return path.Replace('\\', '/');
        }

        public static string RemoveFileNameExtension(FileInfo fi)
        {
            var lastIndex = fi.Name.LastIndexOf(fi.Extension, StringComparison.Ordinal);
            return fi.Name.Remove(lastIndex, fi.Extension.Length);
        }

        public static bool IsInFilter(VCFile vcfile, FakeFilter filter)
        {
            var item = (VCProjectItem) vcfile;

            while ((item.Parent != null) && (item.Kind != "VCProject")) {
                item = (VCProjectItem) item.Parent;

                if (item.Kind == "VCFilter") {
                    var f = (VCFilter) item;
                    if (f.UniqueIdentifier != null
                        && f.UniqueIdentifier.ToLower() == filter.UniqueIdentifier.ToLower())
                        return true;
                }
            }
            return false;
        }

        public static void CollapseFilter(UIHierarchyItem item, UIHierarchy hierarchy, string nodeToCollapseFilter)
        {
            if (string.IsNullOrEmpty(nodeToCollapseFilter))
                return;

            foreach (UIHierarchyItem innerItem in item.UIHierarchyItems) {
                if (innerItem.Name == nodeToCollapseFilter)
                    CollapseFilter(innerItem, hierarchy);
                else if (innerItem.UIHierarchyItems.Count > 0)
                    CollapseFilter(innerItem, hierarchy, nodeToCollapseFilter);
            }
        }

        public static void CollapseFilter(UIHierarchyItem item, UIHierarchy hierarchy)
        {
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
            var fileList = new List<string>();

            VCProject vcpro;
            try {
                vcpro = (VCProject) pro.Object;
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
                return null;
            }

            var configurationName = pro.ConfigurationManager.ActiveConfiguration.ConfigurationName;

            foreach (VCFile vcfile in (IVCCollection) vcpro.Files) {
                // Why project files are also returned?
                if (vcfile.ItemName.EndsWith(".vcxproj.filters", StringComparison.Ordinal))
                    continue;
                var excluded = false;
                var fileConfigurations = (IVCCollection) vcfile.FileConfigurations;
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
                    fileList.Add(ChangePathFormat(vcfile.RelativePath));

                // can also be in any filter
                if (IsWinRCFile(vcfile.Name) && (filter == FilesToList.FL_WinResource))
                    fileList.Add(ChangePathFormat(vcfile.RelativePath));

                if (IsGenerated(vcfile)) {
                    if (filter == FilesToList.FL_Generated)
                        fileList.Add(ChangePathFormat(vcfile.RelativePath));
                    continue;
                }

                if (IsResource(vcfile)) {
                    if (filter == FilesToList.FL_Resources)
                        fileList.Add(ChangePathFormat(vcfile.RelativePath));
                    continue;
                }

                switch (filter) {
                case FilesToList.FL_UiFiles: // form files
                    if (IsUicFile(vcfile.Name))
                        fileList.Add(ChangePathFormat(vcfile.RelativePath));
                    break;
                case FilesToList.FL_HFiles:
                    if (IsHeaderFile(vcfile.Name))
                        fileList.Add(ChangePathFormat(vcfile.RelativePath));
                    break;
                case FilesToList.FL_CppFiles:
                    if (IsSourceFile(vcfile.Name))
                        fileList.Add(ChangePathFormat(vcfile.RelativePath));
                    break;
                }
            }

            return fileList;
        }

        /// <summary>
        /// Removes a file reference from the project and moves the file to the "Deleted" folder.
        /// </summary>
        /// <param name="vcpro"></param>
        /// <param name="fileName"></param>
        public static void RemoveFileInProject(VCProject vcpro, string fileName)
        {
            var qtProj = QtProject.Create(vcpro);
            var fi = new FileInfo(fileName);

            foreach (VCFile vcfile in (IVCCollection) vcpro.Files) {
                if (vcfile.FullPath.ToLower() == fi.FullName.ToLower()) {
                    vcpro.RemoveFile(vcfile);
                    qtProj.MoveFileToDeletedFolder(vcfile);
                }
            }
        }

        public static Project GetSelectedProject(DTE dteObject)
        {
            if (dteObject == null)
                return null;
            Array prjs = null;
            try {
                prjs = (Array) dteObject.ActiveSolutionProjects;
            } catch {
                // When VS2010 is started from the command line,
                // we may catch a "Unspecified error" here.
            }
            if (prjs == null || prjs.Length < 1)
                return null;

            // don't handle multiple selection... use the first one
            if (prjs.GetValue(0) is Project)
                return (Project) prjs.GetValue(0);
            return null;
        }

        public static Project GetActiveDocumentProject(DTE dteObject)
        {
            if (dteObject == null)
                return null;
            var doc = dteObject.ActiveDocument;
            if (doc == null)
                return null;

            if (doc.ProjectItem == null)
                return null;

            return doc.ProjectItem.ContainingProject;
        }

        public static Project GetSingleProjectInSolution(DTE dteObject)
        {
            var projectList = ProjectsInSolution(dteObject);
            if (dteObject == null || dteObject.Solution == null ||
                    projectList.Count != 1)
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
            // can happen sometimes shortly after starting VS
            if (dteObject == null || dteObject.Solution == null
                || ProjectsInSolution(dteObject).Count == 0)
                return null;

            Project pro;

            if ((pro = GetSelectedProject(dteObject)) == null) {
                if ((pro = GetSingleProjectInSolution(dteObject)) == null)
                    pro = GetActiveDocumentProject(dteObject);
            }
            return IsQtProject(pro) ? pro : null;
        }

        public static VCFile[] GetSelectedFiles(DTE dteObject)
        {
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
                    vcitem = (VCProjectItem) item.ProjectItem.Object;
                } catch (Exception) {
                    return null;
                }

                if (vcitem.Kind == "VCFile")
                    files[i - 1] = (VCFile) vcitem;
            }
            files[items.Count] = null;
            return files;
        }

        public static Image GetSharedImage(string name)
        {
            Image image = null;
            var a = Assembly.GetExecutingAssembly();
            using (var imgStream = a.GetManifestResourceStream(name)) {
                if (imgStream != null)
                    image = Image.FromStream(imgStream);
            }
            return image;
        }

        public static RccOptions ParseRccOptions(string cmdLine, VCFile qrcFile)
        {
            var pro = VCProjectToProject((VCProject) qrcFile.project);

            var rccOpts = new RccOptions(pro, qrcFile);

            if (cmdLine.Length > 0) {
                var cmdSplit = cmdLine.Split(' ', '\t');
                for (var i = 0; i < cmdSplit.Length; ++i) {
                    var lowercmdSplit = cmdSplit[i].ToLower();
                    if (lowercmdSplit.Equals("-threshold")) {
                        rccOpts.CompressFiles = true;
                        rccOpts.CompressThreshold = int.Parse(cmdSplit[i + 1]);
                    } else if (lowercmdSplit.Equals("-compress")) {
                        rccOpts.CompressFiles = true;
                        rccOpts.CompressLevel = int.Parse(cmdSplit[i + 1]);
                    }
                }
            }
            return rccOpts;
        }

        public static Project VCProjectToProject(VCProject vcproj)
        {
            return (Project) vcproj.Object;
        }

        public static List<Project> ProjectsInSolution(DTE dteObject)
        {
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

        public static int GetMaximumCommandLineLength()
        {
            var epsilon = 10;       // just to be sure :)
            var os = Environment.OSVersion;
            if (os.Version.Major >= 6 ||
                (os.Version.Major == 5 && os.Version.Minor >= 1))
                return 8191 - epsilon;    // Windows XP and above
            return 2047 - epsilon;
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

        public static bool ArraysEqual(Array array1, Array array2)
        {
            if (array1 == array2)
                return true;

            if (array1 == null || array2 == null)
                return false;

            if (array1.Length != array2.Length)
                return false;

            for (var i = 0; i < array1.Length; i++) {
                if (!Equals(array1.GetValue(i), array2.GetValue(i)))
                    return false;
            }
            return true;
        }

        public static string FindFileInPATH(string fileName)
        {
            var envPATH = Environment.ExpandEnvironmentVariables("%PATH%");
            var directories = envPATH.Split(';');
            foreach (var directory in directories) {
                var fullFilePath = directory;
                if (!fullFilePath.EndsWith("\\", StringComparison.Ordinal))
                    fullFilePath += '\\';
                fullFilePath += fileName;
                if (File.Exists(fullFilePath))
                    return fullFilePath;
            }
            return null;
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
        /// <paramref name="project"/>: Current project.<para/>
        /// <paramref name="configName"/>: Name of selected configuration (e.g. "Debug").<para/>
        /// <paramref name="platformName"/>: Name of selected platform (e.g. "x64").<para/>
        /// <paramref name="filePath"/>(optional): Evaluation context.<para/>
        /// </summary>
        public static bool ExpandString(
            ref string stringToExpand,
            EnvDTE.Project project,
            string configName,
            string platformName,
            string filePath = null)
        {
            if (project == null
                || string.IsNullOrEmpty(configName)
                || string.IsNullOrEmpty(platformName))
                return false;

            var vcProject = project.Object as VCProject;

            if (filePath == null) {
                var vcConfig = (from VCConfiguration _config
                                in (IVCCollection)vcProject.Configurations
                                where _config.Name == configName + "|" + platformName
                                select _config).FirstOrDefault();
                return ExpandString(ref stringToExpand, vcConfig);
            } else {
                var vcFile = (from VCFile _file in (IVCCollection)vcProject.Files
                              where _file.FullPath == filePath
                              select _file).FirstOrDefault();
                if (vcFile == null)
                    return false;

                var vcFileConfig = (from VCFileConfiguration _config
                                    in (IVCCollection)vcFile.FileConfigurations
                                    where _config.Name == configName + "|" + platformName
                                    select _config).FirstOrDefault();
                return ExpandString(ref stringToExpand, vcFileConfig);
            }
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
            var vcConfig = config as VCConfiguration;
            if (vcConfig != null) {
                vcProj = vcConfig.project as VCProject;
                configName = vcConfig.ConfigurationName;
                var vcPlatform = vcConfig.Platform as VCPlatform;
                if (vcPlatform != null)
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
                var vcProjConfig = vcFileConfig.ProjectConfiguration as VCConfiguration;
                if (vcProjConfig != null) {
                    configName = vcProjConfig.ConfigurationName;
                    var vcPlatform = vcProjConfig.Platform as VCPlatform;
                    if (vcPlatform != null)
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
                expanded = expanded.Replace(string.Format("$({0})", propName), propValue);
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
                expanded = expanded.Replace(string.Format("%({0})", metaName), metaValue);
            }

            stringToExpand = expanded;
            return true;
        }

        private static string GetRegistrySoftwareString(string subKeyName, string valueName)
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

        public static string GetWindows10SDKVersion()
        {
#if VS2019
            // In Visual Studio 2019: WindowsTargetPlatformVersion=10.0
            // will be treated as "use latest installed Windows 10 SDK".
            // https://developercommunity.visualstudio.com/comments/407752/view.html
            return "10.0";
#else
            string versionWin10SDK = HelperFunctions.GetRegistrySoftwareString(
                @"Microsoft\Microsoft SDKs\Windows\v10.0", "ProductVersion");
            if (string.IsNullOrEmpty(versionWin10SDK))
                return versionWin10SDK;
            while (versionWin10SDK.Split(new char[] { '.' }).Length < 4)
                versionWin10SDK = versionWin10SDK + ".0";
            return versionWin10SDK;
#endif
        }

        static string _VCPath;
        public static string VCPath
        {
            set { _VCPath = value; }
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
#if VS2019
            Debug.Assert(false, "VCPath for VS2019 is not available through the registry");
            string vcPath = string.Empty;
#elif VS2017
            string vsPath = GetRegistrySoftwareString(@"Microsoft\VisualStudio\SxS\VS7", "15.0");
            if (string.IsNullOrEmpty(vsPath))
                return "";
            string vcPath = Path.Combine(vsPath, "VC");
#elif VS2015
            string vcPath = GetRegistrySoftwareString(@"Microsoft\VisualStudio\SxS\VC7", "14.0");
            if (string.IsNullOrEmpty(vcPath))
                return ""; //could not get registry key
#elif VS2013
            string vcPath = GetRegistrySoftwareString(@"Microsoft\VisualStudio\SxS\VC7", "12.0");
            if (string.IsNullOrEmpty(vcPath))
                return ""; //could not get registry key
#endif
            return vcPath;
        }

        public static bool SetVCVars(ProcessStartInfo startInfo)
        {
            bool isOS64Bit = System.Environment.Is64BitOperatingSystem;
            bool isQt64Bit = QtVersionManager.The().GetVersionInfo(
                QtVersionManager.The().GetDefaultVersion()).is64Bit();

            string vcPath = VCPath;
            if (vcPath == "")
                return false;

            string comspecPath = Environment.GetEnvironmentVariable("COMSPEC");
#if (VS2017 || VS2019)
            string vcVarsCmd = "";
            string vcVarsArg = "";
            if (isOS64Bit && isQt64Bit)
                vcVarsCmd = Path.Combine(vcPath, @"Auxiliary\Build\vcvars64.bat");
            else if (!isOS64Bit && !isQt64Bit)
                vcVarsCmd = Path.Combine(vcPath, @"Auxiliary\Build\vcvars32.bat");
            else if (isOS64Bit && !isQt64Bit)
                vcVarsCmd = Path.Combine(vcPath, @"Auxiliary\Build\vcvarsamd64_x86.bat");
            else if (!isOS64Bit && isQt64Bit)
                vcVarsCmd = Path.Combine(vcPath, @"Auxiliary\Build\vcvarsx86_amd64.bat");
#elif VS2015 || VS2013
            string vcVarsCmd = Path.Combine(vcPath, "vcvarsall.bat");
            string vcVarsArg = "";
            if (isOS64Bit && isQt64Bit)
                vcVarsArg = "amd64";
            else if (!isOS64Bit && !isQt64Bit)
                vcVarsArg = "x86";
            else if (isOS64Bit && !isQt64Bit)
                vcVarsArg = "amd64_x86";
            else if (!isOS64Bit && isQt64Bit)
                vcVarsArg = "x86_amd64";
#endif
            const string markSTX = ":@:@:@";
            const string markEOL = ":#:#:#";
            string command =
                string.Format("/c \"{0}\" {1} && echo {2} && set", vcVarsCmd, vcVarsArg, markSTX);
            var vcVarsStartInfo = new ProcessStartInfo(comspecPath, command);
            vcVarsStartInfo.CreateNoWindow = true;
            vcVarsStartInfo.UseShellExecute = false;
            vcVarsStartInfo.RedirectStandardError = true;
            vcVarsStartInfo.RedirectStandardOutput = true;

            var process = Process.Start(vcVarsStartInfo);
            StringBuilder stdOut = new StringBuilder();

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                stdOut.AppendFormat("{0}\n{1}\n", e.Data, markEOL);
            process.BeginOutputReadLine();

            process.WaitForExit();
            bool ok = (process.ExitCode == 0);
            process.Close();
            if (!ok)
                return false;

            SortedDictionary<string, List<string>> vcVars =
                new SortedDictionary<string, List<string>>();
            string[] split =
                stdOut.ToString().Split(new string[] { "\n", "=", ";" }, StringSplitOptions.None);
            int i = 0;
            for (; i < split.Length && split[i].Trim() != markSTX; i++) {
                //Skip to start of data
            }
            i++; //Advance to next item
            for (; i < split.Length && split[i].Trim() != markEOL; i++) {
                //Skip to end of line
            }
            i++; //Advance to next item
            for (; i < split.Length; i++) {
                //Process first item (variable name)
                string key = split[i].ToUpper().Trim();
                i++; //Advance to next item
                List<string> vcVarValue = vcVars[key] = new List<string>();
                for (; i < split.Length && split[i].Trim() != markEOL; i++) {
                    //Process items up to end of line (variable value(s))
                    vcVarValue.Add(split[i].Trim());
                }
            }

            foreach (var vcVar in vcVars) {
                if (vcVar.Value.Count == 1) {
                    startInfo.EnvironmentVariables[vcVar.Key] = vcVar.Value[0];
                } else {
                    if (!startInfo.EnvironmentVariables.ContainsKey(vcVar.Key)) {
                        foreach (var vcVarValue in vcVar.Value) {
                            if (!string.IsNullOrWhiteSpace(vcVarValue)) {
                                startInfo.EnvironmentVariables[vcVar.Key] += vcVarValue + ";";
                            }
                        }
                    } else {
                        string[] startInfoVariableValues = startInfo.EnvironmentVariables[vcVar.Key]
                            .Split(new string[] { ";" }, StringSplitOptions.None);
                        foreach (var vcVarValue in vcVar.Value) {
                            if (!string.IsNullOrWhiteSpace(vcVarValue)
                                && !startInfoVariableValues.Any(s => s.Trim().Equals(
                                    vcVarValue,
                                    StringComparison.OrdinalIgnoreCase))) {
                                if (!startInfo.EnvironmentVariables[vcVar.Key].EndsWith(";"))
                                    startInfo.EnvironmentVariables[vcVar.Key] += ";";
                                startInfo.EnvironmentVariables[vcVar.Key] += vcVarValue + ";";
                            }
                        }
                    }
                }
            }

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
                if (canonicalPath.StartsWith(currentCanonical,
                    StringComparison.InvariantCultureIgnoreCase)) {
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

        public static bool PathEquals(string path1, string path2)
        {
            return (CanonicalPath(path1).Equals(CanonicalPath(path2),
                StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool PathIsRelativeTo(string path, string subPath)
        {
            return CanonicalPath(path).EndsWith(CanonicalPath(subPath),
                StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
