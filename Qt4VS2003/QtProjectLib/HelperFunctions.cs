/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2011 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

namespace Nokia.QtProjectLib
{
    using Microsoft.Win32;
    using Microsoft.VisualStudio.VCProjectEngine;
    using System;
	using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using System.Drawing;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using EnvDTE;
    using System.Xml;

    public class HelperFunctions
    {
        [DllImport("kernel32.dll",CharSet=CharSet.Auto, SetLastError=true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        // used when creating a new template (needs to be static)
        private static System.CodeDom.Compiler.TempFileCollection tmpFiles = null;

        public static string FindQtDirWithTools()
        {
            string empty = null;
            return FindQtDirWithTools(empty);
        }

        public static string FindQtDirWithTools(Project project)
        {
            QtVersionManager versionManager = QtVersionManager.The();
            string projectQtVersion = null;
            if (HelperFunctions.IsQtProject(project))
            {
                projectQtVersion = versionManager.GetProjectQtVersion(project);
            }
            return FindQtDirWithTools(projectQtVersion);
        }

        public static string FindQtDirWithTools(string projectQtVersion)
        {
            string tool = null;
            return FindQtDirWithTools(tool, projectQtVersion);
        }

        public static string FindQtDirWithTools(string tool, string projectQtVersion)
        {
            if (!string.IsNullOrEmpty(tool))
            {
                if (!tool.ToLower().StartsWith("\\bin\\"))
                    tool = "\\bin\\" + tool;
                if (!tool.ToLower().EndsWith(".exe"))
                    tool += ".exe";
            }

            QtVersionManager versionManager = QtVersionManager.The();
            string qtDir = null;
            if (projectQtVersion != null)
                qtDir = versionManager.GetInstallPath(projectQtVersion);

            if (qtDir == null)
                qtDir = System.Environment.GetEnvironmentVariable("QTDIR");

            bool found = false;
            if (tool == null)
                found = File.Exists(qtDir + "\\bin\\designer.exe")
                    && File.Exists(qtDir + "\\bin\\linguist.exe");
            else
                found = File.Exists(qtDir + tool);
            if (!found)
            {
                VersionInformation exactlyMatchingVersion = null;
                VersionInformation matchingVersion = null;
                VersionInformation somehowMatchingVersion = null;
                VersionInformation viProjectQtVersion = versionManager.GetVersionInfo(projectQtVersion);
                foreach (string qtVersion in versionManager.GetVersions())
                {
                    VersionInformation vi = versionManager.GetVersionInfo(qtVersion);
                    if (tool == null)
                        found = File.Exists(vi.qtDir + "\\bin\\designer.exe")
                            && File.Exists(vi.qtDir + "\\bin\\linguist.exe");
                    else
                        found = File.Exists(vi.qtDir + tool);
                    if (!found)
                        continue;

                    if (viProjectQtVersion != null
                        && vi.qtMajor == viProjectQtVersion.qtMajor
                        && vi.qtMinor == viProjectQtVersion.qtMinor)
                    {
                        exactlyMatchingVersion = vi;
                        break;
                    }
                    if (matchingVersion == null
                        && viProjectQtVersion != null
                        && vi.qtMajor == viProjectQtVersion.qtMajor)
                    {
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
                {
                    qtDir = null;
                }
            }
            return qtDir;
        }

        static public bool HasSourceFileExtension(string fileName)
        {
            fileName = fileName.ToLower();
            if (fileName.EndsWith(".cpp") || fileName.EndsWith(".c")
                || fileName.EndsWith(".cxx"))
                return true;
            return false;
        }

        static public bool HasHeaderFileExtension(string fileName)
        {
            fileName = fileName.ToLower();
            if (fileName.EndsWith(".h") || fileName.EndsWith(".hpp")
                || fileName.EndsWith(".hxx"))
                return true;
            return false;
        }

        static public void SetDebuggingEnvironment(EnvDTE.Project prj)
        {
            SetDebuggingEnvironment(prj, "");
        }

        static public void SetDebuggingEnvironment(EnvDTE.Project prj, string solutionConfig)
        {
            SetDebuggingEnvironment(prj, "PATH=$(QTDIR)\\bin;$(PATH)", false, solutionConfig);
        }

        static public void SetDebuggingEnvironment(EnvDTE.Project prj, string envpath, bool overwrite)
        {
            SetDebuggingEnvironment(prj, envpath, overwrite, "");
        }

        static public void SetDebuggingEnvironment(EnvDTE.Project prj, string envpath, bool overwrite, string solutionConfig)
        {
            // Get platform name from given solution configuration
            // or if not available take the active configuration
            String activePlatformName = "";
            if (solutionConfig == null || solutionConfig.Length == 0)
            {
                // First get active configuration cause not given as parameter
                EnvDTE.Configuration activeConf = prj.ConfigurationManager.ActiveConfiguration;
                solutionConfig = activeConf.ConfigurationName + "|" + activeConf.PlatformName;
                activePlatformName = activeConf.PlatformName;
            }
            else
            {
                activePlatformName = solutionConfig.Split('|')[1];
            }

            VCProject vcprj = prj.Object as VCProject;
            foreach (VCConfiguration conf in vcprj.Configurations as IVCCollection)
            {
                // Set environment only for active (or given) platform
                VCPlatform cur_platform = conf.Platform as VCPlatform;
                if (cur_platform.Name != activePlatformName)
                    continue;

                VCDebugSettings de = conf.DebugSettings as VCDebugSettings;
                string withoutPath = envpath.Remove(envpath.LastIndexOf(";$(PATH)"));
                if (overwrite || de.Environment == null || de.Environment.Length == 0)
                    de.Environment = envpath;
                else if (!de.Environment.Contains(envpath) && !de.Environment.Contains(withoutPath))
                {
                    Match m = Regex.Match(de.Environment, "PATH\\s*=\\s*");
                    if (m.Success)
                    {
                        de.Environment = Regex.Replace(de.Environment, "PATH\\s*=\\s*", withoutPath + ";");
                        if (!de.Environment.Contains("$(PATH)") && !de.Environment.Contains("%PATH%"))
                        {
                            if (!de.Environment.EndsWith(";"))
                                de.Environment = de.Environment + ";";
                            de.Environment += "$(PATH)";
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(de.Environment))
                            de.Environment += "\n";
                        de.Environment += envpath;
                    }
                }
            }
        }

        public static bool IsProjectInSolution(EnvDTE.DTE dteObject, string fullName)
        {
            FileInfo fi = new FileInfo(fullName);

            foreach (EnvDTE.Project p in HelperFunctions.ProjectsInSolution(dteObject)) 
            {
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
            FileInfo fi = new FileInfo(name);
            return fi.FullName;
        }

        static public string NormalizeRelativeFilePath(string path)
        {
            if (path == null)
                return ".\\";

            path = path.Trim();
            path = path.Replace("/", "\\");

            string tmp = "";
            while (tmp != path)
            {
                tmp = path;
                path = path.Replace("\\\\", "\\");
            }

            path = path.Replace("\"", "");

            if (path != "." && !IsAbsoluteFilePath(path) && !path.StartsWith(".\\")
                 && !path.StartsWith("$"))
                path = ".\\" + path;

            if (path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);

            return path;
        }

        static public bool IsAbsoluteFilePath(string path)
        {
            path = path.Trim();
            if (path.Length >= 2 && path[1] == ':')
                return true;
            if (path.StartsWith("\\") || path.StartsWith("/"))
                return true;

            return false;
        } 

        /// <summary>
        /// Returns the normalized file path of a given file. If quote
        /// is true the returned string will be set in quotes otherwise
        /// not.
        /// </summary>
        /// <param name="name">file name</param>
        /// <param name="quote">quote</param>
        static public string NormalizeFilePath(string name, bool quote)
        {
            if (quote == true)
                return "\"" + NormalizeFilePath(name) + "\"";
            else
                return NormalizeFilePath(name);
        }

        /// <summary>
        /// Reads lines from a .pro file that is opened with a StreamReader
        /// and concatenates strings that end with a backslash.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns>the composite string</returns>
        static private string ReadProFileLine(StreamReader streamReader)
        {
            string line = streamReader.ReadLine();
            while (line != null && line.EndsWith("\\")) {
                line = line.Remove(line.Length - 1);
                string appendix = streamReader.ReadLine();
                if (appendix != null) line += appendix;
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
            try
            {
                StreamReader sr = new StreamReader(profile);
                string strLine = "";

                while ((strLine = ReadProFileLine(sr)) != null)
                {
                    strLine = strLine.Replace(" ", "").Replace("\t", "").ToLower();
                    if (strLine.StartsWith("template"))
                    {
                        sr.Close();
                        if (strLine.StartsWith("template=subdirs"))
                            return true;
                        return false;
                    }
                }
                sr.Close();
            }
            catch(System.Exception e)
            {
                Messages.DisplayErrorMessage(e);
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
            FileInfo fi = new FileInfo(file);
            DirectoryInfo di = new DirectoryInfo(path);

            char[] separator = { '\\' };
            string[] fiArray = fi.FullName.Split(separator);
            string dir = di.FullName;
            while (dir.EndsWith("\\"))
                dir = dir.Remove(dir.Length-1, 1);
            string[] diArray = dir.Split(separator);
			
            int minLen = fiArray.Length < diArray.Length ? fiArray.Length : diArray.Length;			
            int i=0, j=0, commonParts=0;
			
            while (i<minLen && fiArray[i].ToLower() == diArray[i].ToLower()) 
            {
                commonParts++;
                i++;
            }

            if (commonParts < 1)
                return fi.FullName;

            string result = "";
			
            for (j=i; j<fiArray.Length; j++)
            {
                if (j==i)
                    result = fiArray[j];
                else
                    result += "\\" + fiArray[j];
            }
            while (i<diArray.Length)
            {
                result = "..\\" + result;
                i++;
            }
            //MessageBox.Show(path + "\n" + file + "\n" + result);
            if (result.StartsWith("..\\"))
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
        public static void ReplaceInCustomBuildTools(EnvDTE.Project project, string oldString, string replaceString)
        {
            VCProject vcPro = (VCProject)project.Object;
            if (vcPro == null)
                return;

            foreach (VCFile vcfile in (IVCCollection)vcPro.Files)
            {
                foreach (VCFileConfiguration config in (IVCCollection)vcfile.FileConfigurations)
                {
                    try
                    {
                        VCCustomBuildTool tool = HelperFunctions.GetCustomBuildTool(config);
                        if (tool == null)
                            continue;

                        tool.CommandLine = ReplaceCaseInsensitive(tool.CommandLine, oldString, replaceString);
                        tool.Description = ReplaceCaseInsensitive(tool.Description, oldString, replaceString);
                        tool.Outputs = ReplaceCaseInsensitive(tool.Outputs, oldString, replaceString);
                        tool.AdditionalDependencies = ReplaceCaseInsensitive(tool.AdditionalDependencies, oldString, replaceString);                        
                    }
                    catch (Exception)
                    {
                    }
                }
            }
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
            VCCustomBuildTool tool = config.Tool as VCCustomBuildTool;
            if (tool == null)
                return null;

            try
            {
                string cmdLine = tool.CommandLine;
            }
            catch
            {
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
            foreach (Property prop in projectItem.Properties)
            {
                if (prop.Name == "ItemType")
                {
                    if ((string)prop.Value != "CustomBuild")
                        prop.Value = "CustomBuild";
                    break;
                }
            }
        }

        public static string ReplaceCaseInsensitive(string original,
                    string pattern, string replacement)
        {
            int count, position0, position1;
            count = position0 = position1 = 0;
            string upperString = original.ToUpper();
            string upperPattern = pattern.ToUpper();
            int inc = (original.Length / pattern.Length) *
                      (replacement.Length - pattern.Length);
            char[] chars = new char[original.Length + Math.Max(0, inc)];
            while ((position1 = upperString.IndexOf(upperPattern,
                                              position0)) != -1)
            {
                for (int i = position0; i < position1; ++i)
                    chars[count++] = original[i];
                for (int i = 0; i < replacement.Length; ++i)
                    chars[count++] = replacement[i];
                position0 = position1 + pattern.Length;
            }
            if (position0 == 0) return original;
            for (int i = position0; i < original.Length; ++i)
                chars[count++] = original[i];
            return new string(chars, 0, count);
        }

        /// <summary>
        /// As Qmake -tp vc Adds the full path to the additional dependencies
        /// we need to do the same when toggling project kind to qmake generated.
        /// </summary>
        /// <param name="project">Project</param>
        /// <returns></returns>
        private static string AddFullPathToAdditionalDependencies(string qtDir, string additionalDependencies)
        {
            string returnString = additionalDependencies;
            returnString =
                Regex.Replace(returnString, "Qt(\\S+4?)\\.lib", qtDir + "\\lib\\Qt${1}.lib");
            returnString =
                Regex.Replace(returnString, "(phonond?4?)\\.lib", qtDir + "\\lib\\${1}.lib");
            returnString =
                Regex.Replace(returnString, "(qtmaind?4?)\\.lib", qtDir + "\\lib\\${1}.lib");
            return returnString;
        }

        /// <summary>
        /// Toggles the kind of a project. If the project is a QMake generated project (qmake -tp vc)
        /// it is transformed to an Add-in Qt project and vice versa.
        /// </summary>
        /// <param name="project">Project</param>
        /// <returns></returns>
        public static void ToggleProjectKind(EnvDTE.Project project)
        {
            string qtDir = null;
            VCProject vcPro = (VCProject)project.Object;
            if (!IsQMakeProject(project))
                return;
            if (IsQtProject(project))
            {
                QtProject qtPro = QtProject.Create(project);
                QtVersionManager vm = QtVersionManager.The();
                qtDir = vm.GetInstallPath(project);

                foreach (string global in (string[])project.Globals.VariableNames)
                {
                    if (global.StartsWith("QtVersion"))
                    {
                        project.Globals.set_VariablePersists(global, false);
                    }
                }

                foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
                {
                    CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                    VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
                    VCLibrarianTool librarian = (VCLibrarianTool)((IVCCollection)config.Tools).Item("VCLibrarianTool");
                    if (compiler != null)
                    {
                        string additionalIncludes = compiler.GetAdditionalIncludeDirectories();
                        additionalIncludes = ReplaceCaseInsensitive(additionalIncludes, "$(QTDIR)", qtDir);
                        compiler.SetAdditionalIncludeDirectories(additionalIncludes);
                    }
                    if (linker != null)
                    {
                        linker.AdditionalLibraryDirectories = ReplaceCaseInsensitive(linker.AdditionalLibraryDirectories, "$(QTDIR)", qtDir);
                        linker.AdditionalDependencies = AddFullPathToAdditionalDependencies(qtDir, linker.AdditionalDependencies);
                    }
                    else
                    {
                        librarian.AdditionalLibraryDirectories = ReplaceCaseInsensitive(librarian.AdditionalLibraryDirectories, "$(QTDIR)", qtDir);
                        librarian.AdditionalDependencies = AddFullPathToAdditionalDependencies(qtDir, librarian.AdditionalDependencies);
                    }
                }

                ReplaceInCustomBuildTools(project, "$(QTDIR)", qtDir);
            }
            else
            {
                qtDir = GetQtDirFromQMakeProject(project);

                QtVersionManager vm = QtVersionManager.The();
                string qtVersion = vm.GetQtVersionFromInstallDir(qtDir);
                if (qtVersion == null)
                    qtVersion = vm.GetDefaultVersion();
                if (qtDir == null)
                    qtDir = vm.GetInstallPath(qtVersion);
                VersionInformation vi = vm.GetVersionInfo(qtVersion);
                string platformName = vi.GetVSPlatformName();
                vm.SaveProjectQtVersion(project, qtVersion, platformName);
                QtProject qtPro = QtProject.Create(project);
                if (!qtPro.SelectSolutionPlatform(platformName) || !qtPro.HasPlatform(platformName))
                {
                    bool newProject = false;
                    qtPro.CreatePlatform("Win32", platformName, null, vi, ref newProject);
                    if (!qtPro.SelectSolutionPlatform(platformName))
                    {
                        Messages.PaneMessage(project.DTE, "Can't select the platform " + platformName + ".");
                    }
                }

                string activeConfig = project.ConfigurationManager.ActiveConfiguration.ConfigurationName;
                VCConfiguration activeVCConfig = (VCConfiguration)((IVCCollection)qtPro.VCProject.Configurations).Item(activeConfig);
                if (activeVCConfig.ConfigurationType == ConfigurationTypes.typeDynamicLibrary)
                {
                    CompilerToolWrapper compiler = CompilerToolWrapper.Create(activeVCConfig);
                    VCLinkerTool linker = (VCLinkerTool)((IVCCollection)activeVCConfig.Tools).Item("VCLinkerTool");
                    string ppdefs = compiler.GetPreprocessorDefinitions();
                    if (ppdefs != null
                        && ppdefs.IndexOf("QT_PLUGIN") > -1
                        && ppdefs.IndexOf("QDESIGNER_EXPORT_WIDGETS") > -1
                        && ppdefs.IndexOf("QtDesigner") > -1
                        && linker.AdditionalDependencies != null
                        && linker.AdditionalDependencies.IndexOf("QtDesigner") > -1)
                    {
                        qtPro.MarkAsDesignerPluginProject();
                    }
                }

                HelperFunctions.CleanupQMakeDependencies(project);

                foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
                {
                    CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                    VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                    if (compiler != null)
                    {
                        List<string> additionalIncludes = compiler.AdditionalIncludeDirectories;
                        if (additionalIncludes != null)
                        {
                            ReplaceDirectory(ref additionalIncludes, qtDir, "$(QTDIR)", project);
                            compiler.AdditionalIncludeDirectories = additionalIncludes;
                        }
                    }
                    if (linker != null)
                    {
                        LinkerToolWrapper linkerToolWrapper = new LinkerToolWrapper(linker);
                        List<string> paths = linkerToolWrapper.AdditionalLibraryDirectories;
                        if (paths != null)
                        {
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
        /// <param name="files"></param>
        /// <param name="project">The project is needed to convert relative paths to absolute paths.</param>
        private static void ReplaceDirectory(ref List<string> paths, string oldDirectory, string replacement, Project project)
        {
            for (int i = 0; i < paths.Count; ++i)
            {
                string dirName = paths[i];
                if (dirName.StartsWith("\"") && dirName.EndsWith("\""))
                {
                    dirName = dirName.Substring(1, dirName.Length - 2);
                }
                if (!Path.IsPathRooted(dirName))
                {
                    // convert to absolute path
                    dirName = Path.Combine(Path.GetDirectoryName(project.FullName), dirName);
                    dirName = Path.GetFullPath(dirName);
                    string alteredDirName = ReplaceCaseInsensitive(dirName, oldDirectory, replacement);
                    if (alteredDirName == dirName)
                        continue;
                    dirName = alteredDirName;
                }
                else
                {
                    dirName = ReplaceCaseInsensitive(dirName, oldDirectory, replacement);
                }
                paths[i] = dirName;
            }
        }

        public static string GetQtDirFromQMakeProject(Project project)
        {
            VCProject vcProject = project.Object as VCProject;
            if (vcProject == null)
                return null;

            try
            {
                foreach (VCConfiguration projectConfig in vcProject.Configurations as IVCCollection)
                {
                    CompilerToolWrapper compiler = CompilerToolWrapper.Create(projectConfig);
                    if (compiler != null)
                    {
                        List<string> additionalIncludeDirectories = compiler.AdditionalIncludeDirectories;
                        if (additionalIncludeDirectories != null)
                        {
                            foreach (string dir in additionalIncludeDirectories)
                            {
                                string subdir = Path.GetFileName(dir);
                                if (subdir != "QtCore" && subdir != "QtGui")    // looking for Qt include directories
                                    continue;
                                string dirName = Path.GetDirectoryName(dir);    // cd ..
                                dirName = Path.GetDirectoryName(dirName);       // cd ..
                                if (!Path.IsPathRooted(dirName))
                                {
                                    string projectDir = Path.GetDirectoryName(project.FullName);
                                    dirName = Path.Combine(projectDir, dirName);
                                    dirName = Path.GetFullPath(dirName);
                                }
                                return dirName;
                            }
                        }
                    }

                    VCLinkerTool linker = (VCLinkerTool)((IVCCollection)projectConfig.Tools).Item("VCLinkerTool");
                    if (linker != null)
                    {
                        LinkerToolWrapper linkerWrapper = new LinkerToolWrapper(linker);
                        List<string> linkerPaths = linkerWrapper.AdditionalDependencies;
                        if (linkerPaths != null)
                        {
                            foreach (string library in linkerPaths)
                            {
                                string lowerLibrary = library.ToLower();
                                int idx = lowerLibrary.IndexOf("\\lib\\qtmain.lib");
                                if (idx == -1)
                                    idx = lowerLibrary.IndexOf("\\lib\\qtmaind.lib");
                                if (idx == -1)
                                    idx = lowerLibrary.IndexOf("\\lib\\qtcore4.lib");
                                if (idx == -1)
                                    idx = lowerLibrary.IndexOf("\\lib\\qtcored4.lib");
                                if (idx == -1)
                                    continue;

                                string dirName = Path.GetDirectoryName(library);
                                dirName = Path.GetDirectoryName(dirName);   // cd ..
                                if (!Path.IsPathRooted(dirName))
                                {
                                    string projectDir = Path.GetDirectoryName(project.FullName);
                                    dirName = Path.Combine(projectDir, dirName);
                                    dirName = Path.GetFullPath(dirName);
                                }

                                return dirName;
                            }
                        }

                        linkerPaths = linkerWrapper.AdditionalLibraryDirectories;
                        if (linker != null && linkerPaths != null)
                        {
                            foreach (string libDir in linkerPaths)
                            {
                                string dirName = libDir;
                                if (!Path.IsPathRooted(dirName))
                                {
                                    string projectDir = Path.GetDirectoryName(project.FullName);
                                    dirName = Path.Combine(projectDir, dirName);
                                    dirName = Path.GetFullPath(dirName);
                                }

                                if (File.Exists(dirName + "\\qtmain.lib") ||
                                    File.Exists(dirName + "\\qtmaind.lib") ||
                                    File.Exists(dirName + "\\QtCore4.lib") ||
                                    File.Exists(dirName + "\\QtCored4.lib"))
                                {
                                    return Path.GetDirectoryName(dirName);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

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

            EnvDTE.Project envPro = proj.Object as EnvDTE.Project;
            if (envPro.Globals == null || envPro.Globals.VariableNames == null)
                return false;

            foreach (string global in envPro.Globals.VariableNames as string[])
                if (global.StartsWith("QtVersion") && envPro.Globals.get_VariablePersists(global))
                    return true;
            return false;
        }

        /// <summary>
        /// Returns true if the specified project is a Qt Project.
        /// </summary>
        /// <param name="proj">project</param>
        public static bool IsQtProject(EnvDTE.Project proj)
        {
            try
            {
                if (proj != null && proj.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                {
                    return HelperFunctions.IsQtProject(proj.Object as VCProject);
                }
            }
            catch { }
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
            string keyword = proj.keyword;
            if (keyword == null || !keyword.StartsWith(Resources.qtProjectKeyword))
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the specified project is a QMake -tp vc Project.
        /// </summary>
        /// <param name="proj">project</param>
        public static bool IsQMakeProject(EnvDTE.Project proj)
        {
            try
            {
                if (proj != null && proj.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
                {
                    return HelperFunctions.IsQMakeProject(proj.Object as VCProject);
                }
            }
            catch { }
            return false;
        }

        public static void CleanupQMakeDependencies(EnvDTE.Project project)
        {
            VCProject vcPro = (VCProject)project.Object;
            // clean up qmake mess
            Regex rxp1 = new Regex("\\bQt\\w+d?4?\\.lib\\b");
            Regex rxp2 = new Regex("\\bQAx\\w+\\.lib\\b");
            Regex rxp3 = new Regex("\\bqtmaind?.lib\\b");
            Regex rxp4 = new Regex("\\bphonond?4?\\.lib\\b");
            foreach (VCConfiguration cfg in (IVCCollection)vcPro.Configurations)
            {
                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)cfg.Tools).Item("VCLinkerTool");
                if (linker == null || linker.AdditionalDependencies == null)
                    continue;
                LinkerToolWrapper linkerWrapper = new LinkerToolWrapper(linker);
                List<string> deps = linkerWrapper.AdditionalDependencies;
                List<string> newDeps = new List<string>();
                foreach (string lib in deps)
                {
                    Match m1 = rxp1.Match(lib);
                    Match m2 = rxp2.Match(lib);
                    Match m3 = rxp3.Match(lib);
                    Match m4 = rxp4.Match(lib);
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
                Dictionary<string, int> uniques = new Dictionary<string,int>();
                foreach (string dep in newDeps)
                {
                    uniques[dep] = 1;
                }
                List<string> uniqueList = new List<string>(uniques.Keys);
                linkerWrapper.AdditionalDependencies = uniqueList;
            }
        }

        /// <summary>
        /// Creates a temporary qt project template file. It creates the filters 
        /// in the order specified by the array.
        /// </summary>
        /// <param name="filters">The filters to add to the project file</param>
        /// <returns></returns>
        public static string CreateProjectTemplateFile(FakeFilter[] filters, bool useKeyword, string platformName)
        {
            // just to be safe
            ReleaseProjectTemplateFile();

            StreamWriter sw;
            tmpFiles = new System.CodeDom.Compiler.TempFileCollection();
            tmpFiles.KeepFiles = false;

            string tmpFile = tmpFiles.AddExtension("vcproj");

            try
            {
                if (SR.LanguageName == "ja")
                    sw = new StreamWriter(tmpFile, false, System.Text.Encoding.GetEncoding(932));
                else
                    sw = new StreamWriter(tmpFile, false);
            } 
            catch (System.Exception e)
            {
                Messages.DisplayErrorMessage(e, SR.GetString("HelperFunctions_TryCreatingNewProject"));
                return null;
            }
  
            if (SR.LanguageName == "ja")
                sw.WriteLine("<?xml version=\"1.0\" encoding = \"shift_jis\"?>");
            else
                sw.WriteLine("<?xml version=\"1.0\" encoding = \"Windows-1252\"?>");
            sw.WriteLine("<VisualStudioProject");
            sw.WriteLine("	ProjectType=\"Visual C++\"");
#if VS2005
            sw.WriteLine("	Version=\"8.00\"");
#elif VS2008
            sw.WriteLine("  Version=\"9.00\"");
#else
			sw.WriteLine("	Version=\"7.10\"");
#endif
            sw.WriteLine("	ProjectGUID=\"{B12702AD-ABFB-343A-A199-8E24837244A3}\"");
            if (useKeyword)
                sw.WriteLine("	Keyword=\"" + Resources.qtProjectKeyword + "\">");
            else
                sw.WriteLine(">");

            sw.WriteLine("	<Platforms>");
            sw.WriteLine("		<Platform");
            sw.WriteLine("			Name=\"" + platformName + "\"/>");
            sw.WriteLine("	</Platforms>");
            sw.WriteLine("	<Configurations>");
            sw.WriteLine("		<Configuration");
            sw.WriteLine("			Name=\"Release|" + platformName + "\">");
            sw.WriteLine("		</Configuration>");
            sw.WriteLine("		<Configuration");
            sw.WriteLine("			Name=\"Debug|" + platformName + "\">");
            sw.WriteLine("		</Configuration>");
            sw.WriteLine("	</Configurations>");
            sw.WriteLine("	<Files>");

            for (int i=0; i<filters.Length; i++)
            {
                sw.WriteLine("		<Filter");
                sw.WriteLine("		    Name=\"" + filters[i].Name + "\"");
                sw.WriteLine("			Filter=\"" + filters[i].Filter + "\"");
                if (!filters[i].ParseFiles)
                    sw.WriteLine("			ParseFiles=\"FALSE\"");
                if (!filters[i].SCCFiles)
                    sw.WriteLine("			SourceControlFiles=\"FALSE\"");
                sw.WriteLine("			UniqueIdentifier=\"" + filters[i].UniqueIdentifier + "\">");
                sw.WriteLine("		</Filter>");
            }

            sw.WriteLine("	</Files>");
            sw.WriteLine("</VisualStudioProject>");

            sw.Close();

            return tmpFile;
        }

        /// <summary>
        /// Deletes the file's directory if it is empty (not deleting the file itself so it must
        /// have been deleted before) and every empty parent directory until the first, non-empty
        /// directory is found.
        /// </summary>
        /// <param term='file'>Start point of the deletion</param>
        public static void DeleteEmptyParentDirs(VCFile file)
        {
            string dir = file.FullPath.Remove(file.FullPath.LastIndexOf(Path.DirectorySeparatorChar));
            DeleteEmptyParentDirs(dir);
        }

        /// <summary>
        /// Deletes the directory if it is emptyand every empty parent directory until the first,
        /// non-empty directory is found.
        /// </summary>
        /// <param term='file'>Start point of the deletion</param>
        public static void DeleteEmptyParentDirs(string directory)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            while (dirInfo.Exists && dirInfo.GetFileSystemInfos().Length == 0)
            {
                DirectoryInfo tmp = dirInfo;
                dirInfo = dirInfo.Parent;
                tmp.Delete();
            }
        }

        /// <summary>
        /// Deletes the temporary project file.
        /// </summary>
        public static void ReleaseProjectTemplateFile()
        {
            if (tmpFiles != null)
                tmpFiles.Delete();
        }

        /// <summary>
        /// Returns the template path. The returned path represents the common
        /// template directory of all templates, i.e. the special template
        /// directory e.g. for new projects has to be added.
        /// </summary>
        public static string GetTemplatePath()
        {
            try 
            {
                string path = "c:\\";
                RegistryKey hkcu = Registry.LocalMachine;
                RegistryKey hkQVSIP = hkcu.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
                if (hkQVSIP == null)
                    return path;
                else
                    return (string)(hkQVSIP.GetValue("TemplateBasePath"));
            } 
            catch 
            {
                throw new Qt4VS2003Exception(SR.GetString("HelperFunctions_ErrorSearchForQtTemplatePath"));
            }
        }

        public static bool HasQObjectDeclaration(VCFile file)
        {
            return CxxFileContainsNotCommented(file, new string[] { "Q_OBJECT", "Q_GADGET" }, true, true);
        }

        public static bool CxxFileContainsNotCommented(VCFile file, string str, bool caseSensitive, bool suppressStrings)
        {
            return CxxFileContainsNotCommented(file, new string[] {str}, caseSensitive, suppressStrings);
        }

        public static bool CxxFileContainsNotCommented(VCFile file, string[] searchStrings, bool caseSensitive, bool suppressStrings)
        {
            if (!caseSensitive)
                for (int i = 0; i < searchStrings.Length; ++i)
                    searchStrings[i] = searchStrings[i].ToLower();

            CxxStreamReader sr = null;
            bool found = false;
            try
            {
                string strLine;
                sr = new CxxStreamReader(file.FullPath);
                while (!found && (strLine = sr.ReadLine(suppressStrings)) != null)
                {
                    if (!caseSensitive)
                        strLine = strLine.ToLower();
                    foreach (string str in searchStrings)
                    {
                        if (strLine.IndexOf(str) != -1)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                sr.Close();
            }
            catch (System.Exception)
            {
                if (sr != null)
                    sr.Close();
            }
            return found;
        }

        public static bool SetEnvironmentVariableEx(string environmentVariable, string variableValue)
        {
            try
            {
                EnvironmentPermission environmentPermission = new EnvironmentPermission(EnvironmentPermissionAccess.Write, environmentVariable);
                environmentPermission.Demand();
                return SetEnvironmentVariable(environmentVariable, variableValue);
            }
            catch
            {
                throw new Qt4VS2003Exception(SR.GetString("HelperFunctions_CannotWriteEnvQTDIR"));
            }
        }

        public static string ChangePathFormat(string path)
        {
            return path.Replace('\\', '/');
        }

        public static string RemoveFileNameExtension(FileInfo fi)
        {
            int lastIndex = fi.Name.LastIndexOf(fi.Extension);
            return fi.Name.Remove(lastIndex, fi.Extension.Length);
        }


        public static bool IsInFilter(VCFile vcfile, FakeFilter filter)
        {
            VCProjectItem item = (VCProjectItem)vcfile;

            while((item.Parent != null) && (item.Kind != "VCProject"))
            {
                item = (VCProjectItem)item.Parent;

                if(item.Kind == "VCFilter")
                {
                    VCFilter f = (VCFilter)item;
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

            foreach (UIHierarchyItem innerItem in item.UIHierarchyItems)
            {
                if (innerItem.Name == nodeToCollapseFilter)
                    CollapseFilter(innerItem, hierarchy);
#if VS2008
                // This recursion would expand all filters in VS2005
                else if (innerItem.UIHierarchyItems.Count > 0)
                {
                    CollapseFilter(innerItem, hierarchy, nodeToCollapseFilter);
                }
#endif
            }
        }

        public static void CollapseFilter(UIHierarchyItem item, UIHierarchy hierarchy)
        {
            UIHierarchyItems subItems = item.UIHierarchyItems;
            if (subItems != null)
            {
                foreach (UIHierarchyItem innerItem in subItems)
                {
                    if (innerItem.UIHierarchyItems.Count > 0)
                    {
                        CollapseFilter(innerItem, hierarchy);

                        if (innerItem.UIHierarchyItems.Expanded)
                        {
                            innerItem.UIHierarchyItems.Expanded = false;
                            if (innerItem.UIHierarchyItems.Expanded == true)
                            {
                                innerItem.Select(vsUISelectionType.vsUISelectionTypeSelect);
                                hierarchy.DoDefaultAction();
                            }
                        }
                    }
                }
            }
            if (item.UIHierarchyItems.Expanded)
            {
                item.UIHierarchyItems.Expanded = false;
                if (item.UIHierarchyItems.Expanded == true)
                {
                    item.Select(vsUISelectionType.vsUISelectionTypeSelect);
                    hierarchy.DoDefaultAction();
                }
            }
        }

        // returns true if some exception occurs
        public static bool IsGenerated(VCFile vcfile)
        {
            try 
            {
                return IsInFilter(vcfile, Filters.GeneratedFiles());
            }
            catch(System.Exception e)
            {
                MessageBox.Show(e.ToString());
                return true;
            }
        }

        // returns true if the file is a rc file (.rc)
        public static bool IsWinRCFile(VCFile vcfile)
        {
            if (vcfile == null)
                return false;

            if (vcfile.Extension.ToLower() == ".rc")
                return true;

            return false;
        }

        // returns true if the file is a translation file (.ts)
        public static bool IsTranslationFile(VCFile vcfile)
        {
            if (vcfile == null)
                return false;

            if (vcfile.Extension.ToLower() == ".ts")
                return true;

            return false;
        }

        // returns false if some exception occurs
        public static bool IsResource(VCFile vcfile)
        {
            try 
            {
                return IsInFilter(vcfile, Filters.ResourceFiles());
            }
            catch(System.Exception)
            {
                return false;
            }
        }

        public static List<string> GetProjectFiles(EnvDTE.Project pro, FilesToList filter)
        {
            List<string> fileList = new List<string>();

            VCProject vcpro;
            try
            {
                vcpro = (VCProject)pro.Object;
            }
            catch(System.Exception e)
            {
                Messages.DisplayErrorMessage(e);
                return null;
            }

            string configurationName = pro.ConfigurationManager.ActiveConfiguration.ConfigurationName;

            foreach(VCFile vcfile in (IVCCollection)vcpro.Files)

            {
                bool excluded = false;
                IVCCollection fileConfigurations = (IVCCollection)vcfile.FileConfigurations;
                foreach (VCFileConfiguration config in fileConfigurations)
                {
                    if (config.ExcludedFromBuild && config.MatchName(configurationName, false)) {
                        excluded = true;
                        break;
                    }
                }

                if (excluded)
                    continue;

                // can be in any filter
                if ((IsTranslationFile(vcfile)) &&
                    (filter == FilesToList.FL_Translation))
                    fileList.Add(ChangePathFormat(vcfile.RelativePath));

                // can also be in any filter
                if ((IsWinRCFile(vcfile)) &&
                    (filter == FilesToList.FL_WinResource))
                    fileList.Add(ChangePathFormat(vcfile.RelativePath));

                if (IsGenerated(vcfile))
                {
                    if (filter == FilesToList.FL_Generated)
                        fileList.Add(ChangePathFormat(vcfile.RelativePath));
                    continue;
                }

                if (IsResource(vcfile))
                {
                    if (filter == FilesToList.FL_Resources)
                        fileList.Add(ChangePathFormat(vcfile.RelativePath));
                    continue;
                }

                switch (filter)
                {
                    case FilesToList.FL_UiFiles: // form files
                        if (vcfile.Extension.ToLower() == ".ui")
                            fileList.Add(ChangePathFormat(vcfile.RelativePath));
                        break;
                    case FilesToList.FL_HFiles:
                        if (HelperFunctions.HasHeaderFileExtension(vcfile.Name))
                            fileList.Add(ChangePathFormat(vcfile.RelativePath));
                        break;
                    case FilesToList.FL_CppFiles:
                        if (HelperFunctions.HasSourceFileExtension(vcfile.Name))
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
            QtProject qtProj = QtProject.Create(vcpro);
            FileInfo fi = new FileInfo(fileName);

            foreach(VCFile vcfile in (IVCCollection)vcpro.Files)
            {
                if (vcfile.FullPath.ToLower() == fi.FullName.ToLower())
                {
                    vcpro.RemoveFile(vcfile);
                    qtProj.MoveFileToDeletedFolder(vcfile);
                }
            }
        }

        /// <summary>
        /// Returns true if the filter specified by its guid is currently
        /// selected and the filter is part of a Qt project. Otherwise, this
        /// function returns false.
        /// </summary>
        /// <param name="filterguid">A FakeFilter object.</param>
        public static bool IsFilterSelected(EnvDTE.DTE dteObject, FakeFilter filter)
        {
            if (dteObject == null)
                return false;

			try
			{
				System.Guid guid = new Guid("{6bb5f8f0-4483-11d3-8bcf-00c04f8ec28c}");
				EnvDTE.SelectedItems itms = dteObject.SelectedItems;

				foreach (EnvDTE.SelectedItem selItem in itms)
				{
					if (selItem.ProjectItem == null)
						continue;
					string kindGuid = selItem.ProjectItem.Kind.Substring(1, selItem.ProjectItem.Kind.Length - 2);
					if (kindGuid.ToLower() == guid.ToString().ToLower())
					{
						VCFilter filt = (VCFilter)selItem.ProjectItem.Object;
						if (filt != null && filt.UniqueIdentifier != null
							&& filt.UniqueIdentifier.ToLower() == filter.UniqueIdentifier.ToLower()
							&& HelperFunctions.IsQtProject(GetSelectedQtProject(dteObject)))
							return true;
					}
				}
				return false;
			}
			catch {}
        
            return false;        
        }

        public static EnvDTE.Project GetSelectedProject(EnvDTE.DTE dteObject)
        {
			if (dteObject == null)
				return null;
            System.Array prjs = null;
            try
            {
                prjs = (System.Array)dteObject.ActiveSolutionProjects;
            }
            catch
            {
                // When VS2010 is started from the command line,
                // we may catch a "Unspecified error" here.
            }
            if (prjs == null || prjs.Length < 1)
                return null;
        
            // don't handle multiple selection... use the first one
            if (prjs.GetValue(0) is EnvDTE.Project)
                return (EnvDTE.Project)prjs.GetValue(0);
            return null;
        }

        public static EnvDTE.Project GetActiveDocumentProject(EnvDTE.DTE dteObject)
        {
			if (dteObject == null)
				return null;
            EnvDTE.Document doc = dteObject.ActiveDocument;
            if (doc == null) 
                return null;

            if (doc.ProjectItem == null)
                return null;

            return doc.ProjectItem.ContainingProject;
        }

        public static EnvDTE.Project GetSingleProjectInSolution(EnvDTE.DTE dteObject)
        {
            List<Project> projectList = ProjectsInSolution(dteObject);
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
        public static EnvDTE.Project GetSelectedQtProject(EnvDTE.DTE dteObject)
        {
            // can happen sometimes shortly after starting VS
            if (dteObject == null || dteObject.Solution == null
                || HelperFunctions.ProjectsInSolution(dteObject).Count == 0)
                return null;

            EnvDTE.Project pro;
            
            if((pro = GetSelectedProject(dteObject)) == null)
                if ((pro = GetSingleProjectInSolution(dteObject)) == null)
                    pro = GetActiveDocumentProject(dteObject);

            return HelperFunctions.IsQtProject(pro) ? pro : null;
        }

        public static VCFile GetSelectedFile(EnvDTE.DTE dteObject)
        {
            if (GetSelectedQtProject(dteObject) == null)
                return null;

            if (dteObject.SelectedItems.Count <= 0)
                return null;

            // choose the first one
            EnvDTE.SelectedItem item = dteObject.SelectedItems.Item(1);
            
            if (item.ProjectItem == null)
                return null;
            
            VCProjectItem vcitem;
            try
            {
                vcitem = (VCProjectItem)item.ProjectItem.Object;
            }
            catch(System.Exception)
            {
                return null;
            }

            if (vcitem.Kind == "VCFile")
                return (VCFile)vcitem;

            return null;
        }

        public static VCFile[] GetSelectedFiles(EnvDTE.DTE dteObject)
        {
            if (GetSelectedQtProject(dteObject) == null)
                return null;

            if (dteObject.SelectedItems.Count <= 0)
                return null;

            EnvDTE.SelectedItems items = dteObject.SelectedItems;

            VCFile[] files = new VCFile[items.Count +1];
            for (int i = 1; i <= items.Count; ++i)
            {
                EnvDTE.SelectedItem item = items.Item(i); 
                if (item.ProjectItem == null)
                    continue;

                VCProjectItem vcitem;
                try
                {
                    vcitem = (VCProjectItem)item.ProjectItem.Object;
                }
                catch (System.Exception)
                {
                    return null;
                }

                if (vcitem.Kind == "VCFile")
                    files[i-1] = (VCFile)vcitem;
            }
            files[items.Count] = null;
            return files;
        }
        
        public static Image GetSharedImage(string name)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            Image image = null;
            name = "Nokia." + name;
            Stream imgStream = a.GetManifestResourceStream(name);
            if(imgStream != null)
            {                    
                image = Image.FromStream(imgStream);
                imgStream.Close();
            }
            return image;
        }

        public static RccOptions ParseRccOptions(string cmdLine, VCFile qrcFile)
        {
            EnvDTE.Project pro = HelperFunctions.VCProjectToProject((VCProject)qrcFile.project);

            RccOptions rccOpts = new RccOptions(pro, qrcFile);
            
            if (cmdLine.Length > 0) 
            {
                string[] cmdSplit = cmdLine.Split(new Char[] {' ','\t'});
                for (int i=0; i<cmdSplit.Length; ++i) 
                {
                    string lowercmdSplit = cmdSplit[i].ToLower();
                    if (lowercmdSplit.Equals("-threshold")) 
                    {                        
                        rccOpts.CompressFiles = true;
                        rccOpts.CompressThreshold = int.Parse(cmdSplit[i+1]);
                    }
                    else if (lowercmdSplit.Equals("-compress"))
                    {
                        rccOpts.CompressFiles = true;
                        rccOpts.CompressLevel = int.Parse(cmdSplit[i+1]);
                    }
                }
            }
            return rccOpts;
        }        

        public static EnvDTE.Project VCProjectToProject(VCProject vcproj)
        {
			return (EnvDTE.Project)vcproj.Object;
        }

        public static List<EnvDTE.Project> ProjectsInSolution(EnvDTE.DTE dteObject)
        {
            List<EnvDTE.Project> projects = new List<EnvDTE.Project>();
            Solution solution = dteObject.Solution;
            if (solution != null)
            {
                int c = solution.Count;
                for (int i = 1; i <= c; ++i)
                {
                    try
                    {
                        Project prj = solution.Projects.Item(i) as Project;
                        if (prj == null)
                            continue;
                        addSubProjects(prj, ref projects);
                    }
                    catch
                    {
                        // Ignore this exception... maybe the next project is ok.
                        // This happens for example for Intel VTune projects.
                    }
                }
            }
            return projects;
        }

        private static void addSubProjects(EnvDTE.Project prj, ref List<Project> projects)
        {
            // If the actual object of the project is null then the project was probably unloaded.
            if (prj.Object == null)
                return;

            if (prj.ConfigurationManager != null &&
                // Is this a Visual C++ project?
                prj.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")
            {
                projects.Add(prj);
            }
            else
            {
                // In this case, prj is a solution folder
                addSubProjects(prj.ProjectItems, ref projects);
            }
        }

        private static void addSubProjects(EnvDTE.ProjectItems subItems, ref List<Project> projects)
        {
            if (subItems == null)
                return;

            foreach (ProjectItem item in subItems)
            {
                Project subprj = null;
                try
                {
                    subprj = item.SubProject;
                }
                catch
                {
                    // The property "SubProject" might not be implemented.
                    // This is the case for Intel Fortran projects. (QTBUG-11567)
                }
                if (subprj != null)
                    addSubProjects(subprj, ref projects);
            }
        }

        public static int GetMaximumCommandLineLength()
        {
            const int epsilon = 10;       // just to be sure :)
            System.OperatingSystem os = System.Environment.OSVersion;
            if (os.Version.Major >= 6 ||
                (os.Version.Major == 5 && os.Version.Minor >= 1))
                return 8191 - epsilon;    // Windows XP and above
            else
                return 2047 - epsilon;
        }

        /// <summary>
        /// Translates the machine type given as command line argument to the linker
        /// to the internal enum type VCProjectEngine.machineTypeOption.
        /// </summary>
        public static machineTypeOption TranslateMachineType(string cmdLineMachine)
        {
            switch (cmdLineMachine.ToUpper())
            {
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

        private static List<string> availablePlatforms;

        /// <summary>
        /// Returns true if the given platform is available in the global settings of Visual Studio.
        /// On error this function returns false.
        /// </summary>
        public static bool IsPlatformAvailable(EnvDTE.DTE dteObject, string platformName)
        {
            if (availablePlatforms == null || availablePlatforms.Count == 0)
            {
                availablePlatforms = new List<string>();
#if VS2005
                // Read the available platforms from WCE.VCPlatform.config
                // instead of using VCProjectEngine, because the project wizards aren't
                // able to list the platforms if VS2005 is used.
                String vcPlatformCfg = dteObject.FullName;
                int idx = vcPlatformCfg.LastIndexOf("\\");
                idx = vcPlatformCfg.LastIndexOf("\\", idx - 1);
                idx = vcPlatformCfg.LastIndexOf("\\", idx - 1);
                vcPlatformCfg = vcPlatformCfg.Substring(0, idx + 1);
                vcPlatformCfg += "VC\\vcpackages\\WCE.VCPlatform.config";

                FileStream stream = new FileStream(vcPlatformCfg, FileMode.Open);
                XmlReader reader = new XmlTextReader(stream);
                while (reader.ReadToFollowing("PlatformName"))
                {
                    availablePlatforms.Add(reader.ReadElementContentAsString());
                }
#else
                VCProjectEngine engine = new VCProjectEngineObject();
                IVCCollection platforms = engine.Platforms as IVCCollection;
                foreach (VCPlatform platform in platforms)
                {
                    availablePlatforms.Add(platform.Name);
                }
#endif
            }

            if (availablePlatforms == null)
                return false;

            return availablePlatforms.Contains(platformName);
        }

        public static bool ArraysEqual(Array array1, Array array2)
        {
            if (array1 == array2)
                return true;

            if (array1 == null || array2 == null)
                return false;

            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
                if (!Object.Equals(array1.GetValue(i), array2.GetValue(i)))
                    return false;
            return true;
        }

        private static QProcess proc = null;

        public static void StartExternalQtApplication(string application, string arguments, string workingDir,
            EnvDTE.Project project, bool checkExitCode, Hashtable errorCodes)
        {
            proc = new QProcess();
            proc.ErrorCodes = errorCodes;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.EnableRaisingEvents = true;
            proc.StartInfo.WorkingDirectory = workingDir;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardOutput = true;

            EnvDTE.DTE dte = project.DTE;
            Messages.ActivateMessagePane();
            string qtDir = HelperFunctions.FindQtDirWithTools(project);

            proc.StartInfo.FileName = qtDir + application;
            proc.StartInfo.Arguments = arguments;
            if (checkExitCode && application.ToLower().IndexOf("uic.exe") > -1)
                proc.Exited += new EventHandler(QtApplicationExited);

            try
            {
                proc.Start();
                if (checkExitCode && application.ToLower().IndexOf("lupdate.exe") > -1 ||
                    checkExitCode && application.ToLower().IndexOf("lrelease.exe") > -1)
                {
                    System.Threading.Thread errorThread 
                        = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReadQtStandardError));

                    errorThread.Start(dte);
                    proc.WaitForExit();
                    errorThread.Join();

                    int exitCode = proc.ExitCode;
                    if (exitCode == 0)
                    {
                        string arg = arguments;
                        int index = arg.IndexOf("-ts");
                        string file = "file: " + arg + " ";
                        if (index > 0)
                            file = "file: " + arg.Substring(index + 3) + " ";

                        FileInfo info = new FileInfo(application);
                        Messages.PaneMessage(project.DTE, "--- (" + 
                            HelperFunctions.RemoveFileNameExtension(info) + ") " + 
                            file + ": Exit Code: " + exitCode);
                    }
                    else
                    {
                        DisplayErrorMessage(proc);
                    }

                    proc.Close();
                }
            }
            catch
            {
                throw new Qt4VS2003Exception(SR.GetString("Helpers_CannotStart", proc.StartInfo.FileName));
            }
        }

        private static void ReadQtStandardError(object param)
        {
            DTE dte = param as DTE;
            if (proc == null)
                return;

            string error;
            while ((error = proc.StandardError.ReadLine()) != null)
            {
                error = error.Trim();
                if (error.Length != 0)
                    Messages.PaneMessage(dte, "--- " + error);
            }
        }

        private static void QtApplicationExited(object sender, EventArgs e)
        {
            QProcess proc = (QProcess)sender;
            DisplayErrorMessage(proc);
            proc.Exited -= new EventHandler(QtApplicationExited);
        }

        private static void DisplayErrorMessage(QProcess proc)
        {
            if (proc.ExitCode != 0)
            {
                if (proc.solutionString(proc.ExitCode) != null)
                    Messages.DisplayErrorMessage(SR.GetString("Helpers_ExitError", proc.ExitCode.ToString())
                        + "\r\n" + proc.errorString(proc.ExitCode),
                        proc.solutionString(proc.ExitCode));
                else
                    Messages.DisplayErrorMessage(SR.GetString("Helpers_ExitError", proc.ExitCode.ToString())
                        + "\r\n" + proc.errorString(proc.ExitCode));
            }
        }

        public static string FindFileInPATH(string fileName)
        {
            string envPATH = System.Environment.ExpandEnvironmentVariables("%PATH%");
            string[] directories = envPATH.Split(new Char[] { ';' });
            foreach (string directory in directories)
            {
                string fullFilePath = directory;
                if (!fullFilePath.EndsWith("\\")) fullFilePath += '\\';
                fullFilePath += fileName;
                if (File.Exists(fullFilePath))
                    return fullFilePath;
            }
            return null;
        }
    }
}
