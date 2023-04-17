/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core
{
    using QtMsBuild;
    using static Utils;

    /// <summary>
    /// QtProject holds the Qt specific properties for a Visual Studio project.
    /// There exists at most one QtProject per EnvDTE.Project.
    /// Use QtProject.Create to get the QtProject for a Project or VCProject.
    /// </summary>
    public class QtProject
    {
        private DTE dte;
        private Project envPro;
        private VCProject vcPro;
        private static readonly Dictionary<Project, QtProject> instances = new();
        private readonly QtMsBuildContainer qtMsBuild;

        public static VisualStudio.IProjectTracker ProjectTracker { get; set; }

        public static QtProject Create(VCProject vcProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Create((Project)vcProject.Object);
        }

        public static QtProject Create(Project project)
        {
            QtProject qtProject = null;
            if (project != null && !instances.TryGetValue(project, out qtProject)) {
                qtProject = new QtProject(project);
                instances.Add(project, qtProject);
            }
            return qtProject;
        }

        public static void ClearInstances()
        {
            instances.Clear();
        }

        private QtProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                throw new QtVSException("Cannot construct a QtProject object without a valid project.");
            envPro = project;
            dte = envPro.DTE;
            vcPro = envPro.Object as VCProject;
            qtMsBuild = new QtMsBuildContainer(new VcPropertyStorageProvider());
        }

        public VCProject VCProject => vcPro;

        public Project Project => envPro;

        public static string GetRuleName(VCConfiguration config, string itemType)
        {
            if (config == null)
                return string.Empty;
            try {
                return config.GetEvaluatedPropertyValue(itemType + "RuleName");
            } catch (Exception exception) {
                exception.Log();
                return string.Empty;
            }
        }

        public static bool IsQtMsBuildEnabled(VCProject project)
        {
            try {
                if (project?.Configurations is IVCCollection configs) {
                    if (configs.Count == 0)
                        return false;
                    var firstConfig = configs.Item(1) as VCConfiguration;
                    var ruleName = GetRuleName(firstConfig, QtMoc.ItemTypeName);
                    return firstConfig?.Rules.Item(ruleName) is IVCRulePropertyStorage;
                }
            } catch (Exception) {
                return false;
            }
            return false;
        }

        public static bool IsQtMsBuildEnabled(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                return false;
            return IsQtMsBuildEnabled(project.Object as VCProject);
        }

        private bool? isQtMsBuildEnabled;
        public bool IsQtMsBuildEnabled()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!isQtMsBuildEnabled.HasValue) {
                if (vcPro != null)
                    isQtMsBuildEnabled = IsQtMsBuildEnabled(vcPro);
                else if (envPro != null)
                    isQtMsBuildEnabled = IsQtMsBuildEnabled(envPro);
                else
                    return false;
            }
            return isQtMsBuildEnabled.Value;
        }

        /// <summary>
        /// Returns the moc-generated file name for the given source or header file.
        /// </summary>
        /// <param name="file">header or source file in the project</param>
        /// <returns></returns>
        private static string GetMocFileName(string file)
        {
            var fi = new FileInfo(file);

            var name = fi.Name;
            if (HelperFunctions.IsHeaderFile(fi.Name))
                return "moc_" + name.Substring(0, name.LastIndexOf('.')) + ".cpp";
            if (HelperFunctions.IsSourceFile(fi.Name))
                return name.Substring(0, name.LastIndexOf('.')) + ".moc";
            return null;
        }

        public static int GetFormatVersion(VCProject vcPro)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (vcPro == null)
                return 0;

            if (vcPro.keyword.StartsWith(Resources.qtProjectKeyword, StringComparison.Ordinal))
                return Convert.ToInt32(vcPro.keyword.Substring(6));

            if (!vcPro.keyword.StartsWith(Resources.qtProjectV2Keyword, StringComparison.Ordinal))
                return 0;

            if (vcPro.Object is not Project { Globals: { VariableNames: string[] variables }} envPro)
                return 100;

            return variables.Any(var => HelperFunctions.HasQt5Version(var, envPro)) ? 200 : 100;
        }

        public static int GetFormatVersion(Project pro)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetFormatVersion(pro?.Object as VCProject);
        }

        public int FormatVersion
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return GetFormatVersion(Project);
            }
        }

        public static string GetPropertyValue(
            Project dteProject,
            string propName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var activeConfig = dteProject.ConfigurationManager?.ActiveConfiguration;
            if (activeConfig == null)
                return null;
            return GetPropertyValue(
                dteProject, activeConfig, propName);
        }

        public static string GetPropertyValue(
            Project dteProject,
            Configuration dteConfig,
            string propName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dteProject == null || dteConfig == null)
                return null;
            return GetPropertyValue(
                dteProject.Object as VCProject,
                dteConfig.ConfigurationName,
                dteConfig.PlatformName,
                propName);
        }

        public static string GetPropertyValue(
            VCProject vcProject,
            string configName,
            string platformName,
            string propName)
        {
            if (vcProject.Configurations is IVCCollection vcConfigs) {
                var configId = $"{configName}|{platformName}";
                if (vcConfigs.Item(configId) is VCConfiguration vcConfig)
                    return GetPropertyValue(vcConfig, propName);
            }
            return null;
        }

        public static string GetPropertyValue(
            VCConfiguration vcConfig,
            string propName)
        {
            return vcConfig.GetEvaluatedPropertyValue(propName);
        }

        /// <summary>
        /// This function adds a uic4 build step to a given file.
        /// </summary>
        /// <param name="file">file</param>
        public void AddUic4BuildStep(VCFile file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (GetFormatVersion(vcPro) >= Resources.qtMinFormatVersion_Settings) {
                file.ItemType = QtUic.ItemTypeName;
            } else {
                // TODO: It would be nice if we can inform the user he's on an old project.
                //if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                //    Notifications.UpdateProjectFormat.Show();
            }
        }

        /// <summary>
        /// Adds a moc step to a given file for this project.
        /// </summary>
        /// <param name="file">file</param>
        public void AddMocStep(VCFile file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (GetFormatVersion(vcPro) >= Resources.qtMinFormatVersion_Settings) {
                file.ItemType = QtMoc.ItemTypeName;
                if (!HelperFunctions.IsSourceFile(file.FullPath))
                    return;
                foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations) {
                    qtMsBuild.SetItemProperty(config, QtMoc.Property.DynamicSource, "input");
                    qtMsBuild.SetItemPropertyByName(config, "QtMocFileName", "%(Filename).moc");
                }
            } else {
                // TODO: It would be nice if we can inform the user he's on an old project.
                //if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                //    Notifications.UpdateProjectFormat.Show();
            }
        }

        /// <summary>
        /// Parses the given file to find an occurrence of a moc.exe generated file include. If
        /// the given file is a header file, the function tries to find the corresponding source
        /// file to use it instead of the header file. Helper function for AddMocStep.
        /// </summary>
        /// <param name="vcFile">Header or source file name.</param>
        /// <returns>
        /// Returns true if the file contains an include of the corresponding moc_xxx.cpp file;
        /// otherwise returns false.
        /// </returns>
        public bool IsMoccedFileIncluded(VCFile vcFile)
        {
            var fullPath = vcFile.FullPath;
            if (HelperFunctions.IsHeaderFile(fullPath))
                fullPath = Path.ChangeExtension(fullPath, ".cpp");

            if (HelperFunctions.IsSourceFile(fullPath)) {
                vcFile = GetFileFromProject(fullPath);
                if (vcFile == null)
                    return false;

                fullPath = vcFile.FullPath;
                var mocFile = "moc_" + Path.GetFileNameWithoutExtension(fullPath) + ".cpp";

#if TODO
                // TODO: Newly created projects need a manual solution rescan if we access the
                // code model too early, right now it fails to properly parse the created files.

                // Try reusing the vc file code model,
                var projectItem = vcFile.Object as ProjectItem;
                if (projectItem != null) {
                    var vcFileCodeModel = projectItem.FileCodeModel as VCFileCodeModel;
                    if (vcFileCodeModel != null) {
                        foreach (VCCodeInclude include in vcFileCodeModel.Includes) {
                            if (include.FullName == mocFile)
                                return true;
                        }
                        return false;
                    }
                }

                // if we fail, we parse the file on our own...
#endif
                CxxStreamReader cxxStream = null;
                try {
                    cxxStream = new CxxStreamReader(fullPath);
                    while (cxxStream.ReadLine() is {} line) {
                        if (Regex.IsMatch(line, "#include *(<|\")" + mocFile + "(\"|>)"))
                            return true;
                    }
                } catch { } finally {
                    cxxStream?.Dispose();
                }
            }
            return false;
        }

        public bool HasMocStep(VCFile file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (file.ItemType == QtMoc.ItemTypeName)
                return true;

            if (HelperFunctions.IsHeaderFile(file.Name))
                return CheckForCommand(file, "moc.exe");

            return HelperFunctions.IsSourceFile(file.Name) && HasCppMocFiles(file);
        }

        public static bool HasUicStep(VCFile file)
        {
            return file.ItemType == QtUic.ItemTypeName || CheckForCommand(file, "uic.exe");
        }

        private static bool CheckForCommand(VCFile file, string cmd)
        {
            if (file == null)
                return false;
            foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations) {
                var tool = HelperFunctions.GetCustomBuildTool(config);
                if (tool == null)
                    return false;
                if (tool.CommandLine != null && tool.CommandLine.Contains(cmd))
                    return true;
            }
            return false;
        }

        public void UpdateRccStep(VCFile qrcFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (GetFormatVersion(vcPro) >= Resources.qtMinFormatVersion_Settings) {
                qrcFile.ItemType = QtRcc.ItemTypeName;
            } else {
                // TODO: It would be nice if we can inform the user he's on an old project.
                //if (QtVsToolsPackage.Instance.Options.UpdateProjectFormat)
                //    Notifications.UpdateProjectFormat.Show();
            }
        }

        public static void ExcludeFromAllBuilds(VCFile file)
        {
            if (file == null)
                return;
            foreach (VCFileConfiguration conf in (IVCCollection)file.FileConfigurations) {
                if (!conf.ExcludedFromBuild)
                    conf.ExcludedFromBuild = true;
            }
        }

        bool IsCppMocFileCustomBuild(VCFile vcFile, VCFile cppFile)
        {
            var mocFilePath = vcFile.FullPath;
            var cppFilePath = cppFile.FullPath;
            if (Path.GetDirectoryName(mocFilePath)
                != Path.GetDirectoryName(cppFilePath)) {
                return false;
            }

            if (Path.GetFileNameWithoutExtension(mocFilePath)
                != Path.GetFileNameWithoutExtension(cppFilePath)) {
                return false;
            }

            return string.Equals(Path.GetExtension(mocFilePath), ".cbt", IgnoreCase);
        }

        List<VCFile> GetCppMocOutputs(List<VCFile> mocFiles)
        {
            List<VCFile> outputFiles = new List<VCFile>();
            foreach (var mocFile in mocFiles) {
                foreach (VCFileConfiguration mocConfig
                    in (IVCCollection)mocFile.FileConfigurations) {

                    var cbtTool = HelperFunctions.GetCustomBuildTool(mocConfig);
                    if (cbtTool == null)
                        continue;
                    foreach (var output in cbtTool.Outputs.Split(';')) {
                        var outputExpanded = output;
                        if (!HelperFunctions.ExpandString(ref outputExpanded, mocConfig))
                            continue;
                        string outputFullPath;
                        try {
                            outputFullPath = Path.GetFullPath(Path.Combine(
                                Path.GetDirectoryName(mocFile.FullPath),
                                outputExpanded));
                        } catch {
                            continue;
                        }
                        var vcFile = GetFileFromProject(outputFullPath);
                        if (vcFile != null)
                            outputFiles.Add(vcFile);
                    }
                }
            }
            return outputFiles;
        }

        List<VCFile> GetCppMocFiles(VCFile cppFile)
        {
            List<VCFile> mocFiles = new List<VCFile>();
            if (cppFile.project is VCProject vcProj) {
                mocFiles.AddRange(from VCFile vcFile
                                  in (IVCCollection)vcProj.Files
                                  where vcFile.ItemType == "CustomBuild"
                                  && IsCppMocFileCustomBuild(vcFile, cppFile)
                                  select vcFile);
                mocFiles.AddRange(GetCppMocOutputs(mocFiles));
            }
            return mocFiles;
        }

        bool IsCppMocFileQtMsBuild(VCFile vcFile, VCFile cppFile)
        {
            foreach (VCFileConfiguration fileConfig in (IVCCollection)vcFile.FileConfigurations) {
                string inputFile = qtMsBuild.GetPropertyValue(fileConfig, QtMoc.Property.InputFile);
                HelperFunctions.ExpandString(ref inputFile, fileConfig);
                if (HelperFunctions.PathIsRelativeTo(inputFile, cppFile.ItemName))
                    return true;
            }
            return false;
        }

        bool HasCppMocFiles(VCFile cppFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!IsQtMsBuildEnabled())
                return File.Exists(Path.ChangeExtension(cppFile.FullPath, ".cbt"));

            if (cppFile.project is VCProject vcProj) {
                foreach (VCFile vcFile in (IVCCollection)vcProj.Files) {
                    switch (vcFile.ItemType) {
                    case "CustomBuild" when IsCppMocFileCustomBuild(vcFile, cppFile):
                    case QtMoc.ItemTypeName when IsCppMocFileQtMsBuild(vcFile, cppFile):
                        return true;
                    }
                }
            }
            return false;
        }

        public void RemoveMocStep(VCFile file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (file.ItemType == QtMoc.ItemTypeName) {
                RemoveMocStepQtMsBuild(file);
            } else if (HelperFunctions.IsHeaderFile(file.Name)) {
                if (file.ItemType == "CustomBuild")
                    RemoveMocStepCustomBuild(file);
            } else {
                foreach (VCFile vcFile in (IVCCollection)vcPro.Files) {
                    switch (vcFile.ItemType) {
                    case QtMoc.ItemTypeName when IsCppMocFileQtMsBuild(vcFile, file):
                        RemoveMocStepQtMsBuild(vcFile);
                        break;
                    case "CustomBuild" when IsCppMocFileCustomBuild(vcFile, file):
                        RemoveMocStepCustomBuild(file);
                        return;
                    }
                }
            }
        }

        public void RemoveMocStepQtMsBuild(VCFile file)
        {
            if (HelperFunctions.IsHeaderFile(file.Name)) {
                file.ItemType = "ClInclude";
            } else if (HelperFunctions.IsSourceFile(file.Name)) {
                file.ItemType = "ClCompile";
            } else {
                file.ItemType = "None";
            }
        }

        /// <summary>
        /// Removes the custom build step of a given file.
        /// </summary>
        /// <param name="file">file</param>
        public void RemoveMocStepCustomBuild(VCFile file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try {
                if (!HasMocStep(file))
                    return;

                if (HelperFunctions.IsHeaderFile(file.Name)) {
                    foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations) {
                        var tool = HelperFunctions.GetCustomBuildTool(config);
                        if (tool == null)
                            continue;

                        var cmdLine = tool.CommandLine;
                        if (cmdLine.Length > 0) {
                            var rex = new Regex(@"(\S*moc.exe|""\S+:\\\.*moc.exe"")");
                            while (true) {
                                var m = rex.Match(cmdLine);
                                if (!m.Success)
                                    break;

                                var start = m.Index;
                                var end = cmdLine.IndexOf("&&", start, StringComparison.Ordinal);
                                var a = cmdLine.IndexOf("\r\n", start, StringComparison.Ordinal);
                                if ((a > -1 && a < end) || (end < 0 && a > -1))
                                    end = a;
                                if (end < 0)
                                    end = cmdLine.Length;

                                cmdLine = cmdLine.Remove(start, end - start).Trim();
                                if (cmdLine.StartsWith("&&", StringComparison.Ordinal))
                                    cmdLine = cmdLine.Remove(0, 2).Trim();
                            }
                            tool.CommandLine = cmdLine;
                        }

                        var reg = new Regex("Moc'ing .+\\.\\.\\.");
                        var addDepends = tool.AdditionalDependencies;
                        addDepends = Regex.Replace(addDepends,
                            @"(\S*moc.exe|""\S+:\\\.*moc.exe"")", string.Empty);
                        addDepends = addDepends.Replace(file.RelativePath, string.Empty);
                        tool.AdditionalDependencies = string.Empty;
                        tool.Description = reg.Replace(tool.Description, string.Empty);
                        tool.Description = tool.Description.Replace("MOC " + file.Name, string.Empty);
                        var baseFileName = file.Name.Remove(file.Name.LastIndexOf('.'));
                        var pattern = "(\"(.*\\\\" + GetMocFileName(file.FullPath)
                            + ")\"|(\\S*" + GetMocFileName(file.FullPath) + "))";
                        string outputMocFile = null;
                        var regExp = new Regex(pattern);
                        tool.Outputs = tool.Outputs.Replace("%(Filename)", baseFileName);
                        var matchList = regExp.Matches(tool.Outputs);
                        if (matchList.Count > 0) {
                            if (matchList[0].Length > 0)
                                outputMocFile = matchList[0].ToString();
                            else if (matchList[1].Length > 1)
                                outputMocFile = matchList[1].ToString();
                        }
                        tool.Outputs = Regex.Replace(tool.Outputs,
                            pattern, string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                        tool.Outputs = Regex.Replace(tool.Outputs,
                            @"\s*;\s*;\s*", ";", RegexOptions.Multiline);
                        tool.Outputs = Regex.Replace(tool.Outputs,
                            @"(^\s*;|\s*;\s*$)", string.Empty, RegexOptions.Multiline);

                        if (outputMocFile != null) {
                            if (outputMocFile.StartsWith("\"", StringComparison.Ordinal))
                                outputMocFile = outputMocFile.Substring(1);
                            if (outputMocFile.EndsWith("\"", StringComparison.Ordinal))
                                outputMocFile = outputMocFile.Substring(0, outputMocFile.Length - 1);
                            HelperFunctions.ExpandString(ref outputMocFile, config);
                        }
                        var mocFile = GetFileFromProject(outputMocFile);
                        if (mocFile != null)
                            RemoveFileFromFilter(mocFile, Filters.GeneratedFiles());
                    }
                } else {
                    foreach (var mocFile in GetCppMocFiles(file)) {
                        RemoveFileFromFilter(mocFile, Filters.GeneratedFiles());
                    }
                }
            } catch {
                throw new QtVSException($"Cannot remove a moc step from file {file.FullPath}");
            }
        }

        /// <summary>
        /// Returns the file (VCFile) specified by the file name from a given
        /// project.
        /// </summary>
        /// <param name="fileName">file name (relative path)</param>
        /// <returns></returns>
        public VCFile GetFileFromProject(string fileName)
        {
            fileName = HelperFunctions.NormalizeRelativeFilePath(fileName);
            if (!HelperFunctions.IsAbsoluteFilePath(fileName)) {
                fileName = HelperFunctions.NormalizeFilePath(vcPro.ProjectDirectory
                    + Path.DirectorySeparatorChar + fileName);
            }
            foreach (VCFile f in (IVCCollection)vcPro.Files) {
                if (f.FullPath.Equals(fileName, IgnoreCase))
                    return f;
            }
            return null;
        }

        /// <summary>
        /// Returns the files specified by the file name from a given project as list of VCFile
        /// objects.
        /// </summary>
        /// <param name="fileName">file name (relative path)</param>
        /// <returns></returns>
        public IEnumerable<VCFile> GetFilesFromProject(string fileName)
        {
            var fi = new FileInfo(HelperFunctions.NormalizeRelativeFilePath(fileName));
            foreach (VCFile f in (IVCCollection)vcPro.Files) {
                if (f.Name.Equals(fi.Name, IgnoreCase))
                    yield return f;
            }
        }

        /// <summary>
        /// Removes a file from the filter.
        /// This file will be deleted!
        /// </summary>
        /// <param name="file">file</param>
        public void RemoveFileFromFilter(VCFile file, FakeFilter filter)
        {
            try {
                var vfilt = FindFilterFromGuid(filter.UniqueIdentifier)
                          ?? FindFilterFromName(filter.Name);

                if (vfilt == null)
                    return;

                RemoveFileFromFilter(file, vfilt);
            } catch {
                throw new QtVSException($"Cannot remove file {file.Name} from filter.");
            }
        }

        /// <summary>
        /// Removes a file from the filter.
        /// This file will be deleted!
        /// </summary>
        /// <param name="file">file</param>
        public void RemoveFileFromFilter(VCFile file, VCFilter filter)
        {
            try {
                filter.RemoveFile(file);
                var fi = new FileInfo(file.FullPath);
                if (fi.Exists)
                    fi.Delete();
            } catch {
            }

            var subfilters = (IVCCollection)filter.Filters;
            for (var i = subfilters.Count; i > 0; i--) {
                try {
                    var subfilter = (VCFilter)subfilters.Item(i);
                    RemoveFileFromFilter(file, subfilter);
                } catch {
                }
            }
        }

        public VCFilter FindFilterFromName(string filtername)
        {
            try {
                foreach (VCFilter vcfilt in (IVCCollection)vcPro.Filters) {
                    if (vcfilt.Name.ToLower() == filtername.ToLower())
                        return vcfilt;
                }
                return null;
            } catch {
                throw new QtVSException("Cannot find filter.");
            }
        }

        public VCFilter FindFilterFromGuid(string filterguid)
        {
            try {
                foreach (VCFilter vcfilt in (IVCCollection)vcPro.Filters) {
                    if (vcfilt.UniqueIdentifier != null
                        && vcfilt.UniqueIdentifier.ToLower() == filterguid.ToLower()) {
                        return vcfilt;
                    }
                }
                return null;
            } catch {
                throw new QtVSException("Cannot find filter.");
            }
        }

        public static void MarkAsQtPlugin(QtProject qtPro)
        {
            foreach (VCConfiguration config in qtPro.VCProject.Configurations as IVCCollection) {
                (config.Rules.Item("QtRule10_Settings") as IVCRulePropertyStorage)
                    .SetPropertyValue("QtPlugin", "true");
            }
        }

        /// <summary>
        /// adjusts the whitespaces, tabs in the given file according to VS settings
        /// </summary>
        /// <param name="file"></param>
        public static void AdjustWhitespace(DTE dte, string file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!File.Exists(file))
                return;

            // only replace whitespaces in known types
            if (!HelperFunctions.IsSourceFile(file) && !HelperFunctions.IsHeaderFile(file)
                && !HelperFunctions.IsUicFile(file)) {
                return;
            }

            try {
                var prop = dte.Properties["TextEditor", "C/C++"];
                var tabSize = Convert.ToInt64(prop.Item("TabSize").Value);
                var insertTabs = Convert.ToBoolean(prop.Item("InsertTabs").Value);

                var oldValue = insertTabs ? "    " : "\t";
                var newValue = insertTabs ? "\t" : GetWhitespaces(tabSize);

                var list = new List<string>();
                var reader = new StreamReader(file);
                while (reader.ReadLine() is {} line) {
                    if (line.StartsWith(oldValue, StringComparison.Ordinal))
                        line = line.Replace(oldValue, newValue);
                    list.Add(line);
                }
                reader.Close();

                var writer = new StreamWriter(file);
                foreach (var l in list)
                    writer.WriteLine(l);
                writer.Close();
            } catch (Exception e) {
                Messages.Print("Cannot adjust whitespace or tabs in file (write)."
                    + Environment.NewLine + $"({e})");
            }
        }

        private static string GetWhitespaces(long size)
        {
            var whitespaces = string.Empty;
            for (long i = 0; i < size; ++i)
                whitespaces += " ";
            return whitespaces;
        }

        public void AddActiveQtBuildStep(string version, string defFile = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (FormatVersion < Resources.qtMinFormatVersion_ClProperties)
                return;

            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations) {
                var idlFile = "\"$(IntDir)/" + envPro.Name + ".idl\"";
                var tblFile = "\"$(IntDir)/" + envPro.Name + ".tlb\"";

                var tool = (VCPostBuildEventTool)((IVCCollection)config.Tools).Item("VCPostBuildEventTool");
                var idc = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /idl " + idlFile + " -version " + version;
                var midl = "midl " + idlFile + " /tlb " + tblFile;
                var idc2 = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /tlb " + tblFile;
                var idc3 = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /regserver";

                tool.CommandLine = idc + "\r\n" + midl + "\r\n" + idc2 + "\r\n" + idc3;
                tool.Description = string.Empty;

                var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
                var librarian = (VCLibrarianTool)((IVCCollection)config.Tools).Item("VCLibrarianTool");

                if (linker != null) {
                    linker.Version = version;
                    linker.ModuleDefinitionFile = defFile ?? envPro.Name + ".def";
                } else {
                    librarian.ModuleDefinitionFile = defFile ?? envPro.Name + ".def";
                }
            }
        }

        public bool UsesPrecompiledHeaders()
        {
            if (vcPro.Configurations is not IVCCollection configurations)
                return false;

            const pchOption pchNone = pchOption.pchNone;
            return configurations.Cast<VCConfiguration>()
                .Select(CompilerToolWrapper.Create)
                .All(compiler => (compiler?.GetUsePrecompiledHeader() ?? pchNone) != pchNone);
        }

        public string GetPrecompiledHeaderThrough()
        {
            if (vcPro.Configurations is not IVCCollection configurations)
                return null;

            return configurations.Cast<VCConfiguration>()
                .Select(CompilerToolWrapper.Create)
                .Select(compiler => compiler?.GetPrecompiledHeaderThrough() ?? "")
                .Where(header => !string.IsNullOrEmpty(header))
                .Select(header => header.ToLower())
                .FirstOrDefault();
        }

        public static void SetPCHOption(VCFile vcFile, pchOption option)
        {
            if (vcFile.FileConfigurations is not IVCCollection fileConfigurations)
                return;

            foreach (VCFileConfiguration config in fileConfigurations)
                CompilerToolWrapper.Create(config)?.SetUsePrecompiledHeader(option);
        }

        public void RemoveGeneratedFiles(string fileName)
        {
            var fi = new FileInfo(fileName);
            var lastIndex = fileName.LastIndexOf(fi.Extension, StringComparison.Ordinal);
            var baseName = fi.Name.Remove(lastIndex, fi.Extension.Length);
            string delName = null;
            if (HelperFunctions.IsHeaderFile(fileName))
                delName = "moc_" + baseName + ".cpp";
            else if (HelperFunctions.IsSourceFile(fileName) && !fileName.StartsWith("moc_", IgnoreCase))
                delName = baseName + ".moc";
            else if (HelperFunctions.IsUicFile(fileName))
                delName = "ui_" + baseName + ".h";
            else if (HelperFunctions.IsQrcFile(fileName))
                delName = "qrc_" + baseName + ".cpp";

            if (delName != null) {
                foreach (var delFile in GetFilesFromProject(delName))
                    RemoveFileFromFilter(delFile, Filters.GeneratedFiles());
            }
        }

        public class CppConfig
        {
            public VCConfiguration Config;
            public IVCRulePropertyStorage Cpp;

            public string GetUserPropertyValue(string pszPropName)
            {
                try {
                    var storage = (Config.project as VCProject) as IVCBuildPropertyStorage;
                    return storage.GetPropertyValue(pszPropName, Config.Name, "UserFile");
                } catch (Exception exception) {
                    exception.Log();
                    return string.Empty;
                }
            }

            public void SetUserPropertyValue(string pszPropName, string pszPropValue)
            {
                try {
                    var storage = (Config.project as VCProject) as IVCBuildPropertyStorage;
                    storage.SetPropertyValue(pszPropName, Config.Name, "UserFile", pszPropValue);
                } catch (Exception exception) {
                    exception.Log();
                }
            }

            public void RemoveUserProperty(string pszPropName)
            {
                try {
                    var storage = (Config.project as VCProject) as IVCBuildPropertyStorage;
                    storage.RemoveProperty(pszPropName, Config.Name, "UserFile");
                } catch (Exception exception) {
                    exception.Log();
                }
            }
        }

        public static IEnumerable<CppConfig> GetCppConfigs(VCProject vcPro)
        {
            return ((IVCCollection)vcPro.Configurations).Cast<VCConfiguration>()
                .Select(x => new CppConfig
                {
                    Config = x,
                    Cpp = x.Rules.Item("CL") as IVCRulePropertyStorage
                })
                .Where(x => x.Cpp != null
                    && x.Config.GetEvaluatedPropertyValue("ApplicationType") != "Linux");
        }

        public static IEnumerable<CppConfig> GetCppDebugConfigs(VCProject vcPro)
        {
            var cppConfigs = GetCppConfigs(vcPro)
                .Select(x => new { Self = x, x.Cpp });
            var cppConfigMacros = cppConfigs
                .Select(x => new
                {
                    x.Self,
                    Macros = x.Cpp.GetEvaluatedPropertyValue("PreprocessorDefinitions")
                })
                .Where(x => !string.IsNullOrEmpty(x.Macros));
            var cppDebugConfigs = cppConfigMacros
                .Where(x => !x.Macros.Split(';').Contains("QT_NO_DEBUG"))
                .Select(x => x.Self);
            return cppDebugConfigs;
        }

        public static bool IsQtQmlDebugDefined(VCProject vcPro)
        {
            var cppConfigs = GetCppConfigs(vcPro)
                .Select(x => new { Self = x, x.Cpp });
            var cppConfigMacros = cppConfigs
                .Select(x => new
                {
                    x.Self,
                    Macros = x.Cpp.GetEvaluatedPropertyValue("PreprocessorDefinitions")
                })
                .Where(x => !string.IsNullOrEmpty(x.Macros));
            return cppConfigMacros
                .Any(x => x.Macros.Split(';').Contains("QT_QML_DEBUG"));
        }

        public static void DefineQtQmlDebug(VCProject vcPro)
        {
            var configs = GetCppDebugConfigs(vcPro).Where(x => x.Cpp
                .GetEvaluatedPropertyValue("PreprocessorDefinitions").Split(';')
                .Contains("QT_QML_DEBUG") == false)
                .Select(x => new
                {
                    x.Cpp,
                    Macros = x.Cpp.GetUnevaluatedPropertyValue("PreprocessorDefinitions")
                });

            foreach (var config in configs) {
                config.Cpp.SetPropertyValue("PreprocessorDefinitions",
                    $"QT_QML_DEBUG;{config.Macros}");
            }
        }

        public static void UndefineQtQmlDebug(VCProject vcPro)
        {
            var configs = GetCppDebugConfigs(vcPro)
                .Where(x => x.Cpp.GetEvaluatedPropertyValue("PreprocessorDefinitions").Split(';')
                    .Contains("QT_QML_DEBUG"))
                .Select(x => new
                {
                    x.Cpp,
                    Macros = x.Cpp.GetUnevaluatedPropertyValue("PreprocessorDefinitions")
                        .Split(';').ToList()
                });

            foreach (var config in configs) {
                config.Macros.Remove("QT_QML_DEBUG");
                config.Cpp.SetPropertyValue("PreprocessorDefinitions",
                    string.Join(";", config.Macros));
            }
        }

        public static bool IsQmlJsDebuggerDefined(VCProject vcPro)
        {
            foreach (var config in GetCppDebugConfigs(vcPro)) {
                var qmlDebug = config.GetUserPropertyValue("QmlDebug");
                if (string.IsNullOrEmpty(qmlDebug))
                    return false;
                var debugArgs = config.GetUserPropertyValue("LocalDebuggerCommandArguments");
                if (string.IsNullOrEmpty(debugArgs))
                    return false;
                if (!debugArgs.Contains(qmlDebug))
                    return false;
            }
            return true;
        }

        public static void DefineQmlJsDebugger(VCProject vcPro)
        {
            var configs = GetCppDebugConfigs(vcPro)
                .Select(x => new
                {
                    Self = x,
                    QmlDebug = x.GetUserPropertyValue("QmlDebug"),
                    Args = x.GetUserPropertyValue("LocalDebuggerCommandArguments")
                })
                .Where(x => string.IsNullOrEmpty(x.QmlDebug) || !x.Args.Contains(x.QmlDebug));

            foreach (var config in configs) {

                config.Self.RemoveUserProperty("LocalDebuggerCommandArguments");
                config.Self.RemoveUserProperty("QmlDebug");
                config.Self.RemoveUserProperty("QmlDebugSettings");

                config.Self.SetUserPropertyValue("QmlDebugSettings", "file:$(ProjectGuid),block");
                config.Self.SetUserPropertyValue("QmlDebug", "-qmljsdebugger=$(QmlDebugSettings)");

                config.Self.SetUserPropertyValue("LocalDebuggerCommandArguments",
                    string.Join(" ", config.Args, "$(QmlDebug)").Trim());
            }
        }

        public static void UndefineQmlJsDebugger(VCProject vcPro)
        {
            var configs = GetCppDebugConfigs(vcPro)
                .Select(x => new
                {
                    Self = x,
                    QmlDebug = x.GetUserPropertyValue("QmlDebug"),
                    Args = x.GetUserPropertyValue("LocalDebuggerCommandArguments")
                })
                .Where(x => !string.IsNullOrEmpty(x.QmlDebug) && x.Args.Contains(x.QmlDebug));

            foreach (var config in configs) {

                config.Self.SetUserPropertyValue("QmlDebug", "##QMLDEBUG##");
                var args = config.Self.GetUserPropertyValue("LocalDebuggerCommandArguments");

                var newArgs = args.Replace("##QMLDEBUG##", "").Trim();
                if (string.IsNullOrEmpty(newArgs))
                    config.Self.RemoveUserProperty("LocalDebuggerCommandArguments");
                else
                    config.Self.SetUserPropertyValue("LocalDebuggerCommandArguments", newArgs);

                config.Self.RemoveUserProperty("QmlDebug");
                config.Self.SetUserPropertyValue("QmlDebugSettings", "false");
            }
        }

        public bool QmlDebug
        {
            get => IsQtQmlDebugDefined(vcPro) && IsQmlJsDebuggerDefined(vcPro);
            set
            {
                bool enabled = IsQtQmlDebugDefined(vcPro) && IsQmlJsDebuggerDefined(vcPro);
                if (value == enabled)
                    return;

                if (value) {
                    DefineQtQmlDebug(vcPro);
                    DefineQmlJsDebugger(vcPro);
                } else {
                    UndefineQtQmlDebug(vcPro);
                    UndefineQmlJsDebugger(vcPro);
                }
            }
        }
    }
}
