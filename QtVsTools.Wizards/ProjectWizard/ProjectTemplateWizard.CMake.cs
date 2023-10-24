/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Core;
    using Core.CMake;
    using Json;
    using static Utils;
    using static QtVsTools.Common.EnumExt;

    public abstract partial class ProjectTemplateWizard : IWizard
    {
        protected enum CMake
        {
            // Parameters for expanding CMake project files
            [String("cmake_user_presets")] UserPresets,
            [String("cmake_qt_modules")] Modules,
            [String("cmake_qt_libs")] Libs,
            [String("cmake_qt_helper")] Helper,
            [String("cmake_project_sources")] ProjectSources
        }

        [DataContract]
        protected class CMakeConfigPreset
        {
            [DataMember(Name = "name", Order = 0)]
            public string Name { get; set; }

            [DataMember(Name = "displayName", EmitDefaultValue = false, Order = 1)]
            public string DisplayName { get; set; }

            [DataMember(Name = "inherits", EmitDefaultValue = false, Order = 2)]
            public List<string> Inherits { get; set; }

            [DataMember(Name = "binaryDir", EmitDefaultValue = false, Order = 3)]
            public string BinaryDir { get; set; }

            [DataContract]
            public class ConfigCacheVariables
            {
                [DataMember(Name = "CMAKE_BUILD_TYPE", EmitDefaultValue = false)]
                public string CMakeBuildType { get; set; }

                [DataMember(Name = "CMAKE_CXX_FLAGS", EmitDefaultValue = false)]
                public string CxxFlags { get; set; }

                [DataMember(Name = "QT_MODULES", EmitDefaultValue = false)]
                public string QtModules { get; set; }
            }

            [DataMember(Name = "cacheVariables", EmitDefaultValue = false, Order = 6)]
            public ConfigCacheVariables CacheVariables { get; set; }

            [DataContract]
            public class ConfigEnvironment
            {
                [DataMember(Name = "QTDIR", EmitDefaultValue = false, Order = 0)]
                public string QtDir { get; set; }

                [DataMember(Name = "PATH", EmitDefaultValue = false, Order = 1)]
                public string Path { get; set; }

                [DataMember(Name = "QML_DEBUG_ARGS", EmitDefaultValue = false, Order = 2)]
                public string QmlDebugArgs { get; set; }
            }

            [DataMember(Name = "environment", EmitDefaultValue = false, Order = 7)]
            public ConfigEnvironment Environment { get; set; }
        }

        [DataContract]
        protected class CMakePresets : Serializable<CMakePresets>
        {
            [DataMember(Name = "version", Order = 0)]
            public int Version { get; set; } = 3;

            [DataMember(Name = "configurePresets", Order = 1)]
            public List<CMakeConfigPreset> ConfigurePresets { get; set; }

            public CMakePresets()
            {
                ConfigurePresets = new List<CMakeConfigPreset>();
            }
        }

        protected bool UseQtCMakeHelper { get; set; } = true;

        private bool IsCMakeFile(string fileName)
        {
            return fileName.Equals("CMakeLists.txt", IgnoreCase)
                || fileName.Equals("CMakeUserPresets.json", IgnoreCase)
                || !UseQtCMakeHelper || fileName.Equals("qt.cmake", IgnoreCase);
        }

        private void ExpandCMake()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Parameter[NewProject.ProjectItems] += string.Join(string.Empty,
                "<None Include=\"CMakeLists.txt\" />\r\n",
                "<None Include=\"CMakeUserPresets.json\" />\r\n",
                UseQtCMakeHelper ? "<None Include=\"qt.cmake\" />\r\n" : "");

            Parameter[CMake.ProjectSources] = "";
            if (Parameter[NewProject.ResourceFile] is { Length: > 0 } rcFile)
                Parameter[CMake.ProjectSources] += $"    {rcFile}\r\n";

            var userPresets = new CMakePresets();
            var qtModules = new SortedSet<string>();
            foreach (IWizardConfiguration config in Configurations) {
                var modules = QtModules.Instance
                    .GetAvailableModules(config.QtVersion.qtMajor)
                    .Join(config.Modules,
                        x => x.proVarQT, y => y, comparer: CaseIgnorer,
                        resultSelector: (x, y) => x.LibraryPrefix.Substring(2));
                foreach (var module in modules)
                    qtModules.Add(module);
                userPresets.ConfigurePresets.Add(ConfigureCMakePreset(config));
            }
            Parameter[CMake.UserPresets] = userPresets.ToJsonString();
            Parameter[CMake.Modules] = string.Join("\r\n        ", qtModules);
            Parameter[CMake.Libs] = string.Join("\r\n        ",
                qtModules.Select(module => $"Qt::{module}"));
            Parameter[CMake.Helper] = QtCMakeHelper;
            Parameter[NewProject.Globals] += @"<QT_CMAKE_TEMPLATE>true</QT_CMAKE_TEMPLATE>";
        }

        protected virtual CMakeConfigPreset ConfigureCMakePreset(IWizardConfiguration config)
        {
            var configPreset = new CMakeConfigPreset
            {
                Name = $"{config.Name}-{config.Platform}",
                DisplayName = $"{config.Name} ({config.Platform})",
                BinaryDir = "${sourceDir}/out/build",
                CacheVariables = new CMakeConfigPreset.ConfigCacheVariables
                {
                    CMakeBuildType = config.IsDebug ? "Debug" : "Release"
                }
            };
            return configPreset;
        }

        protected virtual void OpenCMakeProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionDir = new Uri(
                Path.Combine(Path.GetFullPath(Parameter[NewProject.SolutionDirectory]), "."));
            var projectDir = new Uri(
                Path.Combine(Path.GetFullPath(Parameter[NewProject.DestinationDirectory]), "."));
            if (!Directory.Exists(solutionDir.LocalPath) || !solutionDir.IsBaseOf(projectDir))
                solutionDir = projectDir;

            if (solutionDir != projectDir) {
                File.Move(
                    Path.Combine(projectDir.LocalPath, "CMakeUserPresets.json"),
                    Path.Combine(solutionDir.LocalPath, "CMakeUserPresets.json"));
                var project = Path.GetFileName(solutionDir.LocalPath
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                var subDir = solutionDir.MakeRelativeUri(projectDir).ToString()
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                File.WriteAllText(Path.Combine(solutionDir.LocalPath, "CMakeLists.txt"), $@"
cmake_minimum_required(VERSION 3.16)

project(""{project}"")

add_subdirectory(""{subDir}"")
".Trim('\r', '\n'));
            }
            CMakeProject.Convert(solutionDir.LocalPath);
            Dte.ExecuteCommand("File.OpenFolder", solutionDir.LocalPath);
        }

        private static string QtCMakeHelper { get; } = @"
if(QT_VERSION VERSION_LESS 6.3)
    macro(qt_standard_project_setup)
        set(CMAKE_AUTOMOC ON)
        set(CMAKE_AUTORCC ON)
        set(CMAKE_AUTOUIC ON)
    endmacro()
endif()

if(QT_VERSION VERSION_LESS 6.0)
    macro(qt_add_executable name)
         if(ANDROID)
            add_library(name SHARED ${ARGN})
        else()
            add_executable(${ARGV})
        endif()
    endmacro()
endif()
".Trim('\r', '\n');
    }
}
