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
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace QtVsTools.Core
{
    /// <summary>
    /// Summary description for ProjectExporter.
    /// </summary>
    public class ProjectExporter
    {
        private readonly DTE dteObject;

        public ProjectExporter(DTE dte)
        {
            dteObject = dte;
        }

        private static void MakeFilesRelativePath(VCProject vcproj, List<string> files, string path)
        {
            for (var i = 0; i < files.Count; i++) {
                var relPath = string.Empty;
                if (files[i].IndexOf(':') != 1) {
                    relPath = HelperFunctions.GetRelativePath(path,
                        vcproj.ProjectDirectory + "\\" + files[i]);
                } else {
                    relPath = HelperFunctions.GetRelativePath(path, files[i]);
                }
                files[i] = HelperFunctions.ChangePathFormat(relPath);
            }
        }

        private static bool ContainsFilesWithSpaces(List<string> files)
        {
            for (var i = 0; i < files.Count; i++) {
                if (files[i].IndexOf(' ') != -1)
                    return true;
            }

            return false;
        }

        public static List<string> ConvertFilesToFullPath(List<string> files, string path)
        {
            var ret = new List<string>(files.Count);
            foreach (var file in files) {
                FileInfo fi;
                if (file.IndexOf(':') != 1)
                    fi = new FileInfo(path + "\\" + file);
                else
                    fi = new FileInfo(file);

                ret.Add(fi.FullName);
            }
            return ret;
        }

        private ProSolution CreateProFileSolution(Solution sln)
        {
            ProFileContent content;
            var prosln = new ProSolution(sln);

            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var proj in HelperFunctions.ProjectsInSolution(sln.DTE)) {
                try {
                    // only add qt projects
                    if (HelperFunctions.IsQtProject(proj)) {
                        content = CreateProFileContent(proj);
                        prosln.ProFiles.Add(content);
                    } else if (proj.Kind == ProjectKinds.vsProjectKindSolutionFolder) {
                        addProjectsInFolder(proj, prosln);
                    }
                } catch {
                    // Catch all exceptions. Try to add as many projects as possible.
                }
            }

            return prosln;
        }

        private void addProjectsInFolder(Project solutionFolder, ProSolution sln)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem pi in solutionFolder.ProjectItems) {
                var containedProject = pi.Object as Project;
                if (HelperFunctions.IsQtProject(containedProject)) {
                    var content = CreateProFileContent(containedProject);
                    sln.ProFiles.Add(content);
                } else if (containedProject.Kind == ProjectKinds.vsProjectKindSolutionFolder) {
                    addProjectsInFolder(containedProject, sln);
                }
            }
        }

        private static ProFileContent CreateProFileContent(Project project)
        {
            ProFileOption option;
            var qtPro = QtProject.Create(project);
            var content = new ProFileContent(qtPro.VCProject);

            ThreadHelper.ThrowIfNotOnUIThread();

            // hack to get active config
            var activeConfig = project.ConfigurationManager.ActiveConfiguration.ConfigurationName;
            var activePlatform = project.ConfigurationManager.ActiveConfiguration.PlatformName;
            var config = (VCConfiguration)((IVCCollection)qtPro.VCProject.Configurations).Item(activeConfig);
            var compiler = CompilerToolWrapper.Create(config);
            var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
            var libTool = (VCLibrarianTool)((IVCCollection)config.Tools).Item("VCLibrarianTool");

            var outPut = config.PrimaryOutput;
            var fi = new FileInfo(outPut);
            var destdir = HelperFunctions.GetRelativePath(qtPro.VCProject.ProjectDirectory, fi.DirectoryName);
            destdir = HelperFunctions.ChangePathFormat(destdir);
            var target = qtPro.VCProject.Name;

            option = new ProFileOption("TEMPLATE");
            option.Comment = Resources.ec_Template;
            option.ShortComment = "Template";
            option.NewOption = null; // just one option...
            option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
            content.Options.Add(option);
            if (config.ConfigurationType == ConfigurationTypes.typeApplication)
                option.List.Add("app");
            else
                option.List.Add("lib");

            option = new ProFileOption("TARGET");
            option.Comment = Resources.ec_Target;
            option.ShortComment = "Target Name";
            option.NewOption = null; // just one option...
            option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
            content.Options.Add(option);
            option.List.Add(target);

            option = new ProFileOption("DESTDIR");
            option.Comment = Resources.ec_DestDir;
            option.ShortComment = "Destination Directory";
            option.NewOption = null; // just one option...
            option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
            content.Options.Add(option);
            option.List.Add(destdir);

            // add the qt option
            option = new ProFileOption("QT");
            var optionQT = option;
            option.Comment = Resources.ec_Qt;
            option.ShortComment = "Qt Options";
            option.NewOption = " "; // just space between the options...
            content.Options.Add(option);

            // add the config option
            option = new ProFileOption("CONFIG");
            var optionCONFIG = option;
            option.Comment = Resources.ec_Config;
            option.ShortComment = "Config Options";
            option.NewOption = " "; // just space between the options...
            content.Options.Add(option);

            AddModules(qtPro, optionQT, optionCONFIG);

            if (config.ConfigurationType == ConfigurationTypes.typeStaticLibrary)
                option.List.Add("staticlib");
            if (linker != null) {
                var generateDebugInformation = (linker is IVCRulePropertyStorage linkerRule) ?
                    linkerRule.GetUnevaluatedPropertyValue("GenerateDebugInformation") : null;
                if (generateDebugInformation != "false")
                    option.List.Add("debug");
                else
                    option.List.Add("release");

                if (linker.SubSystem == subSystemOption.subSystemConsole)
                    option.List.Add("console");

                if (linker.AdditionalDependencies != null) {
                    if (linker.AdditionalDependencies.IndexOf("QAxServer", StringComparison.Ordinal) > -1)
                        option.List.Add("qaxserver");
                    else if (linker.AdditionalDependencies.IndexOf("QAxContainer", StringComparison.Ordinal) > -1)
                        option.List.Add("qaxcontainer");
                    else if (linker.AdditionalDependencies.IndexOf("QtHelp", StringComparison.Ordinal) > -1)
                        option.List.Add("help");
                }
            }

            if (qtPro.IsDesignerPluginProject()) {
                option.List.Add("designer");
                option.List.Add("plugin");
            }

            // add defines
            option = new ProFileOption("DEFINES");
            option.Comment = Resources.ec_Defines;
            option.ShortComment = "Defines";
            option.NewOption = " ";
            option.AssignSymbol = ProFileOption.AssignType.AT_PlusEquals;
            content.Options.Add(option);
            AddPreprocessorDefinitions(option, compiler.GetPreprocessorDefinitions());

            // add the include path option
            option = new ProFileOption("INCLUDEPATH");
            option.Comment = Resources.ec_IncludePath;
            option.ShortComment = "Include Path";
            content.Options.Add(option);
            AddIncludePaths(project, option, compiler.GetAdditionalIncludeDirectories());

            option = new ProFileOption("LIBS");
            option.Comment = Resources.ec_Libs;
            option.ShortComment = "Additional Libraries";
            content.Options.Add(option);
            if (linker != null) {
                AddLibraries(project, option, linker.AdditionalLibraryDirectories,
                    linker.AdditionalDependencies);
            } else if (libTool != null) {
                AddLibraries(project, option, libTool.AdditionalLibraryDirectories,
                    libTool.AdditionalDependencies);
            }

            option = new ProFileOption("PRECOMPILED_HEADER");
            option.Comment = Resources.ec_PrecompiledHeader;
            option.ShortComment = "Using Precompiled Headers";
            option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
            content.Options.Add(option);

            if (qtPro.UsesPrecompiledHeaders())
                option.List.Add(compiler.GetPrecompiledHeaderThrough());

            // add the depend path option
            option = new ProFileOption("DEPENDPATH");
            option.Comment = Resources.ec_DependPath;
            option.ShortComment = "Depend Path";
            content.Options.Add(option);
            option.List.Add(".");

            var mocDir = QtVSIPSettings.GetMocDirectory(project, activeConfig.ToLower(), activePlatform.ToLower());
            mocDir = mocDir.Replace('\\', '/');
            option = new ProFileOption("MOC_DIR");
            option.Comment = Resources.ec_MocDir;
            option.ShortComment = "Moc Directory";
            option.NewOption = null; // just one option...
            content.Options.Add(option);
            option.List.Add(mocDir);

            option = new ProFileOption("OBJECTS_DIR");
            option.Comment = Resources.ec_ObjDir;
            option.ShortComment = "Objects Directory";
            option.NewOption = null; // just one option...
            content.Options.Add(option);
            option.List.Add(config.ConfigurationName.ToLower());

            var uiDir = QtVSIPSettings.GetUicDirectory(project);
            uiDir = uiDir.Replace('\\', '/');
            option = new ProFileOption("UI_DIR");
            option.Comment = Resources.ec_UiDir;
            option.ShortComment = "UI Directory";
            option.NewOption = null; // just one option...
            content.Options.Add(option);
            option.List.Add(uiDir);

            var rccDir = QtVSIPSettings.GetRccDirectory(project);
            rccDir = rccDir.Replace('\\', '/');
            option = new ProFileOption("RCC_DIR");
            option.Comment = Resources.ec_RccDir;
            option.ShortComment = "RCC Directory";
            option.NewOption = null; // just one option...
            content.Options.Add(option);
            option.List.Add(rccDir);

            // add the include path option
            option = new ProFileOption("include");
            option.Comment = Resources.ec_Include;
            option.ShortComment = "Include file(s)";
            option.IncludeComment = false; // print the comment in the output file
            option.AssignSymbol = ProFileOption.AssignType.AT_Function;
            content.Options.Add(option);

            // add the translation files
            option = new ProFileOption("TRANSLATIONS");
            option.Comment = Resources.ec_Translations;
            option.ShortComment = "Translation files";
            option.IncludeComment = false;
            content.Options.Add(option);
            option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_Translation));

            // add the rc file
            if (File.Exists(qtPro.VCProject.ProjectDirectory + "\\" + project.Name + ".rc")) {
                option = new ProFileOption("win32:RC_FILE");
                option.Comment = Resources.ec_rcFile;
                option.ShortComment = "Windows resource file";
                option.IncludeComment = false;
                option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                content.Options.Add(option);
                option.List.Add(project.Name + ".rc");
            }

            if (qtPro.IsDesignerPluginProject()) {
                option = new ProFileOption("target.path");
                option.ShortComment = "Install the plugin in the designer plugins directory.";
                option.IncludeComment = true;
                option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                option.List.Add("$$[QT_INSTALL_PLUGINS]/designer");
                content.Options.Add(option);

                option = new ProFileOption("INSTALLS");
                option.IncludeComment = false;
                option.AssignSymbol = ProFileOption.AssignType.AT_PlusEquals;
                option.List.Add("target");
                content.Options.Add(option);
            }

            return content;
        }

        private static ProFileContent CreatePriFileContent(Project project, string priFileDirectory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProFileOption option;
            var qtPro = QtProject.Create(project);
            var content = new ProFileContent(qtPro.VCProject);
            var hasSpaces = false;

            // add the header files
            option = new ProFileOption("HEADERS");
            option.ShortComment = "Header files";
            option.IncludeComment = false;
            content.Options.Add(option);
            option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_HFiles));
            MakeFilesRelativePath(qtPro.VCProject, option.List, priFileDirectory);
            hasSpaces |= ContainsFilesWithSpaces(option.List);

            // add the source files
            option = new ProFileOption("SOURCES");
            option.ShortComment = "Source files";
            option.IncludeComment = false;
            content.Options.Add(option);
            option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_CppFiles));
            MakeFilesRelativePath(qtPro.VCProject, option.List, priFileDirectory);
            hasSpaces |= ContainsFilesWithSpaces(option.List);

            // add the form files
            option = new ProFileOption("FORMS");
            option.ShortComment = "Forms";
            option.IncludeComment = false;
            content.Options.Add(option);
            option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_UiFiles));
            MakeFilesRelativePath(qtPro.VCProject, option.List, priFileDirectory);
            hasSpaces |= ContainsFilesWithSpaces(option.List);

            // add the translation files
            option = new ProFileOption("TRANSLATIONS");
            option.Comment = Resources.ec_Translations;
            option.ShortComment = "Translation file(s)";
            option.IncludeComment = false;
            option.List.AddRange(HelperFunctions.GetProjectFiles(project, FilesToList.FL_Translation));
            MakeFilesRelativePath(qtPro.VCProject, option.List, priFileDirectory);
            hasSpaces |= ContainsFilesWithSpaces(option.List);
            content.Options.Add(option);

            // add the resource files
            option = new ProFileOption("RESOURCES");
            option.Comment = Resources.ec_Resources;
            option.ShortComment = "Resource file(s)";
            option.IncludeComment = false;
            content.Options.Add(option);

            foreach (var resFile in qtPro.GetResourceFiles())
                option.List.Add(resFile.RelativePath.Replace('\\', '/'));

            if (hasSpaces)
                Messages.DisplayWarningMessage(SR.GetString("ExportProject_PriFileContainsSpaces"));

            return content;
        }

        private static void AddPreprocessorDefinitions(ProFileOption option, string preprocessorDefinitions)
        {
            if (preprocessorDefinitions == null)
                return;

            var excludeList = "UNICODE WIN32 NDEBUG QDESIGNER_EXPORT_WIDGETS ";
            excludeList += "QT_THREAD_SUPPORT QT_PLUGIN QT_NO_DEBUG QT_CORE_LIB QT_GUI_LIB";

            foreach (var define in preprocessorDefinitions.Split(';', ',')) {
                if (excludeList.IndexOf(define, StringComparison.OrdinalIgnoreCase) == -1)
                    option.List.Add(define);
            }
        }

        private static void AddIncludePaths(Project project, ProFileOption option, string includePaths)
        {
            if (QtProject.GetFormatVersion(project) >= Resources.qtMinFormatVersion_ClProperties)
                return;

            if (includePaths == null)
                return;

            var versionManager = QtVersionManager.The();
            var qtDir = versionManager.GetInstallPath(project);
            if (qtDir == null)
                qtDir = Environment.GetEnvironmentVariable("QTDIR");
            if (qtDir == null)
                qtDir = "";

            qtDir = HelperFunctions.NormalizeRelativeFilePath(qtDir);

            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var s in includePaths.Split(';', ',')) {
                var d = HelperFunctions.NormalizeRelativeFilePath(s);
                if (!d.StartsWith("$(qtdir)\\include", StringComparison.OrdinalIgnoreCase) &&
                    !d.StartsWith(qtDir + "\\include", StringComparison.OrdinalIgnoreCase) &&
                    !d.EndsWith("win32-msvc2005", StringComparison.OrdinalIgnoreCase)) {
                    if (project.ConfigurationManager.ActiveConfiguration.Object is VCConfiguration vcConfig)
                        HelperFunctions.ExpandString(ref d, vcConfig);
                    if (HelperFunctions.IsAbsoluteFilePath(d))
                        d = HelperFunctions.GetRelativePath(project.FullName, d);
                    if (!HelperFunctions.IsAbsoluteFilePath(d))
                        option.List.Add(HelperFunctions.ChangePathFormat(d));
                }
            }
        }

        private static void AddLibraries(Project project, ProFileOption option, string paths, string deps)
        {
            if (QtProject.GetFormatVersion(project) < Resources.qtMinFormatVersion_ClProperties)
                return;

            var versionManager = QtVersionManager.The();
            var qtDir = versionManager.GetInstallPath(project);
            if (qtDir == null)
                qtDir = Environment.GetEnvironmentVariable("QTDIR");
            if (qtDir == null)
                qtDir = "";
            qtDir = HelperFunctions.NormalizeRelativeFilePath(qtDir);

            ThreadHelper.ThrowIfNotOnUIThread();

            if (paths != null) {
                foreach (var s in paths.Split(';', ',')) {
                    var d = HelperFunctions.NormalizeRelativeFilePath(s);
                    if (!d.StartsWith("$(qtdir)\\lib", StringComparison.OrdinalIgnoreCase) &&
                        !d.StartsWith(qtDir + "\\lib", StringComparison.OrdinalIgnoreCase)) {
                        if (HelperFunctions.IsAbsoluteFilePath(d))
                            d = HelperFunctions.GetRelativePath(project.FullName, d);
                        if (!HelperFunctions.IsAbsoluteFilePath(d))
                            option.List.Add("-L\"" + HelperFunctions.ChangePathFormat(d) + "\"");
                    }
                }
            }

            if (deps != null) {
                foreach (var d in deps.Split(' ')) {
                    if (d.Length > 0 &&
                        !d.StartsWith("$(qtdir)\\lib", StringComparison.OrdinalIgnoreCase) &&
                        !d.StartsWith(qtDir + "\\lib", StringComparison.OrdinalIgnoreCase) &&
                        !d.StartsWith("qt", StringComparison.OrdinalIgnoreCase) &&
                        !d.StartsWith(".\\qt", StringComparison.OrdinalIgnoreCase) && d != ".")
                        option.List.Add("-l" + HelperFunctions.ChangePathFormat(d).Replace(".lib", ""));
                }
            }
        }

        private static void AddModules(QtProject qtPrj, ProFileOption optionQT, ProFileOption optionCONFIG)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var module in QtModules.Instance.GetAvailableModules()) {
                if (!qtPrj.HasModule(module.Id))
                    continue;

                if (module.proVarQT != null)
                    optionQT.List.Add(module.proVarQT);
                if (module.proVarCONFIG != null)
                    optionCONFIG.List.Add(module.proVarCONFIG);
            }
        }

        private void WriteProSolution(ProSolution prosln, bool openFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var sln = prosln.ProjectSolution;
            if (string.IsNullOrEmpty(sln.FileName))
                return;

            var fi = new FileInfo(sln.FullName);
            var slnDir = fi.Directory;
            var createSlnFile = false;

            if ((slnDir != null) && (prosln.ProFiles.Count > 1)) {
                if (MessageBox.Show(SR.GetString("ExportProject_SolutionProFileBuildIn", slnDir.FullName),
                    SR.GetString("ExportSolution"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    createSlnFile = true;
            }

            if (createSlnFile) {
                StreamWriter sw;
                var slnName = HelperFunctions.RemoveFileNameExtension(fi);
                var slnFileName = slnDir.FullName + "\\" + slnName + ".pro";

                if (File.Exists(slnFileName)) {
                    if (MessageBox.Show(SR.GetString("ExportProject_ExistsOverwriteQuestion", slnFileName),
                        SR.GetString("ExportSolution"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        return;
                }

                try {
                    sw = new StreamWriter(File.Create(slnFileName));
                } catch (Exception e) {
                    Messages.DisplayErrorMessage(e);
                    return;
                }

                var content = new ProFileContent(null);

                var option = new ProFileOption("TEMPLATE");
                option.NewOption = null; // just one option...
                option.AssignSymbol = ProFileOption.AssignType.AT_Equals;
                content.Options.Add(option);
                option.List.Add("subdirs");

                option = new ProFileOption("SUBDIRS");
                option.ShortComment = "#Projects";
                content.Options.Add(option);

                string proFullName, relativePath;
                char[] trimChars = { '\\' };
                foreach (var profile in prosln.ProFiles) {
                    var fiProject = new FileInfo(profile.Project.ProjectFile);
                    var projectBaseName = HelperFunctions.RemoveFileNameExtension(fiProject);
                    proFullName = profile.Project.ProjectDirectory + projectBaseName + ".pro";
                    relativePath = HelperFunctions.GetRelativePath(slnDir.FullName, proFullName);
                    relativePath = relativePath.TrimEnd(trimChars);
                    relativePath = HelperFunctions.ChangePathFormat(relativePath.Remove(0, 2));
                    option.List.Add(relativePath);
                }

                using (sw) {
                    sw.WriteLine(Resources.exportSolutionHeader);
                    for (var i = 0; i < content.Options.Count; i++)
                        WriteProFileOption(sw, content.Options[i]);
                }

                if (openFile)
                    dteObject.OpenFile(Constants.vsViewKindTextView, slnFileName).Activate();
            }
        }

        private void WriteProFile(ProFileContent content, string proFile, string priFileToInclude, bool openFile)
        {
            StreamWriter sw;
            if (File.Exists(proFile)) {
                if (MessageBox.Show(SR.GetString("ExportProject_ExistsOverwriteQuestion", proFile),
                    SR.GetString("ExportSolution"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) {
                    return;
                }
            }

            try {
                sw = new StreamWriter(File.Create(proFile));
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
                return;
            }

            if (!string.IsNullOrEmpty(priFileToInclude)) {
                foreach (var option in content.Options) {
                    if (option.Name == "include" && !option.List.Contains(priFileToInclude)) {
                        var relativePriPath = HelperFunctions.GetRelativePath(Path.GetDirectoryName(proFile), priFileToInclude);
                        if (relativePriPath.StartsWith(".\\", StringComparison.Ordinal))
                            relativePriPath = relativePriPath.Substring(2);
                        relativePriPath = HelperFunctions.ChangePathFormat(relativePriPath);
                        option.List.Add(relativePriPath);
                        break;
                    }
                }
            }
            using (sw) {
                sw.WriteLine(Resources.exportPriHeader);
                WriteProFileOptions(sw, content.Options);
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            if (openFile) // open the file in vs
                dteObject.OpenFile(Constants.vsViewKindTextView, proFile).Activate();
        }

        private void WritePriFile(ProFileContent content, string priFile)
        {
            StreamWriter sw;

            try {
                sw = new StreamWriter(File.Create(priFile));
            } catch (Exception e) {
                Messages.DisplayErrorMessage(e);
                return;
            }

            using (sw) {
                sw.WriteLine(Resources.exportProHeader);
                WriteProFileOptions(sw, content.Options);
            }
        }

        private static void WriteProFileOptions(StreamWriter sw, List<ProFileOption> options)
        {
            foreach (var option in options)
                WriteProFileOption(sw, option);
        }

        private static void WriteProFileOption(StreamWriter sw, ProFileOption option)
        {
            if (option.List.Count <= 0)
                return;

            if (option.IncludeComment)
                sw.WriteLine(sw.NewLine + "#" + option.ShortComment);

            if (option.AssignSymbol != ProFileOption.AssignType.AT_Function) {
                sw.Write(option.Name);

                switch (option.AssignSymbol) {
                case ProFileOption.AssignType.AT_Equals:
                    sw.Write(" = ");
                    break;
                case ProFileOption.AssignType.AT_MinusEquals:
                    sw.Write(" -= ");
                    break;
                case ProFileOption.AssignType.AT_PlusEquals:
                    sw.Write(" += ");
                    break;
                }

                for (var i = 0; i < option.List.Count - 1; i++)
                    sw.Write(option.List[i] + option.NewOption);
                sw.Write(option.List[option.List.Count - 1] + sw.NewLine);
            } else {
                for (var i = 0; i < option.List.Count; i++)
                    sw.WriteLine(option.Name + "(" + option.List[i] + ")");
            }
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
                newPath = path + "\\" + filter.Name;
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
            var cmpPriFiles = new List<string>(priFiles.Count);
            foreach (var s in priFiles)
                cmpPriFiles.Add(HelperFunctions.NormalizeFilePath(s).ToLower());
            cmpPriFiles.Sort();

            var cmpProjFiles = new List<string>(projFiles.Count);
            foreach (var s in projFiles)
                cmpProjFiles.Add(HelperFunctions.NormalizeFilePath(s).ToLower());

            ThreadHelper.ThrowIfNotOnUIThread();

            var qtPro = QtProject.Create(vcproj);
            var filterPathTable = new Hashtable(17);
            var pathFilterTable = new Hashtable(17);
            if (!flat && fakeFilter != null) {
                var rootFilter = qtPro.FindFilterFromGuid(fakeFilter.UniqueIdentifier);
                if (rootFilter == null)
                    qtPro.AddFilterToProject(Filters.SourceFiles());

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

                    var i = path.LastIndexOf('\\');
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
                    HelperFunctions.RemoveFileInProject(vcproj, file);
                    Messages.Print("--- (Importing .pri file) file: " + info.Name +
                        " does not exist in .pri file, move to " + vcproj.ProjectDirectory + "Deleted");
                }
            }
        }

        public void ExportToProFile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var sln = dteObject.Solution;
            var prosln = CreateProFileSolution(sln);

            if (prosln.ProFiles.Count <= 0) {
                Messages.DisplayWarningMessage(SR.GetString("ExportProject_NoProjectsToExport"));
                return;
            }

            var expDlg = new ExportProjectDialog();
            expDlg.ProFileSolution = prosln;
            expDlg.StartPosition = FormStartPosition.CenterParent;
            var ww = new MainWinWrapper(dteObject);
            if (expDlg.ShowDialog(ww) == DialogResult.OK) {
                WriteProSolution(prosln, expDlg.OpenFiles);

                // create all the project .pro files
                foreach (var profile in prosln.ProFiles) {
                    if (profile.Export) {
                        var project = HelperFunctions.VCProjectToProject(profile.Project);
                        string priFile = null;
                        if (expDlg.CreatePriFile)
                            priFile = ExportToPriFile(project);
                        else {
                            var priContent = CreatePriFileContent(project, profile.Project.ProjectDirectory);
                            profile.Options.AddRange(priContent.Options);
                        }
                        WriteProFile(profile, profile.Project.ProjectDirectory + profile.Project.Name + ".pro", priFile, expDlg.OpenFiles);
                    }
                }
            }
        }

        public string ExportToPriFile(Project proj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VCProject vcproj;
            if (HelperFunctions.IsQtProject(proj)) {
                try {
                    vcproj = (VCProject)proj.Object;
                } catch (Exception e) {
                    Messages.DisplayErrorMessage(e);
                    return null;
                }

                // make the user able to choose .pri file
                var fd = new SaveFileDialog();
                fd.OverwritePrompt = true;
                fd.CheckPathExists = true;
                fd.Title = SR.GetString("ExportProject_ExportPriFile");
                fd.Filter = "Project Include Files (*.pri)|*.pri";
                fd.InitialDirectory = vcproj.ProjectDirectory;
                fd.FileName = vcproj.Name + ".pri";

                if (fd.ShowDialog() != DialogResult.OK)
                    return null;

                ExportToPriFile(proj, fd.FileName);
                return fd.FileName;
            }
            return null;
        }

        public void ExportToPriFile(Project proj, string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var priFile = new FileInfo(fileName);
            var content = CreatePriFileContent(proj, priFile.DirectoryName);
            WritePriFile(content, priFile.FullName);
        }
    }
}
