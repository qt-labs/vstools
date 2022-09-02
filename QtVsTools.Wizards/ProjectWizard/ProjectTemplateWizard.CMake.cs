/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Core;
    using Json;

    using static StringComparer;
    using static QtVsTools.Common.EnumExt;

    public abstract partial class ProjectTemplateWizard : IWizard
    {
        protected enum CMake
        {
            // Parameters for expanding CMake project files
            [String("cmake_presets")] Presets,
            [String("cmake_user_presets")] UserPresets,
        }

        protected enum CMakeGenerators
        {
            [String("Visual Studio 15 2017")] VS2017,
            [String("Visual Studio 16 2019")] VS2019,
            [String("Visual Studio 17 2022")] VS2022,
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

            [DataMember(Name = "hidden", EmitDefaultValue = false, Order = 3)]
            public bool? Hidden { get; set; }

            [DataMember(Name = "generator", EmitDefaultValue = false, Order = 4)]
            public string Generator { get; set; }

            [DataMember(Name = "architecture", EmitDefaultValue = false, Order = 5)]
            public string Architecture { get; set; }

            [DataContract]
            public class ConfigCacheVariables
            {
                [DataMember(Name = "CMAKE_BUILD_TYPE", EmitDefaultValue = false)]
                public string CMakeBuildType { get; set; }

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
                public string Path { get; set; } = "$penv{PATH};$env{QTDIR}\\bin";
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

        private Regex[] CMakeFilenamePatterns => Lazy.Get(() =>
            CMakeFilenamePatterns, () => new[] { new Regex(@"CMake.*") });

        private bool IsCMakeFile(string fileName)
        {
            return CMakeFilenamePatterns.Any(p => p.IsMatch(fileName));
        }

        private void ExpandCMake()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Parameter[NewProject.ProjectItems] += @"
    <None Include=""CMakeLists.txt"" />
    <None Include=""CMakePresets.json"" />
    <None Include=""CMakeUserPresets.json"" />
";

            var presets = new CMakePresets();
            var userPresets = new CMakePresets();
            foreach (IWizardConfiguration config in Configurations) {
                var modules = QtModules.Instance
                    .GetAvailableModules(config.QtVersion.qtMajor)
                    .Join(config.Modules,
                        x => x.proVarQT, y => y, comparer: InvariantCultureIgnoreCase,
                        resultSelector: (x, y) => x.LibraryPrefix.Substring(2));

                var configPreset = new CMakeConfigPreset
                {
                    Name = $"{config.Name}-{config.Platform}",
                    Hidden = true,
                    Generator
                        = Dte.Version.StartsWith("17.") ? CMakeGenerators.VS2022.Cast<string>()
                        : Dte.Version.StartsWith("16.") ? CMakeGenerators.VS2019.Cast<string>()
                        : Dte.Version.StartsWith("15.") ? CMakeGenerators.VS2017.Cast<string>()
                        : null,
                    Architecture = config.Platform,
                    CacheVariables = new CMakeConfigPreset.ConfigCacheVariables
                    {
                        QtModules = string.Join(";", modules),
                        CMakeBuildType = config.IsDebug ? "Debug" : "Release"
                    }
                };
                presets.ConfigurePresets.Add(configPreset);

                configPreset = new CMakeConfigPreset
                {
                    Name = $"Qt-{config.Name}-{config.Platform}",
                    Inherits = new List<string>
                    {
                        $"{config.Name}-{config.Platform}"
                    },
                    DisplayName= $"{config.Name} ({config.Platform})",
                    Environment = new CMakeConfigPreset.ConfigEnvironment
                    {
                        QtDir = config.QtVersion.qtDir,
                    }
                };
                userPresets.ConfigurePresets.Add(configPreset);
            }
            Parameter[CMake.Presets] = presets.ToJsonString();
            Parameter[CMake.UserPresets] = userPresets.ToJsonString();
            Parameter[NewProject.Globals] += @"<QT_CMAKE_TEMPLATE>true</QT_CMAKE_TEMPLATE>";
        }
    }
}
