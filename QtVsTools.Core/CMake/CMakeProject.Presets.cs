// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace QtVsTools.Core.CMake
{
    using Common;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private async Task<bool> TryLoadPresetsAsync()
        {
            if (File.Exists(PresetsPath))
                Presets = JObject.Parse(await Utils.ReadAllTextAsync(PresetsPath));
            else
                Presets = NullPresets.DeepClone() as JObject;
            if (File.Exists(UserPresetsPath))
                UserPresets = JObject.Parse(await Utils.ReadAllTextAsync(UserPresetsPath));
            else
                UserPresets = NullPresets.DeepClone() as JObject;

            return Presets?["vendor"]?["qt-project.org/Presets"] != null
                || UserPresets?["vendor"]?["qt-project.org/Presets"] != null;
        }

        private void CheckQtPresets()
        {
            if (Presets["configurePresets"] == null) {
                Presets["configurePresets"] = new JArray();
                TouchRecord(Presets);
            }
            if (Presets["vendor"] == null) {
                Presets["vendor"] = new JObject();
                TouchRecord(Presets);
            }
            if (Presets["vendor"]?["qt-project.org/Presets"] == null) {
                Presets["vendor"]!["qt-project.org/Presets"] = new JObject();
                TouchRecord(Presets);
            }

            if (UserPresets["configurePresets"] == null) {
                UserPresets["configurePresets"] = new JArray();
                TouchRecord(UserPresets);
            }
            if (UserPresets["vendor"] == null) {
                UserPresets["vendor"] = new JObject();
                TouchRecord(UserPresets);
            }
            if (UserPresets["vendor"]?["qt-project.org/Presets"] == null) {
                UserPresets["vendor"]!["qt-project.org/Presets"] = new JObject();
                TouchRecord(UserPresets);
            }

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
                    ["vendor"] = new JObject
                    {
                        ["qt-project.org/Qt"] = new JObject()
                    }
                };
                (Presets["configurePresets"] as JArray)?.Add(pathPreset);
                TouchRecord(Presets);
                TouchRecord(pathPreset as JObject);
            }

            var versionPresets = UserPresets["configurePresets"]
                .Where(x => x["vendor"]?["qt-project.org/Version"] != null)
                .ToList();
            foreach (var versionPreset in versionPresets) {
                if (VersionInformation.GetOrAddByName((string)versionPreset["name"]) is { } version) {
                    var qtDir = HelperFunctions.FromNativeSeparators(version.InstallPrefix);
                    var presetQtDir = versionPreset["environment"]?["QTDIR"]?.Value<string>();
                    if (string.Equals(qtDir, presetQtDir, Utils.IgnoreCase))
                        continue;
                    (versionPreset["environment"] ??= new JObject())["QTDIR"] = qtDir;
                    TouchRecord(versionPreset as JObject);
                } else {
                    versionPreset.Remove();
                    TouchRecord(UserPresets);
                }
            }

            var defaultVersion = QtVersionManager.GetDefaultVersion();
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

            if (defaultPreset != null) {
                defaultPreset.Self.Remove();
                TouchRecord(UserPresets);
            }
            var qtDefaultPreset = new JObject
            {
                ["hidden"] = true,
                ["name"] = "Qt-Default",
                ["inherits"] = defaultVersion,
                ["vendor"] = new JObject
                {
                    ["qt-project.org/Default"] = new JObject()
                }
            };
            (UserPresets["configurePresets"] as JArray)?.Add(qtDefaultPreset);
            TouchRecord(UserPresets);
            TouchRecord(qtDefaultPreset);
        }

        private static IEnumerable<string> PresetInherits(JToken presetToken)
        {
            if (presetToken is not JObject preset || preset["inherits"] is not { } inherits)
                return Array.Empty<string>();
            return inherits switch
            {
                JValue value => new[] { value.Value<string>() },
                JArray array => array.Values<string>(),
                _ => Array.Empty<string>()
            };
        }

        private static bool SetValue(JObject obj, string key, JToken value)
        {
            if (JToken.DeepEquals(obj?[key], value))
                return false;
            obj[key] = value;
            return true;
        }

        private void NormalizeQtVersionPreset(JObject preset, VersionInformation versionInfo)
        {
            if (preset == null || versionInfo == null)
                return;

            var changed = false;
            changed |= SetValue(preset, "hidden", true);
            changed |= SetValue(preset, "inherits", "Qt");
            changed |= SetValue(preset, "generator", "Ninja");

            if (preset["environment"] is not JObject environment) {
                environment = new JObject();
                preset["environment"] = environment;
                changed = true;
            }
            changed |= SetValue(environment, "QTDIR",
                HelperFunctions.FromNativeSeparators(versionInfo.InstallPrefix));

            if (preset["architecture"] is not JObject architecture) {
                architecture = new JObject();
                preset["architecture"] = architecture;
                changed = true;
            }
            changed |= SetValue(architecture, "strategy", "external");
            changed |= SetValue(architecture, "value", versionInfo.Platform switch {
                Platform.x86 => "x86",
                Platform.x64 => "x64",
                Platform.arm64 => "arm64",
                _ => null
            });

            if (changed)
                TouchRecord(preset);
        }

        private void CheckQtVersions(bool strict)
        {
            var versionRecords = GetRecords(UserPresets, "qt-project.org/Version")
                .ToDictionary(x => x["name"], x => x);

            var missingVersionNames = QtVersionManager.GetVersions()
                .Where(x => !versionRecords.ContainsKey(x));

            var missingVersions = missingVersionNames
                .Select(name => (Name: name, Info: VersionInformation.GetOrAddByName(name)))
                .Where(x => x.Info != null && !string.IsNullOrEmpty(x.Info.InstallPrefix));

            foreach (var (name, missingVersion) in missingVersions) {
                var platform = missingVersion.Platform;
                var qtDir = missingVersion.InstallPrefix;
                var versionPreset = new JObject
                {
                    ["hidden"] = true,
                    ["name"] = name,
                    ["inherits"] = "Qt",
                    ["environment"] = new JObject
                    {
                        ["QTDIR"] = HelperFunctions.FromNativeSeparators(qtDir)
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
                };
                (UserPresets["configurePresets"] as JArray)?.Add(versionPreset);
                TouchRecord(UserPresets);
                TouchRecord(versionPreset);
            }

            if (!strict)
                return;

            foreach (var versionRecord in versionRecords.Values) {
                var versionName = versionRecord["name"]?.Value<string>();
                if (string.IsNullOrEmpty(versionName))
                    continue;
                var versionInfo = VersionInformation.GetOrAddByName(versionName);
                if (versionInfo == null || string.IsNullOrEmpty(versionInfo.InstallPrefix))
                    continue;
                NormalizeQtVersionPreset(versionRecord, versionInfo);
            }
        }

        private void CheckVisiblePresets()
        {
            var visiblePresets = UserPresets["configurePresets"]?
                .Children<JObject>()
                .Where(preset => !preset.ContainsKey("hidden") || !(bool)preset["hidden"])
                .ToList();

            if (visiblePresets == null || !visiblePresets.Any()) {
                var releasePreset = new JObject
                {
                    ["name"] = "Qt-Release",
                    ["inherits"] = "Qt-Default",
                    ["binaryDir"] = "${sourceDir}/out/build/release",
                    ["cacheVariables"] = new JObject
                    {
                        ["CMAKE_BUILD_TYPE"] = "Release"
                    }
                };
                var debugPreset = new JObject
                {
                    ["name"] = "Qt-Debug",
                    ["inherits"] = "Qt-Default",
                    ["binaryDir"] = "${sourceDir}/out/build/debug",
                    ["cacheVariables"] = new JObject
                    {
                        ["CMAKE_BUILD_TYPE"] = "Debug",
                        ["CMAKE_CXX_FLAGS"] = "-DQT_QML_DEBUG"
                    },
                    ["environment"] = new JObject
                    {
                        ["QML_DEBUG_ARGS"] = $"-qmljsdebugger=file:{{{Guid.NewGuid()}}},block"
                    }
                };
                (UserPresets["configurePresets"] as JArray)?.AddFirst(releasePreset);
                (UserPresets["configurePresets"] as JArray)?.AddFirst(debugPreset);
                TouchRecord(UserPresets);
                TouchRecord(releasePreset);
                TouchRecord(debugPreset);
                return;
            }

            var versionNames = QtVersionManager.GetVersions().Prepend("Qt-Default").ToHashSet();

            // All visible presets must have a reference to a Qt version
            bool IsQtVersion(JToken presetName) => versionNames.Contains(presetName.ToString());

            var presetsMissingQtRef = visiblePresets
                .Where(preset => !preset.ContainsKey("inherits")
                    || (preset["inherits"] is JArray inherits && !inherits.Any(IsQtVersion))
                    || (preset["inherits"] is JValue presetName && !IsQtVersion(presetName)));
            foreach (var preset in presetsMissingQtRef) {
                if (!preset.ContainsKey("inherits"))
                    preset["inherits"] = new JArray();
                else if (preset["inherits"] is not JArray) {
                    var inherits = (string)preset["inherits"];
                    preset["inherits"] = inherits == null ? new JArray() : new JArray {inherits};
                }
                (preset["inherits"] as JArray)?.Add("Qt-Default");
                TouchRecord(preset);
            }
        }
    }
}
