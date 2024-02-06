/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using Core;
    using Core.MsBuild;
    using QtVsTools.Common;
    using VisualStudio;
    using static QtVsTools.Common.EnumExt;
    using WhereConfig = Func<IWizardConfiguration, bool>;

    public interface IWizardConfiguration
    {
        string Name { get; }
        VersionInformation QtVersion { get; }
        string QtVersionName { get; }
        string QtVersionPath { get; }
        string Target { get; }
        string Platform { get; }
        bool IsDebug { get; }
        IEnumerable<string> Modules { get; }
    }

    public enum ProjectTargets
    {
        Windows,
        [String("Windows Store")] WindowsStore,
        [String("Linux (SSH)")] LinuxSSH,
        [String("Linux (WSL)")] LinuxWSL
    }

    public enum ProjectPlatforms
    {
        [String("x64")] X64,
        Win32,
        ARM64,
        ARM
    }

    public abstract partial class ProjectTemplateWizard : IWizard
    {
        LazyFactory Lazy { get; } = new();

        private readonly WhereConfig WhereConfig_SelectAll = x => true;

        protected struct ItemProperty
        {
            public string Key { get; }
            public string Value { get; }
            public WhereConfig WhereConfig { get; }

            public ItemProperty(string key, string value, WhereConfig whereConfig = null)
            {
                Key = key;
                Value = value;
                WhereConfig = whereConfig;
            }

            public static implicit operator ItemProperty[](ItemProperty that)
            {
                return new[] { that };
            }
        }

        protected class ItemGlobalDef
        {
            public string ItemType { get; set; }
            public ItemProperty[] Properties { get; set; }
        }

        protected class ItemDef
        {
            public string ItemType { get; set; }
            public string Include { get; set; }
            public ItemProperty[] Properties { get; set; }
            public string Filter { get; set; }
            public WhereConfig WhereConfig { get; set; }
        }

        [Flags]
        protected enum Options : uint
        {
            Application = 0x000,
            DynamicLibrary = 0x001,
            StaticLibrary = 0x002,
            GUISystem = 0x004,
            ConsoleSystem = 0x008,
            PluginProject = 0x100
        }

        protected abstract Options TemplateType { get; }
        protected abstract WizardData WizardData { get; }
        protected abstract WizardWindow WizardWindow { get; }

        protected virtual IDictionary<string, ItemGlobalDef> ItemGlobals => null;
        protected virtual IEnumerable<ItemDef> ExtraItems => Enumerable.Empty<ItemDef>();
        protected virtual IEnumerable<string> ExtraModules => Enumerable.Empty<string>();
        protected virtual IEnumerable<string> ExtraDefines => Enumerable.Empty<string>();

        protected virtual IEnumerable<IWizardConfiguration> Configurations => WizardData.Configs;
        protected virtual bool UsePrecompiledHeaders => WizardData.UsePrecompiledHeader;

        private Dictionary<string, string> ParameterValues { get; set; }
        protected DTE Dte { get; private set; }

        protected virtual ItemDef PrecompiledHeader => Lazy.Get(() =>
            PrecompiledHeader, () => new ItemDef
            {
                ItemType = "ClInclude",
                Include = "stdafx.h",
                Filter = "Header Files"
            });

        protected virtual ItemDef PrecompiledHeaderSource => Lazy.Get(() =>
            PrecompiledHeaderSource, () => new ItemDef
            {
                ItemType = "ClCompile",
                Include = "stdafx.cpp",
                Properties = new ItemProperty("PrecompiledHeader", "Create"),
                Filter = "Source Files"
            });

        protected class TemplateParameters
        {
            public ProjectTemplateWizard Template { get; set; }

            string ParamKey(Enum param)
            {
                return $"${param.Cast<string>()}$";
            }

            public string this[Enum param]
            {
                get => Template.ParameterValues
                    .TryGetValue(ParamKey(param), out var value) ? value : string.Empty;
                set => Template.ParameterValues[ParamKey(param)] = value;
            }
        }

        protected enum NewProject
        {
            // Read-only parameters
            [String("projectname")] Name,
            [String("safeprojectname")] SafeName,
            [String("destinationdirectory")] DestinationDirectory,
            [String("solutiondirectory")] SolutionDirectory,

            // Custom parameters
            ToolsVersion,
            ProjectConfigurations,
            Properties,
            ProjectGuid,
            Keyword,
            Globals,
            Configurations,
            PropertySheets,
            QtSettings,
            BuildSettings,
            ProjectItems,
            FilterItems,
            ResourceFile
        }

        protected TemplateParameters Parameter => Lazy.Get(() =>
            Parameter, () => new TemplateParameters { Template = this });

        public virtual void ProjectItemFinishedGenerating(ProjectItem projectItem) { }
        public virtual void BeforeOpeningFile(ProjectItem projectItem) { }

        private void CleanupVcxProject()
        {
            try {
                var solutionDir = Path.GetFullPath(Parameter[NewProject.SolutionDirectory]);
                var projectDir = Path.GetFullPath(Parameter[NewProject.DestinationDirectory]);
                if (!Directory.Exists(solutionDir))
                    solutionDir = projectDir;

                var slnFiles = Directory.GetFiles(solutionDir, "*.sln");
                foreach (var slnFile in slnFiles) {
                    if (File.Exists(slnFile))
                        File.Delete(slnFile);
                }

                var dotVsDir = Path.Combine(solutionDir, ".vs");
                if (Directory.Exists(dotVsDir))
                    Directory.Delete(dotVsDir, recursive: true);

                var vcxProjFiles = Directory.GetFiles(projectDir, "*.vcxproj*");
                foreach (var vcxProjFile in vcxProjFiles) {
                    if (File.Exists(vcxProjFile))
                        File.Delete(vcxProjFile);
                }

                var qtVarsProFiles = Directory.GetFiles(projectDir, "qtvars.pro",
                    SearchOption.AllDirectories);
                foreach (string qtVarProFile in qtVarsProFiles) {
                    var projDirUri = new Uri(projectDir);
                    var proFileDirInfo = new DirectoryInfo(Path.GetDirectoryName(qtVarProFile));
                    while (new Uri(proFileDirInfo.Parent.FullName) != projDirUri)
                        proFileDirInfo = proFileDirInfo.Parent;
                    proFileDirInfo.Delete(recursive: true);
                }
            } catch (Exception e) {
                Messages.Log(e);
            }
        }

        public virtual void RunFinished()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (WizardData.ProjectModel == WizardData.ProjectModels.CMake) {
                Dte.Solution.Close();
                CleanupVcxProject();
                OpenCMakeProject();
            }
        }

        public virtual bool ShouldAddProjectItem(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (IsCMakeFile(fileName))
                return WizardData.ProjectModel == WizardData.ProjectModels.CMake;
            else if (WizardData.ProjectModel == WizardData.ProjectModels.CMake)
                return !HelperFunctions.IsQrcFile(filePath);
            return true;
        }

        protected virtual void BeforeWizardRun() { }
        protected virtual void BeforeTemplateExpansion() { }
        protected virtual void OnProjectGenerated(Project project) { }

        public virtual void RunStarted(
            object automationObject,
            Dictionary<string, string> parameterValues,
            WizardRunKind runKind,
            object[] customParams)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Dte = automationObject as DTE;
            ParameterValues = parameterValues;

            if (customParams?[0] is {} templatePath)
                ParameterValues["$templatepath$"] = templatePath.ToString();

            Debug.Assert(WizardWindow != null);

            BeforeWizardRun();

            if (VsServiceProvider.Instance == null) {
                Messages.DisplayErrorMessage(Environment.NewLine + "The Qt VS Tools extension has "
                    + "not been fully loaded yet; the wizard is not available.");
                throw new WizardBackoutException();
            }

            var iVsUIShell = VsServiceProvider.GetService<SVsUIShell, IVsUIShell>();
            if (iVsUIShell == null)
                throw new NullReferenceException("IVsUIShell");

            try {
                iVsUIShell.EnableModeless(0);
                iVsUIShell.GetDialogOwnerHwnd(out IntPtr hwnd);
                WindowHelper.ShowModal(WizardWindow, hwnd);
            } catch (Exception exception) {
                exception.Log(false, true);
                throw;
            } finally {
                iVsUIShell.EnableModeless(1);
            }
            if (!WizardWindow.DialogResult ?? false) {
                try {
                    Directory.Delete(Parameter[NewProject.DestinationDirectory]);
                    Directory.Delete(Parameter[NewProject.SolutionDirectory]);
                } catch { }
                throw new WizardBackoutException();
            }

            BeforeTemplateExpansion();
            Expand();
        }

        public virtual void ProjectFinishedGenerating(Project project)
        {
            OnProjectGenerated(project);
        }

        protected static bool IsLinux(IWizardConfiguration wizConfig)
        {
            return wizConfig.Target.EqualTo(ProjectTargets.LinuxSSH)
                || wizConfig.Target.EqualTo(ProjectTargets.LinuxWSL);
        }

        protected static string GetLinuxCompilerPath(IWizardConfiguration wizConfig)
        {
            if (!IsLinux(wizConfig))
                return string.Empty;
            if (string.IsNullOrEmpty(wizConfig.QtVersionPath))
                return string.Empty;
            string[] linuxPaths = wizConfig.QtVersionPath.Split(':');
            return linuxPaths.Length <= 2 ? string.Empty : linuxPaths[2];
        }

        protected virtual void Expand()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Debug.Assert(ParameterValues != null);
            Debug.Assert(Dte != null);
            Debug.Assert(Configurations != null);
            Debug.Assert(ExtraItems != null);

            ExpandMSBuild();
            if (WizardData.ProjectModel == WizardData.ProjectModels.CMake)
                ExpandCMake();
        }

        private void ExpandMSBuild()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            StringBuilder xml;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Tools version = VS version
            //
            Parameter[NewProject.ToolsVersion] = Dte.Version;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Configurations
            //
            xml = new StringBuilder();
            foreach (IWizardConfiguration c in Configurations) {
                xml.AppendLine(string.Format(@"
    <ProjectConfiguration Include=""{0}|{1}"">
      <Configuration>{0}</Configuration>
      <Platform>{1}</Platform>
    </ProjectConfiguration>",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform));
            }
            Parameter[NewProject.ProjectConfigurations] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties
            //
            xml = new StringBuilder();
            foreach (IWizardConfiguration c in Configurations) {
                xml.AppendLine(string.Format(@"
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform));
                if (IsLinux(c)) {
                    var compilerPath = GetLinuxCompilerPath(c);
                    if (!string.IsNullOrEmpty(compilerPath))
                        xml.AppendLine(string.Format(@"
      <RemoteCCompileToolExe>{0}</RemoteCCompileToolExe>
      <RemoteCppCompileToolExe>{0}</RemoteCppCompileToolExe>
      <RemoteLdToolExe>{0}</RemoteLdToolExe>",
                            /*{0}*/ compilerPath));
                }
                xml.AppendLine(@"
  </PropertyGroup>");
            }
            Parameter[NewProject.Properties] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Globals
            //
            xml = new StringBuilder();
            Parameter[NewProject.ProjectGuid] =  $"{{{Guid.NewGuid().ToString().ToUpper()}}}";
            Parameter[NewProject.Keyword] = MsBuildProjectFormat.QtVsVersionTag;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Globals: Windows
            //
            foreach (IWizardConfiguration c in Configurations
                .Where(c => c.Target.EqualTo(ProjectTargets.Windows))) {
                    xml.AppendLine(string.Format(@"
    <WindowsTargetPlatformVersion Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">{2}</WindowsTargetPlatformVersion>",
                        /*{0}*/ c.Name,
                        /*{1}*/ c.Platform,
                        /*{2}*/ BuildConfig.WindowsTargetPlatformVersion));
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Globals: Windows Store
            //
            foreach (IWizardConfiguration c in Configurations
                .Where(c => c.Target.EqualTo(ProjectTargets.WindowsStore))) {
                xml.AppendLine(string.Format(@"
    <ApplicationType Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">Windows Store</ApplicationType>
    <WindowsTargetPlatformVersion Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">{2}</WindowsTargetPlatformVersion>
    <WindowsTargetPlatformMinVersion Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">{3}</WindowsTargetPlatformMinVersion>
    <MinimumVisualStudioVersion Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">{4}</MinimumVisualStudioVersion>
    <ApplicationTypeRevision Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">{5}</ApplicationTypeRevision>
    <DefaultLanguage Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">en</DefaultLanguage>
    <AppContainerApplication Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">true</AppContainerApplication>",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform,
                    /*{2}*/ BuildConfig.WindowsTargetPlatformVersion,
                    /*{3}*/ "10.0.17134.0", // windows target platform min version
                    /*{4}*/ "15.0", // minimum Visual Studio version
                    /*{5}*/ "10.0")); // application type revision
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Globals: Linux
            //
            foreach (IWizardConfiguration c in Configurations.Where(IsLinux)) {
                xml.AppendLine(string.Format(@"
    <ApplicationType Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">Linux</ApplicationType>
    <ApplicationTypeRevision Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">1.0</ApplicationTypeRevision>
    <TargetLinuxPlatform Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">Generic</TargetLinuxPlatform>
    <LinuxProjectType Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">{{D51BCBC9-82E9-4017-911E-C93873C4EA2B}}</LinuxProjectType>",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform));
            }

            Parameter[NewProject.Globals] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // VC Configurations
            //
            xml = new StringBuilder();
            foreach (IWizardConfiguration c in Configurations) {
                if (!c.Target.TryCast(out ProjectTargets target))
                    continue;
                xml.AppendLine(string.Format(@"
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"" Label=""Configuration"">",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform));
                if (TemplateType.HasFlag(Options.DynamicLibrary)) {
                    xml.AppendLine(@"
    <ConfigurationType>DynamicLibrary</ConfigurationType>");
                } else if (TemplateType.HasFlag(Options.StaticLibrary)) {
                    xml.AppendLine(@"
    <ConfigurationType>StaticLibrary</ConfigurationType>");
                } else {
                    xml.AppendLine(@"
    <ConfigurationType>Application</ConfigurationType>");
                }
                switch (target) {
                case ProjectTargets.Windows:
                case ProjectTargets.WindowsStore:
                    xml.AppendLine(string.Format(@"
    <PlatformToolset>v{0}</PlatformToolset>",
                        /*{0}*/ BuildConfig.PlatformToolset));
                    break;
                case ProjectTargets.LinuxSSH:
                    xml.AppendLine(@"
    <PlatformToolset>Remote_GCC_1_0</PlatformToolset>");
                    break;
                case ProjectTargets.LinuxWSL:
                    xml.AppendLine(@"
    <PlatformToolset>WSL_1_0</PlatformToolset>");
                    break;
                }
                xml.AppendLine(string.Format(@"
    <UseDebugLibraries>{0}</UseDebugLibraries>",
                        /*{0}*/ c.IsDebug ? "true" : "false"));
                if (target == ProjectTargets.WindowsStore) {
                    xml.AppendLine(@"
    <GenerateManifest>false</GenerateManifest>
    <EmbedManifest>false</EmbedManifest>");
                }
                if (!c.IsDebug)
                    xml.AppendLine(@"
    <WholeProgramOptimization>true</WholeProgramOptimization>");
                xml.AppendLine(@"
    <CharacterSet>Unicode</CharacterSet>");
                xml.AppendLine(@"
  </PropertyGroup>");
            }
            Parameter[NewProject.Configurations] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Property sheets
            //
            xml = new StringBuilder();
            foreach (IWizardConfiguration c in Configurations) {
                xml.AppendLine(string.Format(@"
  <ImportGroup Label=""PropertySheets"" Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">
    <Import Project=""$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"" Condition=""exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')"" Label=""LocalAppDataPlatform"" />
    <Import Project=""$(QtMsBuild)\Qt.props"" />
  </ImportGroup>",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform));
            }
            Parameter[NewProject.PropertySheets] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Qt settings
            //
            xml = new StringBuilder();
            foreach (IWizardConfiguration c in Configurations) {
                xml.AppendLine(string.Format(@"
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"" Label=""QtSettings"">",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform));
                ExpandQtSettings(xml, c);
                xml.AppendLine(@"
  </PropertyGroup>");
            }
            Parameter[NewProject.QtSettings] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Build settings
            //
            var mocProperties = ItemGlobals?[QtMoc.ItemTypeName]?.Properties ?? new ItemProperty[] { };
            var clProperties = ItemGlobals?["ClCompile"]?.Properties ?? new ItemProperty[] { };
            var linkProperties = ItemGlobals?["Link"]?.Properties ?? new ItemProperty[] { };

            xml = new StringBuilder();
            foreach (IWizardConfiguration c in Configurations) {
                xml.AppendLine(string.Format(@"
  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"" Label=""Configuration"">",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform));


                ///////////////////////////////////////////////////////////////////////////////////
                // Build settings: C++ compiler
                //
                if (!IsLinux(c)) {
                    // Windows
                    xml.AppendLine(@"
    <ClCompile>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>");
                    if (c.Target.EqualTo(ProjectTargets.WindowsStore)) {
                        xml.AppendLine(@"
      <CompileAsWinRT>false</CompileAsWinRT>
      <PrecompiledHeader>NotUsing</PrecompiledHeader>
      <RuntimeTypeInfo>true</RuntimeTypeInfo>");
                    }
                    if (UsePrecompiledHeaders) {
                        xml.AppendLine(string.Format(@"
      <PrecompiledHeader>Use</PrecompiledHeader>
      <PrecompiledHeaderFile>{0}</PrecompiledHeaderFile>",
                            /*{0}*/ PrecompiledHeader.Include));
                    }
                    if (ExtraDefines?.Any() == true) {
                        xml.AppendLine(string.Format(@"
      <PreprocessorDefinitions>{0};%(PreprocessorDefinitions)</PreprocessorDefinitions>",
                            /*{0}*/ string.Join(";", ExtraDefines)));
                    }
                    foreach (ItemProperty p in clProperties) {
                        xml.AppendLine(string.Format(@"
      <{0}>{1}</{0}>",
                            /*{0}*/ p.Key,
                            /*{1}*/ p.Value));
                    }
                    xml.AppendLine(@"
      <WarningLevel>Level3</WarningLevel>
      <SDLCheck>true</SDLCheck>
      <ConformanceMode>true</ConformanceMode>");
                    if (!c.IsDebug) {
                        xml.AppendLine(@"
      <FunctionLevelLinking>true</FunctionLevelLinking>
      <IntrinsicFunctions>true</IntrinsicFunctions>");
                    }
                    xml.AppendLine(@"
    </ClCompile>");
                } else {
                    // Linux
                    xml.AppendLine(@"
    <ClCompile>
      <PositionIndependentCode>true</PositionIndependentCode>
    </ClCompile>");
                }

                ///////////////////////////////////////////////////////////////////////////////////
                // Build settings: Linker
                //
                if (!IsLinux(c)) {
                    // Windows
                    xml.AppendLine(string.Format(@"
    <Link>
      <SubSystem>{0}</SubSystem>
      <GenerateDebugInformation>{1}</GenerateDebugInformation>",
                        /*{0}*/ TemplateType.HasFlag(Options.ConsoleSystem) ? "Console" : "Windows",
                        /*{1}*/ c.IsDebug ? "true" : "false"));
                    if (c.Target.EqualTo(ProjectTargets.WindowsStore)) {
                        xml.AppendLine(string.Format(@"
      <AdditionalOptions>/APPCONTAINER %(AdditionalOptions)</AdditionalOptions>
      <GenerateManifest>false</GenerateManifest>
      <GenerateWindowsMetadata>false</GenerateWindowsMetadata>
      <TargetMachine>{0}</TargetMachine>",
                            /*{0}*/ c.QtVersion.VC_Link_TargetMachine));
                    }
                    foreach (ItemProperty p in linkProperties) {
                        xml.AppendLine(string.Format(@"
      <{0}>{1}</{0}>",
                            /*{0}*/ p.Key,
                            /*{1}*/ p.Value));
                    }
                    if (!c.IsDebug) {
                        xml.AppendLine(@"
      <EnableCOMDATFolding>true</EnableCOMDATFolding>
      <OptimizeReferences>true</OptimizeReferences>");
                    }
                    xml.AppendLine(@"
    </Link>");
                }

                ///////////////////////////////////////////////////////////////////////////////////
                // Build settings: moc
                //
                if (UsePrecompiledHeaders || mocProperties.Any()) {
                    xml.AppendLine(string.Format(@"
    <{0}>", QtMoc.ItemTypeName));
                    foreach (ItemProperty p in mocProperties) {
                        xml.AppendLine(string.Format(@"
      <{0}>{1}</{0}>",
                            /*{0}*/ p.Key,
                            /*{1}*/ p.Value));
                    }
                    if (UsePrecompiledHeaders) {
                        xml.AppendLine(string.Format(@"
      <{0}>{1};%({0})</{0}>",
                            /*{0}*/ QtMoc.Property.PrependInclude,
                            /*{1}*/ PrecompiledHeader.Include));
                    }
                    xml.AppendLine(string.Format(@"
    </{0}>", QtMoc.ItemTypeName));
                }

                ///////////////////////////////////////////////////////////////////////////////////
                // Build settings: remaining item types
                //
                if (ItemGlobals != null) {
                    foreach (string itemType in ItemGlobals.Keys
                        .Except(new[] { "ClCompile", "Link", QtMoc.ItemTypeName })) {
                        xml.AppendLine(string.Format(@"
    <{0}>",
                            /*{0}*/ itemType));
                        foreach (ItemProperty p in ItemGlobals[itemType].Properties) {
                            xml.AppendLine(string.Format(@"
      <{0}>{1}</{0}>",
                                /*{0}*/ p.Key,
                                /*{1}*/ p.Value));
                        }
                        xml.AppendLine(string.Format(@"
    </{0}>",
                            /*{0}*/ itemType));
                    }
                }
                xml.AppendLine(@"
  </ItemDefinitionGroup>");
            }

            Parameter[NewProject.BuildSettings] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Project items
            //
            IEnumerable<ItemDef> projectItems = ExtraItems
                .Where(item => item.WhereConfig == null
                    || Configurations.Where(item.WhereConfig).Any())
                .Union(UsePrecompiledHeaders
                    ? new[] { PrecompiledHeader, PrecompiledHeaderSource }
                    : Enumerable.Empty<ItemDef>()).ToList();

            xml = new StringBuilder();
            foreach (ItemDef item in projectItems) {
                bool itemHasProperties = item.WhereConfig != null || item.Properties != null;
                xml.Append(string.Format(@"
    <{0} Include=""{1}""{2}",
                    /*{0}*/ item.ItemType,
                    /*{1}*/ item.Include,
                    /*{2}*/ itemHasProperties ? ">" : " />"));

                if (item.Properties != null) {
                    foreach (ItemProperty property in item.Properties) {
                        IEnumerable<IWizardConfiguration> configs = Configurations
                            .Where(property.WhereConfig ?? WhereConfig_SelectAll);
                        foreach (IWizardConfiguration config in configs) {
                            xml.AppendLine(string.Format(@"
      <{0} Condition=""'$(Configuration)|$(Platform)' == '{1}|{2}'"">{3}</{0}>",
                                /*{0}*/ property.Key,
                                /*{1}*/ config.Name,
                                /*{2}*/ config.Platform,
                                /*{3}*/ property.Value));
                        }
                    }
                }

                if (item.WhereConfig != null) {
                    IEnumerable<IWizardConfiguration> excludedConfigs = Configurations
                        .Where(config => !item.WhereConfig(config));
                    foreach (var excludedConfig in excludedConfigs) {
                        xml.AppendLine(string.Format(@"
      <ExcludedFromBuild Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">true</ExcludedFromBuild>",
                            /*{0}*/ excludedConfig.Name,
                            /*{1}*/ excludedConfig.Platform));
                    }
                }

                if (itemHasProperties) {
                    xml.AppendLine(string.Format(@"
    </{0}>",
                        /*{0}*/ item.ItemType));
                }
            }
            Parameter[NewProject.ProjectItems] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Project items: filters
            //
            xml = new StringBuilder();
            foreach (ItemDef item in projectItems) {
                xml.Append(string.Format(@"
    <{0} Include=""{1}"">",
                    /*{0}*/ item.ItemType,
                    /*{1}*/ item.Include));
                xml.AppendLine(string.Format(@"
      <Filter>{0}</Filter>",
                    /*{0}*/ item.Filter));
                xml.AppendLine(string.Format(@"
    </{0}>",
                    /*{0}*/ item.ItemType));
            }
            Parameter[NewProject.FilterItems] = FormatParam(xml);
        }

        protected virtual void ExpandQtSettings(StringBuilder xml, IWizardConfiguration c)
        {
            xml.AppendLine($@"
    <QtInstall>{c.QtVersionName}</QtInstall>
    <QtModules>{string.Join(";", c.Modules.Union(ExtraModules))}</QtModules>
    <QtBuildConfig>{(c.IsDebug ? "debug" : "release")}</QtBuildConfig>");
            if (c.Target.EqualTo(ProjectTargets.WindowsStore)) {
                xml.AppendLine(@"
    <QtDeploy>true</QtDeploy>
    <QtDeployToProjectDir>true</QtDeployToProjectDir>
    <QtDeployVsContent>true</QtDeployVsContent>");
            }
        }

        // Matches empty lines; captures first newline
        static readonly Regex patternEmptyLines
            = new(@"(?:^|(?<FIRST_NL>\r\n))(?:\r\n)+(?![\r\n]|$)|(?:\r\n)+$");

        protected static string FormatParam(StringBuilder paramValue)
        {
            return FormatParam(paramValue.ToString());
        }

        protected static string FormatParam(string paramValue)
        {
            // Remove empty lines; replace with first newline (if any)
            return patternEmptyLines.Replace(paramValue, m => m.Groups["FIRST_NL"].Value);
        }
    }
}
