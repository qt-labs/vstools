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
using Microsoft.VisualStudio.VCCodeModel;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace QtProjectLib
{
    /// <summary>
    /// QtProject holds the Qt specific properties for a Visual Studio project.
    /// There exists at most one QtProject per EnvDTE.Project.
    /// Use QtProject.Create to get the QtProject for a Project or VCProject.
    /// </summary>
    public class QtProject
    {
        private EnvDTE.DTE dte = null;
        private EnvDTE.Project envPro = null;
        private VCProject vcPro = null;
        private MocCmdChecker mocCmdChecker = null;
        private Array lastConfigurationRowNames = null;
        private static Dictionary<Project, QtProject> instances = new Dictionary<Project, QtProject>();

        public static QtProject Create(VCProject vcProject)
        {
            return Create((EnvDTE.Project) vcProject.Object);
        }

        public static QtProject Create(EnvDTE.Project project)
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

        private QtProject(EnvDTE.Project project)
        {
            if (project == null)
                throw new QtVSException(SR.GetString("QtProject_CannotConstructWithoutValidProject"));
            envPro = project;
            dte = envPro.DTE;
            vcPro = envPro.Object as VCProject;
        }

        public VCProject VCProject
        {
            get { return vcPro; }
        }

        public EnvDTE.Project Project
        {
            get { return envPro; }
        }

        public string ProjectDir
        {
            get
            {
                return vcPro.ProjectDirectory;
            }
        }

        /// <summary>
        /// Returns true if the ConfigurationRowNames have changed
        /// since the last evaluation of this property.
        /// </summary>
        public bool ConfigurationRowNamesChanged
        {
            get
            {
                bool ret = false;
                if (lastConfigurationRowNames == null) {
                    lastConfigurationRowNames = envPro.ConfigurationManager.ConfigurationRowNames as Array;
                } else {
                    var currentConfigurationRowNames = envPro.ConfigurationManager.ConfigurationRowNames as Array;
                    if (!HelperFunctions.ArraysEqual(lastConfigurationRowNames, currentConfigurationRowNames)) {
                        lastConfigurationRowNames = currentConfigurationRowNames;
                        ret = true;
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// Returns the file name of the generated ui header file relative to
        /// the project directory.
        /// </summary>
        /// <param name="uiFile">name of the ui file</param>
        public string GetUiGeneratedFileName(string uiFile)
        {
            var fi = new FileInfo(uiFile);
            string file = fi.Name;
            if (fi.Extension == ".ui") {
                return QtVSIPSettings.GetUicDirectory(envPro)
                    + "\\ui_" + file.Remove(file.Length - 3, 3) + ".h";
            }
            return null;
        }

        /// <summary>
        /// Returns the moc-generated file name for the given source or header file.
        /// </summary>
        /// <param name="file">header or source file in the project</param>
        /// <returns></returns>
        private static string GetMocFileName(string file)
        {
            var fi = new FileInfo(file);

            string name = fi.Name;
            if (HelperFunctions.HasHeaderFileExtension(fi.Name))
                return "moc_" + name.Substring(0, name.LastIndexOf('.')) + ".cpp";
            else if (HelperFunctions.HasSourceFileExtension(fi.Name))
                return name.Substring(0, name.LastIndexOf('.')) + ".moc";
            return null;
        }

        /// <summary>
        /// Returns the file name of the generated moc file relative to the
        /// project directory.
        /// </summary>
        /// The directory of the moc file depends on the file configuration.
        /// Every appearance of "$(ConfigurationName)" in the path will be
        /// replaced by the value of configName.
        /// <param name="file">full file name of either the header or the source file</param>
        /// <param name="config">file configuration</param>
        /// <returns></returns>
        private string GetRelativeMocFilePath(string file, string configName, string platformName)
        {
            var fileName = GetMocFileName(file);
            if (fileName == null)
                return null;
            string mocDir = QtVSIPSettings.GetMocDirectory(envPro, configName, platformName)
                + "\\" + fileName;
            if (HelperFunctions.IsAbsoluteFilePath(mocDir))
                mocDir = HelperFunctions.GetRelativePath(vcPro.ProjectDirectory, mocDir);
            return mocDir;
        }

        /// <summary>
        /// Returns the file name of the generated moc file relative to the
        /// project directory.
        /// </summary>
        /// The returned file path may contain the macros $(ConfigurationName) and $(PlatformName).
        /// <param name="file">full file name of either the header or the source file</param>
        /// <returns></returns>
        private string GetRelativeMocFilePath(string file)
        {
            return GetRelativeMocFilePath(file, null, null);
        }

        public string QtPackageVersion()
        {
            if (vcPro == null)
                return null;
            return vcPro.keyword.Remove(0, Resources.qtProjectKeyword.Length);
        }

        /// <summary>
        /// Marks the specified project as a Qt project.
        /// </summary>
        /// <param name="proj">project</param>
        public void MarkAsQtProject(string version)
        {
            vcPro.keyword = Resources.qtProjectKeyword + version;
        }

        public void AddDefine(string define, uint bldConf)
        {
            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                var compiler = CompilerToolWrapper.Create(config);

                if (((!IsDebugConfiguration(config)) && ((bldConf & BuildConfig.Release) != 0)) ||
                    ((IsDebugConfiguration(config)) && ((bldConf & BuildConfig.Debug) != 0))) {
                    compiler.AddPreprocessorDefinition(define);
                }
            }
        }

        public void AddModule(QtModule module)
        {
            if (HasModule(module))
                return;

            var vm = QtVersionManager.The();
            var versionInfo = vm.GetVersionInfo(Project);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                var compiler = CompilerToolWrapper.Create(config);
                var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");

                var info = QtModules.Instance.ModuleInformation(module);
                if (compiler != null) {
                    foreach (string define in info.Defines)
                        compiler.AddPreprocessorDefinition(define);

                    var incPath = info.GetIncludePath();
                    if (!string.IsNullOrEmpty(incPath))
                        compiler.AddAdditionalIncludeDirectories(incPath);
                }
                if (linker != null) {
                    var moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    var linkerWrapper = new LinkerToolWrapper(linker);
                    List<string> additionalDeps = linkerWrapper.AdditionalDependencies;
                    bool dependenciesChanged = false;
                    if (additionalDeps == null || additionalDeps.Count == 0) {
                        additionalDeps = moduleLibs;
                        dependenciesChanged = true;
                    } else {
                        foreach (string moduleLib in moduleLibs)
                            if (!additionalDeps.Contains(moduleLib)) {
                                additionalDeps.Add(moduleLib);
                                dependenciesChanged = true;
                            }
                    }
                    if (dependenciesChanged)
                        linkerWrapper.AdditionalDependencies = additionalDeps;
                }
            }
        }

        public void RemoveModule(QtModule module)
        {
            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                var compiler = CompilerToolWrapper.Create(config);
                var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");

                var info = QtModules.Instance.ModuleInformation(module);
                if (compiler != null) {
                    foreach (string define in info.Defines)
                        compiler.RemovePreprocessorDefinition(define);
                    List<string> additionalIncludeDirs = compiler.AdditionalIncludeDirectories;
                    if (additionalIncludeDirs != null) {
                        var lst = new List<string>(additionalIncludeDirs);
                        if (!string.IsNullOrEmpty(info.IncludePath)) {
                            lst.Remove(info.IncludePath);
                            lst.Remove('\"' + info.IncludePath + '\"');
                        }
                        compiler.AdditionalIncludeDirectories = lst;
                    }
                }
                if (linker != null && linker.AdditionalDependencies != null) {
                    var linkerWrapper = new LinkerToolWrapper(linker);
                    var vm = QtVersionManager.The();
                    var versionInfo = vm.GetVersionInfo(Project);
                    if (versionInfo == null)
                        versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

                    var moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    List<string> additionalDependencies = linkerWrapper.AdditionalDependencies;
                    bool dependenciesChanged = false;
                    foreach (string moduleLib in moduleLibs)
                        if (additionalDependencies.Remove(moduleLib))
                            dependenciesChanged = true;
                    if (dependenciesChanged)
                        linkerWrapper.AdditionalDependencies = additionalDependencies;
                }
            }
        }

        public void UpdateModules(VersionInformation oldVersion, VersionInformation newVersion)
        {
            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");

                if (linker != null) {
                    if (oldVersion == null) {
                        var linkerWrapper = new LinkerToolWrapper(linker);
                        List<string> additionalDependencies = linkerWrapper.AdditionalDependencies;

                        var libsDesktop = new List<string>();
                        foreach (QtModuleInfo module in QtModules.Instance.GetAvailableModuleInformation()) {
                            if (HasModule(module.ModuleId)) {
                                libsDesktop.AddRange(module.AdditionalLibraries);
                            }
                        }
                        List<string> libsToAdd = libsDesktop;

                        bool changed = false;
                        foreach (string libToAdd in libsToAdd) {
                            if (!additionalDependencies.Contains(libToAdd)) {
                                additionalDependencies.Add(libToAdd);
                                changed = true;
                            }
                        }
                        if (changed)
                            linkerWrapper.AdditionalDependencies = additionalDependencies;
                    }

                    if (oldVersion == null || newVersion.IsStaticBuild() != oldVersion.IsStaticBuild()) {
                        var compiler = CompilerToolWrapper.Create(config);
                        if (newVersion.IsStaticBuild()) {
                            if (compiler != null)
                                compiler.RemovePreprocessorDefinition("QT_DLL");
                        } else {
                            if (compiler != null)
                                compiler.AddPreprocessorDefinition("QT_DLL");
                        }
                    }
                }
            }
        }

        public bool HasModule(QtModule module)
        {
            bool foundInIncludes = false;
            bool foundInLibs = false;

            var vm = QtVersionManager.The();
            var versionInfo = vm.GetVersionInfo(Project);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                var compiler = CompilerToolWrapper.Create(config);
                var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");

                var info = QtModules.Instance.ModuleInformation(module);
                if (compiler != null) {
                    var incPath = info.GetIncludePath();
                    if (string.IsNullOrEmpty(incPath))
                        break;
                    if (compiler.GetAdditionalIncludeDirectories() == null)
                        continue;

                    var fixedIncludeDir = FixFilePathForComparison(incPath);
                    var includeDirs = compiler.GetAdditionalIncludeDirectoriesList();
                    foreach (string dir in includeDirs) {
                        if (FixFilePathForComparison(dir) == fixedIncludeDir) {
                            foundInIncludes = true;
                            break;
                        }
                    }
                }

                if (foundInIncludes)
                    break;

                List<string> libs = null;
                if (linker != null) {
                    var linkerWrapper = new LinkerToolWrapper(linker);
                    libs = linkerWrapper.AdditionalDependencies;
                }

                if (libs != null) {
                    foundInLibs = true;
                    var moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    foreach (string moduleLib in moduleLibs) {
                        if (!libs.Contains(moduleLib)) {
                            foundInLibs = false;
                            break;
                        }
                    }
                }
            }
            return foundInIncludes || foundInLibs;
        }

        public void WriteProjectBasicConfigurations(uint type, bool usePrecompiledHeader)
        {
            WriteProjectBasicConfigurations(type, usePrecompiledHeader, null);
        }

        public void WriteProjectBasicConfigurations(uint type, bool usePrecompiledHeader, VersionInformation vi)
        {
            ConfigurationTypes configType = ConfigurationTypes.typeApplication;
            string targetExtension = ".exe";
            string qtVersion = null;
            var vm = QtVersionManager.The();
            if (vi == null) {
                qtVersion = vm.GetDefaultVersion();
                vi = vm.GetVersionInfo(qtVersion);
            }

            switch (type & TemplateType.ProjectType) {
            case TemplateType.DynamicLibrary:
                configType = ConfigurationTypes.typeDynamicLibrary;
                targetExtension = ".dll";
                break;
            case TemplateType.StaticLibrary:
                configType = ConfigurationTypes.typeStaticLibrary;
                targetExtension = ".lib";
                break;
            }

            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                config.ConfigurationType = configType;
                var compiler = CompilerToolWrapper.Create(config);
                var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");
                var librarian = (VCLibrarianTool) ((IVCCollection) config.Tools).Item("VCLibrarianTool");

                // for some stupid reason you have to set this for it to be updated...
                // the default value is the same... +platform now
                config.OutputDirectory = "$(SolutionDir)$(Platform)\\$(Configuration)\\";

                // add some common defines
                compiler.SetPreprocessorDefinitions(vi.GetQMakeConfEntry("DEFINES").Replace(" ", ","));

                if (!vi.IsStaticBuild()) {
                    compiler.AddPreprocessorDefinition("QT_DLL");
                }

                if (linker != null) {
                    if ((type & TemplateType.ConsoleSystem) != 0)
                        linker.SubSystem = subSystemOption.subSystemConsole;
                    else
                        linker.SubSystem = subSystemOption.subSystemWindows;

                    linker.OutputFile = "$(OutDir)\\$(ProjectName)" + targetExtension;
                    linker.AdditionalLibraryDirectories = "$(QTDIR)\\lib";
                    if (vi.IsStaticBuild()) {
                        linker.AdditionalDependencies = vi.GetQMakeConfEntry("QMAKE_LIBS_CORE");
                        if ((type & TemplateType.GUISystem) != 0) {
                            linker.AdditionalDependencies += " " + vi.GetQMakeConfEntry("QMAKE_LIBS_GUI");
                        }
                    }
                } else {
                    librarian.OutputFile = "$(OutDir)\\$(ProjectName)" + targetExtension;
                    librarian.AdditionalLibraryDirectories = "$(QTDIR)\\lib";
                }

                if ((type & TemplateType.GUISystem) != 0)
                    compiler.SetAdditionalIncludeDirectories(QtVSIPSettings.GetUicDirectory(envPro) + ";");

                if ((type & TemplateType.PluginProject) != 0) {
                    compiler.AddPreprocessorDefinition("QT_PLUGIN");
                }

                bool isDebugConfiguration = false;
                if (config.Name.StartsWith("Release")) {
                    compiler.AddPreprocessorDefinition("QT_NO_DEBUG,NDEBUG");
                    compiler.SetDebugInformationFormat(debugOption.debugDisabled);
                    compiler.RuntimeLibrary = runtimeLibraryOption.rtMultiThreadedDLL;
                } else if (config.Name.StartsWith("Debug")) {
                    isDebugConfiguration = true;
                    compiler.SetOptimization(optimizeOption.optimizeDisabled);
                    compiler.SetDebugInformationFormat(debugOption.debugEnabled);
                    compiler.RuntimeLibrary = runtimeLibraryOption.rtMultiThreadedDebugDLL;
                }
                compiler.AddAdditionalIncludeDirectories(
                    ".;" + "$(QTDIR)\\include;" + QtVSIPSettings.GetMocDirectory(envPro));

                compiler.SetTreatWChar_tAsBuiltInType(true);

                if (linker != null)
                    linker.GenerateDebugInformation = isDebugConfiguration;

                if (usePrecompiledHeader)
                    UsePrecompiledHeaders(config);
            }
            if ((type & TemplateType.PluginProject) != 0)
                MarkAsDesignerPluginProject();
        }

        public void MarkAsDesignerPluginProject()
        {
            Project.Globals["IsDesignerPlugin"] = true.ToString();
            if (!Project.Globals.get_VariablePersists("IsDesignerPlugin"))
                Project.Globals.set_VariablePersists("IsDesignerPlugin", true);
        }

        public bool AddApplicationIcon(string iconFileName)
        {
            string projectName = vcPro.ItemName;
            string iconFile = vcPro.ProjectDirectory + "\\" + projectName + ".ico";
            string rcFile = vcPro.ProjectDirectory + "\\" + projectName + ".rc";
            try {
                if (!File.Exists(iconFile)) {
                    File.Copy(iconFileName, iconFile);
                    var attribs = File.GetAttributes(iconFile);
                    File.SetAttributes(iconFile, attribs & (~FileAttributes.ReadOnly));
                }

                StreamWriter sw = null;
                if (!File.Exists(rcFile)) {
                    sw = new StreamWriter(File.Create(rcFile));
                    sw.WriteLine("IDI_ICON1\t\tICON\t\tDISCARDABLE\t\"" + projectName + ".ico\"" + sw.NewLine);
                    sw.Close();
                }
            } catch (System.Exception e) {
                Messages.DisplayErrorMessage(e);
                return false;
            }
            vcPro.AddFile(rcFile);
            return true;
        }

        /// <summary>
        /// This function adds a uic4 build step to a given file.
        /// </summary>
        /// <param name="file">file</param>
        public void AddUic4BuildStep(VCFile file)
        {
            try {
                var uiFile = GetUiGeneratedFileName(file.FullPath);
                var uiBaseName = file.Name.Remove(file.Name.LastIndexOf('.'));
                var uiFileMacro = uiFile.Replace(uiBaseName, ProjectMacros.Name);
                bool uiFileExists = GetFileFromProject(uiFile) != null;

                foreach (VCFileConfiguration config in (IVCCollection) file.FileConfigurations) {
                    var tool = HelperFunctions.GetCustomBuildTool(config);
                    if (tool != null) {
                        tool.AdditionalDependencies = Resources.uic4Command;
                        tool.Description = "Uic'ing " + ProjectMacros.FileName + "...";
                        tool.Outputs = "\"" + uiFileMacro + "\"";
                        tool.CommandLine = "\"" + Resources.uic4Command + "\" -o \""
                            + uiFileMacro + "\" \"" + ProjectMacros.Path + "\"";
                    }

                    var conf = config.ProjectConfiguration as VCConfiguration;
                    var compiler = CompilerToolWrapper.Create(conf);
                    if (compiler != null && !uiFileExists) {
                        var uiDir = QtVSIPSettings.GetUicDirectory(envPro);
                        if (compiler.GetAdditionalIncludeDirectories().IndexOf(uiDir) < 0)
                            compiler.AddAdditionalIncludeDirectories(uiDir);
                    }
                }
                if (!uiFileExists)
                    AddFileInFilter(Filters.GeneratedFiles(), uiFile);
            } catch {
                throw new QtVSException(SR.GetString("QtProject_CannotAddUicStep", file.FullPath));
            }
        }

        /// <summary>
        /// Surrounds the argument by double quotes.
        /// Makes sure, that the trailing double quote is not escaped by a backslash.
        /// Such an escaping backslash may also appear as a macro value.
        /// </summary>
        private static string SafelyQuoteCommandLineArgument(string arg)
        {
            arg = "\"" + arg;
            if (arg.EndsWith("\\"))
                arg += ".";     // make sure, that we don't escape the trailing double quote
            else if (arg.EndsWith(")"))
                arg += "\\.";   // macro value could end with backslash. That would escape the trailing double quote.
            arg += "\"";
            return arg;
        }

        public string GetDefines(VCFileConfiguration conf)
        {
            List<string> defineList = CompilerToolWrapper.Create(conf).PreprocessorDefinitions;

            var projectConfig = conf.ProjectConfiguration as VCConfiguration;
            defineList.AddRange(CompilerToolWrapper.Create(projectConfig).PreprocessorDefinitions);

            var propertySheets = projectConfig.PropertySheets as IVCCollection;
            if (propertySheets != null)
                foreach (VCPropertySheet sheet in propertySheets)
                    defineList.AddRange(GetDefinesFromPropertySheet(sheet));

            string preprocessorDefines = "";
            var alreadyAdded = new List<string>();
            var rxp = new Regex(@"\s|(\$\()");
            foreach (string define in defineList) {
                if (!alreadyAdded.Contains(define)) {
                    var mustSurroundByDoubleQuotes = rxp.IsMatch(define);
                    // Yes, a preprocessor definition can contain spaces or a macro name.
                    // Example: PROJECTDIR=$(InputDir)

                    if (mustSurroundByDoubleQuotes) {
                        preprocessorDefines += " ";
                        preprocessorDefines += SafelyQuoteCommandLineArgument("-D" + define);
                    } else {
                        preprocessorDefines += " -D" + define;
                    }
                    alreadyAdded.Add(define);
                }
            }
            return preprocessorDefines;
        }

        private List<string> GetDefinesFromPropertySheet(VCPropertySheet sheet)
        {
            List<string> defines = CompilerToolWrapper.Create(sheet).PreprocessorDefinitions;
            var propertySheets = sheet.PropertySheets as IVCCollection;
            if (propertySheets != null)
                foreach (VCPropertySheet subSheet in propertySheets)
                    defines.AddRange(GetDefinesFromPropertySheet(subSheet));
            return defines;
        }

        private string GetIncludes(VCFileConfiguration conf)
        {
            var includeList = GetIncludesFromCompilerTool(CompilerToolWrapper.Create(conf));

            var projectConfig = conf.ProjectConfiguration as VCConfiguration;
            includeList.AddRange(GetIncludesFromCompilerTool(CompilerToolWrapper.Create(projectConfig)));

            var propertySheets = projectConfig.PropertySheets as IVCCollection;
            if (propertySheets != null)
                foreach (VCPropertySheet sheet in propertySheets)
                    includeList.AddRange(GetIncludesFromPropertySheet(sheet));

            string includes = "";
            var alreadyAdded = new List<string>();
            foreach (string include in includeList) {
                if (!alreadyAdded.Contains(include)) {
                    var incl = HelperFunctions.NormalizeRelativeFilePath(include);
                    if (incl.Length > 0) {
                        string cmdline = " ";
                        cmdline += SafelyQuoteCommandLineArgument("-I" + incl);
                        includes += cmdline;
                    }
                    alreadyAdded.Add(include);
                }
            }
            return includes;
        }

        private List<string> GetIncludesFromPropertySheet(VCPropertySheet sheet)
        {
            var includeList = GetIncludesFromCompilerTool(CompilerToolWrapper.Create(sheet));
            var propertySheets = sheet.PropertySheets as IVCCollection;
            if (propertySheets != null)
                foreach (VCPropertySheet subSheet in propertySheets)
                    includeList.AddRange(GetIncludesFromPropertySheet(subSheet));
            return includeList;
        }

        private static List<string> GetIncludesFromCompilerTool(CompilerToolWrapper compiler)
        {
            try {
                if (compiler.GetAdditionalIncludeDirectories() != null && compiler.GetAdditionalIncludeDirectories().Length > 0) {
                    var includes = compiler.GetAdditionalIncludeDirectoriesList();
                    return new List<string>(includes);
                }
            } catch { }
            return new List<string>();
        }

        private static bool IsDebugConfiguration(VCConfiguration conf)
        {
            var tool = CompilerToolWrapper.Create(conf);
            if (tool != null) {
                return tool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDebug
                    || tool.RuntimeLibrary == runtimeLibraryOption.rtMultiThreadedDebugDLL;
            }
            return false;
        }

        private string GetPCHMocOptions(VCFile file, CompilerToolWrapper compiler)
        {
            // As .moc files are included, we should not add anything there
            if (!HelperFunctions.HasHeaderFileExtension(file.Name))
                return "";

            string additionalMocOptions = "\"-f" + compiler.GetPrecompiledHeaderThrough().Replace('\\', '/') + "\" ";
            //Get mocDir without .\\ at the beginning of it
            var mocDir = QtVSIPSettings.GetMocDirectory(envPro);
            if (mocDir.StartsWith(".\\"))
                mocDir = mocDir.Substring(2);

            //Get the absolute path
            mocDir = vcPro.ProjectDirectory + mocDir;
            var relPathToFile = HelperFunctions.GetRelativePath(mocDir, file.FullPath).Replace('\\', '/');
            additionalMocOptions += "\"-f" + relPathToFile + "\"";
            return additionalMocOptions;
        }

        /// <summary>
        /// Adds a moc step to a given file for this project.
        /// </summary>
        /// <param name="file">file</param>
        public void AddMocStep(VCFile file)
        {
            try {
                var mocFileName = GetMocFileName(file.FullPath);
                if (mocFileName == null)
                    return;

                var hasDifferentMocFilePerConfig = QtVSIPSettings.HasDifferentMocFilePerConfig(envPro);
                var hasDifferentMocFilePerPlatform = QtVSIPSettings.HasDifferentMocFilePerPlatform(envPro);
                var mocableIsCPP = mocFileName.ToLower().EndsWith(".moc");

                // Fresh C++ headers don't have a usable custom build tool. We must set the item type first.
                if (!mocableIsCPP && file.ItemType != "CustomBuild")
                    file.ItemType = "CustomBuild";

                foreach (VCFileConfiguration config in (IVCCollection) file.FileConfigurations) {
                    var vcConfig = config.ProjectConfiguration as VCConfiguration;
                    var platform = vcConfig.Platform as VCPlatform;
                    string platformName = platform.Name;

                    var mocRelPath = GetRelativeMocFilePath(file.FullPath, vcConfig.ConfigurationName, platformName);
                    string subfilterName = null;
                    if (mocRelPath.Contains(vcConfig.ConfigurationName))
                        subfilterName = vcConfig.ConfigurationName;
                    if (mocRelPath.Contains(platformName)) {
                        if (subfilterName != null)
                            subfilterName += '_';
                        subfilterName += platformName;
                    }
                    var mocFile = GetFileFromProject(mocRelPath);
                    if (mocFile == null) {
                        var fi = new FileInfo(VCProject.ProjectDirectory + "\\" + mocRelPath);
                        if (!fi.Directory.Exists)
                            fi.Directory.Create();
                        mocFile = AddFileInSubfilter(Filters.GeneratedFiles(), subfilterName,
                            mocRelPath);
                        if (mocFileName.ToLower().EndsWith(".moc")) {
                            var mocFileItem = mocFile.Object as ProjectItem;
                            if (mocFileItem != null)
                                HelperFunctions.EnsureCustomBuildToolAvailable(mocFileItem);
                        }
                    }

                    if (mocFile == null)
                        throw new QtVSException(SR.GetString("QtProject_CannotAddMocStep", file.FullPath));

                    VCCustomBuildTool tool = null;
                    string fileToMoc = null;
                    if (!mocableIsCPP) {
                        tool = HelperFunctions.GetCustomBuildTool(config);
                        fileToMoc = ProjectMacros.Path;
                    } else {
                        var mocConf = GetVCFileConfigurationByName(mocFile, vcConfig.Name);
                        tool = HelperFunctions.GetCustomBuildTool(mocConf);
                        fileToMoc = HelperFunctions.GetRelativePath(vcPro.ProjectDirectory, file.FullPath);
                    }
                    if (tool == null)
                        throw new QtVSException(SR.GetString("QtProject_CannotFindCustomBuildTool", file.FullPath));

                    if (hasDifferentMocFilePerPlatform && hasDifferentMocFilePerConfig) {
                        foreach (VCFileConfiguration mocConf in (IVCCollection) mocFile.FileConfigurations) {
                            var projectCfg = mocConf.ProjectConfiguration as VCConfiguration;
                            if (projectCfg.Name != vcConfig.Name || (IsMoccedFileIncluded(file) && !mocableIsCPP)) {
                                if (!mocConf.ExcludedFromBuild)
                                    mocConf.ExcludedFromBuild = true;
                            } else {
                                if (mocConf.ExcludedFromBuild != config.ExcludedFromBuild)
                                    mocConf.ExcludedFromBuild = config.ExcludedFromBuild;
                            }
                        }
                    } else if (hasDifferentMocFilePerPlatform) {
                        foreach (VCFileConfiguration mocConf in (IVCCollection) mocFile.FileConfigurations) {
                            var projectCfg = mocConf.ProjectConfiguration as VCConfiguration;
                            var mocConfPlatform = projectCfg.Platform as VCPlatform;
                            if (projectCfg.ConfigurationName != vcConfig.ConfigurationName)
                                continue;

                            bool exclude = mocConfPlatform.Name != platformName || (IsMoccedFileIncluded(file) && !mocableIsCPP);
                            if (exclude) {
                                if (mocConf.ExcludedFromBuild != exclude)
                                    mocConf.ExcludedFromBuild = exclude;
                            } else {
                                if (mocConf.ExcludedFromBuild != config.ExcludedFromBuild)
                                    mocConf.ExcludedFromBuild = config.ExcludedFromBuild;
                            }
                        }
                    } else if (hasDifferentMocFilePerConfig) {
                        foreach (VCFileConfiguration mocConf in (IVCCollection) mocFile.FileConfigurations) {
                            var projectCfg = mocConf.ProjectConfiguration as VCConfiguration;
                            var mocConfPlatform = projectCfg.Platform as VCPlatform;
                            if (platformName != mocConfPlatform.Name)
                                continue;

                            if (projectCfg.Name != vcConfig.Name || (IsMoccedFileIncluded(file) && !mocableIsCPP)) {
                                if (!mocConf.ExcludedFromBuild)
                                    mocConf.ExcludedFromBuild = true;
                            } else {
                                if (mocConf.ExcludedFromBuild != config.ExcludedFromBuild)
                                    mocConf.ExcludedFromBuild = config.ExcludedFromBuild;
                            }
                        }
                    } else {
                        var moccedFileConfig = GetVCFileConfigurationByName(mocFile, config.Name);
                        if (moccedFileConfig != null) {
                            var cppFile = GetCppFileForMocStep(file);
                            if (cppFile != null && IsMoccedFileIncluded(cppFile)) {
                                if (!moccedFileConfig.ExcludedFromBuild) {
                                    moccedFileConfig.ExcludedFromBuild = true;
                                }
                            } else if (moccedFileConfig.ExcludedFromBuild != config.ExcludedFromBuild)
                                moccedFileConfig.ExcludedFromBuild = config.ExcludedFromBuild;
                        }
                    }

                    string dps = tool.AdditionalDependencies;
                    if (dps.IndexOf("\"" + Resources.moc4Command + "\"") < 0) {
                        if (dps.Length > 0 && !dps.EndsWith(";"))
                            dps += ";";
                        tool.AdditionalDependencies = dps + "\"" + Resources.moc4Command + "\";" + fileToMoc;
                    }

                    tool.Description = "Moc'ing " + file.Name + "...";

                    string inputMocFile = ProjectMacros.Path;
                    if (mocableIsCPP)
                        inputMocFile = file.RelativePath;
                    string output = tool.Outputs;
                    string outputMocFile = "";
                    string outputMocMacro = "";
                    var baseFileName = file.Name.Remove(file.Name.LastIndexOf('.'));
                    string pattern = "(\"(.*\\\\" + mocFileName + ")\"|(\\S*"
                        + mocFileName + "))";
                    var regExp = new Regex(pattern);
                    var matchList = regExp.Matches(tool.Outputs.Replace(ProjectMacros.Name, baseFileName));
                    if (matchList.Count > 0) {
                        if (matchList[0].Length > 0) {
                            outputMocFile = matchList[0].ToString();
                        } else if (matchList[1].Length > 1) {
                            outputMocFile = matchList[1].ToString();
                        }
                        if (outputMocFile.StartsWith("\""))
                            outputMocFile = outputMocFile.Substring(1);
                        if (outputMocFile.EndsWith("\""))
                            outputMocFile = outputMocFile.Substring(0, outputMocFile.Length - 1);
                        var outputMocPath = Path.GetDirectoryName(outputMocFile);
                        var stringToReplace = Path.GetFileName(outputMocFile);
                        outputMocMacro = outputMocPath + "\\" + stringToReplace.Replace(baseFileName, ProjectMacros.Name);
                    } else {
                        outputMocFile = GetRelativeMocFilePath(file.FullPath);
                        var outputMocPath = Path.GetDirectoryName(outputMocFile);
                        var stringToReplace = Path.GetFileName(outputMocFile);
                        outputMocMacro = outputMocPath + "\\" + stringToReplace.Replace(baseFileName, ProjectMacros.Name);
                        if (output.Length > 0 && !output.EndsWith(";"))
                            output += ";";
                        tool.Outputs = output + "\"" + outputMocMacro + "\"";
                    }

                    string newCmdLine = "\"" + Resources.moc4Command + "\" " + QtVSIPSettings.GetMocOptions(envPro)
                        + " \"" + inputMocFile + "\" -o \""
                        + outputMocMacro + "\"";

                    // Tell moc to include the PCH header if we are using precompiled headers in the project
                    var compiler = CompilerToolWrapper.Create(vcConfig);
                    if (compiler.GetUsePrecompiledHeader() != pchOption.pchNone) {
                        newCmdLine += " " + GetPCHMocOptions(file, compiler);
                    }

                    var versionManager = QtVersionManager.The();
                    var versionInfo = new VersionInformation(versionManager.GetInstallPath(envPro));
                    bool mocSupportsIncludes = (versionInfo.qtMajor == 4 && versionInfo.qtMinor >= 2)
                        || versionInfo.qtMajor >= 5;

                    string strDefinesIncludes = "";
                    VCFile cppPropertyFile;
                    if (!mocableIsCPP)
                        cppPropertyFile = GetCppFileForMocStep(file);
                    else
                        cppPropertyFile = GetCppFileForMocStep(mocFile);
                    VCFileConfiguration defineIncludeConfig;
                    if (cppPropertyFile != null)
                        defineIncludeConfig = GetVCFileConfigurationByName(cppPropertyFile, config.Name);
                    else
                        // No file specific defines/includes but at least the project defines/includes are added
                        defineIncludeConfig = config;
                    strDefinesIncludes += GetDefines(defineIncludeConfig);
                    strDefinesIncludes += GetIncludes(defineIncludeConfig);
                    int cmdLineLength = newCmdLine.Length + strDefinesIncludes.Length + 1;
                    if (cmdLineLength > HelperFunctions.GetMaximumCommandLineLength() && mocSupportsIncludes) {
                        // Command line is too long. We must use an options file.
                        string mocIncludeCommands = "";
                        string mocIncludeFile = "\"" + outputMocFile + ".inc\"";
                        string redirectOp = " > ";
                        int maxCmdLineLength = HelperFunctions.GetMaximumCommandLineLength() - (mocIncludeFile.Length + 1);

                        var options = strDefinesIncludes.Split(' ');

                        int i = options.Length - 1;
                        for (; i >= 0; --i) {
                            if (options[i].Length == 0)
                                continue;
                            mocIncludeCommands += "echo " + options[i] + redirectOp + mocIncludeFile + "\r\n";
                            cmdLineLength -= options[i].Length + 1;
                            if (cmdLineLength < maxCmdLineLength)
                                break;
                            if (i == options.Length - 1)    // first loop
                                redirectOp = " >> ";
                        }
                        strDefinesIncludes = "@" + mocIncludeFile;
                        for (int k = 0; k < i; ++k)
                            if (options[k].Length > 0)
                                strDefinesIncludes += " " + options[k];

                        newCmdLine = mocIncludeCommands + newCmdLine + " " + strDefinesIncludes;
                    } else {
                        newCmdLine = newCmdLine + " " + strDefinesIncludes;
                    }

                    if (tool.CommandLine.Trim().Length > 0) {
                        string cmdLine = tool.CommandLine;

                        // remove the moc option file commands
                        {
                            var rex = new Regex("^echo.+[.](moc|cpp)[.]inc\"\r\n", RegexOptions.Multiline);
                            cmdLine = rex.Replace(cmdLine, "");
                        }

                        var m = System.Text.RegularExpressions.Regex.Match(cmdLine,
                            @"(\S*moc.exe|""\S+:\\\.*moc.exe"")");

                        if (m.Success) {
                            int start = m.Index;
                            var end = cmdLine.IndexOf("&&", start);
                            var a = cmdLine.IndexOf("\r\n", start);
                            if ((a > -1 && a < end) || (end < 0 && a > -1))
                                end = a;
                            if (end < 0)
                                end = cmdLine.Length;
                            tool.CommandLine = cmdLine.Replace(cmdLine.Substring(start, end - start), newCmdLine);
                        } else {
                            tool.CommandLine = cmdLine + "\r\n" + newCmdLine;
                        }
                    } else {
                        tool.CommandLine = newCmdLine;
                    }
                }
            } catch {
                throw new QtVSException(SR.GetString("QtProject_CannotAddMocStep", file.FullPath));
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
            if (HelperFunctions.HasHeaderFileExtension(vcFile.FullPath))
                fullPath = Path.ChangeExtension(vcFile.FullPath, ".cpp");

            if (HelperFunctions.HasSourceFileExtension(fullPath)) {
                vcFile = GetFileFromProject(fullPath);
                if (vcFile == null)
                    return false;

                var mocFile = "moc_" + Path.GetFileNameWithoutExtension(vcFile.FullPath) + ".cpp";

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
                CxxStreamReader cxxStream = null;
                try {
                    string line;
                    cxxStream = new CxxStreamReader(vcFile.FullPath);
                    while ((line = cxxStream.ReadLine()) != null) {
                        if (Regex.IsMatch(line, "#include *(<|\")" + mocFile + "(\"|>)"))
                            return true;
                    }
                } catch { } finally {
                    if (cxxStream != null)
                        cxxStream.Dispose();
                }
            }
            return false;
        }

        public bool HasMocStep(VCFile file)
        {
            return HasMocStep(file, null);
        }

        public bool HasMocStep(VCFile file, string mocOutDir)
        {
            if (HelperFunctions.HasHeaderFileExtension(file.Name)) {
                return CheckForCommand(file, "moc.exe");
            } else if (HelperFunctions.HasSourceFileExtension(file.Name)) {
                foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                    string mocFileName = "";
                    if (mocOutDir == null) {
                        var platform = config.Platform as VCPlatform;
                        mocFileName = GetRelativeMocFilePath(file.Name, config.ConfigurationName, platform.Name);
                    } else {
                        var fileName = GetMocFileName(file.FullPath);
                        if (fileName != null) {
                            mocOutDir = mocOutDir.Replace("$(ConfigurationName)", config.ConfigurationName);
                            var platform = config.Platform as VCPlatform;
                            mocOutDir = mocOutDir.Replace("$(PlatformName)", platform.Name);
                            mocFileName = mocOutDir + "\\" + fileName;
                        }
                    }
                    var mocFile = GetFileFromProject(mocFileName);
                    if (mocFileName != null)
                        return CheckForCommand(mocFile, Resources.moc4Command);
                }
            }
            return false;
        }

        public static bool HasUicStep(VCFile file)
        {
            return CheckForCommand(file, Resources.uic4Command);
        }

        private static bool CheckForCommand(VCFile file, string cmd)
        {
            if (file == null)
                return false;
            foreach (VCFileConfiguration config in (IVCCollection) file.FileConfigurations) {
                var tool = HelperFunctions.GetCustomBuildTool(config);
                if (tool == null)
                    return false;
                if (tool.CommandLine != null && tool.CommandLine.Contains(cmd))
                    return true;
            }
            return false;
        }

        public void RefreshRccSteps()
        {
            Messages.PaneMessage(dte, "\r\n=== Update rcc steps ===");
            var files = GetResourceFiles();

            var vcFilter = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (vcFilter != null) {
                var filterFiles = (IVCCollection) vcFilter.Files;
                var filesToDelete = new List<VCFile>();
                foreach (VCFile rmFile in filterFiles) {
                    if (rmFile.Name.ToLower().StartsWith("qrc_"))
                        filesToDelete.Add(rmFile);
                }
                foreach (VCFile rmFile in filesToDelete) {
                    RemoveFileFromFilter(rmFile, vcFilter);
                    HelperFunctions.DeleteEmptyParentDirs(rmFile);
                }
            }

            foreach (VCFile file in files) {
                Messages.PaneMessage(dte, "Update rcc step for " + file.Name + ".");
                var options = new RccOptions(envPro, file);
                UpdateRccStep(file, options);
            }

            Messages.PaneMessage(dte, "\r\n=== " + files.Count.ToString()
                + " rcc steps updated. ===\r\n");
        }

        public void RefreshRccSteps(string oldRccDir)
        {
            RefreshRccSteps();
            UpdateCompilerIncludePaths(oldRccDir, QtVSIPSettings.GetRccDirectory(envPro));
        }

        public void UpdateRccStep(string fileName, RccOptions rccOpts)
        {

            var file = (VCFile) ((IVCCollection) vcPro.Files).Item(fileName);
            UpdateRccStep(file, rccOpts);
        }

        public void UpdateRccStep(VCFile qrcFile, RccOptions rccOpts)
        {
            var vcpro = (VCProject) qrcFile.project;
            EnvDTE.DTE dteObject = ((EnvDTE.Project) vcpro.Object).DTE;

            var qtPro = QtProject.Create(vcpro);
            var parser = new QrcParser(qrcFile.FullPath);
            string filesInQrcFile = ProjectMacros.Path;

            if (parser.parse()) {
                var fi = new FileInfo(qrcFile.FullPath);
                string qrcDir = fi.Directory.FullName + "\\";

                foreach (QrcPrefix prfx in parser.Prefixes) {
                    foreach (QrcItem itm in prfx.Items) {
                        var relativeQrcItemPath = HelperFunctions.GetRelativePath(vcPro.ProjectDirectory,
                            qrcDir + itm.Path);
                        filesInQrcFile += ";" + relativeQrcItemPath;
                        try {
                            var addedFile = qtPro.AddFileInFilter(Filters.ResourceFiles(), relativeQrcItemPath, true);
                            QtProject.ExcludeFromAllBuilds(addedFile);
                        } catch { /* it's not possible to add all kinds of files */ }
                    }
                }
            }

            var nameOnly = HelperFunctions.RemoveFileNameExtension(new FileInfo(qrcFile.FullPath));
            string qrcCppFile = QtVSIPSettings.GetRccDirectory(envPro) + "\\" + "qrc_" + nameOnly + ".cpp";

            try {
                foreach (VCFileConfiguration vfc in (IVCCollection) qrcFile.FileConfigurations) {
                    RccOptions rccOptsCfg = rccOpts;
                    string cmdLine = "";

                    var cbt = HelperFunctions.GetCustomBuildTool(vfc);

                    cbt.AdditionalDependencies = filesInQrcFile;

                    cbt.Description = "Rcc'ing " + ProjectMacros.FileName + "...";

                    cbt.Outputs = qrcCppFile.Replace(nameOnly, ProjectMacros.Name);

                    cmdLine += "\"" + Resources.rcc4Command + "\""
                        + " -name \"" + ProjectMacros.Name + "\"";

                    if (rccOptsCfg == null)
                        rccOptsCfg = HelperFunctions.ParseRccOptions(cbt.CommandLine, qrcFile);

                    if (rccOptsCfg.CompressFiles) {
                        cmdLine += " -threshold " + rccOptsCfg.CompressThreshold.ToString();
                        cmdLine += " -compress " + rccOptsCfg.CompressLevel.ToString();
                    } else {
                        cmdLine += " -no-compress";
                    }
                    cmdLine += " \"" + ProjectMacros.Path + "\" -o " + cbt.Outputs;
                    cbt.CommandLine = cmdLine;
                }
                AddFileInFilter(Filters.GeneratedFiles(), qrcCppFile, true);
            } catch (System.Exception /*e*/) {
                Messages.PaneMessage(dteObject, "*** WARNING (RCC): Couldn't add rcc step");
            }
        }

        public void RemoveRccStep(VCFile file)
        {
            if (file == null)
                return;
            try {
                string relativeQrcFilePath = file.RelativePath;
                var qrcFileInfo = new FileInfo(ProjectDir + "\\" + relativeQrcFilePath);
                if (qrcFileInfo.Exists) {
                    var opts = new RccOptions(Project, file);
                    string qrcCppFile = QtVSIPSettings.GetRccDirectory(envPro) + "\\" + opts.OutputFileName;
                    var generatedFile = GetFileFromProject(qrcCppFile);
                    if (generatedFile != null)
                        RemoveFileFromFilter(generatedFile, Filters.GeneratedFiles());
                }
            } catch (System.Exception e) {
                Messages.DisplayWarningMessage(e);
            }
        }

        static public void ExcludeFromAllBuilds(VCFile file)
        {
            if (file == null)
                return;
            foreach (VCFileConfiguration conf in (IVCCollection) file.FileConfigurations)
                if (!conf.ExcludedFromBuild)
                    conf.ExcludedFromBuild = true;
        }

        /// <summary>
        /// Removes the custom build step of a given file.
        /// </summary>
        /// <param name="file">file</param>
        public void RemoveMocStep(VCFile file)
        {
            try {
                if (!HasMocStep(file))
                    return;

                if (HelperFunctions.HasHeaderFileExtension(file.Name)) {
                    foreach (VCFileConfiguration config in (IVCCollection) file.FileConfigurations) {
                        var tool = HelperFunctions.GetCustomBuildTool(config);
                        if (tool == null)
                            continue;

                        string cmdLine = tool.CommandLine;
                        if (cmdLine.Length > 0) {
                            var rex = new Regex(@"(\S*moc.exe|""\S+:\\\.*moc.exe"")");
                            while (true) {
                                var m = rex.Match(cmdLine);
                                if (!m.Success)
                                    break;

                                int start = m.Index;
                                var end = cmdLine.IndexOf("&&", start);
                                var a = cmdLine.IndexOf("\r\n", start);
                                if ((a > -1 && a < end) || (end < 0 && a > -1))
                                    end = a;
                                if (end < 0)
                                    end = cmdLine.Length;

                                cmdLine = cmdLine.Remove(start, end - start).Trim();
                                if (cmdLine.StartsWith("&&"))
                                    cmdLine = cmdLine.Remove(0, 2).Trim();
                            }
                            tool.CommandLine = cmdLine;
                        }

                        var reg = new Regex("Moc'ing .+\\.\\.\\.");
                        string addDepends = tool.AdditionalDependencies;
                        addDepends = System.Text.RegularExpressions.Regex.Replace(addDepends,
                            @"(\S*moc.exe|""\S+:\\\.*moc.exe"")", "");
                        addDepends = addDepends.Replace(file.RelativePath, "");
                        tool.AdditionalDependencies = "";
                        tool.Description = reg.Replace(tool.Description, "");
                        tool.Description = tool.Description.Replace("MOC " + file.Name, "");
                        var baseFileName = file.Name.Remove(file.Name.LastIndexOf('.'));
                        string pattern = "(\"(.*\\\\" + GetMocFileName(file.FullPath)
                            + ")\"|(\\S*" + GetMocFileName(file.FullPath) + "))";
                        string outputMocFile = null;
                        var regExp = new Regex(pattern);
                        tool.Outputs = tool.Outputs.Replace(ProjectMacros.Name, baseFileName);
                        var matchList = regExp.Matches(tool.Outputs);
                        if (matchList.Count > 0) {
                            if (matchList[0].Length > 0) {
                                outputMocFile = matchList[0].ToString();
                            } else if (matchList[1].Length > 1) {
                                outputMocFile = matchList[1].ToString();
                            }
                        }
                        tool.Outputs = System.Text.RegularExpressions.Regex.Replace(tool.Outputs, pattern, "",
                            RegexOptions.Multiline | RegexOptions.IgnoreCase);
                        tool.Outputs = System.Text.RegularExpressions.Regex.Replace(tool.Outputs,
                            @"\s*;\s*;\s*", ";", RegexOptions.Multiline);
                        tool.Outputs = System.Text.RegularExpressions.Regex.Replace(tool.Outputs,
                            @"(^\s*;|\s*;\s*$)", "", RegexOptions.Multiline);

                        if (outputMocFile != null) {
                            if (outputMocFile.StartsWith("\""))
                                outputMocFile = outputMocFile.Substring(1);
                            if (outputMocFile.EndsWith("\""))
                                outputMocFile = outputMocFile.Substring(0, outputMocFile.Length - 1);
                            outputMocFile = outputMocFile.Replace("$(ConfigurationName)",
                                config.Name.Substring(0, config.Name.IndexOf('|')));
                            outputMocFile = outputMocFile.Replace("$(PlatformName)",
                                config.Name.Remove(0, config.Name.IndexOf('|') + 1));
                        }
                        var mocFile = GetFileFromProject(outputMocFile);
                        if (mocFile != null)
                            RemoveFileFromFilter(mocFile, Filters.GeneratedFiles());
                    }
                } else {
                    if (QtVSIPSettings.HasDifferentMocFilePerConfig(envPro)
                        || QtVSIPSettings.HasDifferentMocFilePerPlatform(envPro)) {
                        foreach (VCFileConfiguration config in (IVCCollection) file.FileConfigurations) {
                            var mocFileName = GetMocFileName(file.Name);
                            var mocFile = GetGeneratedMocFile(mocFileName, config);
                            if (mocFile != null)
                                RemoveFileFromFilter(mocFile, Filters.GeneratedFiles());
                        }
                    } else {
                        var mocFileName = GetMocFileName(file.Name);
                        var mocFile = GetGeneratedMocFile(mocFileName, null);
                        if (mocFile != null)
                            RemoveFileFromFilter(mocFile, Filters.GeneratedFiles());
                    }
                }
            } catch {
                throw new QtVSException(SR.GetString("QtProject_CannotRemoveMocStep", file.FullPath));
            }
        }

        public void RemoveUiHeaderFile(VCFile file)
        {
            if (file == null)
                return;
            try {
                var headerFile = GetUiGeneratedFileName(file.Name);
                var hFile = GetFileFromProject(headerFile);

                if (hFile != null)
                    RemoveFileFromFilter(hFile, Filters.GeneratedFiles());
            } catch (System.Exception e) {
                Messages.DisplayWarningMessage(e);
            }
        }

        public void RemoveUic4BuildStep(VCFile file)
        {
            if (file == null)
                return;
            foreach (VCFileConfiguration config in (IVCCollection) file.FileConfigurations) {
                var tool = HelperFunctions.GetCustomBuildTool(config);
                tool.AdditionalDependencies = "";
                tool.Description = "";
                tool.CommandLine = "";
                tool.Outputs = "";
            }
            RemoveUiHeaderFile(file);
        }

        public List<VCFile> GetResourceFiles()
        {
            var qrcFiles = new List<VCFile>();

            foreach (VCFile f in (IVCCollection) VCProject.Files) {
                if (f.Extension == ".qrc")
                    qrcFiles.Add(f);
            }
            return qrcFiles;
        }

        /// <summary>
        /// Returns the file if it can be found, otherwise null.
        /// </summary>
        /// <param name="filter">filter name</param>
        /// <param name="fileName">relative file path to the project</param>
        /// <returns></returns>
        public VCFile GetFileFromFilter(FakeFilter filter, string fileName)
        {
            var vcfilter = FindFilterFromGuid(filter.UniqueIdentifier);

            // try with name as well
            if (vcfilter == null)
                vcfilter = FindFilterFromName(filter.Name);

            if (vcfilter == null)
                return null;

            try {
                FileInfo fi = null;
                if (Path.IsPathRooted(fileName))
                    fi = new FileInfo(fileName);
                else
                    fi = new FileInfo(ProjectDir + "\\" + fileName);

                if (fi == null)
                    return null;

                foreach (VCFile file in (IVCCollection) vcfilter.Files) {
                    if (file.MatchName(fi.FullName, true))
                        return file;
                }
            } catch { }
            return null;
        }

        /// <summary>
        /// Returns the file (VCFile) specified by the file name from a given
        /// project.
        /// </summary>
        /// <param name="proj">project</param>
        /// <param name="fileName">file name (relative path)</param>
        /// <returns></returns>
        public VCFile GetFileFromProject(string fileName)
        {
            return GetFileFromProject(fileName, true);
        }

        public VCFile GetFileFromProject(string fileName, bool beStrict)
        {
            VCFile vcfile = null;
            fileName = HelperFunctions.NormalizeRelativeFilePath(fileName);

            string nf = fileName;
            if (!HelperFunctions.IsAbsoluteFilePath(fileName))
                nf = HelperFunctions.NormalizeFilePath(vcPro.ProjectDirectory + "\\" + fileName);
            nf = nf.ToLower();

            foreach (VCFile f in (IVCCollection) vcPro.Files) {
                if (f.FullPath.ToLower() == nf)
                    return f;
            }
            if (beStrict || vcfile != null)
                return vcfile;

            var fi = new FileInfo(fileName);
            foreach (VCFile f in (IVCCollection) vcPro.Files) {
                if (f.Name.ToLower() == fi.Name.ToLower())
                    return f;
            }
            return null;
        }

        /// <summary>
        /// Returns the files (List<VCFile>) specified by the file name from a given
        /// project.
        /// </summary>
        /// <param name="proj">project</param>
        /// <param name="fileName">file name (relative path)</param>
        /// <returns></returns>
        public System.Collections.Generic.List<VCFile> GetFilesFromProject(string fileName)
        {
            var tmpList = new System.Collections.Generic.List<VCFile>();
            fileName = HelperFunctions.NormalizeRelativeFilePath(fileName);

            var fi = new FileInfo(fileName);
            foreach (VCFile f in (IVCCollection) vcPro.Files) {
                if (f.Name.ToLower() == fi.Name.ToLower())
                    tmpList.Add(f);
            }
            return tmpList;
        }

        public System.Collections.Generic.List<VCFile> GetAllFilesFromFilter(VCFilter filter)
        {
            var tmpList = new System.Collections.Generic.List<VCFile>();

            foreach (VCFile f in (IVCCollection) filter.Files) {
                tmpList.Add(f);
            }
            foreach (VCFilter subfilter in (IVCCollection) filter.Filters)
                foreach (VCFile file in GetAllFilesFromFilter(subfilter))
                    tmpList.Add(file);

            return tmpList;
        }

        /// <summary>
        /// Adds a file to a filter. If the filter doesn't exist yet, it
        /// will be created. (Doesn't check for duplicates)
        /// </summary>
        /// <param name="filter">fake filter</param>
        /// <param name="fileName">relative file name</param>
        /// <returns>A VCFile object of the added file.</returns>
        public VCFile AddFileInFilter(FakeFilter filter, string fileName)
        {
            return AddFileInFilter(filter, fileName, false);
        }

        public void RemoveItem(ProjectItem item)
        {
            foreach (ProjectItem tmpFilter in Project.ProjectItems) {
                if (tmpFilter.Name == item.Name) {
                    tmpFilter.Remove();
                    return;
                }
                foreach (ProjectItem tmpItem in tmpFilter.ProjectItems)
                    if (tmpItem.Name == item.Name) {
                        tmpItem.Remove();
                        return;
                    }
            }
        }

        /// <summary>
        /// Adds a file to a filter. If the filter doesn't exist yet, it
        /// will be created.
        /// </summary>
        /// <param name="filter">fake filter</param>
        /// <param name="fileName">relative file name</param>
        /// <param name="checkForDuplicates">true if we don't want duplicated files</param>
        /// <returns>A VCFile object of the added file.</returns>
        public VCFile AddFileInFilter(FakeFilter filter, string fileName, bool checkForDuplicates)
        {
            return AddFileInSubfilter(filter, null, fileName, checkForDuplicates);
        }

        public VCFile AddFileInSubfilter(FakeFilter filter, string subfilterName, string fileName)
        {
            return AddFileInSubfilter(filter, subfilterName, fileName, false);
        }

        public VCFile AddFileInSubfilter(FakeFilter filter, string subfilterName, string fileName, bool checkForDuplicates)
        {
            try {
                var vfilt = FindFilterFromGuid(filter.UniqueIdentifier);
                if (vfilt == null) {
                    if (!vcPro.CanAddFilter(filter.Name)) {
                        // check if user already created this filter... then add guid
                        vfilt = FindFilterFromName(filter.Name);
                        if (vfilt == null)
                            throw new QtVSException(SR.GetString("QtProject_CannotAddFilter", filter.Name));
                    } else {
                        vfilt = (VCFilter) vcPro.AddFilter(filter.Name);
                    }

                    vfilt.UniqueIdentifier = filter.UniqueIdentifier;
                    vfilt.Filter = filter.Filter;
                    vfilt.ParseFiles = filter.ParseFiles;
                }

                if (!string.IsNullOrEmpty(subfilterName)) {
                    var lowerSubFilterName = subfilterName.ToLower();
                    bool subfilterFound = false;
                    foreach (VCFilter subfilt in vfilt.Filters as IVCCollection) {
                        if (subfilt.Name.ToLower() == lowerSubFilterName) {
                            vfilt = subfilt;
                            subfilterFound = true;
                            break;
                        }
                    }
                    if (subfilterFound) {
                        // Do filter names differ in upper/lower case?
                        if (subfilterName != vfilt.Name) {
                            try {
                                // Try to rename the filter for aesthetic reasons.
                                vfilt.Name = subfilterName;
                            } catch {
                                // Renaming didn't work. We don't care.
                            }
                        }
                    }
                    if (!subfilterFound) {
                        if (!vfilt.CanAddFilter(subfilterName)) {
                            throw new QtVSException(SR.GetString("QtProject_CannotAddFilter", filter.Name));
                        } else {
                            vfilt = (VCFilter) vfilt.AddFilter(subfilterName);
                        }

                        vfilt.Filter = "cpp;moc";
                        vfilt.SourceControlFiles = false;
                    }
                }

                if (checkForDuplicates) {
                    // check if file exists in filter already
                    var vcFile = GetFileFromFilter(filter, fileName);
                    if (vcFile != null)
                        return vcFile;
                }

                if (vfilt.CanAddFile(fileName))
                    return (VCFile) (vfilt.AddFile(fileName));
                else
                    throw new QtVSException(SR.GetString("QtProject_CannotAddFile", fileName));
            } catch {
                throw new QtVSException(SR.GetString("QtProject_CannotAddFile", fileName));
            }
        }

        /// <summary>
        /// Removes a file from the filter.
        /// This file will be deleted!
        /// </summary>
        /// <param name="project">project</param>
        /// <param name="file">file</param>
        public void RemoveFileFromFilter(VCFile file, FakeFilter filter)
        {
            try {
                var vfilt = FindFilterFromGuid(filter.UniqueIdentifier);

                if (vfilt == null)
                    vfilt = FindFilterFromName(filter.Name);

                if (vfilt == null)
                    return;

                RemoveFileFromFilter(file, vfilt);
            } catch {
                throw new QtVSException(SR.GetString("QtProject_CannotRemoveFile", file.Name));
            }
        }

        /// <summary>
        /// Removes a file from the filter.
        /// This file will be deleted!
        /// </summary>
        /// <param name="project">project</param>
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

            var subfilters = (IVCCollection) filter.Filters;
            for (int i = subfilters.Count; i > 0; i--) {
                try {
                    var subfilter = (VCFilter) subfilters.Item(i);
                    RemoveFileFromFilter(file, subfilter);
                } catch {
                }
            }
        }

        public void MoveFileToDeletedFolder(VCFile vcfile)
        {
            var srcFile = new FileInfo(vcfile.FullPath);

            if (!srcFile.Exists)
                return;

            string destFolder = vcPro.ProjectDirectory + "\\Deleted\\";
            string destName = destFolder + vcfile.Name.Replace(".", "_") + ".bak";
            int fileNr = 0;

            try {
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);

                while (File.Exists(destName)) {
                    fileNr++;
                    destName = destName.Substring(0, destName.LastIndexOf(".")) + ".b";
                    if (fileNr > 9)
                        destName += fileNr.ToString();
                    else
                        destName += "0" + fileNr.ToString();
                }

                srcFile.MoveTo(destName);
            } catch (System.Exception e) {
                Messages.DisplayWarningMessage(e, SR.GetString("QtProject_DeletedFolderFullOrProteced"));
            }
        }

        public VCFilter FindFilterFromName(string filtername)
        {
            try {
                foreach (VCFilter vcfilt in (IVCCollection) vcPro.Filters) {
                    if (vcfilt.Name.ToLower() == filtername.ToLower()) {
                        return vcfilt;
                    }
                }
                return null;
            } catch {
                throw new QtVSException(SR.GetString("QtProject_CannotFindFilter"));
            }
        }

        public VCFilter FindFilterFromGuid(string filterguid)
        {
            try {
                foreach (VCFilter vcfilt in (IVCCollection) vcPro.Filters) {
                    if (vcfilt.UniqueIdentifier != null
                        && vcfilt.UniqueIdentifier.ToLower() == filterguid.ToLower()) {
                        return vcfilt;
                    }
                }
                return null;
            } catch {
                throw new QtVSException(SR.GetString("QtProject_CannotFindFilter"));
            }
        }

        public VCFilter AddFilterToProject(FakeFilter filter)
        {
            try {
                var vfilt = FindFilterFromGuid(filter.UniqueIdentifier);
                if (vfilt == null) {
                    if (!vcPro.CanAddFilter(filter.Name)) {
                        vfilt = FindFilterFromName(filter.Name);
                        if (vfilt == null)
                            throw new QtVSException(SR.GetString("QtProject_ProjectCannotAddFilter", filter.Name));
                    } else {
                        vfilt = (VCFilter) vcPro.AddFilter(filter.Name);
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

        public void AddDirectories()
        {
            try {
                // resource directory
                var fi = new FileInfo(envPro.FullName);
                var dfi = new DirectoryInfo(fi.DirectoryName + "\\" + Resources.resourceDir);
                dfi.Create();

                // generated files directory
                dfi = new DirectoryInfo(fi.DirectoryName + "\\" + Resources.generatedFilesDir);
                dfi.Create();
            } catch {
                throw new QtVSException(SR.GetString("QtProject_CannotCreateResourceDir"));
            }
            AddFilterToProject(Filters.ResourceFiles());
        }

        public void Finish()
        {
            try {
                var solutionExplorer = dte.Windows.Item(Constants.vsWindowKindSolutionExplorer);
                if (solutionExplorer != null) {
                    var hierarchy = (EnvDTE.UIHierarchy) solutionExplorer.Object;
                    EnvDTE.UIHierarchyItems projects = hierarchy.UIHierarchyItems.Item(1).UIHierarchyItems;

                    foreach (EnvDTE.UIHierarchyItem itm in projects) {
                        if (itm.Name == envPro.Name) {
                            foreach (EnvDTE.UIHierarchyItem i in itm.UIHierarchyItems) {
                                if (i.Name == Filters.GeneratedFiles().Name)
                                    i.UIHierarchyItems.Expanded = false;
                            }
                            break;
                        }
                    }
                }
            } catch { }
        }

        public bool IsDesignerPluginProject()
        {
            bool b = false;
            if (Project.Globals.get_VariablePersists("IsDesignerPlugin")) {
                var s = (string) Project.Globals["IsDesignerPlugin"];
                try {
                    b = bool.Parse(s);
                } catch { }
            }
            return b;
        }

        /// <summary>
        /// Adds a file to a specified filter in a project.
        /// </summary>
        /// <param name="project">VCProject</param>
        /// <param name="srcFile">full name of the file to add</param>
        /// <param name="destName">name of the file in the project (relative to the project directory)</param>
        /// <param name="filter">filter</param>
        /// <returns>VCFile</returns>
        public VCFile AddFileToProject(string destName, FakeFilter filter)
        {
            VCFile file = null;
            if (filter != null)
                file = AddFileInFilter(filter, destName);
            else
                file = (VCFile) vcPro.AddFile(destName);

            if (file == null)
                return null;

            if (HelperFunctions.HasHeaderFileExtension(file.Name)) {
                foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                    var compiler = CompilerToolWrapper.Create(config);
                    if (compiler == null)
                        continue;

                    var paths = compiler.GetAdditionalIncludeDirectoriesList();
                    var fi = new FileInfo(file.FullPath);
                    var relativePath = HelperFunctions.GetRelativePath(ProjectDir, fi.Directory.ToString());
                    var fixedRelativePath = FixFilePathForComparison(relativePath);
                    bool pathFound = false;
                    foreach (string p in paths) {
                        if (FixFilePathForComparison(p) == fixedRelativePath) {
                            pathFound = true;
                            break;
                        }
                    }
                    if (!pathFound)
                        compiler.AddAdditionalIncludeDirectories(relativePath);
                }
            }
            return file;
        }

        /// <summary>
        /// adjusts the whitespaces, tabs in the given file according to VS settings
        /// </summary>
        /// <param name="file"></param>
        public void AdjustWhitespace(string file)
        {
            // only replace whitespaces in known types
            if (!HelperFunctions.HasSourceFileExtension(file) &&
                !HelperFunctions.HasHeaderFileExtension(file) && !file.EndsWith(".ui"))
                return;

            try {
                var prop = dte.get_Properties("TextEditor", "C/C++");
                var tabSize = Convert.ToInt64(prop.Item("TabSize").Value);
                var insertTabs = Convert.ToBoolean(prop.Item("InsertTabs").Value);

                string oldValue = insertTabs ? "    " : "\t";
                string newValue = insertTabs ? "\t" : GetWhitespaces(tabSize);

                var list = new List<string>();
                var reader = new StreamReader(file);
                var line = reader.ReadLine();
                while (line != null) {
                    if (line.StartsWith(oldValue))
                        line = line.Replace(oldValue, newValue);
                    list.Add(line);
                    line = reader.ReadLine();
                }
                reader.Close();

                var writer = new StreamWriter(file);
                foreach (string l in list)
                    writer.WriteLine(l);
                writer.Close();
            } catch (Exception e) {
                Messages.DisplayErrorMessage(SR.GetString("QtProject_CannotAdjustWhitespaces", e.ToString()));
            }
        }

        private static string GetWhitespaces(long size)
        {
            string whitespaces = null;
            for (long i = 0; i < size; ++i) {
                whitespaces += " ";
            }
            return whitespaces;
        }

        /// <summary>
        /// Copy a file to the projects folder. Does not add the file to the project.
        /// </summary>
        /// <param name="srcFile">full name of the file to add</param>
        /// <param name="destName">name of the file in the project (relative to the project directory)</param>
        /// <returns>full name of the destination file</returns>
        public string CopyFileToProject(string srcFile, string destName)
        {
            return CopyFileToFolder(srcFile, vcPro.ProjectDirectory, destName);
        }

        public static string CopyFileToFolder(string srcFile, string destFolder, string destName)
        {
            string fullDestName = destFolder + "\\" + destName;
            var fi = new FileInfo(fullDestName);

            bool replace = true;
            if (File.Exists(fullDestName)) {
                if (DialogResult.No == MessageBox.Show(SR.GetString("QtProject_FileExistsInProjectFolder", destName)
                    , Resources.msgBoxCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question)) {
                    replace = false;
                }
            }

            if (replace) {
                if (!fi.Directory.Exists)
                    fi.Directory.Create();
                File.Copy(srcFile, fullDestName, true);
                var attribs = File.GetAttributes(fullDestName);
                File.SetAttributes(fullDestName, attribs & (~FileAttributes.ReadOnly));
            }
            return fi.FullName;
        }

        public static void ReplaceTokenInFile(string file, string token, string replacement)
        {
            string text;
            try {
                var reader = new StreamReader(file);
                text = reader.ReadToEnd();
                reader.Close();
            } catch (System.Exception e) {
                Messages.DisplayErrorMessage(
                    SR.GetString("QtProject_CannotReplaceTokenRead", token, replacement, e.ToString()));
                return;
            }

            try {
                if (token.ToUpper() == "%PRE_DEF%" && !Char.IsLetter(replacement[0]))
                    replacement = "_" + replacement;

                text = text.Replace(token, replacement);
                var writer = new StreamWriter(file);
                writer.Write(text);
                writer.Close();
            } catch (System.Exception e) {
                Messages.DisplayErrorMessage(
                    SR.GetString("QtProject_CannotReplaceTokenWrite", token, replacement, e.ToString()));
            }
        }

        public void RepairGeneratedFilesStructure()
        {
            DeleteGeneratedFiles();
            foreach (VCFile file in (IVCCollection) vcPro.Files) {
                if (!HelperFunctions.HasHeaderFileExtension(file.Name) && !HelperFunctions.HasSourceFileExtension(file.Name))
                    continue;

                if (HelperFunctions.HasQObjectDeclaration(file)) {
                    RemoveMocStep(file);
                    AddMocStep(file);
                }
            }
        }

        public void TranslateFilterNames()
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

        public string CreateQrcFile(string className, string destName)
        {
            var fullDestName = vcPro.ProjectDirectory + "\\" + destName;

            if (!File.Exists(fullDestName)) {
                FileStream s = null;
                try {
                    s = File.Open(fullDestName, FileMode.CreateNew);
                    if (s.CanWrite) {
                        using (var sw = new StreamWriter(s)) {
                            s = null;
                            sw.WriteLine("<RCC>");
                            sw.WriteLine("    <qresource prefix=\"" + className + "\">");
                            sw.WriteLine("    </qresource>");
                            sw.WriteLine("</RCC>");
                        }
                    }
                } finally {
                    if (s != null)
                        s.Dispose();
                }
                var attribs = File.GetAttributes(fullDestName);
                File.SetAttributes(fullDestName, attribs & (~FileAttributes.ReadOnly));
            }

            var fi = new FileInfo(fullDestName);
            return fi.FullName;
        }

        public static void EnableSection(string file, string sectionName, bool enable)
        {
            string text = "";
            bool firstLine = true;
            try {
                var reader = new StreamReader(file);
                var line = reader.ReadLine();
                bool skip = false;
                while (line != null) {
                    if (line.StartsWith("#Begin_" + sectionName)) {
                        skip = !enable;
                    } else if (line.StartsWith("#End_" + sectionName)) {
                        skip = false;
                    } else if (!skip) {
                        if (firstLine) {
                            text = line;
                            firstLine = false;
                        } else {
                            text += "\r\n" + line;
                        }
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
            } catch (System.Exception e) {
                Messages.DisplayErrorMessage(SR.GetString("QtProject_CannotEnableSectionRead", sectionName, e.ToString()));
                return;
            }

            try {
                var writer = new StreamWriter(file);
                writer.Write(text);
                writer.Close();
            } catch (System.Exception e) {
                Messages.DisplayErrorMessage(SR.GetString("QtProject_CannotEnableSectionWrite", sectionName, e.ToString()));
            }
        }

        public void AddActiveQtBuildStep(string version)
        {
            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                string idlFile = "\"$(IntDir)/" + envPro.Name + ".idl\"";
                string tblFile = "\"$(IntDir)/" + envPro.Name + ".tlb\"";

                var tool = (VCPostBuildEventTool) ((IVCCollection) config.Tools).Item("VCPostBuildEventTool");
                string idc = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /idl " + idlFile + " -version " + version;
                string midl = "midl " + idlFile + " /tlb " + tblFile;
                string idc2 = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /tlb " + tblFile;
                string idc3 = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /regserver";

                tool.CommandLine = idc + "\r\n" + midl + "\r\n" + idc2 + "\r\n" + idc3;
                tool.Description = "";

                var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");
                var librarian = (VCLibrarianTool) ((IVCCollection) config.Tools).Item("VCLibrarianTool");

                if (linker != null) {
                    linker.Version = version;
                    linker.ModuleDefinitionFile = envPro.Name + ".def";
                } else {
                    librarian.ModuleDefinitionFile = envPro.Name + ".def";
                }
            }
        }

        private void UpdateCompilerIncludePaths(string oldDir, string newDir)
        {
            var fixedOldDir = FixFilePathForComparison(oldDir);
            var dirs = new string[] {
                FixFilePathForComparison(QtVSIPSettings.GetUicDirectory(envPro)),
                FixFilePathForComparison(QtVSIPSettings.GetMocDirectory(envPro)),
                FixFilePathForComparison(QtVSIPSettings.GetRccDirectory(envPro))};

            bool oldDirIsUsed = false;
            foreach (string dir in dirs) {
                if (dir == fixedOldDir) {
                    oldDirIsUsed = true;
                    break;
                }
            }

            var incList = new List<string>();
            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                var compiler = CompilerToolWrapper.Create(config);
                if (compiler == null)
                    continue;
                List<string> paths = compiler.AdditionalIncludeDirectories;
                if (paths.Count == 0)
                    continue;

                if (!oldDirIsUsed)
                    for (int i = paths.Count - 1; i >= 0; --i)
                        if (FixFilePathForComparison(paths[i]) == fixedOldDir)
                            paths.RemoveAt(i);

                incList.Clear();
                foreach (string path in paths) {
                    var tmp = HelperFunctions.NormalizeRelativeFilePath(path);
                    if (tmp.Length > 0 && !incList.Contains(tmp))
                        incList.Add(tmp);
                }
                bool alreadyThere = false;
                var fixedNewDir = FixFilePathForComparison(newDir);
                foreach (string include in incList) {
                    if (FixFilePathForComparison(include) == fixedNewDir) {
                        alreadyThere = true;
                        break;
                    }
                }
                if (!alreadyThere)
                    incList.Add(HelperFunctions.NormalizeRelativeFilePath(newDir));

                compiler.AdditionalIncludeDirectories = incList;
            }
        }

        private static string FixFilePathForComparison(string path)
        {
            path = HelperFunctions.NormalizeRelativeFilePath(path);
            return path.ToLower();
        }

        public void UpdateUicSteps(string oldUicDir, bool update_inc_path)
        {
            Messages.PaneMessage(dte, "\r\n=== Update uic steps ===");
            var vcFilter = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (vcFilter != null) {
                var filterFiles = (IVCCollection) vcFilter.Files;
                for (int i = filterFiles.Count; i > 0; i--) {
                    var file = (VCFile) filterFiles.Item(i);
                    if (file.Name.ToLower().StartsWith("ui_")) {
                        RemoveFileFromFilter(file, vcFilter);
                        HelperFunctions.DeleteEmptyParentDirs(file);
                    }
                }
            }

            int updatedFiles = 0;
            int j = 0;

            VCFile[] files = new VCFile[((IVCCollection) vcPro.Files).Count];
            foreach (VCFile file in (IVCCollection) vcPro.Files) {
                files[j++] = file;
            }

            foreach (VCFile file in files) {
                if (file.Name.EndsWith(".ui") && !IsUic3File(file)) {
                    AddUic4BuildStep(file);
                    Messages.PaneMessage(dte, "Update uic step for " + file.Name + ".");
                    ++updatedFiles;
                }
            }
            if (update_inc_path) {
                UpdateCompilerIncludePaths(oldUicDir, QtVSIPSettings.GetUicDirectory(envPro));
            }

            Messages.PaneMessage(dte, "\r\n=== " + updatedFiles.ToString()
                + " uic steps updated. ===\r\n");
        }

        private static bool IsUic3File(VCFile file)
        {
            foreach (VCFileConfiguration config in (IVCCollection) file.FileConfigurations) {
                var tool = HelperFunctions.GetCustomBuildTool(config);
                if (tool == null)
                    return false;
                if (tool.CommandLine.IndexOf("uic3.exe") > -1)
                    return true;
            }
            return false;
        }

        public bool UsePrecompiledHeaders(VCConfiguration config)
        {
            var compiler = CompilerToolWrapper.Create(config);
            return UsePrecompiledHeaders(compiler);
        }

        private bool UsePrecompiledHeaders(CompilerToolWrapper compiler)
        {
            try {
                compiler.SetUsePrecompiledHeader(pchOption.pchUseUsingSpecific);
                var pcHeaderThrough = GetPrecompiledHeaderThrough();
                if (string.IsNullOrEmpty(pcHeaderThrough))
                    pcHeaderThrough = "stdafx.h";
                compiler.SetPrecompiledHeaderThrough(pcHeaderThrough);
                var pcHeaderFile = GetPrecompiledHeaderFile();
                if (string.IsNullOrEmpty(pcHeaderFile))
                    pcHeaderFile = ".\\$(ConfigurationName)/"
                    + Project.Name + ".pch";
                compiler.SetPrecompiledHeaderFile(pcHeaderFile);
                return true;
            } catch {
                return false;
            }
        }

        public bool UsesPrecompiledHeaders()
        {
            foreach (VCConfiguration config in vcPro.Configurations as IVCCollection) {
                if (!UsesPrecompiledHeaders(config))
                    return false;
            }
            return true;
        }

        public static bool UsesPrecompiledHeaders(VCConfiguration config)
        {
            var compiler = CompilerToolWrapper.Create(config);
            return UsesPrecompiledHeaders(compiler);
        }

        private static bool UsesPrecompiledHeaders(CompilerToolWrapper compiler)
        {
            try {
                if (compiler.GetUsePrecompiledHeader() != pchOption.pchNone)
                    return true;
            } catch { }
            return false;
        }

        public string GetPrecompiledHeaderThrough()
        {
            foreach (VCConfiguration config in vcPro.Configurations as IVCCollection) {
                var header = GetPrecompiledHeaderThrough(config);
                if (header != null)
                    return header;
            }
            return null;
        }

        public static string GetPrecompiledHeaderThrough(VCConfiguration config)
        {
            var compiler = CompilerToolWrapper.Create(config);
            return GetPrecompiledHeaderThrough(compiler);
        }

        private static string GetPrecompiledHeaderThrough(CompilerToolWrapper compiler)
        {
            try {
                var header = compiler.GetPrecompiledHeaderThrough();
                if (!string.IsNullOrEmpty(header))
                    return header.ToLower();
            } catch { }
            return null;
        }

        public string GetPrecompiledHeaderFile()
        {
            foreach (VCConfiguration config in vcPro.Configurations as IVCCollection) {
                var file = GetPrecompiledHeaderFile(config);
                if (!string.IsNullOrEmpty(file))
                    return file;
            }
            return null;
        }

        public static string GetPrecompiledHeaderFile(VCConfiguration config)
        {
            var compiler = CompilerToolWrapper.Create(config);
            return GetPrecompiledHeaderFile(compiler);
        }

        private static string GetPrecompiledHeaderFile(CompilerToolWrapper compiler)
        {
            try {
                var file = compiler.GetPrecompiledHeaderFile();
                if (!string.IsNullOrEmpty(file))
                    return file;
            } catch { }
            return null;
        }

        public static void SetPCHOption(VCFile vcFile, pchOption option)
        {
            foreach (VCFileConfiguration config in vcFile.FileConfigurations as IVCCollection) {
                var compiler = CompilerToolWrapper.Create(config);
                compiler.SetUsePrecompiledHeader(option);
            }
        }

        private static VCFileConfiguration GetVCFileConfigurationByName(VCFile file, string configName)
        {
            foreach (VCFileConfiguration cfg in (IVCCollection) file.FileConfigurations) {
                if (cfg.Name == configName)
                    return cfg;
            }
            return null;
        }

        /// <summary>
        /// Searches for the generated file inside the "Generated Files" filter.
        /// The function looks for the given filename and uses the fileConfig's
        /// ConfigurationName and Platform if moc directory contains $(ConfigurationName)
        /// and/or $(PlatformName).
        /// Otherwise it just uses the "Generated Files" filter
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileConfig"></param>
        /// <returns></returns>
        private VCFile GetGeneratedMocFile(string fileName, VCFileConfiguration fileConfig)
        {
            if (QtVSIPSettings.HasDifferentMocFilePerConfig(envPro)
                || QtVSIPSettings.HasDifferentMocFilePerPlatform(envPro)) {
                var projectConfig = (VCConfiguration) fileConfig.ProjectConfiguration;
                string configName = projectConfig.ConfigurationName;
                string platformName = ((VCPlatform) projectConfig.Platform).Name;
                var generatedFiles = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
                if (generatedFiles == null)
                    return null;
                foreach (VCFilter filt in (IVCCollection) generatedFiles.Filters)
                    if (filt.Name == configName + "_" + platformName ||
                        filt.Name == configName || filt.Name == platformName)
                        foreach (VCFile filtFile in (IVCCollection) filt.Files)
                            if (filtFile.FullPath.EndsWith(fileName))
                                return filtFile;

                //If a project from the an AddIn prior to 1.1.0 was loaded, the generated files are located directly
                //in the generated files filter.
                string relativeMocPath = QtVSIPSettings.GetMocDirectory(envPro, configName, platformName) + '\\' + fileName;
                //Remove .\ at the beginning of the mocPath
                if (relativeMocPath.StartsWith(".\\"))
                    relativeMocPath = relativeMocPath.Remove(0, 2);
                foreach (VCFile filtFile in (IVCCollection) generatedFiles.Files)
                    if (filtFile.FullPath.EndsWith(relativeMocPath, StringComparison.OrdinalIgnoreCase))
                        return filtFile;
            } else {
                var generatedFiles = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
                foreach (VCFile filtFile in (IVCCollection) generatedFiles.Files)
                    if (filtFile.FullPath.EndsWith('\\' + fileName))
                        return filtFile;
            }
            return null;
        }

        public void RefreshMocSteps()
        {
            foreach (VCFile vcfile in (IVCCollection) vcPro.Files) {
                RefreshMocStep(vcfile, false);
            }
        }

        public void RefreshMocStep(VCFile vcfile)
        {
            RefreshMocStep(vcfile, true);
        }

        /// <summary>
        /// Updates the moc command line for the given header or source file
        /// containing the Q_OBJECT macro.
        /// If the function is called from a property change for a single file
        /// (singleFile =  true) we may have to look for the according header
        /// file and refresh the moc step for this file, if it contains Q_OBJECT.
        /// </summary>
        /// <param name="vcfile"></param>
        private void RefreshMocStep(VCFile vcfile, bool singleFile)
        {
            var isHeaderFile = HelperFunctions.HasHeaderFileExtension(vcfile.FullPath);
            if (!isHeaderFile && !HelperFunctions.HasSourceFileExtension(vcfile.FullPath))
                return;

            if (mocCmdChecker == null)
                mocCmdChecker = new MocCmdChecker();

            foreach (VCFileConfiguration config in (IVCCollection) vcfile.FileConfigurations) {
                try {
                    VCCustomBuildTool tool = null;
                    VCFile mocable = null;
                    if (isHeaderFile) {
                        mocable = vcfile;
                        tool = HelperFunctions.GetCustomBuildTool(config);
                    } else {
                        var mocFileName = GetMocFileName(vcfile.FullPath);
                        var mocFile = GetGeneratedMocFile(mocFileName, config);
                        if (mocFile != null) {
                            var mocFileConfig = GetVCFileConfigurationByName(mocFile, config.Name);
                            tool = HelperFunctions.GetCustomBuildTool(mocFileConfig);
                            mocable = mocFile;
                        }
                        // It is possible that the function was called from a source file's property change, it is possible that
                        // we have to obtain the tool from the according header file
                        if (tool == null && singleFile) {
                            string headerName = vcfile.FullPath.Remove(vcfile.FullPath.LastIndexOf('.')) + ".h";
                            mocFileName = GetMocFileName(headerName);
                            mocFile = GetGeneratedMocFile(mocFileName, config);
                            if (mocFile != null) {
                                mocable = GetFileFromProject(headerName);
                                var customBuildConfig = GetVCFileConfigurationByName(mocable, config.Name);
                                tool = HelperFunctions.GetCustomBuildTool(customBuildConfig);
                            }
                        }
                    }
                    if (tool == null || tool.CommandLine.ToLower().IndexOf("moc.exe") == -1)
                        continue;

                    var srcMocFile = GetSourceFileForMocStep(mocable);
                    var cppFile = GetCppFileForMocStep(mocable);
                    if (srcMocFile == null)
                        continue;
                    bool mocableIsCPP = (srcMocFile == cppFile);

                    string pchParameters = null;
                    VCFileConfiguration defineIncludeConfig = null;
                    CompilerToolWrapper compiler = null;
                    if (cppFile == null) {
                        // No file specific defines/includes but at least the project defines/includes are added
                        defineIncludeConfig = config;
                        compiler = CompilerToolWrapper.Create(config.ProjectConfiguration as VCConfiguration);
                    } else {
                        defineIncludeConfig = GetVCFileConfigurationByName(cppFile, config.Name);
                        compiler = CompilerToolWrapper.Create(defineIncludeConfig);
                    }

                    if (compiler != null && compiler.GetUsePrecompiledHeader() != pchOption.pchNone)
                        pchParameters = GetPCHMocOptions(srcMocFile, compiler);

                    string outputFileName = QtVSIPSettings.GetMocDirectory(envPro) + "\\";
                    if (mocableIsCPP) {
                        outputFileName += ProjectMacros.Name;
                        outputFileName += ".moc";
                    } else {
                        outputFileName += "moc_";
                        outputFileName += ProjectMacros.Name;
                        outputFileName += ".cpp";
                    }

                    var newCmdLine = mocCmdChecker.NewCmdLine(tool.CommandLine,
                        GetIncludes(defineIncludeConfig),
                        GetDefines(defineIncludeConfig),
                        QtVSIPSettings.GetMocOptions(envPro), srcMocFile.RelativePath,
                        pchParameters,
                        outputFileName);

                    // The tool's command line automatically gets a trailing "\r\n".
                    // We have to remove it to make the check below work.
                    string origCommandLine = tool.CommandLine;
                    if (origCommandLine.EndsWith("\r\n"))
                        origCommandLine = origCommandLine.Substring(0, origCommandLine.Length - 2);

                    if (newCmdLine != null && newCmdLine != origCommandLine) {
                        // We have to delete the old moc file in order to trigger custom build step.
                        var configName = config.Name.Remove(config.Name.IndexOf("|"));
                        var platformName = config.Name.Substring(config.Name.IndexOf("|") + 1);
                        var projectPath = envPro.FullName.Remove(envPro.FullName.LastIndexOf('\\'));
                        var mocRelPath = GetRelativeMocFilePath(srcMocFile.FullPath, configName, platformName);
                        var mocPath = Path.Combine(projectPath, mocRelPath);
                        if (File.Exists(mocPath))
                            File.Delete(mocPath);
                        tool.CommandLine = newCmdLine;
                    }
                } catch {
                    Messages.PaneMessage(dte, "ERROR: failed to refresh moc step for " + vcfile.ItemName);
                }
            }
        }

        public void OnExcludedFromBuildChanged(VCFile vcFile, VCFileConfiguration vcFileCfg)
        {
            // Update the ExcludedFromBuild flags of the mocced file
            // according to the ExcludedFromBuild flag of the mocable source file.
            var moccedFileName = GetMocFileName(vcFile.Name);
            if (string.IsNullOrEmpty(moccedFileName))
                return;

            var moccedFile = GetGeneratedMocFile(moccedFileName, vcFileCfg);

            if (moccedFile != null) {
                VCFile cppFile = null;
                if (HelperFunctions.HasHeaderFileExtension(vcFile.Name))
                    cppFile = GetCppFileForMocStep(vcFile);

                var moccedFileConfig = GetVCFileConfigurationByName(moccedFile, vcFileCfg.Name);
                if (moccedFileConfig != null) {
                    if (cppFile != null && IsMoccedFileIncluded(cppFile)) {
                        if (!moccedFileConfig.ExcludedFromBuild) {
                            moccedFileConfig.ExcludedFromBuild = true;
                        }
                    } else if (moccedFileConfig.ExcludedFromBuild != vcFileCfg.ExcludedFromBuild)
                        moccedFileConfig.ExcludedFromBuild = vcFileCfg.ExcludedFromBuild;
                }
            }
        }

        /// <summary>
        /// Helper function for RefreshMocStep.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private VCFile GetSourceFileForMocStep(VCFile file)
        {
            if (HelperFunctions.HasHeaderFileExtension(file.Name))
                return file;
            string fileName = file.Name;
            if (fileName.ToLower().EndsWith(".moc")) {
                fileName = fileName.Substring(0, fileName.Length - 4) + ".cpp";
                if (fileName.Length > 0) {
                    foreach (VCFile f in (IVCCollection) vcPro.Files) {
                        if (f.FullPath.ToLower().EndsWith("\\" + fileName.ToLower()))
                            return f;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Helper function for Refresh/UpdateMocStep.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private VCFile GetCppFileForMocStep(VCFile file)
        {
            string fileName = null;
            if (HelperFunctions.HasHeaderFileExtension(file.Name) || file.Name.EndsWith(".moc"))
                fileName = file.Name.Remove(file.Name.LastIndexOf('.')) + ".cpp";
            if (fileName != null && fileName.Length > 0) {
                foreach (VCFile f in (IVCCollection) vcPro.Files) {
                    if (f.FullPath.ToLower().EndsWith("\\" + fileName.ToLower()))
                        return f;
                }
            }
            return null;
        }

        public void UpdateMocSteps(string oldMocDir)
        {
            Messages.PaneMessage(dte, "\r\n=== Update moc steps ===");
            var orgFiles = new List<VCFile>();
            var abandonedMocFiles = new List<string>();
            var vcFilter = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (vcFilter != null) {
                var generatedFiles = GetAllFilesFromFilter(vcFilter);
                for (int i = generatedFiles.Count - 1; i >= 0; i--) {
                    VCFile file = generatedFiles[i];
                    string fileName = null;
                    if (file.Name.ToLower().StartsWith("moc_")) {
                        fileName = file.Name.Substring(4, file.Name.Length - 8) + ".h";
                    } else if (file.Name.ToLower().EndsWith(".moc")) {
                        fileName = file.Name.Substring(0, file.Name.Length - 4) + ".cpp";
                    }
                    if (fileName != null) {
                        bool found = false;
                        foreach (VCFile f in (IVCCollection) vcPro.Files) {
                            if (f.FullPath.ToLower().EndsWith("\\" + fileName.ToLower())) {
                                if (!orgFiles.Contains(f) && HasMocStep(f, oldMocDir))
                                    orgFiles.Add(f);
                                RemoveFileFromFilter(file, vcFilter);
                                HelperFunctions.DeleteEmptyParentDirs(file);
                                found = true;
                                break;
                            }
                        }
                        if (!found) {
                            // We can't find foo.h for moc_foo.cpp or
                            // we can't find foo.cpp for foo.moc, thus we put the
                            // filename moc_foo.cpp / foo.moc into an error list.
                            abandonedMocFiles.Add(file.Name);
                        }
                    }
                }
            }

            UpdateCompilerIncludePaths(oldMocDir, QtVSIPSettings.GetMocDirectory(envPro));
            foreach (VCFile file in orgFiles) {
                try {
                    RemoveMocStep(file);
                    AddMocStep(file);
                } catch (QtVSException e) {
                    Messages.PaneMessage(dte, e.Message);
                    continue;
                }
                Messages.PaneMessage(dte, "Moc step updated successfully for " + file.Name + ".");
            }

            foreach (string s in abandonedMocFiles)
                Messages.PaneMessage(dte, "Moc step update failed for " + s +
                    ". Reason: Could not determine source file for moccing.");

            Messages.PaneMessage(dte, "\r\n=== Moc steps updated. Successful: " + orgFiles.Count.ToString()
                + "   Failed: " + abandonedMocFiles.Count.ToString() + " ===\r\n");

            CleanupFilter(vcFilter);
        }

        private void Clean()
        {
            SolutionConfigurations solutionConfigs = envPro.DTE.Solution.SolutionBuild.SolutionConfigurations;
            var backup = new List<KeyValuePair<SolutionContext, bool>>();
            foreach (SolutionConfiguration config in solutionConfigs) {
                SolutionContexts solutionContexts = config.SolutionContexts;
                if (solutionContexts == null)
                    continue;

                foreach (SolutionContext context in solutionContexts) {
                    backup.Add(new KeyValuePair<SolutionContext, bool>(context, context.ShouldBuild));
                    if (envPro.FullName.Contains(context.ProjectName)
                        && context.PlatformName == envPro.ConfigurationManager.ActiveConfiguration.PlatformName)
                        context.ShouldBuild = true;
                    else
                        context.ShouldBuild = false;
                }
            }
            envPro.DTE.Solution.SolutionBuild.Clean(true);
            foreach (KeyValuePair<SolutionContext, bool> item in backup)
                item.Key.ShouldBuild = item.Value;
        }

        private void CleanupFilter(VCFilter filter)
        {
            var subFilters = filter.Filters as IVCCollection;
            if (subFilters == null)
                return;

            for (int i = subFilters.Count; i > 0; i--) {
                var subFilter = subFilters.Item(i) as VCFilter;
                var subFilterFilters = subFilter.Filters as IVCCollection;
                if (subFilterFilters == null)
                    continue;

                CleanupFilter(subFilter);

                bool filterOrFileFound = false;
                foreach (object itemObject in subFilter.Items as IVCCollection) {
                    if (itemObject is VCFilter || itemObject is VCFile) {
                        filterOrFileFound = true;
                        break;
                    }
                }
                if (!filterOrFileFound) {
                    filter.RemoveFilter(subFilter);
                }
            }
        }

        /// <summary>
        /// Changes the Qt version of this project.
        /// </summary>
        /// <param name="oldVersion">the current Qt version</param>
        /// <param name="newVersion">the new Qt version we want to change to</param>
        /// <param name="newProjectCreated">is set to true if a new Project object has been created</param>
        /// <returns>true, if the operation performed successfully</returns>
        public bool ChangeQtVersion(string oldVersion, string newVersion, ref bool newProjectCreated)
        {
            newProjectCreated = false;
            var versionManager = QtVersionManager.The();
            var viOld = versionManager.GetVersionInfo(oldVersion);
            var viNew = versionManager.GetVersionInfo(newVersion);

            string vsPlatformNameOld = null;
            if (viOld != null)
                vsPlatformNameOld = viOld.GetVSPlatformName();
            var vsPlatformNameNew = viNew.GetVSPlatformName();
            bool bRefreshMocSteps = (vsPlatformNameNew != vsPlatformNameOld);

            try {
                if (vsPlatformNameOld != vsPlatformNameNew) {
                    if (!SelectSolutionPlatform(vsPlatformNameNew) || !HasPlatform(vsPlatformNameNew)) {
                        CreatePlatform(vsPlatformNameOld, vsPlatformNameNew, viOld, viNew, ref newProjectCreated);
                        bRefreshMocSteps = false;
                        UpdateMocSteps(QtVSIPSettings.GetMocDirectory(envPro));
                    }
                }
                ConfigurationManager configManager = envPro.ConfigurationManager;
                if (configManager.ActiveConfiguration.PlatformName != vsPlatformNameNew) {
                    string projectName = envPro.FullName;
                    envPro.Save(null);
                    dte.Solution.Remove(envPro);
                    envPro = dte.Solution.AddFromFile(projectName, false);
                    dte = envPro.DTE;
                    vcPro = envPro.Object as VCProject;
                }
            } catch {
                Messages.DisplayErrorMessage(SR.GetString("CannotChangeQtVersion"));
                return false;
            }

            // We have to delete the generated files because of
            // major differences between the platforms or Qt-Versions.
            if (vsPlatformNameOld != vsPlatformNameNew || viOld.qtPatch != viNew.qtPatch
                || viOld.qtMinor != viNew.qtMinor || viOld.qtMajor != viNew.qtMajor) {
                DeleteGeneratedFiles();
                Clean();
            }

            if (bRefreshMocSteps)
                RefreshMocSteps();

            SetQtEnvironment(newVersion);
            UpdateModules(viOld, viNew);
            versionManager.SaveProjectQtVersion(envPro, newVersion, vsPlatformNameNew);
            return true;
        }

        public bool HasPlatform(string platformName)
        {
            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                var platform = (VCPlatform) config.Platform;
                if (platform.Name == platformName) {
                    return true;
                }
            }
            return false;
        }

        public bool SelectSolutionPlatform(string platformName)
        {
            foreach (SolutionConfiguration solutionCfg in dte.Solution.SolutionBuild.SolutionConfigurations) {
                SolutionContexts contexts = solutionCfg.SolutionContexts;
                for (int i = 1; i <= contexts.Count; ++i) {
                    SolutionContext ctx = null;
                    try {
                        ctx = contexts.Item(i);
                    } catch (System.ArgumentException) {
                        // This may happen if we encounter an unloaded project.
                        continue;
                    }

                    if (ctx.PlatformName == platformName
                        && solutionCfg.Name == dte.Solution.SolutionBuild.ActiveConfiguration.Name) {
                        solutionCfg.Activate();
                        return true;
                    }
                }
            }

            return false;
        }

        public void RemovePlatform(string platformName)
        {
            try {
                ConfigurationManager cfgMgr = envPro.ConfigurationManager;
                cfgMgr.DeletePlatform(platformName);
            } catch { }
        }

        public void CreatePlatform(string oldPlatform, string newPlatform,
                                   VersionInformation viOld, VersionInformation viNew, ref bool newProjectCreated)
        {
            try {
                ConfigurationManager cfgMgr = envPro.ConfigurationManager;
                cfgMgr.AddPlatform(newPlatform, oldPlatform, true);
                vcPro.AddPlatform(newPlatform);
                newProjectCreated = false;
            } catch {
                // That stupid ConfigurationManager can't handle platform names
                // containing dots (e.g. "Windows Mobile 5.0 Pocket PC SDK (ARMV4I)")
                // So we have to do it the nasty way...
                string projectFileName = envPro.FullName;
                envPro.Save(null);
                dte.Solution.Remove(envPro);
                AddPlatformToVCProj(projectFileName, oldPlatform, newPlatform);
                envPro = dte.Solution.AddFromFile(projectFileName, false);
                vcPro = (VCProject) envPro.Object;
                newProjectCreated = true;
            }

            // update the platform settings
            foreach (VCConfiguration config in (IVCCollection) vcPro.Configurations) {
                var vcplatform = (VCPlatform) config.Platform;
                if (vcplatform.Name == newPlatform) {
                    if (viOld != null)
                        RemovePlatformDependencies(config, viOld);
                    SetupConfiguration(config, viNew);
                }
            }

            SelectSolutionPlatform(newPlatform);
        }

        public static void RemovePlatformDependencies(VCConfiguration config, VersionInformation viOld)
        {
            var compiler = CompilerToolWrapper.Create(config);
            var minuend = new HashSet<string>(compiler.PreprocessorDefinitions);
            minuend.ExceptWith(viOld.GetQMakeConfEntry("DEFINES").Split(new char[] { ' ', '\t' }));
            compiler.SetPreprocessorDefinitions(string.Join(",", minuend));
        }

        public void SetupConfiguration(VCConfiguration config, VersionInformation viNew)
        {
            var compiler = CompilerToolWrapper.Create(config);
            var ppdefs = new HashSet<string>(compiler.PreprocessorDefinitions);
            ppdefs.UnionWith(viNew.GetQMakeConfEntry("DEFINES").Split(new char[] { ' ', '\t' }));
            compiler.SetPreprocessorDefinitions(string.Join(",", ppdefs));

            var linker = (VCLinkerTool) ((IVCCollection) config.Tools).Item("VCLinkerTool");
            if (linker == null)
                return;

            linker.SubSystem = subSystemOption.subSystemWindows;
            SetTargetMachine(linker, viNew);
        }

        private void DeleteGeneratedFiles()
        {
            var genFilter = Filters.GeneratedFiles();
            var genVCFilter = FindFilterFromGuid(genFilter.UniqueIdentifier);
            if (genVCFilter == null)
                return;

            bool error = false;
            error = DeleteFilesFromFilter(genVCFilter);
            if (error)
                Messages.PaneMessage(dte, SR.GetString("DeleteGeneratedFilesError"));
        }

        private bool DeleteFilesFromFilter(VCFilter filter)
        {
            bool error = false;
            foreach (VCFile f in filter.Files as IVCCollection) {
                try {
                    var fi = new FileInfo(f.FullPath);
                    if (fi.Exists)
                        fi.Delete();
                    HelperFunctions.DeleteEmptyParentDirs(fi.Directory.ToString());
                } catch {
                    error = true;
                }
            }
            foreach (VCFilter filt in filter.Filters as IVCCollection)
                if (DeleteFilesFromFilter(filt))
                    error = true;
            return error;
        }

        public void RemoveGeneratedFiles(string fileName)
        {
            var fi = new FileInfo(fileName);
            var lastIndex = fileName.LastIndexOf(fi.Extension);
            var baseName = fi.Name.Remove(lastIndex, fi.Extension.Length);
            string delName = null;
            if (HelperFunctions.HasHeaderFileExtension(fileName))
                delName = "moc_" + baseName + ".cpp";
            else if (HelperFunctions.HasSourceFileExtension(fileName) && !fileName.ToLower().StartsWith("moc_"))
                delName = baseName + ".moc";
            else if (fileName.ToLower().EndsWith(".ui"))
                delName = "ui_" + baseName + ".h";
            else if (fileName.ToLower().EndsWith(".qrc"))
                delName = "qrc_" + baseName + ".cpp";

            if (delName != null) {
                foreach (VCFile delFile in GetFilesFromProject(delName))
                    RemoveFileFromFilter(delFile, Filters.GeneratedFiles());
            }
        }

        public void RemoveResFilesFromGeneratedFilesFilter()
        {
            var filesToRemove = new List<VCFile>();
            var generatedFiles = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (generatedFiles == null)
                return;

            foreach (VCFile filtFile in (IVCCollection) generatedFiles.Files)
                if (filtFile.FullPath.ToLower().EndsWith(".res"))
                    filesToRemove.Add(filtFile);
            foreach (VCFile resFile in filesToRemove)
                resFile.Remove();
        }

        static private void AddPlatformToVCProj(string projectFileName, string oldPlatformName, string newPlatformName)
        {
            var tempFileName = Path.GetTempFileName();
            var fi = new FileInfo(projectFileName);
            fi.CopyTo(tempFileName, true);

            var myXmlDocument = new XmlDocument();
            myXmlDocument.Load(tempFileName);
            AddPlatformToVCProj(myXmlDocument, oldPlatformName, newPlatformName);
            myXmlDocument.Save(projectFileName);

            fi = new FileInfo(tempFileName);
            fi.Delete();
        }

        static private void AddPlatformToVCProj(XmlDocument doc, string oldPlatformName, string newPlatformName)
        {
            var vsProj = doc.DocumentElement.SelectSingleNode("/VisualStudioProject");
            var platforms = vsProj.SelectSingleNode("Platforms");
            if (platforms == null) {
                platforms = doc.CreateElement("Platforms");
                vsProj.AppendChild(platforms);
            }
            var platform = platforms.SelectSingleNode("Platform[@Name='" + newPlatformName + "']");
            if (platform == null) {
                platform = doc.CreateElement("Platform");
                ((XmlElement) platform).SetAttribute("Name", newPlatformName);
                platforms.AppendChild(platform);
            }

            var configurations = vsProj.SelectSingleNode("Configurations");
            var cfgList = configurations.SelectNodes("Configuration[@Name='Debug|" + oldPlatformName + "'] | " +
                                                             "Configuration[@Name='Release|" + oldPlatformName + "']");
            foreach (XmlNode oldCfg in cfgList) {
                var newCfg = (XmlElement) oldCfg.Clone();
                newCfg.SetAttribute("Name", oldCfg.Attributes["Name"].Value.Replace(oldPlatformName, newPlatformName));
                configurations.AppendChild(newCfg);
            }

            const string fileCfgPath = "Files/Filter/File/FileConfiguration";
            var fileCfgList = vsProj.SelectNodes(fileCfgPath + "[@Name='Debug|" + oldPlatformName + "'] | " +
                                                         fileCfgPath + "[@Name='Release|" + oldPlatformName + "']");
            foreach (XmlNode oldCfg in fileCfgList) {
                var newCfg = (XmlElement) oldCfg.Clone();
                newCfg.SetAttribute("Name", oldCfg.Attributes["Name"].Value.Replace(oldPlatformName, newPlatformName));
                oldCfg.ParentNode.AppendChild(newCfg);
            }
        }

        static private void SetTargetMachine(VCLinkerTool linker, VersionInformation versionInfo)
        {
            var qMakeLFlagsWindows = versionInfo.GetQMakeConfEntry("QMAKE_LFLAGS_WINDOWS");
            var rex = new Regex("/MACHINE:(\\S+)");
            var match = rex.Match(qMakeLFlagsWindows);
            if (match.Success) {
                linker.TargetMachine = HelperFunctions.TranslateMachineType(match.Groups[1].Value);
            } else {
                var platformName = versionInfo.GetVSPlatformName();
                if (platformName == "Win32")
                    linker.TargetMachine = machineTypeOption.machineX86;
                else if (platformName == "x64")
                    linker.TargetMachine = machineTypeOption.machineAMD64;
                else
                    linker.TargetMachine = machineTypeOption.machineNotSet;
            }

            string subsystemOption = "";
            string linkerOptions = linker.AdditionalOptions;
            if (linkerOptions == null)
                linkerOptions = "";

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

        public VCConfiguration GetActiveVCConfiguration()
        {
            string activeConfigName = Project.ConfigurationManager.ActiveConfiguration.ConfigurationName;
            return (VCConfiguration) ((IVCCollection) VCProject.Configurations).Item(activeConfigName);
        }

        public void CollapseFilter(string filterName)
        {
            var solutionExplorer = (UIHierarchy) dte.Windows.Item(Constants.vsext_wk_SProjectWindow).Object;
            if (solutionExplorer.UIHierarchyItems.Count == 0)
                return;

            dte.SuppressUI = true;
            var projectItem = FindProjectHierarchyItem(solutionExplorer);
            if (projectItem != null) {
                HelperFunctions.CollapseFilter(projectItem, solutionExplorer, filterName);
            }
            dte.SuppressUI = false;
        }

        private UIHierarchyItem FindProjectHierarchyItem(UIHierarchy hierarchy)
        {
            if (hierarchy.UIHierarchyItems.Count == 0)
                return null;

            var solution = hierarchy.UIHierarchyItems.Item(1);
            UIHierarchyItem projectItem = null;
            foreach (UIHierarchyItem solutionItem in solution.UIHierarchyItems) {
                projectItem = FindProjectHierarchyItem(solutionItem);
                if (projectItem != null)
                    break;
            }
            return projectItem;
        }

        private UIHierarchyItem FindProjectHierarchyItem(UIHierarchyItem root)
        {
            UIHierarchyItem projectItem = null;
            try {
                if (root.Name == envPro.Name)
                    return root;

                foreach (UIHierarchyItem childItem in root.UIHierarchyItems) {
                    projectItem = FindProjectHierarchyItem(childItem);
                    if (projectItem != null)
                        break;
                }
            } catch {
            }
            return projectItem;
        }

        /// <summary>
        /// Gets the Qt version of the project
        /// </summary>
        public string GetQtVersion()
        {
            return QtVersionManager.The().GetProjectQtVersion(envPro);
        }

        /// <summary>
        /// Sets the Qt environment for the project's Qt version.
        /// </summary>
        public void SetQtEnvironment()
        {
            SetQtEnvironment(QtVersionManager.The().GetProjectQtVersion(envPro));
        }

        /// <summary>
        /// Sets the Qt environment for the given Qt version.
        /// </summary>
        public void SetQtEnvironment(string qtVersion)
        {
            SetQtEnvironment(qtVersion, "");
        }

        /// <summary>
        /// Sets the Qt environment for the given Qt version.
        /// </summary>
        public void SetQtEnvironment(string qtVersion, string solutionConfig)
        {
            if (string.IsNullOrEmpty(qtVersion))
                return;

            string qtDir = null;
            if (qtVersion != "$(QTDIR)")
                qtDir = QtVersionManager.The().GetInstallPath(qtVersion);
            HelperFunctions.SetEnvironmentVariableEx("QTDIR", qtDir);
            try {
                var propertyAccess = (IVCBuildPropertyStorage) vcPro;
                var vcprj = envPro.Object as VCProject;

                // Get platform name from given solution configuration
                // or if not available take the active configuration
                string activePlatformName = "";
                if (solutionConfig == null || solutionConfig.Length == 0) {
                    // First get active configuration cause not given as parameter
                    EnvDTE.Configuration activeConf = envPro.ConfigurationManager.ActiveConfiguration;
                    solutionConfig = activeConf.ConfigurationName + "|" + activeConf.PlatformName;
                    activePlatformName = activeConf.PlatformName;
                } else {
                    activePlatformName = solutionConfig.Split('|')[1];
                }

                // Find all configurations for platform and set property for all of them
                // This is to get QTDIR property set for all configurations same time so
                // we can be sure it is set and is equal between debug and release
                foreach (VCConfiguration conf in vcprj.Configurations as IVCCollection) {
                    var cur_platform = conf.Platform as VCPlatform;
                    if (cur_platform.Name == activePlatformName) {
                        string cur_solution = conf.ConfigurationName + "|" + cur_platform.Name;
                        propertyAccess.SetPropertyValue("QTDIR", cur_solution, "UserFile", qtDir);
                    }
                }

            } catch (Exception) {
                Messages.PaneMessage(envPro.DTE, SR.GetString("QtProject_CannotAccessUserFile", vcPro.ItemName));
            }

            HelperFunctions.SetDebuggingEnvironment(envPro);
        }
    }
}
