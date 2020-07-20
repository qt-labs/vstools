/****************************************************************************
**
** Copyright (C) 2020 The Qt Company Ltd.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;

using QtVsTools.Common;
using QtVsTools.VisualStudio;

using QtProjectLib;
using QtProjectLib.QtMsBuild;

namespace QtVsTools.Wizards.ProjectWizard
{
    using static EnumExt;
    using WhereConfig = Func<IWizardConfiguration, bool>;

    public interface IWizardConfiguration
    {
        string Name { get; }
        VersionInformation QtVersion { get; }
        string QtVersionName { get; }
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
        ARM,
    }

    public abstract class ProjectTemplateWizard : IWizard
    {
        protected readonly WhereConfig WhereConfig_SelectAll = (x => true);

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

            public static implicit operator ItemProperty[] (ItemProperty that)
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

        protected Dictionary<string, string> ParameterValues { get; private set; }
        protected EnvDTE.DTE Dte { get; private set; }

        ItemDef _PrecompiledHeaderFile;
        protected virtual ItemDef PrecompiledHeader => _PrecompiledHeaderFile
            ?? (_PrecompiledHeaderFile = new ItemDef
            {
                ItemType = "ClInclude",
                Include = "stdafx.h",
                Filter = "Header Files"
            });

        ItemDef _PrecompiledHeaderSourceFile;
        protected virtual ItemDef PrecompiledHeaderSource => _PrecompiledHeaderSourceFile
            ?? (_PrecompiledHeaderSourceFile = new ItemDef
            {
                ItemType = "ClCompile",
                Include = "stdafx.cpp",
                Properties = new ItemProperty("PrecompiledHeader", "Create"),
                Filter = "Source Files",
            });

        protected class TemplateParameters
        {
            public ProjectTemplateWizard Template { get; set; }

            string ParamKey(Enum param)
            {
                return string.Format("${0}$", param.Cast<string>());
            }

            public string this[Enum param]
            {
                get => Template.ParameterValues[ParamKey(param)];
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
        }

        TemplateParameters _Parameter;
        protected TemplateParameters Parameter => _Parameter
            ?? (_Parameter = new TemplateParameters { Template = this });

        protected QtVersionManager VersionManager => QtVersionManager.The();

        public virtual void ProjectItemFinishedGenerating(ProjectItem projectItem) { }
        public virtual void BeforeOpeningFile(ProjectItem projectItem) { }
        public virtual void RunFinished() { }
        public virtual bool ShouldAddProjectItem(string filePath) => true;

        protected virtual void BeforeWizardRun() { }
        protected virtual void BeforeTemplateExpansion() { }
        protected virtual void OnProjectGenerated(Project project) { }

        public virtual void RunStarted(
            object automationObject,
            Dictionary<string, string> parameterValues,
            WizardRunKind runKind,
            object[] customParams)
        {
            Dte = automationObject as DTE;
            ParameterValues = parameterValues;

            Debug.Assert(WizardWindow != null);

            BeforeWizardRun();

            var iVsUIShell = VsServiceProvider.GetService<SVsUIShell, IVsUIShell>();
            if (iVsUIShell == null)
                throw new NullReferenceException("IVsUIShell");

            try {
                IntPtr hwnd;
                iVsUIShell.EnableModeless(0);
                iVsUIShell.GetDialogOwnerHwnd(out hwnd);
                WindowHelper.ShowModal(WizardWindow, hwnd);
            } catch (QtVSException exception) {
                Messages.DisplayErrorMessage(exception.Message);
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

        protected virtual void Expand()
        {
            Debug.Assert(ParameterValues != null);
            Debug.Assert(Dte != null);
            Debug.Assert(Configurations != null);
            Debug.Assert(ExtraItems != null);

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
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">
  </PropertyGroup>",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform));
            }
            Parameter[NewProject.Properties] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Globals
            //
            xml = new StringBuilder();
            Parameter[NewProject.ProjectGuid] = HelperFunctions.NewProjectGuid();
            Parameter[NewProject.Keyword] = Resources.QtVSVersionTag;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Globals: Windows
            //
            foreach (IWizardConfiguration c in Configurations
                .Where(c => c.Target.EqualTo(ProjectTargets.Windows))) {
                if (!string.IsNullOrEmpty(c.QtVersion.VC_WindowsTargetPlatformVersion)) {
                    xml.AppendLine(string.Format(@"
    <WindowsTargetPlatformVersion Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"">{2}</WindowsTargetPlatformVersion>",
                        /*{0}*/ c.Name,
                        /*{1}*/ c.Platform,
                        /*{2}*/ c.QtVersion.VC_WindowsTargetPlatformVersion));
                }
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
                    /*{2}*/ c.QtVersion.VC_WindowsTargetPlatformVersion,
                    /*{3}*/ c.QtVersion.VC_WindowsTargetPlatformMinVersion,
                    /*{4}*/ c.QtVersion.VC_MinimumVisualStudioVersion,
                    /*{5}*/ c.QtVersion.VC_ApplicationTypeRevision));
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Globals: Linux
            //
            foreach (IWizardConfiguration c in Configurations.Where(c => IsLinux(c))) {
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
                ProjectTargets target;
                if (!c.Target.TryCast(out target))
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
    <PlatformToolset>{0}</PlatformToolset>",
                            /*{0}*/ c.QtVersion.VC_PlatformToolset));
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
                if (IsLinux(c)) {
                    xml.AppendLine(string.Format(@"
    <UseDebugLibraries>{0}</UseDebugLibraries>",
                        /*{0}*/ c.IsDebug ? "true" : "false"));
                } else if (target == ProjectTargets.WindowsStore) {
                    xml.AppendLine(@"
    <GenerateManifest>false</GenerateManifest>
    <EmbedManifest>false</EmbedManifest>");
                }
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
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == '{0}|{1}'"" Label=""QtSettings"">
    <QtInstall>{2}</QtInstall>
    <QtModules>{3}</QtModules>
    <QtBuildConfig>{4}</QtBuildConfig>",
                    /*{0}*/ c.Name,
                    /*{1}*/ c.Platform,
                    /*{2}*/ c.QtVersionName,
                    /*{3}*/ string.Join(";", c.Modules.Union(ExtraModules)),
                    /*{4}*/ c.IsDebug ? "debug" : "release"));
                if (c.Target.EqualTo(ProjectTargets.WindowsStore)) {
                    xml.AppendLine(@"
    <QtDeploy>true</QtDeploy>");
                }
                xml.AppendLine(@"
  </PropertyGroup>");
            }
            Parameter[NewProject.QtSettings] = FormatParam(xml);

            ///////////////////////////////////////////////////////////////////////////////////////
            // Build settings
            //
            IEnumerable<ItemProperty> mocProperties
                = ItemGlobals?[QtMoc.ItemTypeName]?.Properties ?? Enumerable.Empty<ItemProperty>();
            IEnumerable<ItemProperty> clProperties
                = ItemGlobals?["ClCompile"]?.Properties ?? Enumerable.Empty<ItemProperty>();
            IEnumerable<ItemProperty> linkProperties
                = ItemGlobals?["Link"]?.Properties ?? Enumerable.Empty<ItemProperty>();

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
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>");
                    if (c.IsDebug) {
                        xml.AppendLine(@"
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <Optimization>Disabled</Optimization>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>");
                    } else {
                        xml.AppendLine(@"
      <DebugInformationFormat>None</DebugInformationFormat>
      <Optimization>MaxSpeed</Optimization>
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>");
                    }
                    if (c.Target.EqualTo(ProjectTargets.WindowsStore)) {
                        xml.AppendLine(@"
      <CompileAsWinRT>false</CompileAsWinRT>
      <PrecompiledHeader>NotUsing</PrecompiledHeader>
      <RuntimeTypeInfo>true</RuntimeTypeInfo>");
                    }
                    if (UsePrecompiledHeaders) {
                        xml.AppendLine(string.Format(@"
      <UsePrecompiledHeader>Use</UsePrecompiledHeader>
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
                .Where((ItemDef item) => item.WhereConfig == null
                    || Configurations.Where(item.WhereConfig).Any())
                .Union(UsePrecompiledHeaders
                    ? new[] { PrecompiledHeader, PrecompiledHeaderSource }
                    : Enumerable.Empty<ItemDef>());

            xml = new StringBuilder();
            foreach (ItemDef item in projectItems) {
                bool itemHasProperties = (item.WhereConfig != null || item.Properties != null);
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

        // Matches empty lines; captures first newline
        static readonly Regex patternEmptyLines
            = new Regex(@"(?:^|(?<FIRST_NL>\r\n))(?:\r\n)+(?![\r\n]|$)|(?:\r\n)+$");

        protected static string FormatParam(StringBuilder paramValue)
        {
            return FormatParam(paramValue.ToString());
        }

        protected static string FormatParam(string paramValue)
        {
            // Remove empty lines; replace with first newline (if any)
            paramValue = patternEmptyLines.Replace(paramValue,
                (Match m) => m.Groups["FIRST_NL"].Value);

            return paramValue;
        }
    }
}
