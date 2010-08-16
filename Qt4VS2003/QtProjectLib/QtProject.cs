/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
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
    using EnvDTE;
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.VisualStudio.VCProjectEngine;

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
        private static Dictionary<Project, QtProject> instances = new Dictionary<Project,QtProject>();

        public static QtProject Create(VCProject vcProject)
        {
            return Create((EnvDTE.Project)vcProject.Object);
        }

        public static QtProject Create(EnvDTE.Project project)
        {
            QtProject qtProject = null;
            if (!instances.TryGetValue(project, out qtProject))
            {
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
                throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotConstructWithoutValidProject"));
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
                if (lastConfigurationRowNames == null)
                {
                    lastConfigurationRowNames = envPro.ConfigurationManager.ConfigurationRowNames as Array;
                }
                else
                {
                    Array currentConfigurationRowNames = envPro.ConfigurationManager.ConfigurationRowNames as Array;
                    if (!HelperFunctions.ArraysEqual(lastConfigurationRowNames, currentConfigurationRowNames))
                    {
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
            FileInfo fi = new FileInfo(uiFile);
            string file = fi.Name;
            if (fi.Extension == ".ui") 
            {
                return QtVSIPSettings.GetUicDirectory(envPro)
                    + "\\ui_" + file.Remove(file.Length-3, 3) + ".h";
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
            FileInfo fi = new FileInfo(file);

            string name = fi.Name;
            if (HelperFunctions.HasHeaderFileExtension(fi.Name)) 
                return "moc_" + name.Substring(0, name.LastIndexOf('.')) + ".cpp";
            else if (HelperFunctions.HasSourceFileExtension(fi.Name))
                return name.Substring(0, name.LastIndexOf('.')) + ".moc";
            else
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
            string fileName = GetMocFileName(file);
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
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);

                if (((!IsDebugConfiguration(config)) && ((bldConf & BuildConfig.Release) != 0)) ||
                    ((IsDebugConfiguration(config)) && ((bldConf & BuildConfig.Debug) != 0)))
                {
                    compiler.AddPreprocessorDefinition(define);
                }				
            }
        }

        public void AddModule(QtModule module)
        {
            if (HasModule(module))
                return;

            QtVersionManager vm = QtVersionManager.The();
            VersionInformation versionInfo = vm.GetVersionInfo(this.Project);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                QtModuleInfo info = QtModules.Instance.ModuleInformation(module);
                if (compiler != null)
                {
                    foreach(string define in info.Defines)
                        compiler.AddPreprocessorDefinition(define);

                    if (info.IncludePath.Length > 0)
                        compiler.AddAdditionalIncludeDirectories(info.IncludePath);
                }
                if (linker != null)
                {
                    List<string> moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    LinkerToolWrapper linkerWrapper = new LinkerToolWrapper(linker);
                    List<string> additionalDeps = linkerWrapper.AdditionalDependencies;
                    bool dependenciesChanged = false;
                    if (additionalDeps == null || additionalDeps.Count == 0)
                    {
                        additionalDeps = moduleLibs;
                        dependenciesChanged = true;
                    }
                    else
                    {
                        foreach (string moduleLib in moduleLibs)
                            if (!additionalDeps.Contains(moduleLib))
                            {
                                additionalDeps.Add(moduleLib);
                                dependenciesChanged = true;
                            }
                    }
                    if (dependenciesChanged)
                        linkerWrapper.AdditionalDependencies = additionalDeps;
                }

#if ENABLE_WINCE
                if (info.HasDLL && config.DeploymentTool != null)
                    AddDeploySettings(null, module, config, info, versionInfo);
#endif
            }
        }

        public void RemoveModule(QtModule module)
        {
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                QtModuleInfo info = QtModules.Instance.ModuleInformation(module);
                if (compiler != null)
                {
                    foreach (string define in info.Defines)
                        compiler.RemovePreprocessorDefinition(define);
                    List<string> additionalIncludeDirs = compiler.AdditionalIncludeDirectories;
                    if (additionalIncludeDirs != null)
                    {
                        List<string> lst = new List<string>(additionalIncludeDirs);
                        lst.Remove(info.IncludePath);
                        lst.Remove('\"' + info.IncludePath + '\"');
                        compiler.AdditionalIncludeDirectories = lst;
                    }
                }
                if (linker != null && linker.AdditionalDependencies != null)
                {
                    LinkerToolWrapper linkerWrapper = new LinkerToolWrapper(linker);
                    QtVersionManager vm = QtVersionManager.The();
                    VersionInformation versionInfo = vm.GetVersionInfo(this.Project);
                    if (versionInfo == null)
                        versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

                    List<string> moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    List<string> additionalDependencies = linkerWrapper.AdditionalDependencies;
                    bool dependenciesChanged = false;
                    foreach (string moduleLib in moduleLibs)
                        if (additionalDependencies.Remove(moduleLib))
                            dependenciesChanged = true;
                    if (dependenciesChanged)
                        linkerWrapper.AdditionalDependencies = additionalDependencies;
                }

#if ENABLE_WINCE
                if (info.HasDLL && config.DeploymentTool != null)
                    RemoveDeploySettings(null, module, config, info);
#endif
            }
        }

        public void UpdateModules(VersionInformation oldVersion, VersionInformation newVersion)
        {
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                if (linker != null)
                {
                    if (oldVersion == null || oldVersion.IsWinCEVersion() != newVersion.IsWinCEVersion())
                    {
                        LinkerToolWrapper linkerWrapper = new LinkerToolWrapper(linker);
                        List<string> additionalDependencies = linkerWrapper.AdditionalDependencies;

                        List<string> libsDesktop = new List<string>();
                        List<string> libsWinCE = new List<string>();
                        foreach (QtModuleInfo module in QtModules.Instance.GetAvailableModuleInformation())
                        {
                            if (HasModule(module.ModuleId))
                            {
                                libsDesktop.AddRange(module.AdditionalLibrariesWinCE);
                                libsWinCE.AddRange(module.AdditionalLibraries);
                            }
                        }
                        List<string> libsToAdd = null;
                        List<string> libsToRemove = null;
                        if (newVersion.IsWinCEVersion())
                        {
                            libsToAdd = libsWinCE;
                            libsToRemove = libsDesktop;
                        }
                        else
                        {
                            libsToAdd = libsDesktop;
                            libsToRemove = libsWinCE;
                        }

                        bool changed = false;
                        foreach (string libToRemove in libsToRemove)
                        {
                            if (additionalDependencies.Remove(libToRemove))
                                changed = true;
                        }
                        foreach (string libToAdd in libsToAdd)
                        {
                            if (!additionalDependencies.Contains(libToAdd))
                            {
                                additionalDependencies.Add(libToAdd);
                                changed = true;
                            }
                        }
                        if (changed)
                            linkerWrapper.AdditionalDependencies = additionalDependencies;
                    }

#if ENABLE_WINCE
                    if (newVersion.IsWinCEVersion() && newVersion.IsStaticBuild() &&
                        oldVersion != null &&
                        !(oldVersion.IsWinCEVersion() && oldVersion.IsStaticBuild()) &&
                        config.DeploymentTool != null)
                    {
                        RemoveQtDeploys(config);
                    }
#endif

                    if (oldVersion == null || newVersion.IsStaticBuild() != oldVersion.IsStaticBuild())
                    {
                        CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                        if (newVersion.IsStaticBuild())
                        {
                            linker.AdditionalDependencies =
                                Regex.Replace(linker.AdditionalDependencies, "Qt(\\S+)4\\.lib", "Qt${1}.lib");
                            linker.AdditionalDependencies =
                                Regex.Replace(linker.AdditionalDependencies, "(phonond?)4\\.lib", "${1}.lib");
                            if (compiler != null)
                                compiler.RemovePreprocessorDefinition("QT_DLL");
                        }
                        else
                        {
                            linker.AdditionalDependencies =
                                Regex.Replace(linker.AdditionalDependencies, "Qt(\\S+[^4])\\.lib", "Qt${1}4.lib");
                            linker.AdditionalDependencies =
                                Regex.Replace(linker.AdditionalDependencies, "(phonond?)\\.lib", "${1}4.lib");
                            if (compiler != null)
                                compiler.AddPreprocessorDefinition("QT_DLL");
                        }
                    }

#if ENABLE_WINCE
                    if (newVersion.IsWinCEVersion() && !newVersion.IsStaticBuild() &&
                        (oldVersion == null ||
                        !(oldVersion.IsWinCEVersion() && !oldVersion.IsStaticBuild())) &&
                        config.DeploymentTool != null)
                    {
                        MatchCollection matches = Regex.Matches(linker.AdditionalDependencies, "Qt(\\S+)4\\.lib");
                        foreach (Match m in matches)
                        {
                            string moduleName = m.ToString().Substring(0, m.ToString().Length - 5);
                            if (config.ConfigurationName.StartsWith("Debug"))
                                moduleName = moduleName.Substring(0, moduleName.Length - 1);
                            QtModule module = QtModules.Instance.ModuleIdByName(moduleName);
                            AddDeploySettings(null, module, config, null, newVersion);
                        }
                    }
#endif
                }
            }
        }

        public bool HasModule(QtModule module)
        {
            bool foundInIncludes = false;
            bool foundInLibs = false;

            QtVersionManager vm = QtVersionManager.The();
            VersionInformation versionInfo = vm.GetVersionInfo(this.Project);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                QtModuleInfo info = QtModules.Instance.ModuleInformation(module);
                if (compiler != null)
                {
                    if (compiler.GetAdditionalIncludeDirectories() == null)
                        continue;

                    string[] includeDirs = compiler.GetAdditionalIncludeDirectories().Split(new char[] { ',', ';' });
                    foreach (string dir in includeDirs)
                    {
                        if (FixFilePathForComparison(dir) == FixFilePathForComparison(info.IncludePath))
                        {
                            foundInIncludes = true;
                            break;
                        }
                    }
                }

                if (foundInIncludes)
                    break;

                List<string> libs = null;
                if (linker != null)
                {
                    LinkerToolWrapper linkerWrapper = new LinkerToolWrapper(linker);
                    libs = linkerWrapper.AdditionalDependencies;
                }

                if (libs != null)
                {
                    foundInLibs = true;
                    List<string> moduleLibs = info.GetLibs(IsDebugConfiguration(config), versionInfo);
                    foreach (string moduleLib in moduleLibs)
                    {
                        if (!libs.Contains(moduleLib))
                        {
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
            QtVersionManager vm = QtVersionManager.The();
            if (vi == null) vi = vm.GetVersionInfo(vm.GetDefaultVersion());

            switch (type & TemplateType.ProjectType)
            {
                case TemplateType.DynamicLibrary:
                    configType = ConfigurationTypes.typeDynamicLibrary;
                    targetExtension = ".dll";
                    break;
                case TemplateType.StaticLibrary:
                    configType = ConfigurationTypes.typeStaticLibrary;
                    targetExtension = ".lib";
                    break;
            }

            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                config.ConfigurationType = configType;
                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
                VCLibrarianTool librarian = (VCLibrarianTool)((IVCCollection)config.Tools).Item("VCLibrarianTool");

                // for some stupid reason you have to set this for it to be updated...
                // the default value is the same...
                config.OutputDirectory = "$(SolutionDir)$(ConfigurationName)";

                // add some common defines
                compiler.SetPreprocessorDefinitions(vi.GetQMakeConfEntry("DEFINES").Replace(" ", ","));

                if (linker != null)
                {
                    if (vi.IsWinCEVersion())
                    {
                        linker.SubSystem = subSystemOption.subSystemNotSet;
                        SetTargetMachine(linker, vi);
                    }
                    else
                    {
                        if ((type & TemplateType.ConsoleSystem) != 0)
                            linker.SubSystem = subSystemOption.subSystemConsole;
                        else
                            linker.SubSystem = subSystemOption.subSystemWindows;
                    }
                    linker.OutputFile = "$(OutDir)\\$(ProjectName)" + targetExtension;
                    linker.AdditionalLibraryDirectories = "$(QTDIR)\\lib";
                    if (vi.IsStaticBuild())
                    {
                        linker.AdditionalDependencies = vi.GetQMakeConfEntry("QMAKE_LIBS_CORE");
                        if ((type & TemplateType.GUISystem) != 0)
                        {
                            linker.AdditionalDependencies += " " + vi.GetQMakeConfEntry("QMAKE_LIBS_GUI");
                            if (vi.IsWinCEVersion())
                                linker.AdditionalDependencies += " qmenu_wince.res";
                        }
                    }
                }
                else
                {
                    librarian.OutputFile = "$(OutDir)\\$(ProjectName)" + targetExtension;
                    librarian.AdditionalLibraryDirectories = "$(QTDIR)\\lib";
                }

                if ((type & TemplateType.GUISystem) != 0)
                    compiler.SetAdditionalIncludeDirectories(QtVSIPSettings.GetUicDirectory(envPro) + ";");

                if ((type & TemplateType.PluginProject) != 0)
                {
                    compiler.AddPreprocessorDefinition("QT_PLUGIN");
                }

                bool isDebugConfiguration = false;
                if (config.Name.StartsWith("Release"))
                {
                    compiler.AddPreprocessorDefinition("QT_NO_DEBUG,NDEBUG");
                    compiler.SetDebugInformationFormat(debugOption.debugDisabled);
                    compiler.SetRuntimeLibrary(runtimeLibraryOption.rtMultiThreadedDLL);
                }
                else if (config.Name.StartsWith("Debug"))
                {
                    isDebugConfiguration = true;
                    compiler.SetOptimization(optimizeOption.optimizeDisabled);
                    compiler.SetDebugInformationFormat(debugOption.debugEnabled);
                    compiler.SetRuntimeLibrary(runtimeLibraryOption.rtMultiThreadedDebugDLL);
                }
                compiler.AddAdditionalIncludeDirectories(
                    "$(QTDIR)\\include;" + QtVSIPSettings.GetMocDirectory(envPro));

                compiler.SetTreatWChar_tAsBuiltInType(false);

                if (linker != null)
                    linker.GenerateDebugInformation = isDebugConfiguration;

#if ENABLE_WINCE
                if (vi.IsWinCEVersion())
                {
                    compiler.SetWarningLevel(warningLevelOption.warningLevel_3);
                    compiler.SetBufferSecurityCheck(isDebugConfiguration);
                    DeploymentToolWrapper deploymentTool = DeploymentToolWrapper.Create(config);
                    if (deploymentTool != null)
                    {
                        deploymentTool.AddWinCEMSVCStandardLib(isDebugConfiguration, dte);

                        string signatureFile = vi.GetSignatureFile();
                        if (signatureFile != null)
                        {
                            Object postBuildEventToolObj = ((IVCCollection)config.Tools).Item("VCPostBuildEventTool");
                            VCPostBuildEventTool postBuildEventTool = postBuildEventToolObj as VCPostBuildEventTool;
                            if (postBuildEventTool != null)
                            {
                                string cmdline = postBuildEventTool.CommandLine;
                                if (cmdline == null)
                                    cmdline = "";
                                if (cmdline.Length > 0)
                                    postBuildEventTool.CommandLine += "\n";
                                cmdline += "signtool sign /F " + signatureFile + " \"$(TargetPath)\"";
                                postBuildEventTool.CommandLine = cmdline;
                            }
                        }
                    }
                }
#endif

                if (usePrecompiledHeader)
                    UsePrecompiledHeaders(config);
            }
            if ((type & TemplateType.PluginProject) != 0)
                MarkAsDesignerPluginProject();
#if VS2010
            UpdateQtDirPropertySheet(vi.qtDir);
#endif

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
            try
            {
                if (!File.Exists(iconFile))
                {
                    File.Copy(iconFileName, iconFile);
                    FileAttributes attribs = File.GetAttributes(iconFile);
                    File.SetAttributes(iconFile, attribs & (~FileAttributes.ReadOnly));
                }
                
                StreamWriter sw = null;
                if (!File.Exists(rcFile)) 
                {
                    sw = new StreamWriter(File.Create(rcFile));
                    sw.WriteLine("IDI_ICON1\t\tICON\t\tDISCARDABLE\t\"" + projectName + ".ico\"" + sw.NewLine);
                    sw.Close();
                }
            }
            catch (System.Exception e)
            {
                Messages.DisplayErrorMessage(e);
                return false;
            }
            vcPro.AddFile(rcFile);
            return true;
        }

		public void AddUic4BuildStep(string fileName)
		{

            VCFile file = (VCFile)((IVCCollection)vcPro.Files).Item(fileName);

            if (file != null)
				AddUic4BuildStep(file);
		}

        /// <summary>
        /// This function adds a uic4 build step to a given file.
        /// </summary>
        /// <param name="file">file</param>
        public void AddUic4BuildStep(VCFile file)
        {
            try 
            {
                string uiFile = this.GetUiGeneratedFileName(file.FullPath);
                string uiBaseName = file.Name.Remove(file.Name.LastIndexOf('.'));
                string uiFileMacro = uiFile.Replace(uiBaseName, ProjectMacros.Name);
                bool uiFileExists = GetFileFromProject(uiFile) != null;

                foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations) 
                {
                    VCCustomBuildTool tool = HelperFunctions.GetCustomBuildTool(config);
                    tool.AdditionalDependencies = Resources.uic4Command;
                    tool.Description = "Uic'ing " + ProjectMacros.FileName + "...";
                    tool.Outputs = "\"" + uiFileMacro + "\"";
                    tool.CommandLine = "\"" + Resources.uic4Command + "\" -o \"" 
                        + uiFileMacro + "\" \"" + ProjectMacros.Path + "\"";

                    VCConfiguration conf = config.ProjectConfiguration as VCConfiguration;
                    CompilerToolWrapper compiler = CompilerToolWrapper.Create(conf);
                    if (compiler != null && !uiFileExists) 
                    {
                        string uiDir = QtVSIPSettings.GetUicDirectory(envPro);
                        if (compiler.GetAdditionalIncludeDirectories().IndexOf(uiDir) < 0)
                            compiler.AddAdditionalIncludeDirectories(uiDir);
                    }                    
                }
                if (!uiFileExists) 
                    AddFileInFilter(Filters.GeneratedFiles(), uiFile);                
            } 
            catch
            {
	            throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotAddUicStep", file.FullPath));
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
            List<string> defineList = GetDefinesFromCompilerTool(CompilerToolWrapper.Create(conf));

            VCConfiguration projectConfig = conf.ProjectConfiguration as VCConfiguration;
            defineList.AddRange(GetDefinesFromCompilerTool(CompilerToolWrapper.Create(projectConfig)));

            IVCCollection propertySheets = projectConfig.PropertySheets as IVCCollection;
            if (propertySheets != null)
                foreach (VCPropertySheet sheet in propertySheets)
                    defineList.AddRange(GetDefinesFromPropertySheet(sheet));

            string preprocessorDefines = "";
            List<string> alreadyAdded = new List<string>();
            Regex rxp = new Regex(@"\s|(\$\()");
            foreach (string define in defineList)
            {
                if (!alreadyAdded.Contains(define))
                {
                    bool mustSurroundByDoubleQuotes = rxp.IsMatch(define);
                    // Yes, a preprocessor definition can contain spaces or a macro name.
                    // Example: PROJECTDIR=$(InputDir)

                    if (mustSurroundByDoubleQuotes)
                    {
                        preprocessorDefines += " ";
                        preprocessorDefines += SafelyQuoteCommandLineArgument("-D" + define);
                    }
                    else
                    {
                        preprocessorDefines += " -D" + define;
                    }
                    alreadyAdded.Add(define);
                }
            }
            return preprocessorDefines;
        }

        private List<string> GetDefinesFromPropertySheet(VCPropertySheet sheet)
        {
            List<string> defines = GetDefinesFromCompilerTool(CompilerToolWrapper.Create(sheet));
            IVCCollection propertySheets = sheet.PropertySheets as IVCCollection;
            if (propertySheets != null)
                foreach (VCPropertySheet subSheet in propertySheets)
                    defines.AddRange(GetDefinesFromPropertySheet(subSheet));
            return defines;
        }

        private static List<string> GetDefinesFromCompilerTool(CompilerToolWrapper compiler)
        {
            try
            {
                if (compiler.GetPreprocessorDefinitions() != null)
                {
                    string[] defines = compiler.GetPreprocessorDefinitions().Split(new char[] { ',', ';' },
                                                                                   StringSplitOptions.RemoveEmptyEntries);
                    return new List<string>(defines);
                }
            }
            catch { }
            return new List<string>();
        }

        private string GetIncludes(VCFileConfiguration conf)
        {
            List<string> includeList = GetIncludesFromCompilerTool(CompilerToolWrapper.Create(conf));

            VCConfiguration projectConfig = conf.ProjectConfiguration as VCConfiguration;
            includeList.AddRange(GetIncludesFromCompilerTool(CompilerToolWrapper.Create(projectConfig)));

            IVCCollection propertySheets = projectConfig.PropertySheets as IVCCollection;
            if (propertySheets != null)
                foreach (VCPropertySheet sheet in propertySheets)
                    includeList.AddRange(GetIncludesFromPropertySheet(sheet));

            string includes = "";
            List<string> alreadyAdded = new List<string>();
            foreach (string include in includeList)
            {
                if (!alreadyAdded.Contains(include))
                {
                    string incl = HelperFunctions.NormalizeRelativeFilePath(include);
                    if (incl.Length > 0)
                    {
                        string cmdline = " ";
                        cmdline += SafelyQuoteCommandLineArgument("-I" + incl);
                        includes += cmdline;
                    }
                    alreadyAdded.Add(incl);
                }
            }
            return includes;
        }

        private List<string> GetIncludesFromPropertySheet(VCPropertySheet sheet)
        {
            List<string> includeList = GetIncludesFromCompilerTool(CompilerToolWrapper.Create(sheet));
            IVCCollection propertySheets = sheet.PropertySheets as IVCCollection;
            if (propertySheets != null)
                foreach (VCPropertySheet subSheet in propertySheets)
                    includeList.AddRange(GetIncludesFromPropertySheet(subSheet));
            return includeList;
        }

        private static List<string> GetIncludesFromCompilerTool(CompilerToolWrapper compiler)
        {
            try
            {
                if (compiler.GetAdditionalIncludeDirectories() != null)
                {
                    string[] includes = compiler.GetAdditionalIncludeDirectories().Split(new char[] { ',', ';' });
                    return new List<string>(includes);
                }
            }
            catch { }
            return new List<string>();
        }

        private static bool IsDebugConfiguration(VCConfiguration conf)
        {
            VCLinkerTool linker = (VCLinkerTool)((IVCCollection)conf.Tools).Item("VCLinkerTool");
            if (linker != null && linker.GenerateDebugInformation == true)
                return true;
            return false;
        }

        private string GetPCHMocOptions(VCFile file, CompilerToolWrapper compiler)
        {
            // As .moc files are included, we should not add anything there
            if (!HelperFunctions.HasHeaderFileExtension(file.Name))
                return "";

            string additionalMocOptions = "\"-f" + compiler.GetPrecompiledHeaderThrough() + "\" ";
            //Get mocDir without .\\ at the beginning of it
            string mocDir = QtVSIPSettings.GetMocDirectory(envPro);
            if (mocDir.StartsWith(".\\"))
                mocDir = mocDir.Substring(2);

            //Get the absolute path
            mocDir = vcPro.ProjectDirectory + mocDir;
            string relPathToFile = HelperFunctions.GetRelativePath(mocDir, file.FullPath);
            additionalMocOptions += "\"-f" + relPathToFile + "\"";
            return additionalMocOptions;
        }

        /// <summary>
		/// Adds a moc step to a given file for this project.
		/// </summary>
		/// <param name="file">file</param>
		public void AddMocStep(VCFile file)
		{
            try
			{
                string mocFileName = GetMocFileName(file.FullPath);
                if (mocFileName == null)
                    return;

                bool hasDifferentMocFilePerConfig = QtVSIPSettings.HasDifferentMocFilePerConfig(envPro);
                bool hasDifferentMocFilePerPlatform = QtVSIPSettings.HasDifferentMocFilePerPlatform(envPro);
                bool mocableIsCPP = mocFileName.ToLower().EndsWith(".moc");

                foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations)
                {
                    VCConfiguration vcConfig = config.ProjectConfiguration as VCConfiguration;
                    VCPlatform platform = vcConfig.Platform as VCPlatform;
                    string platformName = platform.Name;

                    string mocRelPath = GetRelativeMocFilePath(file.FullPath, vcConfig.ConfigurationName, platformName);
                    string subfilterName = null;
                    if (mocRelPath.Contains(vcConfig.ConfigurationName))
                        subfilterName = vcConfig.ConfigurationName;
                    if (mocRelPath.Contains(platformName))
                    {
                        if (subfilterName != null)
                            subfilterName += '_';
                        subfilterName += platformName;
                    }
                    VCFile mocFile = GetFileFromProject(mocRelPath);
                    if (mocFile == null)
                    {
                        FileInfo fi = new FileInfo(this.VCProject.ProjectDirectory + "\\" + mocRelPath);
                        if (!fi.Directory.Exists)
                            fi.Directory.Create();
                        mocFile = AddFileInSubfilter(Filters.GeneratedFiles(), subfilterName,
                            mocRelPath);
#if VS2010
                        if (mocFileName.ToLower().EndsWith(".moc"))
                        {
                            ProjectItem mocFileItem = mocFile.Object as ProjectItem;
                            if (mocFileItem != null)
                                HelperFunctions.EnsureCustomBuildToolAvailable(mocFileItem);
                        }
#endif
                    }

                    if (mocFile == null)
                        throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotAddMocStep", file.FullPath));

                    VCCustomBuildTool tool = null;
                    string fileToMoc = null;
                    if (!mocableIsCPP)
                    {
                        tool = HelperFunctions.GetCustomBuildTool(config);
                        fileToMoc = ProjectMacros.Path;
                    }
                    else
                    {
                        VCFileConfiguration mocConf = GetVCFileConfigurationByName(mocFile, vcConfig.Name);
                        tool = HelperFunctions.GetCustomBuildTool(mocConf);
                        fileToMoc = HelperFunctions.GetRelativePath(vcPro.ProjectDirectory, file.FullPath);
                    }
                    if (tool == null)
                        throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotFindCustomBuildTool", file.FullPath));

                    if (hasDifferentMocFilePerPlatform && !hasDifferentMocFilePerConfig)
                    {
                        foreach (VCFileConfiguration mocConf in (IVCCollection)mocFile.FileConfigurations)
                        {
                            VCConfiguration projectCfg = mocConf.ProjectConfiguration as VCConfiguration;
                            VCPlatform mocConfPlatform = projectCfg.Platform as VCPlatform;
                            bool exclude = mocConfPlatform.Name != platformName;
                            if (exclude)
                            {
                                if (mocConf.ExcludedFromBuild != exclude)
                                    mocConf.ExcludedFromBuild = exclude;
                            }
                            else
                            {
                                if (mocConf.ExcludedFromBuild != config.ExcludedFromBuild)
                                    mocConf.ExcludedFromBuild = config.ExcludedFromBuild;
                            }
                        }
                    }
                    else if (hasDifferentMocFilePerConfig)
                    {
                        foreach (VCFileConfiguration mocConf in (IVCCollection)mocFile.FileConfigurations)
                        {
                            VCConfiguration projectCfg = mocConf.ProjectConfiguration as VCConfiguration;
                            if (projectCfg.Name != vcConfig.Name || (IsMoccedFileIncluded(file) && !mocableIsCPP))
                            {
                                if (!mocConf.ExcludedFromBuild)
                                    mocConf.ExcludedFromBuild = true;
                            }
                            else
                            {
                                if (mocConf.ExcludedFromBuild != config.ExcludedFromBuild)
                                    mocConf.ExcludedFromBuild = config.ExcludedFromBuild;
                            }
                        }
                    }
                    else
                    {
                        VCFileConfiguration moccedFileConfig = GetVCFileConfigurationByName(mocFile, config.Name);
                        if (moccedFileConfig != null)
                        {
                            VCFile cppFile = GetCppFileForMocStep(mocFile);
                            if (cppFile != null && IsMoccedFileIncluded(cppFile))
                            {
                                if (!moccedFileConfig.ExcludedFromBuild)
                                {
                                    moccedFileConfig.ExcludedFromBuild = true;
                                }
                            }
                            else if (moccedFileConfig.ExcludedFromBuild != config.ExcludedFromBuild)
                                moccedFileConfig.ExcludedFromBuild = config.ExcludedFromBuild;
                        }
                    }

                    string dps = tool.AdditionalDependencies;
                    if (dps.IndexOf("\"" + Resources.moc4Command + "\"") < 0) 
                    {
                        if (dps.Length > 0 && !dps.EndsWith(";"))
                            dps += ";";
                        tool.AdditionalDependencies = dps + "\"" + Resources.moc4Command + "\";" + fileToMoc;
                    }

                    tool.Description = "Moc'ing " + ProjectMacros.FileName + "...";

                    string output = tool.Outputs;
                    string outputMocFile = "";
                    string outputMocMacro = "";
                    string baseFileName = file.Name.Remove(file.Name.LastIndexOf('.'));
                    string pattern = "(\"(.*\\\\" + mocFileName + ")\"|(\\S*"
                        + mocFileName + "))";
                    System.Text.RegularExpressions.Regex regExp = new Regex(pattern);
                    MatchCollection matchList = regExp.Matches(tool.Outputs.Replace(ProjectMacros.Name, baseFileName));
                    if (matchList.Count > 0)
                    {
                        if (matchList[0].Length > 0)
                        {
                            outputMocFile = matchList[0].ToString();
                        } 
                        else if (matchList[1].Length > 1)
                        {
                            outputMocFile = matchList[1].ToString();
                        }
                        if (outputMocFile.StartsWith("\""))
                            outputMocFile = outputMocFile.Substring(1);
                        if (outputMocFile.EndsWith("\""))
                            outputMocFile = outputMocFile.Substring(0, outputMocFile.Length-1);
                        string outputMocPath = Path.GetDirectoryName(outputMocFile);
                        string stringToReplace = Path.GetFileName(outputMocFile);
                        outputMocMacro = outputMocPath + "\\" + stringToReplace.Replace(baseFileName, ProjectMacros.Name);
                    }
                    else
                    {
                        outputMocFile = GetRelativeMocFilePath(file.FullPath);
                        string outputMocPath = Path.GetDirectoryName(outputMocFile);
                        string stringToReplace = Path.GetFileName(outputMocFile);
                        outputMocMacro = outputMocPath + "\\" + stringToReplace.Replace(baseFileName, ProjectMacros.Name);
                        if (output.Length > 0 && !output.EndsWith(";"))
                            output += ";";
                        tool.Outputs = output + "\"" + outputMocMacro + "\"";
                    }

                    string newCmdLine = "\"" + Resources.moc4Command + "\" " + QtVSIPSettings.GetMocOptions(envPro)
                        + " \"" + fileToMoc + "\" -o \""
                        + outputMocMacro + "\"";

                    // Tell moc to include the PCH header if we are using precompiled headers in the project
                    CompilerToolWrapper compiler = CompilerToolWrapper.Create(vcConfig);
                    if (compiler.GetUsePrecompiledHeader() != pchOption.pchNone)
                    {
                        newCmdLine += " " + GetPCHMocOptions(file, compiler);
                    }

                    QtVersionManager versionManager = QtVersionManager.The();
                    VersionInformation versionInfo = new VersionInformation(versionManager.GetInstallPath(envPro));
                    bool mocSupportsIncludes = versionInfo.qtMajor >= 4 && versionInfo.qtMinor >= 2;

                    string strDefinesIncludes = "";
                    VCFile cppPropertyFile;
                    if (!mocableIsCPP)
                        cppPropertyFile = GetCppFileForMocStep(file);
                    else
                        cppPropertyFile = GetCppFileForMocStep(mocFile);
                    VCFileConfiguration cppConfig = GetVCFileConfigurationByName(cppPropertyFile, config.Name);
                    strDefinesIncludes += GetDefines(cppConfig);
                    strDefinesIncludes += GetIncludes(cppConfig);
                    strDefinesIncludes = HelperFunctions.RemoveDuplicates(strDefinesIncludes, ' ');
                    int cmdLineLength = newCmdLine.Length + strDefinesIncludes.Length + 1;
                    if (cmdLineLength > HelperFunctions.GetMaximumCommandLineLength() && mocSupportsIncludes)
                    {
                        // Command line is too long. We must use an options file.
                        string mocIncludeCommands = "";
                        string mocIncludeFile = "\"" + outputMocFile + ".inc\"";
                        string redirectOp = " > ";
                        int maxCmdLineLength = HelperFunctions.GetMaximumCommandLineLength() - (mocIncludeFile.Length + 1);

                        string[] options = strDefinesIncludes.Split(' ');

                        int i = options.Length - 1;
                        for (; i >= 0; --i)
                        {
                            if (options[i].Length == 0)
                                continue;
                            mocIncludeCommands += "echo " + options[i] + redirectOp + mocIncludeFile + "\r\n";
                            cmdLineLength -= options[i].Length + 1;
                            if (cmdLineLength < maxCmdLineLength)
                                break;
                            if (i == options.Length - 1)    // first loop
                                redirectOp = " >> ";
                        }
                        strDefinesIncludes =  "@" + mocIncludeFile;
                        for (int k = 0; k < i; ++k)
                            if (options[k].Length > 0)
                                strDefinesIncludes += " " + options[k];

                        newCmdLine = mocIncludeCommands + newCmdLine + " " + strDefinesIncludes;
                    }

                    if (tool.CommandLine.Trim().Length > 0)
                    {
                        string cmdLine = tool.CommandLine;

                        // remove the moc option file commands
                        {
                            Regex rex = new Regex("^echo.+[.](moc|cpp)[.]inc\"\r\n", RegexOptions.Multiline);
                            cmdLine = rex.Replace(cmdLine, "");
                        }

                        Match m = System.Text.RegularExpressions.Regex.Match(cmdLine,
                            @"(\S*moc.exe|""\S+:\\\.*moc.exe"")");

                        if (m.Success)
                        {
                            int start = m.Index;
                            int end = cmdLine.IndexOf("&&", start);
                            int a = cmdLine.IndexOf("\r\n", start);
                            if ((a > -1 && a < end) || (end < 0 && a > -1))
                                end = a;
                            if (end < 0)
                                end = cmdLine.Length;
                            tool.CommandLine = cmdLine.Replace(cmdLine.Substring(start, end - start), newCmdLine);
                        }
                        else
                        {
                            tool.CommandLine = cmdLine + "\r\n" + newCmdLine;
                        }
                    }
                    else
                    {
                        tool.CommandLine = newCmdLine;
                    }
                }
			}
			catch 
			{
				throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotAddMocStep", file.FullPath));
			}
		}

        /// <summary>
        /// Helper function for AddMocStep.
        /// </summary>
        /// <param name="file">header or source file name</param>
        /// <returns>True, if the file contains an include of the
        /// corresponding moc_xxx.cpp file. False in all other cases</returns>
        public bool IsMoccedFileIncluded(VCFile file)
        {
            bool isHeaderFile = HelperFunctions.HasHeaderFileExtension(file.FullPath);
            if (isHeaderFile || HelperFunctions.HasSourceFileExtension(file.FullPath))
            {
                string srcName;
                if (isHeaderFile)
                    srcName = file.FullPath.Substring(0, file.FullPath.LastIndexOf(".")) + ".cpp";
                else
                    srcName = file.FullPath;
                VCFile f = GetFileFromProject(srcName);
                CxxStreamReader sr = null;
                if (f != null) 
                {
                    try
                    {
                        string strLine;
                        sr = new CxxStreamReader(f.FullPath);
                        string baseName = file.Name.Substring(0, file.Name.LastIndexOf("."));
                        while ((strLine = sr.ReadLine()) != null)
                        {
                            if (strLine.IndexOf("#include \"moc_" + baseName + ".cpp\"") != -1 ||
                                strLine.IndexOf("#include <moc_" + baseName + ".cpp>") != -1)
                            {
                                sr.Close();
                                return true;
                            }
                        }
                        sr.Close();
                    }
                    catch (System.Exception)
                    {
                        // do nothing
                        if (sr != null)
                            sr.Close();
                        return false;
                    }
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
            if (HelperFunctions.HasHeaderFileExtension(file.Name))
            {
                return CheckForCommand(file, "moc.exe");
            }
            else if (HelperFunctions.HasSourceFileExtension(file.Name))
            {
                foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
                {
                    string mocFileName = "";
                    if (mocOutDir == null) 
                    {
                        VCPlatform platform = config.Platform as VCPlatform;
                        mocFileName = GetRelativeMocFilePath(file.Name, config.ConfigurationName, platform.Name);
                    } 
                    else 
                    {
                        string fileName = GetMocFileName(file.FullPath);
                        if (fileName != null) 
                        {
                            mocOutDir = mocOutDir.Replace("$(ConfigurationName)", config.ConfigurationName);
                            VCPlatform platform = config.Platform as VCPlatform;
                            mocOutDir = mocOutDir.Replace("$(PlatformName)", platform.Name);
                            mocFileName = mocOutDir + "\\" + fileName;
                        }
                    }
                    VCFile mocFile = GetFileFromProject(mocFileName);
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
            foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations)
            {
                VCCustomBuildTool tool = HelperFunctions.GetCustomBuildTool(config);
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
            List<VCFile> files = GetResourceFiles();

            VCFilter vcFilter = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (vcFilter != null)
            {
                IVCCollection filterFiles = (IVCCollection)vcFilter.Files;
                List<VCFile> filesToDelete = new List<VCFile>();
                foreach (VCFile rmFile in filterFiles)
                {
                    if (rmFile.Name.ToLower().StartsWith("qrc_"))
                        filesToDelete.Add(rmFile);
                }
                foreach (VCFile rmFile in filesToDelete)
                {
                    RemoveFileFromFilter(rmFile, vcFilter);
                    HelperFunctions.DeleteEmptyParentDirs(rmFile);
                }
            }

            foreach (VCFile file in files)
            {
                Messages.PaneMessage(dte, "Update rcc step for " + file.Name + ".");
                RccOptions options = new RccOptions(envPro, file);
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

            VCFile file = (VCFile)((IVCCollection)vcPro.Files).Item(fileName);
            UpdateRccStep(file, rccOpts);
        }

        public void UpdateRccStep(VCFile qrcFile, RccOptions rccOpts)
        {
            VCProject vcpro = (VCProject)qrcFile.project;
            EnvDTE.DTE dteObject = ((EnvDTE.Project)vcpro.Object).DTE;

            QtProject qtPro = QtProject.Create(vcpro);
            QrcParser parser = new QrcParser(qrcFile.FullPath);
            string filesInQrcFile = ProjectMacros.Path;

            if (parser.parse())
            {
                FileInfo fi = new FileInfo(qrcFile.FullPath);
                string qrcDir = fi.Directory.FullName + "\\";

                foreach (QrcPrefix prfx in parser.Prefixes) 
                {
                    foreach (QrcItem itm in prfx.Items)
                    {
                        string relativeQrcItemPath = HelperFunctions.GetRelativePath(this.vcPro.ProjectDirectory,
                            qrcDir + itm.Path);
                        filesInQrcFile += ";" + relativeQrcItemPath;
                        try
                        {
                            VCFile addedFile = qtPro.AddFileInFilter(Filters.ResourceFiles(), relativeQrcItemPath, true);
                            QtProject.ExcludeFromAllBuilds(addedFile);
                        } 
                        catch { /* it's not possible to add all kinds of files */ }
                    }
                }
            }

            string nameOnly = HelperFunctions.RemoveFileNameExtension(new FileInfo(qrcFile.FullPath));
            string qrcCppFile = QtVSIPSettings.GetRccDirectory(envPro) + "\\" + "qrc_" + nameOnly + ".cpp";
            
            try 
            {
                foreach (VCFileConfiguration vfc in (IVCCollection)qrcFile.FileConfigurations) 
                {
                    RccOptions rccOptsCfg = rccOpts;
                    string cmdLine = "";

                    VCCustomBuildTool cbt = HelperFunctions.GetCustomBuildTool(vfc);

                    cbt.AdditionalDependencies = filesInQrcFile;

                    cbt.Description = "Rcc'ing " + ProjectMacros.FileName + "...";

                    cbt.Outputs = qrcCppFile.Replace(nameOnly, ProjectMacros.Name);

                    cmdLine += "\"" + Resources.rcc4Command + "\""
                        + " -name \"" + ProjectMacros.Name + "\"";

                    if (rccOptsCfg == null)
                        rccOptsCfg = HelperFunctions.ParseRccOptions(cbt.CommandLine, qrcFile);
                        
                    if (rccOptsCfg.CompressFiles)
                    {
                        cmdLine += " -threshold " + rccOptsCfg.CompressThreshold.ToString();
                        cmdLine += " -compress " + rccOptsCfg.CompressLevel.ToString();
                    }
                    else
                    {
                        cmdLine += " -no-compress";
                    }
                    cmdLine += " \"" + ProjectMacros.Path + "\" -o " + cbt.Outputs;
                    cbt.CommandLine = cmdLine;
                }
                AddFileInFilter(Filters.GeneratedFiles(), qrcCppFile, true);
            }
            catch(System.Exception /*e*/) 
            {
                Messages.PaneMessage(dteObject, "*** WARNING (RCC): Couldn't add rcc step");
            }
        }

        public void RemoveRccStep(VCFile file)
        {
            if (file == null)
                return;
            try 
            {
                string relativeQrcFilePath = file.RelativePath;
                FileInfo qrcFileInfo = new FileInfo(ProjectDir + "\\" + relativeQrcFilePath);            
                if (qrcFileInfo.Exists)
                {
                    RccOptions opts = new RccOptions(Project, file);
                    string qrcCppFile = QtVSIPSettings.GetRccDirectory(envPro) + "\\" + opts.OutputFileName;
                    VCFile generatedFile = GetFileFromProject(qrcCppFile);
                    if (generatedFile != null) 
                        RemoveFileFromFilter(generatedFile, Filters.GeneratedFiles());
                }
            }
            catch (System.Exception e) 
            {
                Messages.DisplayWarningMessage(e);
            }
        }

        static public void ExcludeFromAllBuilds(VCFile file)
        {
            if (file == null)
                return;
            foreach (VCFileConfiguration conf in (IVCCollection)file.FileConfigurations)
                if (!conf.ExcludedFromBuild)
                    conf.ExcludedFromBuild = true;
        }

		/// <summary>
		/// Removes the custom build step of a given file.
		/// </summary>
		/// <param name="file">file</param>
		public void RemoveMocStep(VCFile file)
		{
            try
            {
                if (!HasMocStep(file))
                    return;

                if (HelperFunctions.HasHeaderFileExtension(file.Name))
                {
                    foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations)
                    {
                        VCCustomBuildTool tool = HelperFunctions.GetCustomBuildTool(config);
                        if (tool == null)
                            continue;

                        string cmdLine = tool.CommandLine;
                        if (cmdLine.Length > 0) 
                        {
                            Regex rex = new Regex(@"(\S*moc.exe|""\S+:\\\.*moc.exe"")");
                            while (true)
                            {
                                Match m = rex.Match(cmdLine);
                                if (!m.Success)
                                    break;

                                int start = m.Index;
                                int end = cmdLine.IndexOf("&&", start);
                                int a = cmdLine.IndexOf("\r\n", start);
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
                        string addDepends = tool.AdditionalDependencies;
                        addDepends = System.Text.RegularExpressions.Regex.Replace(addDepends,
                            @"(\S*moc.exe|""\S+:\\\.*moc.exe"")", "");
                        addDepends = addDepends.Replace(file.RelativePath, "");
                        tool.AdditionalDependencies = "";
                        tool.Description = tool.Description.Replace("Moc'ing " + file.Name + "...", "");
                        tool.Description = tool.Description.Replace("Moc'ing " + ProjectMacros.FileName + "...", "");
                        tool.Description = tool.Description.Replace("MOC " + file.Name, "");
                        string baseFileName = file.Name.Remove(file.Name.LastIndexOf('.'));
                        string pattern = "(\"(.*\\\\" + GetMocFileName(file.FullPath)
                            + ")\"|(\\S*" + GetMocFileName(file.FullPath) + "))";
                        string outputMocFile = null;
                        System.Text.RegularExpressions.Regex regExp = new Regex(pattern);
                        tool.Outputs = tool.Outputs.Replace(ProjectMacros.Name, baseFileName);
                        MatchCollection matchList = regExp.Matches(tool.Outputs);
                        if (matchList.Count > 0) 
                        {
                            if (matchList[0].Length > 0) 
                            {
                                outputMocFile = matchList[0].ToString();
                            } 
                            else if (matchList[1].Length > 1) 
                            {
                                outputMocFile = matchList[1].ToString();
                            }
                        }                        
                        tool.Outputs = System.Text.RegularExpressions.Regex.Replace(tool.Outputs, pattern, "",
                            RegexOptions.Multiline|RegexOptions.IgnoreCase);
                        tool.Outputs = System.Text.RegularExpressions.Regex.Replace(tool.Outputs,
                            @"\s*;\s*;\s*", ";", RegexOptions.Multiline);
                        tool.Outputs = System.Text.RegularExpressions.Regex.Replace(tool.Outputs,
                            @"(^\s*;|\s*;\s*$)", "", RegexOptions.Multiline);
                        
                        if (outputMocFile != null) 
                        {
                            if (outputMocFile.StartsWith("\""))
                                outputMocFile = outputMocFile.Substring(1);
                            if (outputMocFile.EndsWith("\""))
                                outputMocFile = outputMocFile.Substring(0, outputMocFile.Length-1);
                            outputMocFile = outputMocFile.Replace("$(ConfigurationName)",
                                config.Name.Substring(0, config.Name.IndexOf('|')));
                            outputMocFile = outputMocFile.Replace("$(PlatformName)",
                                config.Name.Remove(0, config.Name.IndexOf('|') + 1));
                        }                        
                        VCFile mocFile = GetFileFromProject(outputMocFile);
                        if (mocFile != null)
                            RemoveFileFromFilter(mocFile, Filters.GeneratedFiles());
                    }
                } 
                else 
                {
                    if (QtVSIPSettings.HasDifferentMocFilePerConfig(envPro)
                        || QtVSIPSettings.HasDifferentMocFilePerPlatform(envPro))
                    {
                        foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations)
                        {
                            VCFile mocFile = GetGeneratedMocFile(file.FullPath, config);
                            if (mocFile != null)
                                RemoveFileFromFilter(mocFile, Filters.GeneratedFiles());
                        }
                    } 
                    else 
                    {
                        VCFile mocFile = GetGeneratedMocFile(file.FullPath, null);
                        if (mocFile != null)
                            RemoveFileFromFilter(mocFile, Filters.GeneratedFiles());
                    }
                }
            }
            catch 
            {
                throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotRemoveMocStep", file.FullPath));
            }
		}
        
        public void RemoveUiHeaderFile(VCFile file)
        {
			if (file == null)
				return;
			try 
            {
                string headerFile = GetUiGeneratedFileName(file.Name);
                VCFile hFile = GetFileFromProject(headerFile);

                if (hFile != null) 
                    RemoveFileFromFilter(hFile, Filters.GeneratedFiles());
            }
            catch (System.Exception e) 
            {
                Messages.DisplayWarningMessage(e);
            }
        }

		public void RemoveUic4BuildStep(VCFile file)
		{
			if (file == null)
				return;
			foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations)
			{
                VCCustomBuildTool tool = HelperFunctions.GetCustomBuildTool(config);
				tool.AdditionalDependencies = "";
				tool.Description = "";
				tool.CommandLine = "";
				tool.Outputs = "";
			}
			RemoveUiHeaderFile(file);
		}

        public List<VCFile> GetResourceFiles()
        {
            List<VCFile> qrcFiles = new List<VCFile>();

            foreach (VCFile f in (IVCCollection)VCProject.Files)
            {
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
            return GetFileFromFilter(filter, fileName, true);
        }

        public VCFile GetFileFromFilter(FakeFilter filter, string fileName, bool isRelativeToProject)
        {
            VCFilter vcfilter = FindFilterFromGuid(filter.UniqueIdentifier);

            // try with name as well
            if (vcfilter == null)
                vcfilter = FindFilterFromName(filter.Name);

            if (vcfilter == null) 
                return null;

            try 
            {
                FileInfo fi = null;
                if (isRelativeToProject)
                    fi = new FileInfo(ProjectDir + "\\" + fileName);
                else
                    fi = new FileInfo(fileName);
                
                foreach (VCFile file in (IVCCollection)vcfilter.Files)
                {
                    if (file.MatchName(fi.FullName, true))
                        return file;
                }
            }
            catch {}			
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

            foreach (VCFile f in (IVCCollection)vcPro.Files)
            {
                if (f.FullPath.ToLower() == nf)
                    return f;
            }
            if (beStrict || vcfile != null)
                return vcfile;


            FileInfo fi = new FileInfo(fileName);
            foreach (VCFile f in (IVCCollection)vcPro.Files)
            {
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
            System.Collections.Generic.List<VCFile> tmpList = new System.Collections.Generic.List<VCFile>();
            fileName = HelperFunctions.NormalizeRelativeFilePath(fileName);

            FileInfo fi = new FileInfo(fileName);
            foreach (VCFile f in (IVCCollection)vcPro.Files)
            {
                if (f.Name.ToLower() == fi.Name.ToLower())
                    tmpList.Add(f);
            }
            if (tmpList.Count == 0)
                return null;
            else
                return tmpList;
        }

        public System.Collections.Generic.List<VCFile> GetAllFilesFromFilter(VCFilter filter)
        {
            System.Collections.Generic.List<VCFile> tmpList = new System.Collections.Generic.List<VCFile>();

            foreach (VCFile f in (IVCCollection)filter.Files)
            {
                    tmpList.Add(f);
            }
            foreach (VCFilter subfilter in (IVCCollection)filter.Filters)
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
            foreach (ProjectItem tmpFilter in this.Project.ProjectItems)
            {
                if (tmpFilter.Name == item.Name)
                {
                    tmpFilter.Remove();
                    return;
                }
                foreach (ProjectItem tmpItem in tmpFilter.ProjectItems)
                    if (tmpItem.Name == item.Name)
                    {
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
            try 
            {
                VCFilter vfilt = FindFilterFromGuid(filter.UniqueIdentifier);
                if (vfilt == null) 
                {
                    if (!vcPro.CanAddFilter(filter.Name)) 
                    {
                        // check if user already created this filter... then add guid
                        vfilt = FindFilterFromName(filter.Name);
                        if (vfilt == null)
                            throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotAddFilter", filter.Name));
                    }
                    else
                    {
                        vfilt = (VCFilter)vcPro.AddFilter(filter.Name);
                    }

                    vfilt.UniqueIdentifier = filter.UniqueIdentifier;
                    vfilt.Filter = filter.Filter;
                    vfilt.ParseFiles = filter.ParseFiles;
                }

                if (!string.IsNullOrEmpty(subfilterName))
                {
                    bool subfilterFound = false;
                    foreach (VCFilter subfilt in vfilt.Filters as IVCCollection)
                        if (subfilt.Name == subfilterName)
                        {
                            vfilt = subfilt;
                            subfilterFound = true;
                            break;
                        }
                    if (!subfilterFound)
                    {
                        if (!vfilt.CanAddFilter(subfilterName))
                        {
                            throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotAddFilter", filter.Name));
                        }
                        else
                        {
                            vfilt = (VCFilter)vfilt.AddFilter(subfilterName);
                        }

                        vfilt.Filter = "cpp;moc";
                        vfilt.SourceControlFiles = false;
                    }
                }

                if (checkForDuplicates)
                {
                    // check if file exists in filter already
                    VCFile vcFile = GetFileFromFilter(filter, fileName);
                    if (vcFile != null)
                        return vcFile;
                }

                if (vfilt.CanAddFile(fileName))
                    return (VCFile)(vfilt.AddFile(fileName));
                else
                    throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotAddFile", fileName));
            }
            catch 
            {
                throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotAddFile", fileName));
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
            try
            {
                VCFilter vfilt = FindFilterFromGuid(filter.UniqueIdentifier);

                if (vfilt == null)
                    vfilt = FindFilterFromName(filter.Name);

                if (vfilt == null)
                    return;

                RemoveFileFromFilter(file, vfilt);
            }
            catch
            {
                throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotRemoveFile", file.Name));
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
            try
            {
                filter.RemoveFile(file);
                FileInfo fi = new FileInfo(file.FullPath);
                if (fi.Exists)
                    fi.Delete();
            }
            catch
            {
            }

            IVCCollection subfilters = (IVCCollection)filter.Filters;
            for (int i = subfilters.Count; i > 0; i--)
            {
                try
                {
                    VCFilter subfilter = (VCFilter)subfilters.Item(i);
                    RemoveFileFromFilter(file, subfilter);
                }
                catch
                {
                }
            }
        }

        public void MoveFileToDeletedFolder(VCFile vcfile)
        {
            FileInfo srcFile = new FileInfo(vcfile.FullPath);

            if (!srcFile.Exists)
                return;

            string destFolder = vcPro.ProjectDirectory + "\\Deleted\\";
            string destName = destFolder + vcfile.Name.Replace(".","_") + ".bak";
            int fileNr = 0;

            try
            {
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);

                while(File.Exists(destName))
                {
                    fileNr++;
                    destName = destName.Substring(0,destName.LastIndexOf(".")) + ".b";
                    if (fileNr>9)
                        destName += fileNr.ToString();
                    else
                        destName += "0" + fileNr.ToString();
                }

                srcFile.MoveTo(destName);
            }
            catch(System.Exception e)
            {
                Messages.DisplayWarningMessage(e, SR.GetString("QtProject_DeletedFolderFullOrProteced"));
            }
        }

        public VCFilter FindFilterFromName(string filtername)
        {
            try 
            {
                foreach (VCFilter vcfilt in (IVCCollection)vcPro.Filters)
                {
                    if (vcfilt.Name.ToLower() == filtername.ToLower())
                    {
                        return vcfilt;
                    }
                }
                return null;
            }
            catch 
            {
                throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotFindFilter"));
            }
        }

		public VCFilter FindFilterFromGuid(string filterguid)
		{
			try 
			{
				foreach (VCFilter vcfilt in (IVCCollection)vcPro.Filters)
				{
					if (vcfilt.UniqueIdentifier != null
                        && vcfilt.UniqueIdentifier.ToLower() == filterguid.ToLower())
					{
						return vcfilt;
					}
				}
				return null;
			}
			catch 
			{
                throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotFindFilter"));
			}
		}

        public VCFilter AddFilterToProject(FakeFilter filter)
        {
            try 
            {
                VCFilter vfilt = FindFilterFromGuid(filter.UniqueIdentifier);
                if (vfilt == null)
                {
                    if (!vcPro.CanAddFilter(filter.Name)) 
                    {
                        vfilt = FindFilterFromName(filter.Name);
                        if (vfilt == null)
                            throw new Qt4VS2003Exception(SR.GetString("QtProject_ProjectCannotAddFilter", filter.Name));
                    }
                    else
                    {
                        vfilt = (VCFilter)vcPro.AddFilter(filter.Name);
                    }

                    vfilt.UniqueIdentifier = filter.UniqueIdentifier;
                    vfilt.Filter = filter.Filter;
                    vfilt.ParseFiles = filter.ParseFiles;
                }
                return vfilt;
            }
            catch
            {
                throw new Qt4VS2003Exception(SR.GetString("QtProject_ProjectCannotAddResourceFilter"));
            }
        }
		
		public void AddDirectories()
		{
			try 
			{
				// resource directory
				FileInfo fi = new FileInfo(envPro.FullName);
				DirectoryInfo dfi = new DirectoryInfo(fi.DirectoryName + "\\" + Resources.resourceDir);
				dfi.Create();

				// generated files directory
				dfi = new DirectoryInfo(fi.DirectoryName + "\\" + Resources.generatedFilesDir);
				dfi.Create();
			}
			catch 
			{
				throw new Qt4VS2003Exception(SR.GetString("QtProject_CannotCreateResourceDir"));
			}
            AddFilterToProject(Filters.ResourceFiles());
		}

        public void Finish()
        {
            try
            {
                EnvDTE.Window solutionExplorer = dte.Windows.Item(Constants.vsWindowKindSolutionExplorer);
                if (solutionExplorer != null)
                {
                    EnvDTE.UIHierarchy hierarchy = (EnvDTE.UIHierarchy)solutionExplorer.Object;
                    EnvDTE.UIHierarchyItems projects = hierarchy.UIHierarchyItems.Item(1).UIHierarchyItems;

                    foreach (EnvDTE.UIHierarchyItem itm in projects)
                    {
                        if (itm.Name == envPro.Name)
                        {
                            foreach (EnvDTE.UIHierarchyItem i in itm.UIHierarchyItems)
                            {
                                if (i.Name == Filters.GeneratedFiles().Name)
                                    i.UIHierarchyItems.Expanded = false;
                            }
                            break;
                        }
                    }
                }
            }
            catch {}            
        }

        public bool IsDesignerPluginProject()
        {
            bool b = false;
            if (Project.Globals.get_VariablePersists("IsDesignerPlugin")) 
            {
                string s = (string)Project.Globals["IsDesignerPlugin"];
                try 
                {
                    b = bool.Parse(s);
                } 
                catch {}
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
                file = (VCFile)vcPro.AddFile(destName);

            if (file == null)
                return null;

            if (HelperFunctions.HasHeaderFileExtension(file.Name))
            {
                foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
                {
                    CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                    if (compiler == null)
                        continue;

                    string[] paths = compiler.GetAdditionalIncludeDirectories().Split(new char[] { ',', ';' });
                    FileInfo fi = new FileInfo(file.FullPath);
                    string newPath = FixFilePathForComparison(fi.Directory.Name);
                    newPath = FixFilePathForComparison(HelperFunctions.GetRelativePath(this.ProjectDir, fi.Directory.ToString()));
                    bool pathFound = false;
                    foreach (string p in paths) {
                        if (FixFilePathForComparison(p) == newPath)
                        {
                            pathFound = true;
                            break;
                        }
                    }
                    if (!pathFound)
                        compiler.AddAdditionalIncludeDirectories(
                            HelperFunctions.GetRelativePath(this.ProjectDir, fi.Directory.ToString()));
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

            try
            {
                EnvDTE.Properties prop = dte.get_Properties("TextEditor", "C/C++");
                long tabSize = Convert.ToInt64(prop.Item("TabSize").Value);
                bool insertTabs = Convert.ToBoolean(prop.Item("InsertTabs").Value);

                string oldValue = insertTabs ? "    " : "\t";
                string newValue = insertTabs ? "\t" : GetWhitespaces(tabSize);

                List<string> list = new List<string>();
                StreamReader reader = new StreamReader(file);
                string line = reader.ReadLine();
                while (line != null) 
                {
                    if (line.StartsWith(oldValue))
                        line = line.Replace(oldValue, newValue);
                    list.Add(line);
                    line = reader.ReadLine();
                }
                reader.Close();
                
                StreamWriter writer = new StreamWriter(file);
                foreach (string l in list)
                    writer.WriteLine(l);
                writer.Close();
            }
            catch (Exception e)
            {
                Messages.DisplayErrorMessage(SR.GetString("QtProject_CannotAdjustWhitespaces", e.ToString()));
            }
        }

        private static string GetWhitespaces(long size)
        {
            string whitespaces = null;
            for (long i = 0; i < size; ++i)
            {
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
            FileInfo fi = new FileInfo(fullDestName);

            bool replace = true;
            if (File.Exists(fullDestName))
            {
                if (DialogResult.No == MessageBox.Show(SR.GetString("QtProject_FileExistsInProjectFolder", destName)
                    , Resources.msgBoxCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    replace = false;
                }
            }

            if (replace)
            {
                if (!fi.Directory.Exists)
                    fi.Directory.Create();
                File.Copy(srcFile, fullDestName, true);
                FileAttributes attribs = File.GetAttributes(fullDestName);
                File.SetAttributes(fullDestName, attribs & (~FileAttributes.ReadOnly));
            }
            return fi.FullName;
        }

        public static void ReplaceTokenInFile(string file, string token, string replacement)
        {
            string text;
            try
            {
                StreamReader reader = new StreamReader(file);
                text = reader.ReadToEnd();
                reader.Close();
            }
            catch(System.Exception e)
            {
                Messages.DisplayErrorMessage(
                    SR.GetString("QtProject_CannotReplaceTokenRead", token, replacement, e.ToString()));
                return;
            }

            try
            {
                if (token.ToUpper() == "%PRE_DEF%" && !Char.IsLetter(replacement[0]))
                    replacement = "_" + replacement;

                text = text.Replace(token, replacement);
                StreamWriter writer = new StreamWriter(file);
                writer.Write(text);
                writer.Close();
            }
            catch(System.Exception e)
            {
                Messages.DisplayErrorMessage(
                    SR.GetString("QtProject_CannotReplaceTokenWrite", token, replacement, e.ToString()));
            }
        }

        public void RepairGeneratedFilesStructure()
        {
            DeleteGeneratedFiles();
            string[] qobjectMacros = new string[] { "Q_OBJECT", "Q_GADGET" };
            foreach (VCFile file in (IVCCollection)vcPro.Files)
                if (HelperFunctions.HasMacros(file, qobjectMacros))
                {
                    if (HasMocStep(file))
                        RemoveMocStep(file);

                    AddMocStep(file);
                }
        }

        public void TranslateFilterNames()
        {
            IVCCollection filters = vcPro.Filters as IVCCollection;
            if (filters == null)
                return;

            foreach (VCFilter filter in filters)
            {
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
			string fullDestName = vcPro.ProjectDirectory + "\\" + destName;

			if (!File.Exists(fullDestName))
			{
				FileStream s = File.Open(fullDestName, FileMode.CreateNew);
				if (s.CanWrite) 
				{
					StreamWriter sw = new StreamWriter(s);
					sw.WriteLine("<RCC>");
					sw.WriteLine("    <qresource prefix=\"" + className + "\">");
					sw.WriteLine("    </qresource>");
					sw.WriteLine("</RCC>");
					sw.Close();
				}
				s.Close();
				FileAttributes attribs = File.GetAttributes(fullDestName);
				File.SetAttributes(fullDestName, attribs & (~FileAttributes.ReadOnly));
			}

			FileInfo fi = new FileInfo(fullDestName);
			return fi.FullName;		
		}

        public static void EnableSection(string file, string sectionName, bool enable)
        {
            string text = "";
            bool firstLine = true;
            try
            {
                StreamReader reader = new StreamReader(file);
                string line = reader.ReadLine();
                bool skip = false;
                while (line != null)
                {
                    if (line.StartsWith("#Begin_" + sectionName))
                    {
                        skip = !enable;                        
                    } 
                    else if (line.StartsWith("#End_" + sectionName))
                    {
                        skip = false;                        
                    }
                    else if (!skip) 
                    {
                        if (firstLine) 
                        {
                            text = line;
                            firstLine = false;
                        } 
                        else 
                        {
                            text += "\r\n" + line;
                        }
                    }
                    line = reader.ReadLine();
                }                
                reader.Close();
            }
            catch(System.Exception e)
            {
                Messages.DisplayErrorMessage(SR.GetString("QtProject_CannotEnableSectionRead", sectionName, e.ToString()));
                return;
            }

            try
            {
                StreamWriter writer = new StreamWriter(file);
                writer.Write(text);
                writer.Close();
            }
            catch(System.Exception e)
            {
                Messages.DisplayErrorMessage(SR.GetString("QtProject_CannotEnableSectionWrite", sectionName, e.ToString()));
            }
        }

        public void AddActiveQtBuildStep(string version)
        {
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                string idlFile = "\"$(IntDir)/" + envPro.Name + ".idl\"";
                string tblFile = "\"$(IntDir)/" + envPro.Name + ".tlb\"";

                VCPostBuildEventTool tool = (VCPostBuildEventTool)((IVCCollection)config.Tools).Item("VCPostBuildEventTool");
                string idc = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /idl " + idlFile + " -version " + version;
                string midl = "midl " + idlFile + " /tlb " + tblFile;
                string idc2 = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /tlb " + tblFile;
                string idc3 = "$(QTDIR)\\bin\\idc.exe \"$(TargetPath)\" /regserver";

                tool.CommandLine = idc + "\r\n" + midl + "\r\n" + idc2 + "\r\n" + idc3;
                tool.Description = "";
                
                VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
                VCLibrarianTool librarian = (VCLibrarianTool)((IVCCollection)config.Tools).Item("VCLibrarianTool");

                if (linker != null)
                {
                    linker.Version = version;
                    linker.ModuleDefinitionFile = envPro.Name + ".def";
                }
                else
                {
                    librarian.ModuleDefinitionFile = envPro.Name + ".def";
                }
            }
        }
                
        private void UpdateCompilerIncludePaths(string oldDir, string newDir)
        {
            string fixedOldDir = FixFilePathForComparison(oldDir);
            string[] dirs = new string[] {
                FixFilePathForComparison(QtVSIPSettings.GetUicDirectory(envPro)),
                FixFilePathForComparison(QtVSIPSettings.GetMocDirectory(envPro)),
                FixFilePathForComparison(QtVSIPSettings.GetRccDirectory(envPro))};

            bool oldDirIsUsed = false;
            foreach (string dir in dirs)
            {
                if (dir == fixedOldDir)
                {
                    oldDirIsUsed = true;
                    break;
                }
            }

            List<string> incList = new List<string>();
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations) 
            {
                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                if (compiler == null)
                    continue;
                fixedOldDir = FixFilePathForComparison(oldDir);
                string additionalIncludeDirs = compiler.GetAdditionalIncludeDirectories();
                if (additionalIncludeDirs == null)
                    continue;
                string[] paths = compiler.GetAdditionalIncludeDirectories().Split(new char[] { ',', ';' });
                string newPath = "";                
                incList.Clear();

                if (!oldDirIsUsed)
                {
                    // remove old path
                    foreach (string path in paths)
                    {
                        if (FixFilePathForComparison(path) != fixedOldDir)
                            newPath += ";" + path;
                    }
                    if (newPath.StartsWith(";"))
                        newPath = newPath.Substring(1);
                    compiler.SetAdditionalIncludeDirectories(newPath);
                    paths = compiler.GetAdditionalIncludeDirectories().Split(new char[] { ',', ';' });
                    newPath = ""; 
                }

                foreach (string path in paths) {
                    string tmp = HelperFunctions.NormalizeRelativeFilePath(path);
                    if (tmp.Length > 0 && !incList.Contains(tmp.ToLower()))
                    {                           
                        newPath += ";" + tmp;
                        incList.Add(tmp.ToLower());
                    }
                }
                if (!incList.Contains(FixFilePathForComparison(newDir)))
                    newPath += ";" + HelperFunctions.NormalizeRelativeFilePath(newDir);
                
                if (newPath.StartsWith(";"))
                    newPath = newPath.Substring(1);

                compiler.SetAdditionalIncludeDirectories(newPath);
            }
        }

        private static string FixFilePathForComparison(string path)
        {
            path = HelperFunctions.NormalizeRelativeFilePath(path);
            return path.ToLower();
        }

        public void UpdateUicSteps(string oldUicDir)
        {
            Messages.PaneMessage(dte, "\r\n=== Update uic steps ===");
            VCFilter vcFilter = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (vcFilter != null) 
            {
                IVCCollection filterFiles = (IVCCollection)vcFilter.Files;
                for (int i = filterFiles.Count; i > 0; i--)
                {
                    VCFile file = (VCFile)filterFiles.Item(i);
                    if (file.Name.ToLower().StartsWith("ui_")) 
                    {
                        RemoveFileFromFilter(file, vcFilter);
                        HelperFunctions.DeleteEmptyParentDirs(file);
                    }
                }
            }

            int updatedFiles = 0;
            int j = 0;            

            VCFile[] files = new VCFile[((IVCCollection)vcPro.Files).Count];            
            foreach (VCFile file in (IVCCollection)vcPro.Files)      
            {
                files[j++] = file;
            }
            
            foreach (VCFile file in files)
            {
                if (file.Name.EndsWith(".ui") && !IsUic3File(file))
                {
                    AddUic4BuildStep(file.FullPath);
                    Messages.PaneMessage(dte, "Update uic step for " + file.Name + ".");
                    ++updatedFiles;
                }
            }
            UpdateCompilerIncludePaths(oldUicDir, QtVSIPSettings.GetUicDirectory(envPro));

            Messages.PaneMessage(dte, "\r\n=== " + updatedFiles.ToString()
                + " uic steps updated. ===\r\n");
        }

        private static bool IsUic3File(VCFile file)
        {
            foreach (VCFileConfiguration config in (IVCCollection)file.FileConfigurations)
            {
                VCCustomBuildTool tool = HelperFunctions.GetCustomBuildTool(config);
                if (tool == null)
                    return false;
                if (tool.CommandLine.IndexOf("uic3.exe") > -1)
                    return true;
            }
            return false;
        }

        public bool UsePrecompiledHeaders(VCConfiguration config)
        {
            CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
            return UsePrecompiledHeaders(compiler);
        }

        private bool UsePrecompiledHeaders(CompilerToolWrapper compiler)
        {
            try
            {
                compiler.SetUsePrecompiledHeader(pchOption.pchUseUsingSpecific);
                string pcHeaderThrough = GetPrecompiledHeaderThrough();
                if (string.IsNullOrEmpty(pcHeaderThrough))
                    pcHeaderThrough = "stdafx.h";
                compiler.SetPrecompiledHeaderThrough(pcHeaderThrough);
                string pcHeaderFile = GetPrecompiledHeaderFile();
                if (string.IsNullOrEmpty(pcHeaderFile))
                    pcHeaderFile = ".\\$(ConfigurationName)/"
                    + Project.Name + ".pch";
                compiler.SetPrecompiledHeaderFile(pcHeaderFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UsesPrecompiledHeaders()
        {
            foreach (VCConfiguration config in vcPro.Configurations as IVCCollection)
            {
                if (!UsesPrecompiledHeaders(config))
                    return false;
            }
            return true;
        }

        public static bool UsesPrecompiledHeaders(VCConfiguration config)
        {
            CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
            return UsesPrecompiledHeaders(compiler);
        }

        private static bool UsesPrecompiledHeaders(CompilerToolWrapper compiler)
        {
            try
            {
                if (compiler.GetUsePrecompiledHeader() != pchOption.pchNone)
                    return true;
            }
            catch { }
            return false;
        }

        public string GetPrecompiledHeaderThrough()
        {
            foreach (VCConfiguration config in vcPro.Configurations as IVCCollection)
            {
                string header = GetPrecompiledHeaderThrough(config);
                if (header != null)
                    return header;
            }
            return null;
        }

        public static string GetPrecompiledHeaderThrough(VCConfiguration config)
        {
            CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
            return GetPrecompiledHeaderThrough(compiler);
        }

        private static string GetPrecompiledHeaderThrough(CompilerToolWrapper compiler)
        {
            try
            {
                string header = compiler.GetPrecompiledHeaderThrough();
                if (!string.IsNullOrEmpty(header))
                    return header;
            }
            catch { }
            return null;
        }

        public string GetPrecompiledHeaderFile()
        {
            foreach (VCConfiguration config in vcPro.Configurations as IVCCollection)
            {
                string file = GetPrecompiledHeaderFile(config);
                if (!string.IsNullOrEmpty(file))
                    return file;
            }
            return null;
        }

        public static string GetPrecompiledHeaderFile(VCConfiguration config)
        {
            CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
            return GetPrecompiledHeaderFile(compiler);
        }

        private static string GetPrecompiledHeaderFile(CompilerToolWrapper compiler)
        {
            try
            {
                string file = compiler.GetPrecompiledHeaderFile();
                if (!string.IsNullOrEmpty(file))
                    return file;
            }
            catch { }
            return null;
        }

        public static void SetPCHOption(VCFile vcFile, pchOption option)
        {
            foreach (VCFileConfiguration config in vcFile.FileConfigurations as IVCCollection)
            {
                CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
                compiler.SetUsePrecompiledHeader(option);
            }
        }

        private static VCFileConfiguration GetVCFileConfigurationByName(VCFile file, string configName)
        {
            foreach (VCFileConfiguration cfg in (IVCCollection)file.FileConfigurations)
            {
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
                || QtVSIPSettings.HasDifferentMocFilePerPlatform(envPro))
            {
                VCConfiguration projectConfig = (VCConfiguration)fileConfig.ProjectConfiguration;
                string configName = projectConfig.ConfigurationName;
                string platformName = ((VCPlatform)projectConfig.Platform).Name;
                VCFilter generatedFiles = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
                if (generatedFiles == null)
                    return null;
                foreach (VCFilter filt in (IVCCollection)generatedFiles.Filters)
                    if (filt.Name == configName + "_" + platformName ||
                        filt.Name == configName || filt.Name == platformName)
                        foreach (VCFile filtFile in (IVCCollection)filt.Files)
                            if (filtFile.FullPath.EndsWith(fileName))
                                return filtFile;

                //If a project from the an AddIn prior to 1.1.0 was loaded, the generated files are located directly
                //in the generated files filter.
                string relativeMocPath = QtVSIPSettings.GetMocDirectory(envPro, configName, platformName) + '\\' + fileName;
                //Remove .\ at the beginning of the mocPath
                if (relativeMocPath.StartsWith(".\\"))
                    relativeMocPath = relativeMocPath.Remove(0, 2);
                foreach (VCFile filtFile in (IVCCollection)generatedFiles.Files)
                    if (filtFile.FullPath.EndsWith(relativeMocPath, StringComparison.OrdinalIgnoreCase))
                        return filtFile;
            }
            else
            {
                VCFilter generatedFiles = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
                foreach (VCFile filtFile in (IVCCollection)generatedFiles.Files)
                    if (filtFile.FullPath.EndsWith('\\' + fileName))
                        return filtFile;
            }
            return null;
        }

        public void RefreshMocSteps()
        {
            foreach (VCFile vcfile in (IVCCollection)vcPro.Files)
            {
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
            bool isHeaderFile = HelperFunctions.HasHeaderFileExtension(vcfile.FullPath);
            if (!isHeaderFile && !HelperFunctions.HasSourceFileExtension(vcfile.FullPath))
                return;

            if (mocCmdChecker == null)
                mocCmdChecker = new MocCmdChecker();

            foreach (VCFileConfiguration config in (IVCCollection)vcfile.FileConfigurations)
            {
                try
                {
                    VCCustomBuildTool tool = null;
                    VCFile mocable = null;
                    if (isHeaderFile)
                    {
                        mocable = vcfile;
                        tool = HelperFunctions.GetCustomBuildTool(config);
                    }
                    else
                    {
                        string mocFileName = GetMocFileName(vcfile.FullPath);
                        VCFile mocFile = GetGeneratedMocFile(mocFileName, config);
                        if (mocFile != null)
                        {
                            VCFileConfiguration mocFileConfig = GetVCFileConfigurationByName(mocFile, config.Name);
                            tool = HelperFunctions.GetCustomBuildTool(mocFileConfig);
                            mocable = mocFile;
                        }
                        // It is possible that the function was called from a source file's property change, it is possible that
                        // we have to obtain the tool from the according header file
                        if (tool == null && singleFile)
                        {
                            string headerName = vcfile.FullPath.Remove(vcfile.FullPath.LastIndexOf('.')) + ".h";
                            mocFileName = GetMocFileName(headerName);
                            mocFile = GetGeneratedMocFile(mocFileName, config);
                            if (mocFile != null)
                            {
                                mocable = GetFileFromProject(headerName);
                                VCFileConfiguration customBuildConfig = GetVCFileConfigurationByName(mocable, config.Name);
                                tool = HelperFunctions.GetCustomBuildTool(customBuildConfig);
                            }
                        }
                    }
                    if (tool == null  || tool.CommandLine.ToLower().IndexOf("moc.exe") == -1)
                        continue;
                        
                    VCFile srcMocFile = GetSourceFileForMocStep(mocable);
                    VCFile cppFile = GetCppFileForMocStep(mocable);
                    if (srcMocFile == null)
                        continue;
                    bool mocableIsCPP = (srcMocFile == cppFile);

                    string pchParameters = null;
                    VCFileConfiguration cppConfig = GetVCFileConfigurationByName(cppFile, config.Name);
                    CompilerToolWrapper compiler = CompilerToolWrapper.Create(cppConfig);
                    if (compiler.GetUsePrecompiledHeader() != pchOption.pchNone)
                        pchParameters = GetPCHMocOptions(srcMocFile, compiler);

                    string outputFileName = QtVSIPSettings.GetMocDirectory(envPro) + "\\";
                    if (mocableIsCPP)
                    {
                        outputFileName += ProjectMacros.Name;
                        outputFileName += ".moc";
                    }
                    else
                    {
                        outputFileName += "moc_";
                        outputFileName += ProjectMacros.Name;
                        outputFileName += ".cpp";
                    }

                    string newCmdLine = mocCmdChecker.NewCmdLine(tool.CommandLine,
                        GetIncludes(cppConfig),
                        GetDefines(cppConfig),
                        QtVSIPSettings.GetMocOptions(envPro), srcMocFile.RelativePath,
                        pchParameters,
                        outputFileName);

                    // The tool's command line automatically gets a trailing "\r\n".
                    // We have to remove it to make the check below work.
                    string origCommandLine = tool.CommandLine;
                    if (origCommandLine.EndsWith("\r\n"))
                        origCommandLine = origCommandLine.Substring(0, origCommandLine.Length - 2);

                    if (newCmdLine != null && newCmdLine != origCommandLine)
                    {
                        // We have to delete the old moc file in order to trigger custom build step.
                        string configName = config.Name.Remove(config.Name.IndexOf("|"));
                        string platformName = config.Name.Substring(config.Name.IndexOf("|") + 1);
                        string projectPath = envPro.FullName.Remove(envPro.FullName.LastIndexOf('\\'));
                        string mocRelPath = GetRelativeMocFilePath(srcMocFile.FullPath, configName, platformName);
                        string mocPath = Path.Combine(projectPath, mocRelPath);
                        if (File.Exists(mocPath))
                            File.Delete(mocPath);
                        tool.CommandLine = newCmdLine;
                    }
                }
                catch
                {
                    Messages.PaneMessage(dte, "ERROR: failed to refresh moc step for " + vcfile.ItemName);
                }
            }
        }

        public void OnExcludedFromBuildChanged(VCFile vcFile, VCFileConfiguration vcFileCfg)
        {
            // Update the ExcludedFromBuild flags of the mocced file
            // according to the ExcludedFromBuild flag of the mocable source file.
            string moccedFileName = GetMocFileName(vcFile.Name);
            if (string.IsNullOrEmpty(moccedFileName))
                return;

            VCFile moccedFile = GetGeneratedMocFile(moccedFileName, vcFileCfg);

            if (moccedFile != null)
            {
                VCFile cppFile = null;
                if (HelperFunctions.HasHeaderFileExtension(vcFile.Name))
                    cppFile = GetCppFileForMocStep(vcFile);
            
                VCFileConfiguration moccedFileConfig = GetVCFileConfigurationByName(moccedFile, vcFileCfg.Name);
                if (moccedFileConfig != null)
                {
                    if (cppFile != null && IsMoccedFileIncluded(cppFile))
                    {
                        if (!moccedFileConfig.ExcludedFromBuild)
                        {
                            moccedFileConfig.ExcludedFromBuild = true;
                        }
                    }
                    else if (moccedFileConfig.ExcludedFromBuild != vcFileCfg.ExcludedFromBuild)
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
            if (fileName.ToLower().EndsWith(".moc")) 
            {
                fileName = fileName.Substring(0, fileName.Length - 4) + ".cpp";
                if (fileName.Length > 0) 
                {
                    foreach (VCFile f in (IVCCollection)vcPro.Files)
                    {
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
            if (fileName != null && fileName.Length > 0)
            {
                foreach (VCFile f in (IVCCollection)vcPro.Files)
                {
                    if (f.FullPath.ToLower().EndsWith("\\" + fileName.ToLower())) 
                        return f;
                }
            }
            return null;
        }

        public void UpdateMocSteps(string oldMocDir)
        {
            Messages.PaneMessage(dte, "\r\n=== Update moc steps ===");
            List<VCFile> orgFiles = new List<VCFile>();
            List<string> abandonedMocFiles = new List<string>();
            VCFilter vcFilter = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (vcFilter != null)
            {
                List<VCFile> generatedFiles = GetAllFilesFromFilter(vcFilter);
                for (int i = generatedFiles.Count - 1; i >= 0; i--)
                {
                    VCFile file = generatedFiles[i];
                    string fileName = null;
                    if (file.Name.ToLower().StartsWith("moc_"))
                    {
                        fileName = file.Name.Substring(4, file.Name.Length - 8) + ".h";
                    }
                    else if (file.Name.ToLower().EndsWith(".moc"))
                    {
                        fileName = file.Name.Substring(0, file.Name.Length - 4) + ".cpp";
                    }
                    if (fileName != null)
                    {
                        bool found = false;
                        foreach (VCFile f in (IVCCollection)vcPro.Files)
                        {
                            if (f.FullPath.ToLower().EndsWith("\\" + fileName.ToLower()))
                            {
                                if (!orgFiles.Contains(f) && HasMocStep(f, oldMocDir))
                                    orgFiles.Add(f);
                                RemoveFileFromFilter(file, vcFilter);
                                HelperFunctions.DeleteEmptyParentDirs(file);
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            // We can't find foo.h for moc_foo.cpp or 
                            // we can't find foo.cpp for foo.moc, thus we put the
                            // filename moc_foo.cpp / foo.moc into an error list.
                            abandonedMocFiles.Add(file.Name);
                        }
                    }
                }                
            }

            UpdateCompilerIncludePaths(oldMocDir, QtVSIPSettings.GetMocDirectory(envPro));
            foreach (VCFile file in orgFiles) 
            {
                try
                {
                    RemoveMocStep(file);
                    AddMocStep(file);
                }
                catch (Qt4VS2003Exception e)
                {
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
            List<KeyValuePair<SolutionContext, bool>> backup = new List<KeyValuePair<SolutionContext, bool>>();
            foreach (SolutionConfiguration config in solutionConfigs)
            {
                foreach (SolutionContext context in config.SolutionContexts)
                {
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
            IVCCollection subFilters = filter.Filters as IVCCollection;
            if (subFilters == null)
                return;

            for (int i = subFilters.Count; i > 0; i--)
            {
                VCFilter subFilter = subFilters.Item(i)as VCFilter;
                IVCCollection subFilterFilters = subFilter.Filters as IVCCollection;
                if (subFilterFilters == null)
                    continue;

                CleanupFilter(subFilter);

                bool filterOrFileFound = false;
                foreach (object itemObject in subFilter.Items as IVCCollection)
                {
                    if (itemObject is VCFilter || itemObject is VCFile)
                    {
                        filterOrFileFound = true;
                        break;
                    }
                }
                if (!filterOrFileFound)
                {
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
            QtVersionManager versionManager = QtVersionManager.The();
            VersionInformation viOld = versionManager.GetVersionInfo(oldVersion);
            VersionInformation viNew = versionManager.GetVersionInfo(newVersion);

            string vsPlatformNameOld = null;
            if (viOld != null)
                vsPlatformNameOld = viOld.GetVSPlatformName();
            string vsPlatformNameNew = viNew.GetVSPlatformName();
            bool bRefreshMocSteps = (vsPlatformNameNew != vsPlatformNameOld);

            try
            {
                if (vsPlatformNameOld != vsPlatformNameNew)
                {
                    if (!SelectSolutionPlatform(vsPlatformNameNew) || !HasPlatform(vsPlatformNameNew))
                    {
                        CreatePlatform(vsPlatformNameOld, vsPlatformNameNew, viOld, viNew, ref newProjectCreated);
                        bRefreshMocSteps = false;
                        UpdateMocSteps(QtVSIPSettings.GetMocDirectory(envPro));
                    }
                }
                ConfigurationManager configManager = envPro.ConfigurationManager;
                if (configManager.ActiveConfiguration.PlatformName != vsPlatformNameNew)
                {
                    envPro.Save(null);
                    dte.Solution.Remove(envPro);
                    envPro = dte.Solution.AddFromFile(envPro.FullName, false);
                }
            }
            catch
            {
                return false;
            }

            // We have to delete the generated files because of 
            // major differences between the platforms or Qt-Versions.
            if (vsPlatformNameOld != vsPlatformNameNew || viOld.qtPatch != viNew.qtPatch
                || viOld.qtMinor != viNew.qtMinor || viOld.qtMajor != viNew.qtMajor)
            {
                DeleteGeneratedFiles();
                Clean();
            }

            if (bRefreshMocSteps)
                RefreshMocSteps();

#if VS2010
            UpdateQtDirPropertySheet(viNew.qtDir);
#endif
            HelperFunctions.SetEnvironmentVariable("QTDIR", viNew.qtDir);

            UpdateModules(viOld, viNew);
            return true;
        }

        public bool HasPlatform(string platformName)
        {
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                VCPlatform platform = (VCPlatform)config.Platform;
                if (platform.Name == platformName)
                {
                    return true;
                }
            }
            return false;
        }

        public bool SelectSolutionPlatform(string platformName)
        {
            foreach (SolutionConfiguration solutionCfg in dte.Solution.SolutionBuild.SolutionConfigurations)
            {
                SolutionContexts contexts = solutionCfg.SolutionContexts;
                for (int i = 1; i <= contexts.Count; ++i)
                {
                    SolutionContext ctx = null;
                    try
                    {
                        ctx = contexts.Item(i);
                    }
                    catch (System.ArgumentException)
                    {
                        // This may happen if we encounter an unloaded project.
                        continue;
                    }

                    if (ctx.PlatformName == platformName
                        && solutionCfg.Name == dte.Solution.SolutionBuild.ActiveConfiguration.Name)
                    {
                        solutionCfg.Activate();
                        return true;
                    }
                }
            }

            return false;
        }

        public void RemovePlatform(string platformName)
        {
            try
            {
                ConfigurationManager cfgMgr = envPro.ConfigurationManager;
                cfgMgr.DeletePlatform(platformName);
            }
            catch { }
        }

        public void CreatePlatform(string oldPlatform, string newPlatform,
                                   VersionInformation viOld, VersionInformation viNew, ref bool newProjectCreated)
        {
            try
            {
                ConfigurationManager cfgMgr = envPro.ConfigurationManager;
                cfgMgr.AddPlatform(newPlatform, oldPlatform, true);
                vcPro.AddPlatform(newPlatform);
                newProjectCreated = false;
            }
            catch
            {
                // That stupid ConfigurationManager can't handle platform names
                // containing dots (e.g. "Windows Mobile 5.0 Pocket PC SDK (ARMV4I)")
                // So we have to do it the nasty way...
                string projectFileName = envPro.FullName;
                envPro.Save(null);
                dte.Solution.Remove(envPro);
                AddPlatformToVCProj(projectFileName, oldPlatform, newPlatform);
                envPro = dte.Solution.AddFromFile(projectFileName, false);
                vcPro = (VCProject)envPro.Object;
                newProjectCreated = true;
            }

            // update the platform settings
            foreach (VCConfiguration config in (IVCCollection)vcPro.Configurations)
            {
                VCPlatform vcplatform = (VCPlatform)config.Platform;
                if (vcplatform.Name == newPlatform)
                {
                    if (viOld != null)
                        RemovePlatformDependencies(config, viOld);
                    SetupConfiguration(config, viNew);
                }
            }

            SelectSolutionPlatform(newPlatform);
        }

        public static void RemovePlatformDependencies(VCConfiguration config, VersionInformation viOld)
        {
            CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
            SimpleSet minuend = new SimpleSet(compiler.GetPreprocessorDefinitions().Split(new char[] { ',' }));
            SimpleSet subtrahend = new SimpleSet(viOld.GetQMakeConfEntry("DEFINES").Split(new char[] { ' ', '\t' }));
            compiler.SetPreprocessorDefinitions(minuend.Minus(subtrahend).JoinElements(','));
        }

        public void SetupConfiguration(VCConfiguration config, VersionInformation viNew)
        {
            bool isWinPlatform = (!viNew.IsWinCEVersion());

            CompilerToolWrapper compiler = CompilerToolWrapper.Create(config);
            SimpleSet ppdefs = new SimpleSet(compiler.GetPreprocessorDefinitions().Split(new char[] { ',' }));
            ICollection newPPDefs = viNew.GetQMakeConfEntry("DEFINES").Split(new char[] { ' ', '\t' });
            compiler.SetPreprocessorDefinitions(ppdefs.Union(newPPDefs).JoinElements(','));

#if ENABLE_WINCE
            // search prepocessor definitions for Qt modules and add deployment settings
            if (!isWinPlatform)
            {
                DeploymentToolWrapper deploymentTool = DeploymentToolWrapper.Create(config);
                if (deploymentTool != null)
                {
                    deploymentTool.Clear();
                    deploymentTool.AddWinCEMSVCStandardLib(IsDebugConfiguration(config), dte);

                    List<QtModuleInfo> availableQtModules = QtModules.Instance.GetAvailableModuleInformation();
                    foreach (string s in ppdefs.Elements)
                    {
                        foreach (QtModuleInfo moduleInfo in availableQtModules)
                        {
                            if (moduleInfo.Defines.Contains(s))
                                AddDeploySettings(deploymentTool, moduleInfo.ModuleId, config, null, viNew);
                        }
                    }
                }
            }
#endif

            VCLinkerTool linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");
            if (linker == null)
                return;

            if (isWinPlatform)
                linker.SubSystem = subSystemOption.subSystemWindows;
            else
                linker.SubSystem = subSystemOption.subSystemNotSet;

            SetTargetMachine(linker, viNew);
        }

#if ENABLE_WINCE
        private void AddDeploySettings(DeploymentToolWrapper deploymentTool, QtModule module,
                                       VCConfiguration config, QtModuleInfo moduleInfo,
                                       VersionInformation versionInfo)
        {
            // for static Qt builds it doesn't make sense 
            // to add deployment settings for Qt modules
            if (versionInfo.IsStaticBuild())
                return;

            if (moduleInfo == null)
                moduleInfo = QtModules.Instance.ModuleInformation(module);

            if (moduleInfo == null || !moduleInfo.HasDLL)
                return;

            if (deploymentTool == null)
                deploymentTool = DeploymentToolWrapper.Create(config);
            if (deploymentTool == null)
                return;

            const string destDir = "%CSIDL_PROGRAM_FILES%\\$(ProjectName)";
            const string qtSrcDir = "$(QTDIR)\\lib";
            string filename = moduleInfo.GetDllFileName(IsDebugConfiguration(config));

            if (deploymentTool.GetAdditionalFiles().IndexOf(filename) < 0)
                deploymentTool.Add(filename, qtSrcDir, destDir);

            // add dependent modules
            foreach (QtModule dependentModule in moduleInfo.dependentModules)
                AddDeploySettings(deploymentTool, dependentModule, config, null, versionInfo);
        }

        private void RemoveDeploySettings(DeploymentToolWrapper deploymentTool, QtModule module,
                                       VCConfiguration config, QtModuleInfo moduleInfo)
        {
            if (moduleInfo == null)
                moduleInfo = QtModules.Instance.ModuleInformation(module);
            if (deploymentTool == null)
                deploymentTool = DeploymentToolWrapper.Create(config);
            if (deploymentTool == null)
                return;

            const string destDir = "%CSIDL_PROGRAM_FILES%\\$(ProjectName)";
            const string qtSrcDir = "$(QTDIR)\\lib";
            string filename = moduleInfo.GetDllFileName(IsDebugConfiguration(config));

            if (deploymentTool.GetAdditionalFiles().IndexOf(filename) >= 0)
                deploymentTool.Remove(filename, qtSrcDir, destDir);

            // remove dependent modules
            foreach (QtModule dependentModule in moduleInfo.dependentModules)
            {
                if (!HasModule(dependentModule))
                    RemoveDeploySettings(deploymentTool, dependentModule, config, null);
            }
        }

        private static void RemoveQtDeploys(VCConfiguration config)
        {
            DeploymentToolWrapper deploymentTool = DeploymentToolWrapper.Create(config);
            if (deploymentTool == null)
                return;
            string additionalFiles = deploymentTool.GetAdditionalFiles();
            additionalFiles = Regex.Replace(additionalFiles, "Qt[^\\|]*\\|[^\\|]*\\|[^\\|]*\\|[^;^$]*[;$]{0,1}", "");
            if (additionalFiles.EndsWith(";"))
                additionalFiles = additionalFiles.Substring(0, additionalFiles.Length - 1);
            deploymentTool.SetAdditionalFiles(additionalFiles);
        }
#endif

        private void DeleteGeneratedFiles()
        {
            FakeFilter genFilter = Filters.GeneratedFiles();
            VCFilter genVCFilter = FindFilterFromGuid(genFilter.UniqueIdentifier);
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
            foreach (VCFile f in filter.Files as IVCCollection)
            {
                try
                {
                    FileInfo fi = new FileInfo(f.FullPath);
                    if (fi.Exists)
                        fi.Delete();
                    HelperFunctions.DeleteEmptyParentDirs(fi.Directory.ToString());
                }
                catch
                {
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
            FileInfo fi = new FileInfo(fileName);
            int lastIndex = fileName.LastIndexOf(fi.Extension);
            string baseName = fi.Name.Remove(lastIndex, fi.Extension.Length);
            string delName = null;
            if (HelperFunctions.HasHeaderFileExtension(fileName))
                delName = "moc_" + baseName + ".cpp";
            else if (HelperFunctions.HasSourceFileExtension(fileName) && !fileName.ToLower().StartsWith("moc_"))
                delName = baseName + ".moc";
            else if (fileName.ToLower().EndsWith(".ui"))
                delName = "ui_" + baseName + ".h";
            else if (fileName.ToLower().EndsWith(".qrc"))
                delName = "qrc_" + baseName + ".cpp";

            if (delName != null)
            {
                foreach (VCFile delFile in GetFilesFromProject(delName))
                    RemoveFileFromFilter(delFile, Filters.GeneratedFiles());
            }
        }

        public void RemoveResFilesFromGeneratedFilesFilter()
        {
            List<VCFile> filesToRemove = new List<VCFile>();
            VCFilter generatedFiles = FindFilterFromGuid(Filters.GeneratedFiles().UniqueIdentifier);
            if (generatedFiles == null)
                return;

            foreach (VCFile filtFile in (IVCCollection)generatedFiles.Files)
                if (filtFile.FullPath.ToLower().EndsWith(".res"))
                    filesToRemove.Add(filtFile);
            foreach (VCFile resFile in filesToRemove)
                resFile.Remove();
        }

        static private void AddPlatformToVCProj(string projectFileName, string oldPlatformName, string newPlatformName)
        {
            string tempFileName = Path.GetTempFileName();
            FileInfo fi = new FileInfo(projectFileName);
            fi.CopyTo(tempFileName, true);

            XmlDocument myXmlDocument = new XmlDocument();
            myXmlDocument.Load(tempFileName);
            AddPlatformToVCProj(myXmlDocument, oldPlatformName, newPlatformName);
            myXmlDocument.Save(projectFileName);

            fi = new FileInfo(tempFileName);
            fi.Delete();
        }

        static private void AddPlatformToVCProj(XmlDocument doc, string oldPlatformName, string newPlatformName)
        {
            XmlNode vsProj = doc.DocumentElement.SelectSingleNode("/VisualStudioProject");
            XmlNode platforms = vsProj.SelectSingleNode("Platforms");
            if (platforms == null)
            {
                platforms = doc.CreateElement("Platforms");
                vsProj.AppendChild(platforms);
            }
            XmlNode platform = platforms.SelectSingleNode("Platform[@Name='" + newPlatformName + "']");
            if (platform == null)
            {
                platform = doc.CreateElement("Platform");
                ((XmlElement)platform).SetAttribute("Name", newPlatformName);
                platforms.AppendChild(platform);
            }

            XmlNode configurations = vsProj.SelectSingleNode("Configurations");
            XmlNodeList cfgList = configurations.SelectNodes("Configuration[@Name='Debug|" + oldPlatformName + "'] | " +
                                                             "Configuration[@Name='Release|" + oldPlatformName + "']");
            foreach (XmlNode oldCfg in cfgList)
            {
                XmlElement newCfg = (XmlElement)oldCfg.Clone();
                newCfg.SetAttribute("Name", oldCfg.Attributes["Name"].Value.Replace(oldPlatformName, newPlatformName));
                configurations.AppendChild(newCfg);
            }

            const string fileCfgPath = "Files/Filter/File/FileConfiguration";
            XmlNodeList fileCfgList = vsProj.SelectNodes(fileCfgPath + "[@Name='Debug|" + oldPlatformName + "'] | " +
                                                         fileCfgPath + "[@Name='Release|" + oldPlatformName + "']");
            foreach (XmlNode oldCfg in fileCfgList)
            {
                XmlElement newCfg = (XmlElement)oldCfg.Clone();
                newCfg.SetAttribute("Name", oldCfg.Attributes["Name"].Value.Replace(oldPlatformName, newPlatformName));
                oldCfg.ParentNode.AppendChild(newCfg);
            }
        }

        static private void SetTargetMachine(VCLinkerTool linker, VersionInformation versionInfo)
        {
            String qMakeLFlagsWindows = versionInfo.GetQMakeConfEntry("QMAKE_LFLAGS_WINDOWS");
            Regex rex = new Regex("/MACHINE:(\\S+)");
            Match match = rex.Match(qMakeLFlagsWindows);
            if (match.Success)
            {
                linker.TargetMachine = HelperFunctions.TranslateMachineType(match.Groups[1].Value);
            }
            else
            {
                string platformName = versionInfo.GetVSPlatformName();
                if (platformName == "Win32")
                    linker.TargetMachine = machineTypeOption.machineX86;
                else if (platformName == "x64")
                    linker.TargetMachine = machineTypeOption.machineAMD64;
                else
                    linker.TargetMachine = machineTypeOption.machineNotSet;
            }

            String subsystemOption = "";
            String linkerOptions = linker.AdditionalOptions;
            if (linkerOptions == null)
                linkerOptions = "";

            rex = new Regex("(/SUBSYSTEM:\\S+)");
            match = rex.Match(qMakeLFlagsWindows);
            if (match.Success)
                subsystemOption = match.Groups[1].Value;

            match = rex.Match(linkerOptions);
            if (match.Success)
            {
                linkerOptions = rex.Replace(linkerOptions, subsystemOption);
            }
            else
            {
                if (linkerOptions.Length > 0)
                    linkerOptions += " ";
                linkerOptions += subsystemOption;
            }
            linker.AdditionalOptions = linkerOptions;
        }

        public VCConfiguration GetActiveVCConfiguration()
        {
            string activeConfigName = Project.ConfigurationManager.ActiveConfiguration.ConfigurationName;
            return (VCConfiguration)((IVCCollection)VCProject.Configurations).Item(activeConfigName);
        }

        public void CollapseFilter(string filterName)
        {
            UIHierarchy solutionExplorer = (UIHierarchy)dte.Windows.Item(Constants.vsext_wk_SProjectWindow).Object;
            if (solutionExplorer.UIHierarchyItems.Count == 0)
                return;

            UIHierarchyItem projectItem = FindProjectHierarchyItem(solutionExplorer);
            if (projectItem != null)
            {
                projectItem.DTE.SuppressUI = true;

                HelperFunctions.CollapseFilter(projectItem, solutionExplorer, filterName);

                projectItem.DTE.SuppressUI = false;
            }
        }

        private UIHierarchyItem FindProjectHierarchyItem(UIHierarchy hierarchy)
        {
            if (hierarchy.UIHierarchyItems.Count == 0)
                return null;
            UIHierarchyItem rootItem = hierarchy.UIHierarchyItems.Item(1);
            return FindProjectHierarchyItem(rootItem);
        }

        private UIHierarchyItem FindProjectHierarchyItem(UIHierarchyItem root)
        {
            try
            {
                UIHierarchyItems items = root.UIHierarchyItems;
                for (int i = 1; i <= items.Count; i++)
                    if (items.Item(i).Name == envPro.Name)
                    {
                        return items.Item(i);
                    }
                    else if (items.Item(i).UIHierarchyItems.Count > 0)
                    {
                        UIHierarchyItem projectItem = FindProjectHierarchyItem(items.Item(i));
                        if (projectItem != null)
                            return projectItem;
                    }
            }
            catch
            {
            }
            return null;
        }

#if VS2010
        public void UpdateQtDirPropertySheet(string newQtDir)
        {
            string propSheetFileName = vcPro.ProjectDirectory + Resources.qtSheetKeyword + ".props";
            if (!File.Exists(propSheetFileName))
            {
                StreamWriter sw = new StreamWriter(propSheetFileName);
                sw.WriteLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                sw.WriteLine(@"<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">");
                sw.WriteLine(@"<ImportGroup Label=""PropertySheets"" />");
                sw.WriteLine(@"<PropertyGroup Label=""UserMacros"" />");
                sw.WriteLine(@"<PropertyGroup />");
                sw.WriteLine(@"<ItemDefinitionGroup />");
                sw.WriteLine(@"<ItemGroup />");
                sw.WriteLine(@"</Project>");
                sw.Close();
            }
            
            foreach (VCConfiguration vcConfig in vcPro.Configurations as IVCCollection)
            {
                VCPropertySheet qtDirSheet = null;
                IVCCollection sheets = vcConfig.PropertySheets as IVCCollection;
                foreach (VCPropertySheet sheet in sheets)
                {
                    if (sheet.Name == Resources.qtSheetKeyword)
                    {
                        qtDirSheet = sheet;
                        break;
                    }
                }
                if (qtDirSheet == null)
                {
                    try
                    {
                        qtDirSheet = vcConfig.AddPropertySheet(propSheetFileName);
                    }
                    catch (Exception e)
                    {
                        Messages.PaneMessage(dte, "Couldn't create property sheet. Exception: " + e.Message);
                    }
                }

                VCUserMacro qtDirMacro = null;
                foreach (VCUserMacro macro in qtDirSheet.UserMacros)
                {
                    if (macro.Name == "QTDIR")
                    {
                        qtDirMacro = macro;
                        break;
                    }
                }
                if (qtDirMacro == null)
                {
                    qtDirMacro = qtDirSheet.AddUserMacro("QTDIR", newQtDir);
                    qtDirMacro.PerformEnvironmentSet = true;
                    qtDirSheet.Save();
                }
                else if (qtDirMacro.Value != newQtDir)
                {
                    qtDirMacro.Value = newQtDir;
                    qtDirMacro.PerformEnvironmentSet = true;
                    qtDirSheet.Save();
                }
            }
        }
#endif
    }
}
