# Qt VS Tools - Project format versions

## Contents

* [Project format v1.0](#project-format-v10)
* [Project format v2.0](#project-format-v20)
* [Project format v3.0](#project-format-v30)
* [Project format v3.1](#project-format-v31)
* [Project format v3.2](#project-format-v32)
* [Project format v3.3](#project-format-v33)
* [Project format v3.4](#project-format-v34)
* [Example projects](#example-projects)
    * [v1.0](#v10)
    * [v2.0](#v20)
    * [v3.0](#v30)
    * [v3.1](#v31)
    * [v3.2](#v32)
    * [v3.3](#v33)
    * [v3.4](#v34)

## Project format v1.0

Output of `qmake` using the VC template:

    qmake -tp vc

### Project file outline

- ItemGroup **ProjectConfiguration**
- PropertyGroup **Globals**
    - Keyword **Qt4VSv1.0**
- Import **Microsoft.Cpp.Default.props**
- PropertyGroup **Configuration**
- Import **Microsoft.Cpp.props**
- ImportGroup **ExtensionSettings**
- ImportGroup **PropertySheets**
- PropertyGroup **UserMacros**
- PropertyGroup
    - Property **OutDir**
    - Property **IntDir**
    - ...
- ItemDefinitionGroup
    - **ClCompile**
    -  **Link**
    - ...
- ItemGroup
    - **CustomBuild** (*MOC header*)
    - *N* x **ClCompile** (*MOC-generated CPP source*), where N = number of configurations
        - (*N* - 1) x **ExcludedFromBuild** (*configuration*)
    - **CustomBuild** (*QRC resources*)
    - *N* x **ClCompile** (*RCC-generated CPP source*), where N = number of configurations
        - (*N* - 1) x **ExcludedFromBuild** (*configuration*)
    - **CustomBuild** (*UI form*)
    - **ClInclude** (*UIC-generated header*)
    - **ClInclude** (*other headers*)
    - **ClCompile** (*other CPP sources*)
    - ...
- Import **Microsoft.Cpp.targets**
- ImportGroup **ExtensionTargets**

## Project format v2.0
**[Qt Visual Studio Tools v2.1.1](https://download.qt.io/official_releases/vsaddin/2.1.1/)**

### Project file outline

- ItemGroup **ProjectConfiguration**
- PropertyGroup **Globals**
    - Keyword **Qt4VSv1.0**
- Import **Microsoft.Cpp.Default.props**
- PropertyGroup **Configuration**
- Import **Microsoft.Cpp.props**
- ImportGroup **ExtensionSettings**
- ImportGroup **Shared**
- ImportGroup **PropertySheets**
- PropertyGroup **UserMacros**
- PropertyGroup
    - Property **OutDir**
    - Property **IntDir**
    - ...
- ItemDefinitionGroup
    - **ClCompile**
    -  **Link**
    - ...
- ItemGroup
    - **CustomBuild** (*MOC header*)
    - *N* x **ClCompile** (*MOC-generated CPP source*), where N = number of configurations
        - (*N* - 1) x **ExcludedFromBuild** (*configuration*)
    - **CustomBuild** (*QRC resources*)
    - *N* x **ClCompile** (*RCC-generated CPP source*), where N = number of configurations
        - (*N* - 1) x **ExcludedFromBuild** (*configuration*)
    - **CustomBuild** (*UI form*)
    - **ClInclude** (*UIC-generated header*)
    - **ClInclude** (*other headers*)
    - **ClCompile** (*other CPP sources*)
    - ...
- Import **Microsoft.Cpp.targets**
- ImportGroup **ExtensionTargets**
- ProjectExtensions / VisualStudio
    - UserProperties
        - **MocDir**
        - **UicDir**
        - **RccDir**
        - **lupdateOptions**
        - **lupdateOnBuild**
        - **lreleaseOptions**
        - **Qt5Version_x0020_x64**
        - **MocOptions**

## Project format v3.0
**[Qt Visual Studio Tools v2.4.0](https://download.qt.io/official_releases/vsaddin/2.4.0/)**

**`Edit Qt settings in property pages (V3 format)`**  
`commit 805e9ed6f14fb0a4d9dd8ce6a23631ca215b304a`  
`Author: Miguel Costa <miguel.costa@qt.io>`  
`Date:   Thu Jun 20 13:54:49 2019 +0200`

    Qt settings are now configured in the project property pages. This
    includes the possibility to have different versions of the Qt settings
    per project configuration. Previously, the Qt settings were edited in a
    custom dialog, allowing only a single version of the settings that
    would apply to all project configurations.

    This change breaks the current version of the support for Qt VS Tools
    projects. A new version (V3) of the project format is introduced. This
    new format allows Qt settings to be stored in the same way as other
    project properties. Previous versions will still work and a later change
    will introduce the possibility to convert from previous versions to V3.

### Project file outline

- ItemGroup **ProjectConfiguration**
- PropertyGroup **Globals**
    - Keyword **QtVS_v300**
- Import **Microsoft.Cpp.Default.props**
- PropertyGroup **Configuration**
- Import **Microsoft.Cpp.props**
- PropertyGroup **if %QTMSBUILD% undefined**
    - Property **QtMsBuild** = $(MSBuildProjectDirectory)\QtMsBuild
- PropertyGroup
    - Property **OutDir**
    - Property **IntDir**
    - ...
- Target **QtMsBuildNotFound**
- Import **qt.props**
- PropertyGroup **QtSettings**
- ImportGroup **ExtensionSettings**
- ImportGroup **Shared**
- ImportGroup **PropertySheets**
- PropertyGroup **UserMacros**
- ItemDefinitionGroup
    - **ClCompile**
    - **Link**
    - **QtMoc**
    - **QtRcc**
    - **QtUic**
    - ...
- ItemGroup
    - **QtMoc** (*MOC header*)
    - **QtRcc** (*QRC resources*)
    - **QtUic** (*UI form*)
    - **ClInclude** (*other headers*)
    - **ClCompile** (*other CPP sources*)
    - ...
- Import **Microsoft.Cpp.targets**
- Import **qt.targets**
- ImportGroup **ExtensionTargets**

## Project format v3.1
**[Qt Visual Studio Tools v2.4.2](https://download.qt.io/official_releases/vsaddin/2.4.2/)**

**`Implement project format v3.1`**  
`commit fdbec35c590f524f6f9ce8f2e6a7328f2f3df508`  
`Author: Miguel Costa <miguel.costa@qt.io>`  
`Date:   Wed Sep 18 18:47:30 2019 +0200`

    This change introduces a revision of the v3 project format, which will
    now allow Qt settings to reference user macros defined in imported
    property sheets. This includes the following changes to the order of
    property evaluation:
     - "QtSettings" property group moved to after the import of user
     property sheets;
     - QtInstall property moved from the "Configuration" property group
    to the "QtSettings" property group;
     - Import of qt.props moved to after the "QtSettings" property group.

### Project file outline

- ItemGroup **ProjectConfiguration**
- PropertyGroup **Globals**
    - Keyword **QtVS_v301**
- Import **Microsoft.Cpp.Default.props**
- PropertyGroup **Configuration**
    - Property **QtInstall**
- Import **Microsoft.Cpp.props**
- PropertyGroup (*if `%QTMSBUILD%` undefined*)
    - Property **QtMsBuild** = $(MSBuildProjectDirectory)\QtMsBuild
- PropertyGroup
    - Property **OutDir**
    - Property **IntDir**
    - ...
- Target **QtMsBuildNotFound**
- ImportGroup **ExtensionSettings**
- ImportGroup **Shared**
- ImportGroup **PropertySheets**
- PropertyGroup **UserMacros**
- Import **qt.props**
- PropertyGroup **QtSettings**
- ItemDefinitionGroup
    - **ClCompile**
    - **Link**
    - **QtMoc**
    - **QtRcc**
    - **QtUic**
    - ...
- ItemGroup
    - **QtMoc** (*MOC header*)
    - **QtRcc** (*QRC resources*)
    - **QtUic** (*UI form*)
    - **ClInclude** (*other headers*)
    - **ClCompile** (*other CPP sources*)
    - ...
- Import **Microsoft.Cpp.targets**
- Import **qt.targets**
- ImportGroup **ExtensionTargets**

## Project format v3.2
**[Qt Visual Studio Tools v2.5.1](https://download.qt.io/official_releases/vsaddin/2.5.1/)**

**`Fix incompatibilities with property sheets`**  
`commit 1a93741cadaa26b49cc4dc02d09ea7249cbea6fe`  
`Author: Miguel Costa <miguel.costa@qt.io>`  
`Date:   Thu Nov 21 16:21:35 2019 +0100`

    Updated the format of Qt projects to better match the requirements for
    Visual Studio C++ projects (*) that enable integrating with the IDE, in
    particular, that allow external property sheets to be referenced.
(*) https://docs.microsoft.com/en-us/cpp/build/reference/cxproj-file-structure#per-configuration-propertygroup-elements

### Project file outline

- ItemGroup **ProjectConfiguration**
- PropertyGroup **Globals**
    - Keyword **QtVS_v302**
    - Property (*if `%QTMSBUILD%` undefined*) **QtMsBuild** = $(MSBuildProjectDirectory)\QtMsBuild
- Import **Microsoft.Cpp.Default.props**
- PropertyGroup **Configuration**
    - Property **QtInstall**
- Import **Microsoft.Cpp.props**
- Target **QtMsBuildNotFound**
- ImportGroup **ExtensionSettings**
- ImportGroup **Shared**
- ImportGroup **PropertySheets**
- PropertyGroup **UserMacros**
- Import **qt_defaults.props**
- PropertyGroup **QtSettings**
- Import **qt.props**
- ItemDefinitionGroup
    - **ClCompile**
    - **Link**
    - **QtMoc**
    - **QtRcc**
    - **QtUic**
    - ...
- ItemGroup
    - **QtMoc** (*MOC header*)
    - **QtRcc** (*QRC resources*)
    - **QtUic** (*UI form*)
    - **ClInclude** (*other headers*)
    - **ClCompile** (*other CPP sources*)
    - ...
- Import **Microsoft.Cpp.targets**
- Import **qt.targets**
- ImportGroup **ExtensionTargets**

## Project format v3.3
**[Qt Visual Studio Tools v2.6.0](https://download.qt.io/official_releases/vsaddin/2.6.0/)**

**`Fix ignoring VC property changes (project format v3.3)`**  
`commit 621efbcc92be6e1a9869a15a850b9bd8cccf2e97`  
`Author: Miguel Costa <miguel.costa@qt.io>`  
`Date:   Tue Jun 9 10:24:54 2020 +0200`

    This change introduces project format version 3.3, which fixes issues
    related to modified values of VC properties (e.g. $(IntDir)) being
    ignored when evaluating Qt build settings.

    MSBuild evaluates properties by order of definition; dependencies are
    resolved by using the latest evaluation of referred properties. As such,
    any subsequent changes to the value of dependencies will not be
    reflected in previously evaluated properties.

    Redefinitions of VC properties are stored in uncategorized property
    groups (i.e. <PropertyGroup> elements without a Label attrib) inside the
    MSBuild project file; if no available group is found, Visual Studio will
    create a new one. The incorrect evaluation of Qt properties happens when
    new property groups are created after the definition of Qt properties,
    such that the evaluation of Qt properties will be using outdated values
    of VC properties.

    Project format version 3.3 addresses this issue by adding property
    groups for VC property storage in a correct location, with respect to
    Qt build settings definitions.

### Project file outline

- ItemGroup **ProjectConfiguration**
- PropertyGroup **Globals**
    - Keyword **QtVS_v303**
    - Property (*if `%QTMSBUILD%` undefined*) **QtMsBuild** = $(MSBuildProjectDirectory)\QtMsBuild
- Import **Microsoft.Cpp.Default.props**
- PropertyGroup **Configuration**
    - Property **QtInstall**
- Import **Microsoft.Cpp.props**
- Target **QtMsBuildNotFound**
- ImportGroup **ExtensionSettings**
- ImportGroup **Shared**
- ImportGroup **PropertySheets**
- PropertyGroup **UserMacros**
- Import **qt_defaults.props**
- PropertyGroup
    - Property **OutDir**
    - Property **IntDir**
    - ...
- PropertyGroup **QtSettings**
- Import **qt.props**
- ItemDefinitionGroup
    - **ClCompile**
    - **Link**
    - **QtMoc**
    - **QtRcc**
    - **QtUic**
    - ...
- ItemGroup
    - **QtMoc** (*MOC header*)
    - **QtRcc** (*QRC resources*)
    - **QtUic** (*UI form*)
    - **ClInclude** (*other headers*)
    - **ClCompile** (*other CPP sources*)
    - ...
- Import **Microsoft.Cpp.targets**
- Import **qt.targets**
- ImportGroup **ExtensionTargets**

## Project format v3.4
**>= [Qt Visual Studio Tools v2.7.1](https://download.qt.io/official_releases/vsaddin/2.7.1/)**

**`Integrate Qt.props in the VS Property Manager`**  
`commit cb9ec156845a9efc163a9c67d7a54c8ca790e805`  
`Author: Miguel Costa <miguel.costa@qt.io>`  
`Date:   Fri Dec 11 16:58:06 2020 +0100`

    The Qt property definitions file (Qt.props) will now be shown in the
    evaluation list of the Property Manager window. This allows the user to
    customize the order of evaluation of Qt.props in relation to other
    property files loaded during the build, thereby defining the correct
    dependency between Qt properties and other build settings. Previously,
    the order of evaluation of Qt.props was fixed and could only be changed
    by manually editing the project file's XML.

    Allowing Qt.props to be manipulated in the Property Manager window will
    enable the user to define properties and default metadata within the
    Qt.props itself. To ensure these custom definitions are not lost when
    installing Qt/MSBuild files, the Qt.props file will no longer be
    replaced during start-up.

### Project file outline

- ItemGroup **ProjectConfiguration**
- PropertyGroup **Globals**
    - Keyword **QtVS_v304**
    - Property (*if `%QTMSBUILD%` undefined*) **QtMsBuild** = $(MSBuildProjectDirectory)\QtMsBuild
- Import **Microsoft.Cpp.Default.props**
- PropertyGroup **Configuration**
    - Property **QtInstall**
- Import **Microsoft.Cpp.props**
- Import **qt_defaults.props**
- PropertyGroup **QtSettings**
- Target **QtMsBuildNotFound**
- ImportGroup **ExtensionSettings**
- ImportGroup **Shared**
- ImportGroup **PropertySheets**
    - Import **Qt.props**
- PropertyGroup **UserMacros**
- PropertyGroup
    - Property **OutDir**
    - Property **IntDir**
    - ...
- ItemDefinitionGroup
    - **ClCompile**
    - **Link**
    - **QtMoc**
    - **QtRcc**
    - **QtUic**
    - ...
- ItemGroup
    - **QtMoc** (*MOC header*)
    - **QtRcc** (*QRC resources*)
    - **QtUic** (*UI form*)
    - **ClInclude** (*other headers*)
    - **ClCompile** (*other CPP sources*)
    - ...
- Import **Microsoft.Cpp.targets**
- Import **qt.targets**
- ImportGroup **ExtensionTargets**

## Example projects

### v1.0

#### `.vcxproj`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{E5EA0EEF-BDC2-3484-A4F0-BA6C71E224CF}</ProjectGuid>
    <RootNamespace>QtProjectV100</RootNamespace>
    <Keyword>Qt4VSv1.0</Keyword>
    <WindowsTargetPlatformVersion>10.0.19041.0</WindowsTargetPlatformVersion>
    <WindowsTargetPlatformMinVersion>10.0.19041.0</WindowsTargetPlatformMinVersion>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;" Label="Configuration">
    <PlatformToolset>v142</PlatformToolset>
    <OutputDirectory>release\</OutputDirectory>
    <ATLMinimizesCRunTimeLibraryUsage>false</ATLMinimizesCRunTimeLibraryUsage>
    <CharacterSet>NotSet</CharacterSet>
    <ConfigurationType>Application</ConfigurationType>
    <IntermediateDirectory>release\</IntermediateDirectory>
    <PrimaryOutput>QtProjectV100</PrimaryOutput>
  </PropertyGroup>
  <PropertyGroup Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;" Label="Configuration">
    <PlatformToolset>v142</PlatformToolset>
    <OutputDirectory>debug\</OutputDirectory>
    <ATLMinimizesCRunTimeLibraryUsage>false</ATLMinimizesCRunTimeLibraryUsage>
    <CharacterSet>NotSet</CharacterSet>
    <ConfigurationType>Application</ConfigurationType>
    <IntermediateDirectory>debug\</IntermediateDirectory>
    <PrimaryOutput>QtProjectV100</PrimaryOutput>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <ImportGroup Label="ExtensionSettings" />
  <ImportGroup Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;" Label="PropertySheets">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists(&apos;$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props&apos;)" />
  </ImportGroup>
  <ImportGroup Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;" Label="PropertySheets">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists(&apos;$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props&apos;)" />
  </ImportGroup>
  <PropertyGroup Label="UserMacros" />
  <PropertyGroup>
    <OutDir Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">release\</OutDir>
    <IntDir Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">release\</IntDir>
    <TargetName Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">QtProjectV100</TargetName>
    <IgnoreImportLibrary Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">true</IgnoreImportLibrary>
    <LinkIncremental Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">false</LinkIncremental>
    <OutDir Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">debug\</OutDir>
    <IntDir Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">debug\</IntDir>
    <TargetName Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">QtProjectV100</TargetName>
    <IgnoreImportLibrary Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">true</IgnoreImportLibrary>
  </PropertyGroup>
  <ItemDefinitionGroup Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">
    <ClCompile>
      <AdditionalIncludeDirectories>.;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include\QtWidgets;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include\QtGui;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include\QtANGLE;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include\QtCore;release;.;/include;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\mkspecs\win32-msvc;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <AdditionalOptions>-Zc:rvalueCast -Zc:inline -Zc:strictStrings -Zc:throwingNew -Zc:referenceBinding -Zc:__cplusplus -w34100 -w34189 -w44996 -w44456 -w44457 -w44458 %(AdditionalOptions)</AdditionalOptions>
      <AssemblerListingLocation>release\</AssemblerListingLocation>
      <BrowseInformation>false</BrowseInformation>
      <DebugInformationFormat>None</DebugInformationFormat>
      <DisableSpecificWarnings>4577;4467;%(DisableSpecificWarnings)</DisableSpecificWarnings>
      <ExceptionHandling>Sync</ExceptionHandling>
      <ObjectFileName>release\</ObjectFileName>
      <Optimization>MaxSpeed</Optimization>
      <PreprocessorDefinitions>_WINDOWS;UNICODE;_UNICODE;WIN32;_ENABLE_EXTENDED_ALIGNED_STORAGE;WIN64;NDEBUG;QT_NO_DEBUG;QT_WIDGETS_LIB;QT_GUI_LIB;QT_CORE_LIB;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <PreprocessToFile>false</PreprocessToFile>
      <ProgramDataBaseFileName></ProgramDataBaseFileName>
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
      <SuppressStartupBanner>true</SuppressStartupBanner>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <WarningLevel>Level3</WarningLevel>
    </ClCompile>
    <Link>
      <AdditionalDependencies>C:\lib\Qt\5.15.10\msvc2019_64\lib\Qt5Widgets.lib;C:\lib\Qt\5.15.10\msvc2019_64\lib\Qt5Gui.lib;C:\lib\Qt\5.15.10\msvc2019_64\lib\Qt5Core.lib;C:\lib\Qt\5.15.10\msvc2019_64\lib\qtmain.lib;shell32.lib;%(AdditionalDependencies)</AdditionalDependencies>
      <AdditionalLibraryDirectories>C:\openssl\lib;C:\Utils\my_sql\mysql-5.7.25-winx64\lib;C:\Utils\postgresql\pgsql\lib;%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>
      <AdditionalOptions>&quot;/MANIFESTDEPENDENCY:type=&apos;win32&apos; name=&apos;Microsoft.Windows.Common-Controls&apos; version=&apos;6.0.0.0&apos; publicKeyToken=&apos;6595b64144ccf1df&apos; language=&apos;*&apos; processorArchitecture=&apos;*&apos;&quot; %(AdditionalOptions)</AdditionalOptions>
      <DataExecutionPrevention>true</DataExecutionPrevention>
      <GenerateDebugInformation>false</GenerateDebugInformation>
      <IgnoreImportLibrary>true</IgnoreImportLibrary>
      <LinkIncremental>false</LinkIncremental>
      <OptimizeReferences>true</OptimizeReferences>
      <OutputFile>$(OutDir)\QtProjectV100.exe</OutputFile>
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
      <PreprocessorDefinitions>_WINDOWS;UNICODE;_UNICODE;WIN32;_ENABLE_EXTENDED_ALIGNED_STORAGE;WIN64;NDEBUG;QT_NO_DEBUG;QT_WIDGETS_LIB;QT_GUI_LIB;QT_CORE_LIB;%(PreprocessorDefinitions)</PreprocessorDefinitions>
    </ResourceCompile>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">
    <ClCompile>
      <AdditionalIncludeDirectories>.;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include\QtWidgets;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include\QtGui;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include\QtANGLE;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\include\QtCore;debug;.;/include;..\..\..\..\lib\Qt\5.15.10\msvc2019_64\mkspecs\win32-msvc;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <AdditionalOptions>-Zc:rvalueCast -Zc:inline -Zc:strictStrings -Zc:throwingNew -Zc:referenceBinding -Zc:__cplusplus -w34100 -w34189 -w44996 -w44456 -w44457 -w44458 %(AdditionalOptions)</AdditionalOptions>
      <AssemblerListingLocation>debug\</AssemblerListingLocation>
      <BrowseInformation>false</BrowseInformation>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <DisableSpecificWarnings>4577;4467;%(DisableSpecificWarnings)</DisableSpecificWarnings>
      <ExceptionHandling>Sync</ExceptionHandling>
      <ObjectFileName>debug\</ObjectFileName>
      <Optimization>Disabled</Optimization>
      <PreprocessorDefinitions>_WINDOWS;UNICODE;_UNICODE;WIN32;_ENABLE_EXTENDED_ALIGNED_STORAGE;WIN64;QT_WIDGETS_LIB;QT_GUI_LIB;QT_CORE_LIB;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <PreprocessToFile>false</PreprocessToFile>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
      <SuppressStartupBanner>true</SuppressStartupBanner>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <WarningLevel>Level3</WarningLevel>
    </ClCompile>
    <Link>
      <AdditionalDependencies>C:\lib\Qt\5.15.10\msvc2019_64\lib\Qt5Widgetsd.lib;C:\lib\Qt\5.15.10\msvc2019_64\lib\Qt5Guid.lib;C:\lib\Qt\5.15.10\msvc2019_64\lib\Qt5Cored.lib;C:\lib\Qt\5.15.10\msvc2019_64\lib\qtmaind.lib;shell32.lib;%(AdditionalDependencies)</AdditionalDependencies>
      <AdditionalLibraryDirectories>C:\openssl\lib;C:\Utils\my_sql\mysql-5.7.25-winx64\lib;C:\Utils\postgresql\pgsql\lib;%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>
      <AdditionalOptions>&quot;/MANIFESTDEPENDENCY:type=&apos;win32&apos; name=&apos;Microsoft.Windows.Common-Controls&apos; version=&apos;6.0.0.0&apos; publicKeyToken=&apos;6595b64144ccf1df&apos; language=&apos;*&apos; processorArchitecture=&apos;*&apos;&quot; %(AdditionalOptions)</AdditionalOptions>
      <DataExecutionPrevention>true</DataExecutionPrevention>
      <GenerateDebugInformation>true</GenerateDebugInformation>
      <IgnoreImportLibrary>true</IgnoreImportLibrary>
      <OutputFile>$(OutDir)\QtProjectV100.exe</OutputFile>
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
    <ClCompile Include="QtProjectV100.cpp" />
    <ClCompile Include="main.cpp" />
  </ItemGroup>
  <ItemGroup>
    <CustomBuild Include="QtProjectV100.h">
      <AdditionalInputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">QtProjectV100.h;release\moc_predefs.h;C:\lib\Qt\5.15.10\msvc2019_64\bin\moc.exe;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">C:\lib\Qt\5.15.10\msvc2019_64\bin\moc.exe  -DUNICODE -D_UNICODE -DWIN32 -D_ENABLE_EXTENDED_ALIGNED_STORAGE -DWIN64 -DNDEBUG -DQT_NO_DEBUG -DQT_WIDGETS_LIB -DQT_GUI_LIB -DQT_CORE_LIB --compiler-flavor=msvc --include C:/dev/tests/formats/100/release/moc_predefs.h -IC:/lib/Qt/5.15.10/msvc2019_64/mkspecs/win32-msvc -IC:/dev/tests/formats/100 -IC:/lib/Qt/5.15.10/msvc2019_64/include -IC:/lib/Qt/5.15.10/msvc2019_64/include/QtWidgets -IC:/lib/Qt/5.15.10/msvc2019_64/include/QtGui -IC:/lib/Qt/5.15.10/msvc2019_64/include/QtANGLE -IC:/lib/Qt/5.15.10/msvc2019_64/include/QtCore -I&quot;C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Tools\MSVC\14.29.30133\ATLMFC\include&quot; -I&quot;C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Tools\MSVC\14.29.30133\include&quot; -I&quot;C:\Program Files (x86)\Windows Kits\NETFXSDK\4.8\include\um&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\ucrt&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\shared&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\um&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\winrt&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\cppwinrt&quot; QtProjectV100.h -o release\moc_QtProjectV100.cpp</Command>
      <Message Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">MOC QtProjectV100.h</Message>
      <Outputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">release\moc_QtProjectV100.cpp;%(Outputs)</Outputs>
      <AdditionalInputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">QtProjectV100.h;debug\moc_predefs.h;C:\lib\Qt\5.15.10\msvc2019_64\bin\moc.exe;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">C:\lib\Qt\5.15.10\msvc2019_64\bin\moc.exe  -DUNICODE -D_UNICODE -DWIN32 -D_ENABLE_EXTENDED_ALIGNED_STORAGE -DWIN64 -DQT_WIDGETS_LIB -DQT_GUI_LIB -DQT_CORE_LIB --compiler-flavor=msvc --include C:/dev/tests/formats/100/debug/moc_predefs.h -IC:/lib/Qt/5.15.10/msvc2019_64/mkspecs/win32-msvc -IC:/dev/tests/formats/100 -IC:/lib/Qt/5.15.10/msvc2019_64/include -IC:/lib/Qt/5.15.10/msvc2019_64/include/QtWidgets -IC:/lib/Qt/5.15.10/msvc2019_64/include/QtGui -IC:/lib/Qt/5.15.10/msvc2019_64/include/QtANGLE -IC:/lib/Qt/5.15.10/msvc2019_64/include/QtCore -I&quot;C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Tools\MSVC\14.29.30133\ATLMFC\include&quot; -I&quot;C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Tools\MSVC\14.29.30133\include&quot; -I&quot;C:\Program Files (x86)\Windows Kits\NETFXSDK\4.8\include\um&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\ucrt&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\shared&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\um&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\winrt&quot; -I&quot;C:\Program Files (x86)\Windows Kits\10\include\10.0.19041.0\cppwinrt&quot; QtProjectV100.h -o debug\moc_QtProjectV100.cpp</Command>
      <Message Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">MOC QtProjectV100.h</Message>
      <Outputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">debug\moc_QtProjectV100.cpp;%(Outputs)</Outputs>
    </CustomBuild>
  </ItemGroup>
  <ItemGroup>
    <ClCompile Include="debug\moc_QtProjectV100.cpp">
      <ExcludedFromBuild Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">true</ExcludedFromBuild>
    </ClCompile>
    <ClCompile Include="release\moc_QtProjectV100.cpp">
      <ExcludedFromBuild Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">true</ExcludedFromBuild>
    </ClCompile>
    <CustomBuild Include="debug\moc_predefs.h.cbt">
      <FileType>Document</FileType>
      <ExcludedFromBuild Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">true</ExcludedFromBuild>
      <AdditionalInputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">..\..\..\..\lib\Qt\5.15.10\msvc2019_64\mkspecs\features\data\dummy.cpp;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">cl -BxC:\lib\Qt\5.15.10\msvc2019_64\bin\qmake.exe -nologo -Zc:wchar_t -FS -Zc:rvalueCast -Zc:inline -Zc:strictStrings -Zc:throwingNew -Zc:referenceBinding -Zc:__cplusplus -Zi -MDd -W3 -w34100 -w34189 -w44996 -w44456 -w44457 -w44458 -wd4577 -wd4467 -E ..\..\..\..\lib\Qt\5.15.10\msvc2019_64\mkspecs\features\data\dummy.cpp 2&gt;NUL &gt;debug\moc_predefs.h</Command>
      <Message Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">Generate moc_predefs.h</Message>
      <Outputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">debug\moc_predefs.h;%(Outputs)</Outputs>
    </CustomBuild>
    <CustomBuild Include="release\moc_predefs.h.cbt">
      <FileType>Document</FileType>
      <AdditionalInputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">..\..\..\..\lib\Qt\5.15.10\msvc2019_64\mkspecs\features\data\dummy.cpp;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">cl -BxC:\lib\Qt\5.15.10\msvc2019_64\bin\qmake.exe -nologo -Zc:wchar_t -FS -Zc:rvalueCast -Zc:inline -Zc:strictStrings -Zc:throwingNew -Zc:referenceBinding -Zc:__cplusplus -O2 -MD -W3 -w34100 -w34189 -w44996 -w44456 -w44457 -w44458 -wd4577 -wd4467 -E ..\..\..\..\lib\Qt\5.15.10\msvc2019_64\mkspecs\features\data\dummy.cpp 2&gt;NUL &gt;release\moc_predefs.h</Command>
      <Message Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">Generate moc_predefs.h</Message>
      <Outputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">release\moc_predefs.h;%(Outputs)</Outputs>
      <ExcludedFromBuild Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">true</ExcludedFromBuild>
    </CustomBuild>
    <ClCompile Include="debug\qrc_QtProjectV100.cpp">
      <ExcludedFromBuild Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">true</ExcludedFromBuild>
    </ClCompile>
    <ClCompile Include="release\qrc_QtProjectV100.cpp">
      <ExcludedFromBuild Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">true</ExcludedFromBuild>
    </ClCompile>
    <ClInclude Include="ui_QtProjectV100.h" />
  </ItemGroup>
  <ItemGroup>
    <CustomBuild Include="QtProjectV100.ui">
      <FileType>Document</FileType>
      <AdditionalInputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">QtProjectV100.ui;C:\lib\Qt\5.15.10\msvc2019_64\bin\uic.exe;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">C:\lib\Qt\5.15.10\msvc2019_64\bin\uic.exe QtProjectV100.ui -o ui_QtProjectV100.h</Command>
      <Message Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">UIC QtProjectV100.ui</Message>
      <Outputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">ui_QtProjectV100.h;%(Outputs)</Outputs>
      <AdditionalInputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">QtProjectV100.ui;C:\lib\Qt\5.15.10\msvc2019_64\bin\uic.exe;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">C:\lib\Qt\5.15.10\msvc2019_64\bin\uic.exe QtProjectV100.ui -o ui_QtProjectV100.h</Command>
      <Message Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">UIC QtProjectV100.ui</Message>
      <Outputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">ui_QtProjectV100.h;%(Outputs)</Outputs>
    </CustomBuild>
  </ItemGroup>
  <ItemGroup>
    <CustomBuild Include="QtProjectV100.qrc">
      <FileType>Document</FileType>
      <AdditionalInputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">QtProjectV100.qrc;C:\lib\Qt\5.15.10\msvc2019_64\bin\rcc.exe;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">C:\lib\Qt\5.15.10\msvc2019_64\bin\rcc.exe -name QtProjectV100 QtProjectV100.qrc -o release\qrc_QtProjectV100.cpp</Command>
      <Message Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">RCC QtProjectV100.qrc</Message>
      <Outputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Release|x64&apos;">release\qrc_QtProjectV100.cpp;%(Outputs)</Outputs>
      <AdditionalInputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">QtProjectV100.qrc;C:\lib\Qt\5.15.10\msvc2019_64\bin\rcc.exe;%(AdditionalInputs)</AdditionalInputs>
      <Command Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">C:\lib\Qt\5.15.10\msvc2019_64\bin\rcc.exe -name QtProjectV100 QtProjectV100.qrc -o debug\qrc_QtProjectV100.cpp</Command>
      <Message Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">RCC QtProjectV100.qrc</Message>
      <Outputs Condition="&apos;$(Configuration)|$(Platform)&apos;==&apos;Debug|x64&apos;">debug\qrc_QtProjectV100.cpp;%(Outputs)</Outputs>
    </CustomBuild>
  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Label="ExtensionTargets" />
</Project>
```

### v2.0

#### `.vcxproj`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{B12702AD-ABFB-343A-A199-8E24837244A3}</ProjectGuid>
    <Keyword>Qt4VSv1.0</Keyword>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v140</PlatformToolset>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v140</PlatformToolset>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <ImportGroup Label="ExtensionSettings" />
  <ImportGroup Label="Shared" />
  <ImportGroup Label="PropertySheets" />
  <PropertyGroup Label="UserMacros" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <OutDir>$(SolutionDir)$(Platform)\$(Configuration)\</OutDir>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <OutDir>$(SolutionDir)$(Platform)\$(Configuration)\</OutDir>
  </PropertyGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <ClCompile>
      <PreprocessorDefinitions>UNICODE;_UNICODE;WIN32;_ENABLE_EXTENDED_ALIGNED_STORAGE;WIN64;QT_DLL;QT_CORE_LIB;QT_GUI_LIB;QT_WIDGETS_LIB;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>.\GeneratedFiles;.;$(QTDIR)\include;.\GeneratedFiles\$(ConfigurationName);$(QTDIR)\include\QtCore;$(QTDIR)\include\QtGui;$(QTDIR)\include\QtWidgets;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <Optimization>Disabled</Optimization>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <OutputFile>$(OutDir)\$(ProjectName).exe</OutputFile>
      <AdditionalLibraryDirectories>$(QTDIR)\lib;%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>
      <GenerateDebugInformation>true</GenerateDebugInformation>
      <AdditionalDependencies>qtmaind.lib;Qt5Cored.lib;Qt5Guid.lib;Qt5Widgetsd.lib;%(AdditionalDependencies)</AdditionalDependencies>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <ClCompile>
      <PreprocessorDefinitions>UNICODE;_UNICODE;WIN32;_ENABLE_EXTENDED_ALIGNED_STORAGE;WIN64;QT_DLL;QT_NO_DEBUG;NDEBUG;QT_CORE_LIB;QT_GUI_LIB;QT_WIDGETS_LIB;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>.\GeneratedFiles;.;$(QTDIR)\include;.\GeneratedFiles\$(ConfigurationName);$(QTDIR)\include\QtCore;$(QTDIR)\include\QtGui;$(QTDIR)\include\QtWidgets;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <DebugInformationFormat />
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <OutputFile>$(OutDir)\$(ProjectName).exe</OutputFile>
      <AdditionalLibraryDirectories>$(QTDIR)\lib;%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>
      <GenerateDebugInformation>false</GenerateDebugInformation>
      <AdditionalDependencies>qtmain.lib;Qt5Core.lib;Qt5Gui.lib;Qt5Widgets.lib;%(AdditionalDependencies)</AdditionalDependencies>
    </Link>
  </ItemDefinitionGroup>
  <ItemGroup>
    <ClCompile Include="GeneratedFiles\Debug\moc_QtProjectV200.cpp">
      <ExcludedFromBuild Condition="'$(Configuration)|$(Platform)'=='Release|x64'">true</ExcludedFromBuild>
    </ClCompile>
    <ClCompile Include="GeneratedFiles\qrc_QtProjectV200.cpp">
      <PrecompiledHeader Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
      </PrecompiledHeader>
      <PrecompiledHeader Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
      </PrecompiledHeader>
    </ClCompile>
    <ClCompile Include="GeneratedFiles\Release\moc_QtProjectV200.cpp">
      <ExcludedFromBuild Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">true</ExcludedFromBuild>
    </ClCompile>
    <ClCompile Include="main.cpp" />
    <ClCompile Include="QtProjectV200.cpp" />
  </ItemGroup>
  <ItemGroup>
    <CustomBuild Include="QtProjectV200.h">
      <AdditionalInputs Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">$(QTDIR)\bin\moc.exe;%(FullPath)</AdditionalInputs>
      <Message Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">Moc%27ing QtProjectV200.h...</Message>
      <Outputs Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">.\GeneratedFiles\$(ConfigurationName)\moc_%(Filename).cpp</Outputs>
      <Command Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">"$(QTDIR)\bin\moc.exe"  "%(FullPath)" -o ".\GeneratedFiles\$(ConfigurationName)\moc_%(Filename).cpp"  -DUNICODE -D_UNICODE -DWIN32 -D_ENABLE_EXTENDED_ALIGNED_STORAGE -DWIN64 -DQT_DLL -DQT_CORE_LIB -DQT_GUI_LIB -DQT_WIDGETS_LIB "-I.\GeneratedFiles" "-I." "-I$(QTDIR)\include" "-I.\GeneratedFiles\$(ConfigurationName)\." "-I$(QTDIR)\include\QtCore" "-I$(QTDIR)\include\QtGui" "-I$(QTDIR)\include\QtWidgets"</Command>
      <AdditionalInputs Condition="'$(Configuration)|$(Platform)'=='Release|x64'">$(QTDIR)\bin\moc.exe;%(FullPath)</AdditionalInputs>
      <Message Condition="'$(Configuration)|$(Platform)'=='Release|x64'">Moc%27ing QtProjectV200.h...</Message>
      <Outputs Condition="'$(Configuration)|$(Platform)'=='Release|x64'">.\GeneratedFiles\$(ConfigurationName)\moc_%(Filename).cpp</Outputs>
      <Command Condition="'$(Configuration)|$(Platform)'=='Release|x64'">"$(QTDIR)\bin\moc.exe"  "%(FullPath)" -o ".\GeneratedFiles\$(ConfigurationName)\moc_%(Filename).cpp"  -DUNICODE -D_UNICODE -DWIN32 -D_ENABLE_EXTENDED_ALIGNED_STORAGE -DWIN64 -DQT_DLL -DQT_NO_DEBUG -DNDEBUG -DQT_CORE_LIB -DQT_GUI_LIB -DQT_WIDGETS_LIB "-I.\GeneratedFiles" "-I." "-I$(QTDIR)\include" "-I.\GeneratedFiles\$(ConfigurationName)\." "-I$(QTDIR)\include\QtCore" "-I$(QTDIR)\include\QtGui" "-I$(QTDIR)\include\QtWidgets"</Command>
    </CustomBuild>
  </ItemGroup>
  <ItemGroup>
    <CustomBuild Include="QtProjectV200.ui">
      <AdditionalInputs Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">$(QTDIR)\bin\uic.exe;%(AdditionalInputs)</AdditionalInputs>
      <Message Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">Uic%27ing %(Identity)...</Message>
      <Outputs Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">.\GeneratedFiles\ui_%(Filename).h;%(Outputs)</Outputs>
      <Command Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">"$(QTDIR)\bin\uic.exe" -o ".\GeneratedFiles\ui_%(Filename).h" "%(FullPath)"</Command>
      <AdditionalInputs Condition="'$(Configuration)|$(Platform)'=='Release|x64'">$(QTDIR)\bin\uic.exe;%(AdditionalInputs)</AdditionalInputs>
      <Message Condition="'$(Configuration)|$(Platform)'=='Release|x64'">Uic%27ing %(Identity)...</Message>
      <Outputs Condition="'$(Configuration)|$(Platform)'=='Release|x64'">.\GeneratedFiles\ui_%(Filename).h;%(Outputs)</Outputs>
      <Command Condition="'$(Configuration)|$(Platform)'=='Release|x64'">"$(QTDIR)\bin\uic.exe" -o ".\GeneratedFiles\ui_%(Filename).h" "%(FullPath)"</Command>
    </CustomBuild>
  </ItemGroup>
  <ItemGroup>
    <ClInclude Include="GeneratedFiles\ui_QtProjectV200.h" />
  </ItemGroup>
  <ItemGroup>
    <CustomBuild Include="QtProjectV200.qrc">
      <AdditionalInputs Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">%(FullPath);%(AdditionalInputs)</AdditionalInputs>
      <Message Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">Rcc%27ing %(Identity)...</Message>
      <Outputs Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">.\GeneratedFiles\qrc_%(Filename).cpp;%(Outputs)</Outputs>
      <Command Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">"$(QTDIR)\bin\rcc.exe" -name "%(Filename)" -no-compress "%(FullPath)" -o .\GeneratedFiles\qrc_%(Filename).cpp</Command>
      <AdditionalInputs Condition="'$(Configuration)|$(Platform)'=='Release|x64'">%(FullPath);%(AdditionalInputs)</AdditionalInputs>
      <Message Condition="'$(Configuration)|$(Platform)'=='Release|x64'">Rcc%27ing %(Identity)...</Message>
      <Outputs Condition="'$(Configuration)|$(Platform)'=='Release|x64'">.\GeneratedFiles\qrc_%(Filename).cpp;%(Outputs)</Outputs>
      <Command Condition="'$(Configuration)|$(Platform)'=='Release|x64'">"$(QTDIR)\bin\rcc.exe" -name "%(Filename)" -no-compress "%(FullPath)" -o .\GeneratedFiles\qrc_%(Filename).cpp</Command>
    </CustomBuild>
  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
  <ProjectExtensions>
    <VisualStudio>
      <UserProperties MocDir=".\GeneratedFiles\$(ConfigurationName)" UicDir=".\GeneratedFiles" RccDir=".\GeneratedFiles" lupdateOptions="" lupdateOnBuild="0" lreleaseOptions="" Qt5Version_x0020_x64="msvc2019_64" MocOptions="" />
    </VisualStudio>
  </ProjectExtensions>
</Project>
```

### v3.0

#### `.vcxproj`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="16.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{B12702AD-ABFB-343A-A199-8E24837244A3}</ProjectGuid>
    <Keyword>QtVS_v300</Keyword>
    <WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v142</PlatformToolset>
    <QtInstall>msvc2019_64</QtInstall>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v142</PlatformToolset>
    <QtInstall>msvc2019_64</QtInstall>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <PropertyGroup Condition="'$(QtMsBuild)'=='' or !Exists('$(QtMsBuild)\qt.targets')">
    <QtMsBuild>$(MSBuildProjectDirectory)\QtMsBuild</QtMsBuild>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <OutDir>$(SolutionDir)$(Platform)\$(Configuration)\</OutDir>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <OutDir>$(SolutionDir)$(Platform)\$(Configuration)\</OutDir>
  </PropertyGroup>
  <Target Name="QtMsBuildNotFound" BeforeTargets="CustomBuild;ClCompile" Condition="!Exists('$(QtMsBuild)\qt.targets') or !Exists('$(QtMsBuild)\qt.props')">
    <Message Importance="High" Text="QtMsBuild: could not locate qt.targets, qt.props; project may not build correctly." />
  </Target>
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.props')">
    <Import Project="$(QtMsBuild)\qt.props" />
  </ImportGroup>
  <PropertyGroup Label="QtSettings" Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <QtModules>core;gui;widgets</QtModules>
  </PropertyGroup>
  <PropertyGroup Label="QtSettings" Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <QtModules>core;gui;widgets</QtModules>
  </PropertyGroup>
  <ImportGroup Label="ExtensionSettings" />
  <ImportGroup Label="Shared" />
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <PropertyGroup Label="UserMacros" />
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <ClCompile>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <Optimization>Disabled</Optimization>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <OutputFile>$(OutDir)\$(ProjectName).exe</OutputFile>
      <GenerateDebugInformation>true</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <ClCompile>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <DebugInformationFormat />
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <OutputFile>$(OutDir)\$(ProjectName).exe</OutputFile>
      <GenerateDebugInformation>false</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemGroup>
    <ClCompile Include="main.cpp" />
    <ClCompile Include="QtProjectV300.cpp" />
  </ItemGroup>
  <ItemGroup>
    <QtMoc Include="QtProjectV300.h" />
  </ItemGroup>
  <ItemGroup>
    <QtUic Include="QtProjectV300.ui" />
  </ItemGroup>
  <ItemGroup>
    <QtRcc Include="QtProjectV300.qrc" />
  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.targets')">
    <Import Project="$(QtMsBuild)\qt.targets" />
  </ImportGroup>
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
</Project>
```

### v3.1

#### `.vcxproj`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="16.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{B12702AD-ABFB-343A-A199-8E24837244A3}</ProjectGuid>
    <Keyword>QtVS_v301</Keyword>
    <WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v142</PlatformToolset>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v142</PlatformToolset>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <PropertyGroup Condition="'$(QtMsBuild)'=='' or !Exists('$(QtMsBuild)\qt.targets')">
    <QtMsBuild>$(MSBuildProjectDirectory)\QtMsBuild</QtMsBuild>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <OutDir>$(SolutionDir)$(Platform)\$(Configuration)\</OutDir>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <OutDir>$(SolutionDir)$(Platform)\$(Configuration)\</OutDir>
  </PropertyGroup>
  <Target Name="QtMsBuildNotFound" BeforeTargets="CustomBuild;ClCompile" Condition="!Exists('$(QtMsBuild)\qt.targets') or !Exists('$(QtMsBuild)\qt.props')">
    <Message Importance="High" Text="QtMsBuild: could not locate qt.targets, qt.props; project may not build correctly." />
  </Target>
  <ImportGroup Label="ExtensionSettings" />
  <ImportGroup Label="Shared" />
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <PropertyGroup Label="UserMacros" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt_defaults.props')">
    <Import Project="$(QtMsBuild)\qt_defaults.props" />
  </ImportGroup>
  <PropertyGroup Label="QtSettings" Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <QtInstall>msvc2019_64</QtInstall>
    <QtModules>core;gui;widgets</QtModules>
  </PropertyGroup>
  <PropertyGroup Label="QtSettings" Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <QtInstall>msvc2019_64</QtInstall>
    <QtModules>core;gui;widgets</QtModules>
  </PropertyGroup>
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.props')">
    <Import Project="$(QtMsBuild)\qt.props" />
  </ImportGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
    <ClCompile>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <Optimization>Disabled</Optimization>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <OutputFile>$(OutDir)\$(ProjectName).exe</OutputFile>
      <GenerateDebugInformation>true</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'">
    <ClCompile>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <DebugInformationFormat />
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <OutputFile>$(OutDir)\$(ProjectName).exe</OutputFile>
      <GenerateDebugInformation>false</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemGroup>
    <ClCompile Include="main.cpp" />
    <ClCompile Include="QtProjectV301.cpp" />
  </ItemGroup>
  <ItemGroup>
    <QtMoc Include="QtProjectV301.h" />
  </ItemGroup>
  <ItemGroup>
    <QtUic Include="QtProjectV301.ui" />
  </ItemGroup>
  <ItemGroup>
    <QtRcc Include="QtProjectV301.qrc" />
  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.targets')">
    <Import Project="$(QtMsBuild)\qt.targets" />
  </ImportGroup>
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
</Project>
```

### v3.2

#### `.vcxproj`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="16.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{10A7CC05-6663-4C63-906F-E56EDF8218CA}</ProjectGuid>
    <Keyword>QtVS_v302</Keyword>
    <WindowsTargetPlatformVersion Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">10.0.19041.0</WindowsTargetPlatformVersion>
    <WindowsTargetPlatformVersion Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">10.0.19041.0</WindowsTargetPlatformVersion>
    <QtMsBuild Condition="'$(QtMsBuild)'=='' OR !Exists('$(QtMsBuild)\qt.targets')"
      >$(MSBuildProjectDirectory)\QtMsBuild</QtMsBuild>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v142</PlatformToolset>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v142</PlatformToolset>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <Target Name="QtMsBuildNotFound"
    BeforeTargets="CustomBuild;ClCompile"
    Condition="!Exists('$(QtMsBuild)\qt.targets') or !Exists('$(QtMsBuild)\qt.props')">
    <Message Importance="High"
      Text="QtMsBuild: could not locate qt.targets, qt.props; project may not build correctly." />
  </Target>
  <ImportGroup Label="ExtensionSettings" />
  <ImportGroup Label="Shared" />
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <PropertyGroup Label="UserMacros" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt_defaults.props')">
    <Import Project="$(QtMsBuild)\qt_defaults.props" />
  </ImportGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="QtSettings">
    <QtInstall>msvc2019_64</QtInstall>
    <QtModules>core;gui;widgets</QtModules>
    <QtBuildConfig>debug</QtBuildConfig>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="QtSettings">
    <QtInstall>msvc2019_64</QtInstall>
    <QtModules>core;gui;widgets</QtModules>
    <QtBuildConfig>release</QtBuildConfig>
  </PropertyGroup>
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.props')">
    <Import Project="$(QtMsBuild)\qt.props" />
  </ImportGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="Configuration">
    <ClCompile>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <Optimization>Disabled</Optimization>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <GenerateDebugInformation>true</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="Configuration">
    <ClCompile>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <DebugInformationFormat>None</DebugInformationFormat>
      <Optimization>MaxSpeed</Optimization>
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <GenerateDebugInformation>false</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemGroup>
    <QtRcc Include="QtProjectV302.qrc"/>
    <QtUic Include="QtProjectV302.ui"/>
    <QtMoc Include="QtProjectV302.h"/>
    <ClCompile Include="QtProjectV302.cpp"/>
    <ClCompile Include="main.cpp"/>

  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.targets')">
    <Import Project="$(QtMsBuild)\qt.targets" />
  </ImportGroup>
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
</Project>
```

### v3.3

#### `.vcxproj`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="16.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{812E4050-B861-4918-A0DC-53053B848372}</ProjectGuid>
    <Keyword>QtVS_v303</Keyword>
    <WindowsTargetPlatformVersion Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">10.0.19041.0</WindowsTargetPlatformVersion>
    <WindowsTargetPlatformVersion Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">10.0.19041.0</WindowsTargetPlatformVersion>
    <QtMsBuild Condition="'$(QtMsBuild)'=='' OR !Exists('$(QtMsBuild)\qt.targets')"
      >$(MSBuildProjectDirectory)\QtMsBuild</QtMsBuild>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v142</PlatformToolset>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v142</PlatformToolset>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <Target Name="QtMsBuildNotFound"
    BeforeTargets="CustomBuild;ClCompile"
    Condition="!Exists('$(QtMsBuild)\qt.targets') or !Exists('$(QtMsBuild)\qt.props')">
    <Message Importance="High"
      Text="QtMsBuild: could not locate qt.targets, qt.props; project may not build correctly." />
  </Target>
  <ImportGroup Label="ExtensionSettings" />
  <ImportGroup Label="Shared" />
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <PropertyGroup Label="UserMacros" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt_defaults.props')">
    <Import Project="$(QtMsBuild)\qt_defaults.props" />
  </ImportGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="QtSettings">
    <QtInstall>msvc2019_64</QtInstall>
    <QtModules>core;gui;widgets</QtModules>
    <QtBuildConfig>debug</QtBuildConfig>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="QtSettings">
    <QtInstall>msvc2019_64</QtInstall>
    <QtModules>core;gui;widgets</QtModules>
    <QtBuildConfig>release</QtBuildConfig>
  </PropertyGroup>
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.props')">
    <Import Project="$(QtMsBuild)\qt.props" />
  </ImportGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="Configuration">
    <ClCompile>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <Optimization>Disabled</Optimization>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <GenerateDebugInformation>true</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="Configuration">
    <ClCompile>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <DebugInformationFormat>None</DebugInformationFormat>
      <Optimization>MaxSpeed</Optimization>
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <GenerateDebugInformation>false</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemGroup>
    <QtRcc Include="QtProjectV303.qrc"/>
    <QtUic Include="QtProjectV303.ui"/>
    <QtMoc Include="QtProjectV303.h"/>
    <ClCompile Include="QtProjectV303.cpp"/>
    <ClCompile Include="main.cpp"/>

  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.targets')">
    <Import Project="$(QtMsBuild)\qt.targets" />
  </ImportGroup>
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
</Project>
```

### v3.4

#### `.vcxproj`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="17.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{923588D5-2AA5-4B0F-8110-56BEEBB531D5}</ProjectGuid>
    <Keyword>QtVS_v304</Keyword>
    <WindowsTargetPlatformVersion Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">10.0.19041.0</WindowsTargetPlatformVersion>
    <WindowsTargetPlatformVersion Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">10.0.19041.0</WindowsTargetPlatformVersion>
    <QtMsBuild Condition="'$(QtMsBuild)'=='' OR !Exists('$(QtMsBuild)\qt.targets')"
      >$(MSBuildProjectDirectory)\QtMsBuild</QtMsBuild>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v143</PlatformToolset>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v143</PlatformToolset>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt_defaults.props')">
    <Import Project="$(QtMsBuild)\qt_defaults.props" />
  </ImportGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="QtSettings">
    <QtInstall>5.15.10_msvc2019_64</QtInstall>
    <QtModules>core;gui;widgets</QtModules>
    <QtBuildConfig>debug</QtBuildConfig>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="QtSettings">
    <QtInstall>5.15.10_msvc2019_64</QtInstall>
    <QtModules>core;gui;widgets</QtModules>
    <QtBuildConfig>release</QtBuildConfig>
  </PropertyGroup>
  <Target Name="QtMsBuildNotFound"
    BeforeTargets="CustomBuild;ClCompile"
    Condition="!Exists('$(QtMsBuild)\qt.targets') or !Exists('$(QtMsBuild)\qt.props')">
    <Message Importance="High"
      Text="QtMsBuild: could not locate qt.targets, qt.props; project may not build correctly." />
  </Target>
  <ImportGroup Label="ExtensionSettings" />
  <ImportGroup Label="Shared" />
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
    <Import Project="$(QtMsBuild)\Qt.props" />
  </ImportGroup>
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
    <Import Project="$(QtMsBuild)\Qt.props" />
  </ImportGroup>
  <PropertyGroup Label="UserMacros" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'">
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'">
  </PropertyGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x64'" Label="Configuration">
    <ClCompile>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
      <Optimization>Disabled</Optimization>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <GenerateDebugInformation>true</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x64'" Label="Configuration">
    <ClCompile>
      <TreatWChar_tAsBuiltInType>true</TreatWChar_tAsBuiltInType>
      <MultiProcessorCompilation>true</MultiProcessorCompilation>
      <DebugInformationFormat>None</DebugInformationFormat>
      <Optimization>MaxSpeed</Optimization>
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <GenerateDebugInformation>false</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>
  <ItemGroup>
    <QtRcc Include="QtProjectV304.qrc"/>
    <QtUic Include="QtProjectV304.ui"/>
    <QtMoc Include="QtProjectV304.h"/>
    <ClCompile Include="QtProjectV304.cpp"/>
    <ClCompile Include="main.cpp"/>

  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Condition="Exists('$(QtMsBuild)\qt.targets')">
    <Import Project="$(QtMsBuild)\qt.targets" />
  </ImportGroup>
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
</Project>
```
