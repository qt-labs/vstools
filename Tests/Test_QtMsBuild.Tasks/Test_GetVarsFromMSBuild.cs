/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Tasks
{
    using QtVsTools.QtMsBuild.Tasks;

    [TestClass]
    public class Test_GetVarsFromMSBuild
    {
        private string QMake_MSBuild = @"
DEFINES=/Project/ItemDefinitionGroup/ClCompile/PreprocessorDefinitions;
INCLUDEPATH=/Project/ItemDefinitionGroup/ClCompile/AdditionalIncludeDirectories;
STDCPP=/Project/ItemDefinitionGroup/ClCompile/LanguageStandard;
RUNTIME=/Project/ItemDefinitionGroup/ClCompile/RuntimeLibrary;
CL_OPTIONS=/Project/ItemDefinitionGroup/ClCompile/AdditionalOptions;
LIBS=/Project/ItemDefinitionGroup/Link/AdditionalDependencies;
LINK_OPTIONS=/Project/ItemDefinitionGroup/Link/AdditionalOptions;
";
        private ITaskItem[] QMake_MSBuild_VarDef => ParseVarDefs(QMake_MSBuild);

        [TestMethod]
        public void ExcludedValues()
        {
            var workDir = SetupWorkDir();
            Assert.IsTrue(workDir is { Length: > 0 });
            Assert.IsTrue(GetVarsFromMSBuild.Execute(
                $@"{workDir}\qtvars.vcxproj", QMake_MSBuild_VarDef,
                new[] { $"{Path.GetTempPath()}*", "/include" }, out var msbuildVars));
            Assert.IsTrue(msbuildVars.All(x => x.GetMetadata("Value")
                .IndexOf(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase) == -1));
            Assert.IsTrue(msbuildVars.All(x => x.GetMetadata("Value")
                .IndexOf("/include", StringComparison.OrdinalIgnoreCase) == -1));
            CleanupWorkDir(workDir);
        }

        private static ITaskItem[] ParseVarDefs(string varDefs)
        {
            return varDefs.Split(
                new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('=') is { Length: 2 } varDef
                    ? new TaskItem(x, new Dictionary<string, string>
                    {
                        { "Name", varDef[0] },
                        { "XPath", varDef[1] }
                    })
                    : throw new ArgumentException())
                .ToArray();
        }

        private static string SetupWorkDir()
        {
            var workDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(workDir);
            File.WriteAllText($@"{workDir}\qtvars.vcxproj",
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""ProjectConfigurations"">
    <ProjectConfiguration Include=""Debug|x64"">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label=""Globals"">
    <ProjectGuid></ProjectGuid>
    <RootNamespace>qtvars</RootNamespace>
    <Keyword>Qt4VSv1.0</Keyword>
  </PropertyGroup>
  <Import Project=""$(VCTargetsPath)\Microsoft.Cpp.Default.props"" />
  <PropertyGroup Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"" Label=""Configuration"">
    <PlatformToolset>v143</PlatformToolset>
    <OutputDirectory>.\</OutputDirectory>
    <ATLMinimizesCRunTimeLibraryUsage>false</ATLMinimizesCRunTimeLibraryUsage>
    <CharacterSet>NotSet</CharacterSet>
    <ConfigurationType>Application</ConfigurationType>
    <PrimaryOutput>qtvars</PrimaryOutput>
  </PropertyGroup>
  <Import Project=""$(VCTargetsPath)\Microsoft.Cpp.props"" />
  <ImportGroup Label=""ExtensionSettings"" />
  <ImportGroup Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"" Label=""PropertySheets"">
    <Import Project=""$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"" Condition=""exists(&apos;$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props&apos;)"" />
  </ImportGroup>
  <PropertyGroup Label=""UserMacros"" />
  <PropertyGroup>
    <OutDir Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"">.\</OutDir>
    <TargetName Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"">qtvars</TargetName>
    <IgnoreImportLibrary Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"">true</IgnoreImportLibrary>
  </PropertyGroup>
  <ItemDefinitionGroup Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"">
    <ClCompile>
      <AdditionalIncludeDirectories>{workDir};C:\lib\Qt\6.5.1\msvc2019_64\include;C:\lib\Qt\6.5.1\msvc2019_64\include\QtWidgets;C:\lib\Qt\6.5.1\msvc2019_64\include\QtGui;C:\lib\Qt\6.5.1\msvc2019_64\include\QtCore;{workDir};/include;C:\lib\Qt\6.5.1\msvc2019_64\mkspecs\win32-msvc;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <AdditionalOptions>-Zc:rvalueCast -Zc:inline -Zc:strictStrings -Zc:throwingNew -permissive- -Zc:__cplusplus -Zc:externConstexpr -utf-8 %(AdditionalOptions)</AdditionalOptions>
      <AssemblerListingLocation>.\</AssemblerListingLocation>
      <BrowseInformation>false</BrowseInformation>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <ExceptionHandling>Sync</ExceptionHandling>
      <LanguageStandard>stdcpp17</LanguageStandard>
      <ObjectFileName>.\</ObjectFileName>
      <Optimization>Disabled</Optimization>
      <PreprocessorDefinitions>_WINDOWS;UNICODE;_UNICODE;WIN32;_ENABLE_EXTENDED_ALIGNED_STORAGE;WIN64;QT_WIDGETS_LIB;QT_GUI_LIB;QT_CORE_LIB;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <PreprocessToFile>false</PreprocessToFile>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
      <SuppressStartupBanner>true</SuppressStartupBanner>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <UseFullPaths>false</UseFullPaths>
      <WarningLevel>TurnOffAllWarnings</WarningLevel>
    </ClCompile>
    <Link>
      <AdditionalDependencies>C:\lib\Qt\6.5.1\msvc2019_64\lib\Qt6Widgetsd.lib;C:\lib\Qt\6.5.1\msvc2019_64\lib\Qt6Guid.lib;C:\lib\Qt\6.5.1\msvc2019_64\lib\Qt6Cored.lib;C:\lib\Qt\6.5.1\msvc2019_64\lib\Qt6EntryPointd.lib;shell32.lib;%(AdditionalDependencies)</AdditionalDependencies>
      <AdditionalOptions>&quot;/MANIFESTDEPENDENCY:type=&apos;win32&apos; name=&apos;Microsoft.Windows.Common-Controls&apos; version=&apos;6.0.0.0&apos; publicKeyToken=&apos;6595b64144ccf1df&apos; language=&apos;*&apos; processorArchitecture=&apos;*&apos;&quot; %(AdditionalOptions)</AdditionalOptions>
      <DataExecutionPrevention>true</DataExecutionPrevention>
      <GenerateDebugInformation>true</GenerateDebugInformation>
      <IgnoreImportLibrary>true</IgnoreImportLibrary>
      <OutputFile>$(OutDir)\qtvars.exe</OutputFile>
      <RandomizedBaseAddress>true</RandomizedBaseAddress>
      <SubSystem>Windows</SubSystem>
      <SuppressStartupBanner>true</SuppressStartupBanner>
    </Link>
    <Midl>
      <DefaultCharType>Unsigned</DefaultCharType>
      <EnableErrorChecks>None</EnableErrorChecks>
      <WarningLevel>0</WarningLevel>
    </Midl>
    <ResourceCompile>
      <PreprocessorDefinitions>_WINDOWS;UNICODE;_UNICODE;WIN32;_ENABLE_EXTENDED_ALIGNED_STORAGE;WIN64;QT_WIDGETS_LIB;QT_GUI_LIB;QT_CORE_LIB;_DEBUG;%(PreprocessorDefinitions)</PreprocessorDefinitions>
    </ResourceCompile>
  </ItemDefinitionGroup>
  <ItemGroup>
    <CustomBuild Include=""moc_predefs.h.cbt"">
      <FileType>Document</FileType>
      <AdditionalInputs Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"">C:\lib\Qt\6.5.1\msvc2019_64\mkspecs\features\data\dummy.cpp;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"">cl -BxC:\lib\Qt\6.5.1\msvc2019_64\bin\qmake.exe -nologo -Zc:wchar_t -FS -Zc:rvalueCast -Zc:inline -Zc:strictStrings -Zc:throwingNew -permissive- -Zc:__cplusplus -Zc:externConstexpr -Zi -MDd -std:c++17 -utf-8 -W0 -E C:\lib\Qt\6.5.1\msvc2019_64\mkspecs\features\data\dummy.cpp 2&gt;NUL &gt;moc_predefs.h</Command>
      <Message Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"">Generate moc_predefs.h</Message>
      <Outputs Condition=""&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;"">moc_predefs.h;%(Outputs)</Outputs>
    </CustomBuild>
  </ItemGroup>
  <Import Project=""$(VCTargetsPath)\Microsoft.Cpp.targets"" />
  <ImportGroup Label=""ExtensionTargets"" />
</Project>");
            return workDir;
        }

        private void CleanupWorkDir(string workDir)
        {
            File.Delete($@"{workDir}\qtvars.vcxproj");
            Directory.Delete(workDir);
        }
    }
}
