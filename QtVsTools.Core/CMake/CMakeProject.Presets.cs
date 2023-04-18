/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace QtVsTools.Core.CMake
{
    using static Instances;
    using static Utils;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
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
                        ["qt-project.org/Qt"] = new JObject { }
                    }
                };
                (Presets["configurePresets"] as JArray).Add(pathPreset);
            }

            var versionNames = VersionManager.GetVersions().ToHashSet();
            var versionPresets = UserPresets["configurePresets"]
                .Where(x => x["vendor"]?["qt-project.org/Version"] != null);
            foreach (var versionPreset in versionPresets.ToList()) {
                if (VersionManager.GetVersionInfo((string)versionPreset["name"]) is var version) {
                    var qtDir = version.InstallPrefix.Replace('\\', '/');
                    var presetQtDir = versionPreset["environment"]?["QTDIR"]?.Value<string>();
                    if (qtDir.Equals(presetQtDir, IgnoreCase))
                        continue;
                    (versionPreset["environment"] ??= new JObject())["QTDIR"] = qtDir;
                } else {
                    versionPreset.Remove();
                }
            }

            var defaultVersionName = VersionManager.GetDefaultVersion();
            var defaultVersionPreset = UserPresets["configurePresets"]
                .Where(x => x["vendor"]?["qt-project.org/Default"] != null).FirstOrDefault();
            if (defaultVersionPreset == null
                || (
                    defaultVersionPreset["name"].Value<string>() is string name
                    && name != "Qt-Default"
                ) || (
                    defaultVersionPreset["inherits"].Value<string>() is string inherits
                    && inherits != defaultVersionName
                )) {
                defaultVersionPreset?.Remove();
                (UserPresets["configurePresets"] as JArray).Add(new JObject
                {
                    ["hidden"] = true,
                    ["name"] = "Qt-Default",
                    ["inherits"] = defaultVersionName,
                    ["vendor"] = new JObject
                    {
                        ["qt-project.org/Default"] = new JObject()
                    }
                });
            }
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
                (UserPresets["configurePresets"] as JArray).Add(new JObject
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
                        ["strategy"] = "set",
                        ["value"]
                            = platform == Platform.x86 ? "x86"
                            : platform == Platform.x64 ? "x64"
                            : platform == Platform.arm64 ? "arm64"
                            : null
                    },
                    ["generator"] = "Visual Studio 16 2019",
                    ["vendor"] = new JObject
                    {
                        ["qt-project.org/Version"] = new JObject()
                    }
                });
            }
        }

        private void CheckVisiblePresets()
        {
            var visiblePresets = Presets["configurePresets"]?
                .Children<JObject>()
                .Where(preset => !preset.ContainsKey("hidden") || !(bool)preset["hidden"]);

            if (visiblePresets != null) {
                // CMakePresets.json should have no visible presets
                foreach (var preset in visiblePresets) {
                    var presetName = preset["name"]?.Value<string>();
                    if (string.IsNullOrEmpty(presetName))
                        continue;
                    if (preset["inherits"] is not JArray presetInherits)
                        presetInherits = new JArray();
                    if (preset["inherits"] is JValue)
                        presetInherits.Add((string)preset["inherits"]);
                    var userPresetInherits = new JArray(presetInherits)
                    {
                        $"_{presetName}"
                    };

                    if (!preset.ContainsKey("inherits"))
                        preset["inherits"] = new JArray();
                    else if (preset["inherits"] is not JArray)
                        preset["inherits"] = new JArray { (string)preset["inherits"] };
                    (preset["inherits"] as JArray).Add("Qt-Default");

                    (UserPresets["configurePresets"] as JArray).AddFirst(new JObject
                    {
                        ["name"] = presetName,
                        ["inherits"] = userPresetInherits
                    });
                    preset["name"] = $"_{presetName}";
                    preset["hidden"] = true;
                    preset.Remove("inherits");
                }
            }

            visiblePresets = UserPresets["configurePresets"]?
                .Children<JObject>()
                .Where(preset => !preset.ContainsKey("hidden") || !(bool)preset["hidden"]);

            if (visiblePresets == null || visiblePresets.Count() == 0) {
                (UserPresets["configurePresets"] as JArray).AddFirst(new JObject
                {
                    ["name"] = "Debug",
                    ["inherits"] = "Qt-Default",
                    ["cacheVariables"] = new JObject
                    {
                        ["CMAKE_BUILD_TYPE"] = "Debug"
                    }
                });
                return;
            }

            var versionNames = VersionManager.GetVersions().Prepend("Qt-Default");

            // All visible presets must have a reference to a Qt version
            var presetsMissingQtRef = visiblePresets.Where(preset =>
                !preset.ContainsKey("inherits")
                || (preset["inherits"] is JArray inherits
                    && !inherits.Any(presetName => versionNames.Contains((string)presetName)))
                || (preset["inherits"] is JValue
                    && preset["inherits"].Value<string>() is var presetName
                    && !versionNames.Contains(presetName)));
            foreach (var preset in presetsMissingQtRef) {
                if (!preset.ContainsKey("inherits"))
                    preset["inherits"] = new JArray();
                else if (preset["inherits"] is not JArray)
                    preset["inherits"] = new JArray { (string)preset["inherits"] };
                (preset["inherits"] as JArray).Add("Qt-Default");
            }
        }
    }
}