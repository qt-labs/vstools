/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace QtVsTools.Core.CMake
{
    using static Instances;
    using static Common.Utils;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private async Task<bool> TryLoadPresetsAsync()
        {
            if (File.Exists(PresetsPath))
                Presets = JObject.Parse(await ReadAllTextAsync(PresetsPath));
            else
                Presets = NullPresets.DeepClone() as JObject;
            if (File.Exists(UserPresetsPath))
                UserPresets = JObject.Parse(await ReadAllTextAsync(UserPresetsPath));
            else
                UserPresets = NullPresets.DeepClone() as JObject;

            return Presets?["vendor"]?["qt-project.org/Presets"] != null
                || UserPresets?["vendor"]?["qt-project.org/Presets"] != null;
        }

        private void CheckQtPresets()
        {
            Presets["configurePresets"] ??= new JArray();
            Presets["vendor"] ??= new JObject();
            Presets["vendor"]["qt-project.org/Presets"] ??= new JObject();

            UserPresets["configurePresets"] ??= new JArray();
            UserPresets["vendor"] ??= new JObject();
            UserPresets["vendor"]["qt-project.org/Presets"] ??= new JObject();

            var pathPreset = Presets["configurePresets"]
                .FirstOrDefault(x => x["vendor"]?["qt-project.org/Qt"] != null);
            if (pathPreset == null) {
                pathPreset = new JObject
                {
                    ["hidden"] = true,
                    ["name"] = "Qt",
                    ["cacheVariables"] = new JObject
                    {
                        ["CMAKE_PREFIX_PATH"] = "$env{QTDIR}"
                    },
                    ["environment"] = new JObject
                    {
                        ["PATH"] = "$penv{PATH};$env{QTDIR}/bin"
                    },
                    ["vendor"] = new JObject
                    {
                        ["qt-project.org/Qt"] = new JObject()
                    }
                };
                (Presets["configurePresets"] as JArray)?.Add(pathPreset);
            }

            var versionPresets = UserPresets["configurePresets"]
                .Where(x => x["vendor"]?["qt-project.org/Version"] != null)
                .ToList();
            foreach (var versionPreset in versionPresets) {
                if (VersionManager.GetVersionInfo((string)versionPreset["name"]) is { } version) {
                    var qtDir = version.InstallPrefix.Replace('\\', '/');
                    var presetQtDir = versionPreset["environment"]?["QTDIR"]?.Value<string>();
                    if (qtDir.Equals(presetQtDir, IgnoreCase))
                        continue;
                    (versionPreset["environment"] ??= new JObject())["QTDIR"] = qtDir;
                } else {
                    versionPreset.Remove();
                }
            }

            var defaultVersion = VersionManager.GetDefaultVersion();
            var defaultPreset = UserPresets["configurePresets"]
                .Select(x => new
                {
                    Self = x,
                    IsDefault = x["vendor"]?["qt-project.org/Default"] is not null,
                    Name = x?["name"]?.Value<string>(),
                    InheritsDefault = PresetInherits(x).Any(y => y == defaultVersion)
                })
                .FirstOrDefault(x => x.IsDefault);

            if (defaultPreset is { Name: "Qt-Default", InheritsDefault: true })
                return;

            defaultPreset?.Self.Remove();
            (UserPresets["configurePresets"] as JArray)?.Add(new JObject
            {
                ["hidden"] = true,
                ["name"] = "Qt-Default",
                ["inherits"] = defaultVersion,
                ["vendor"] = new JObject
                {
                    ["qt-project.org/Default"] = new JObject()
                }
            });
        }

        private static IEnumerable<string> PresetInherits(JToken presetToken)
        {
            if (presetToken is not JObject preset)
                return Array.Empty<string>();
            if (preset["inherits"] is not { } inherits)
                return Array.Empty<string>();
            return inherits switch
            {
                JValue value => new[] { value.Value<string>() },
                JArray array => array.Values<string>(),
                _ => Array.Empty<string>()
            };
        }

        private void CheckQtVersions()
        {
            var versionRecords = GetRecords(UserPresets, "qt-project.org/Version")
                .ToDictionary(x => x["name"], x => x);

            var missingVersionNames = VersionManager.GetVersions()
                .Where(x => !versionRecords.ContainsKey(x));

            var missingVersions = missingVersionNames
                .Select(VersionManager.GetVersionInfo)
                .Where(x => x != null && !string.IsNullOrEmpty(x.InstallPrefix));

            foreach (var missingVersion in missingVersions) {
                var platform = missingVersion.platform();
                var qtDir = missingVersion.InstallPrefix;
                (UserPresets["configurePresets"] as JArray)?.Add(new JObject
                {
                    ["hidden"] = true,
                    ["name"] = missingVersion.name,
                    ["inherits"] = "Qt",
                    ["environment"] = new JObject
                    {
                        ["QTDIR"] = qtDir.Replace('\\', '/')
                    },
                    ["architecture"] = new JObject
                    {
                        ["strategy"] = "external",
                        ["value"] = platform switch {
                            Platform.x86 => "x86",
                            Platform.x64 => "x64",
                            Platform.arm64 => "arm64",
                            _ => null
                        }
                    },
                    ["generator"] = "Ninja",
                    ["vendor"] = new JObject
                    {
                        ["qt-project.org/Version"] = new JObject()
                    }
                });
            }
        }

        private void CheckVisiblePresets()
        {
            var visiblePresets = UserPresets["configurePresets"]?
                .Children<JObject>()
                .Where(preset => !preset.ContainsKey("hidden") || !(bool)preset["hidden"])
                .ToList();

            if (visiblePresets == null || !visiblePresets.Any()) {
                (UserPresets["configurePresets"] as JArray)?.AddFirst(new JObject
                {
                    ["name"] = "Qt-Release",
                    ["inherits"] = "Qt-Default",
                    ["binaryDir"] = "${sourceDir}/out/build",
                    ["cacheVariables"] = new JObject
                    {
                        ["CMAKE_BUILD_TYPE"] = "Release"
                    }
                });
                (UserPresets["configurePresets"] as JArray)?.AddFirst(new JObject
                {
                    ["name"] = "Qt-Debug",
                    ["inherits"] = "Qt-Default",
                    ["binaryDir"] = "${sourceDir}/out/build",
                    ["cacheVariables"] = new JObject
                    {
                        ["CMAKE_BUILD_TYPE"] = "Debug",
                        ["CMAKE_CXX_FLAGS"] = "-DQT_QML_DEBUG"
                    },
                    ["environment"] = new JObject
                    {
                        ["QML_DEBUG_ARGS"] = $"-qmljsdebugger=file:{{{Guid.NewGuid()}}},block"
                    }
                });
                return;
            }

            var versionNames = VersionManager.GetVersions().Prepend("Qt-Default").ToHashSet();

            // All visible presets must have a reference to a Qt version
            bool isQtVersion(JToken presetName) => versionNames.Contains(presetName.ToString());
            var presetsMissingQtRef = visiblePresets.Where(preset => !preset.ContainsKey("inherits")
                || (preset["inherits"] is JArray inherits && !inherits.Any(isQtVersion))
                || (preset["inherits"] is JValue presetName && !isQtVersion(presetName)));
            foreach (var preset in presetsMissingQtRef) {
                if (!preset.ContainsKey("inherits"))
                    preset["inherits"] = new JArray();
                else if (preset["inherits"] is not JArray)
                    preset["inherits"] = new JArray { (string)preset["inherits"] };
                (preset["inherits"] as JArray)?.Add("Qt-Default");
            }
        }
    }
}
