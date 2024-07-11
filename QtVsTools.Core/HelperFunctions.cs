/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core
{
    using MsBuild;
    using static Common.Utils;

    public static class HelperFunctions
    {
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

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            try {
                return Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToUpperInvariant();
            } catch (Exception) {
                return null;
            }
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
            path = ToNativeSeparator(path);

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
            int i = 0, j, commonParts = 0;

            while (i < minLen && string.Equals(fiArray[i], diArray[i], IgnoreCase)) {
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

        public static bool HasQObjectDeclaration(VCFile file)
        {
            var macroNames = Array.Empty<string>();
            var project = file?.project as VCProject;
            var config = project?.ActiveConfiguration;
            if (config?.Rules.Item("QtRule10_Settings") is IVCRulePropertyStorage props)
                macroNames = props.GetEvaluatedPropertyValue("MocMacroNames").Split(';');
            return CxxStream.ContainsNotCommented(file, macroNames, StringComparison.Ordinal, true);
        }

        public static bool MoveToRelativePath(this VCFile file, string relativePath)
        {
            var prevPath = file.FullPath;
            var prevRelativePath = file.RelativePath;
            try {
                file.RelativePath = relativePath;
                Directory.CreateDirectory(Path.GetDirectoryName(file.FullPath));
                File.Move(prevPath, file.FullPath);
                return true;
            } catch {
                file.RelativePath = prevRelativePath;
                return false;
            }
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

        // returns true if some exception occurs
        public static bool IsGenerated(VCFile vcFile)
        {
            try {
                return vcFile.IsInFilter(FakeFilter.GeneratedFiles());
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
                return true;
            }
        }

        // returns false if some exception occurs
        public static bool IsResource(VCFile vcFile)
        {
            try {
                return vcFile.IsInFilter(FakeFilter.ResourceFiles());
            } catch (Exception) {
                return false;
            }
        }
        public static List<string> GetProjectFiles(EnvDTE.Project dteProject, FilesToList filter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetProjectFiles(dteProject?.Object as VCProject, filter);
        }

        public static List<string> GetProjectFiles(MsBuildProject project, FilesToList filter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetProjectFiles(project?.VcProject, filter);
        }

        public static List<string> GetProjectFiles(VCProject vcPro, FilesToList filter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vcPro is not { Files: IVCCollection vcFiles })
                return null;

            var configurationName = vcPro.ActiveConfiguration.ConfigurationName;

            var fileList = new List<string>();
            foreach (VCFile vcFile in vcFiles) {
                if (vcFile.ItemName.EndsWith(".vcxproj.filters", StringComparison.Ordinal))
                    continue; // Why are project files also returned?

                var excluded = false;
                var fileConfigurations = (IVCCollection)vcFile.FileConfigurations;
                foreach (VCFileConfiguration config in fileConfigurations) {
                    if (config.ExcludedFromBuild && config.MatchName(configurationName, false)) {
                        excluded = true;
                        break;
                    }
                }

                if (excluded)
                    continue;

                // can be in any filter
                if (IsTranslationFile(vcFile.Name) && filter == FilesToList.FL_Translation)
                    fileList.Add(FromNativeSeparators(vcFile.RelativePath));

                // can also be in any filter
                if (IsWinRCFile(vcFile.Name) && filter == FilesToList.FL_WinResource)
                    fileList.Add(FromNativeSeparators(vcFile.RelativePath));

                if (IsGenerated(vcFile)) {
                    if (filter == FilesToList.FL_Generated)
                        fileList.Add(FromNativeSeparators(vcFile.RelativePath));
                    continue;
                }

                if (IsResource(vcFile)) {
                    if (filter == FilesToList.FL_Resources)
                        fileList.Add(FromNativeSeparators(vcFile.RelativePath));
                    continue;
                }

                switch (filter) {
                case FilesToList.FL_UiFiles: // form files
                    if (IsUicFile(vcFile.Name))
                        fileList.Add(FromNativeSeparators(vcFile.RelativePath));
                    break;
                case FilesToList.FL_HFiles:
                    if (IsHeaderFile(vcFile.Name))
                        fileList.Add(FromNativeSeparators(vcFile.RelativePath));
                    break;
                case FilesToList.FL_CppFiles:
                    if (IsSourceFile(vcFile.Name))
                        fileList.Add(FromNativeSeparators(vcFile.RelativePath));
                    break;
                case FilesToList.FL_QmlFiles:
                    if (IsQmlFile(vcFile.Name))
                        fileList.Add(FromNativeSeparators(vcFile.RelativePath));
                    break;
                }
            }

            return fileList;
        }

        public static VCProject GetSelectedProject(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                if (dteObject?.ActiveSolutionProjects is not Array projects || projects.Length == 0)
                    return null;
                return (projects.GetValue(0) as Project)?.Object as VCProject;
            } catch (Exception exception) {
                // When VS2010 is started from the command line,
                // we may catch a "Unspecified error" here.
                exception.Log();
            }
            return null;
        }

        /// <summary>
        /// Returns the the current selected Qt Project. If not project is selected
        /// or if the selected project is not a Qt project this function returns null.
        /// </summary>
        public static MsBuildProject GetSelectedQtProject(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Can happen sometimes shortly after starting VS.
            var projectList = ProjectsInSolution(dteObject);
            if (projectList.Count == 0)
                return null;

            VCProject project = null;
            // Grab the first active project.
            if (GetSelectedProject(dteObject) is {} active)
                project = active;

            // Grab the first project out of the list of projects. If there are
            // several projects than there is no way to know which one to select.
            if (projectList.Count == 1 && projectList[0] is {} first)
                project = first;

            // Last try, get the project from an active document.
            if (dteObject?.ActiveDocument?.ProjectItem?.ContainingProject is {} containing)
                project = containing.Object as VCProject;

            return MsBuildProject.GetOrAdd(project);
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

        public static List<VCProject> ProjectsInSolution(DTE dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dteObject == null)
                return new List<VCProject>();

            var projects = new List<VCProject>();
            var solution = dteObject.Solution;
            if (solution != null) {
                var c = solution.Count;
                for (var i = 1; i <= c; ++i) {
                    try {
                        var prj = solution.Projects.Item(i);
                        if (prj == null)
                            continue;
                        AddSubProjects(prj, ref projects);
                    } catch {
                        // Ignore this exception... maybe the next project is ok.
                        // This happens for example for Intel VTune projects.
                    }
                }
            }
            return projects;
        }

        private static void AddSubProjects(Project prj, ref List<VCProject> projects)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If the actual object of the project is null then the project was probably unloaded.
            if (prj.Object == null)
                return;

            // Is this a Visual C++ project?
            if (prj is { ConfigurationManager: {}, Kind: ProjectTypes.C_PLUS_PLUS })
                projects.Add(prj.Object as VCProject);
            else // In this case, prj is a solution folder
                AddSubProjects(prj.ProjectItems, ref projects);
        }

        private static void AddSubProjects(ProjectItems subItems, ref List<VCProject> projects)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (subItems == null)
                return;

            foreach (ProjectItem item in subItems) {
                Project subProject = null;
                try {
                    subProject = item.SubProject;
                } catch {
                    // The property "SubProject" might not be implemented.
                    // This is the case for Intel Fortran projects. (QTBUG-11567)
                }
                if (subProject != null)
                    AddSubProjects(subProject, ref projects);
            }
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
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                return canonicalPath;
            }
            return canonicalPath;
        }
    }
}
