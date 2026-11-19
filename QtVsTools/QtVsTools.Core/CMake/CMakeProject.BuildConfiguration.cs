// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace QtVsTools.Core.CMake
{
    public partial class CMakeProject
    {
        public string GetConfigurationFileName()
        {
            return HelperFunctions.ToNativeSeparator($@"{RootPath}\.vs\ProjectSettings.json");
        }

        public string GetActiveConfigurationName()
        {
            // first try the file created by Visual Studio
            var activeConfigFilename = GetConfigurationFileName();
            if (File.Exists(activeConfigFilename)) {
                try {
                    var json = JObject.Parse(File.ReadAllText(activeConfigFilename));
                    return json["CurrentProjectSetting"]?.ToString();
                } catch (Exception exception) {
                    exception.Log();
                }
            }

            // resort to CMakeUserPresets.json if we failed previously
            var userPresets = $@"{RootPath}\CMakeUserPresets.json";
            if (!File.Exists(userPresets))
                return null;

            try {
                var json = JObject.Parse(File.ReadAllText(userPresets));
                var configurePresets = json["configurePresets"] as JArray;
                var firstPreset = configurePresets?[0] as JObject;
                return firstPreset?["displayName"]?.ToString();
            } catch (Exception exception) {
                exception.Log();
            }

            return null;
        }
    }
}
