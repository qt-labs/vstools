/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.CMake
{
    using static SyntaxAnalysis.RegExpr;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private enum QtStatus { False, NullPresets, ConversionPending, True }

        private QtStatus Status { get; set; } = QtStatus.False;

        private async Task CheckQtStatusAsync()
        {
            await GetAsync("CheckQtStatus");
            if (ActiveProject != this)
                return;
            try {
                await StateMachineAsync();
            } catch (Exception ex) {
                ex.Log();
            }
            Release("CheckQtStatus");
        }

        private async Task StateMachineAsync()
        {
            string[] lists;
            try {
                lists = Directory.GetFiles(RootPath, "CMakeLists.txt", SearchOption.AllDirectories);
            } catch (Exception ex) {
                ex.Log();
                return;
            }

            switch (Status) {
            case QtStatus.False:
            case QtStatus.NullPresets:
                if (HasQtReference(lists))
                    Status = TryLoadQtConfig() ? QtStatus.True : QtStatus.ConversionPending;
                break;
            case QtStatus.ConversionPending:
                return;
            case QtStatus.True:
                if (!HasQtReference(lists))
                    Status = QtStatus.False;
                else if (!TryLoadQtConfig())
                    Status = QtStatus.ConversionPending;
                break;
            }

            switch (Status) {
            case QtStatus.False:
                return;
            case QtStatus.NullPresets:
                try {
                    if (File.ReadAllText(PresetsPath) == NullPresetsText)
                        File.Delete(PresetsPath);
                    if (File.ReadAllText(UserPresetsPath) == NullPresetsText)
                        File.Delete(UserPresetsPath);
                } catch (Exception ex) {
                    ex.Log();
                }
                Status = QtStatus.False;
                return;
            case QtStatus.ConversionPending:
                if (!IsAutoConfigurable()) {
                    await ShowConversionConfirmationAsync();
                    return;
                }
                Status = QtStatus.True;
                break;
            case QtStatus.True:
                break;
            }

            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            VerifyChecksums();
            CheckQtPresets();
            CheckQtVersions();
            CheckVisiblePresets();
            if (SaveIfRequired() && Index != null)
                await Index.InvalidateFileScannerCache();
        }

        private bool HasQtReference(IEnumerable<string> listFiles)
        {
            foreach (var listFile in listFiles) {
                var listFilePath = Path.Combine(RootPath, listFile);
                if (!File.Exists(listFilePath))
                    continue;
                try {
                    if (!CMakeListsParser.Parse(File.ReadAllText(listFilePath)).Any())
                        continue;
                    if (IsCompatible())
                        return true;
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(ShowIncompatibleProjectAsync);
                    return false;
                } catch (ParseErrorException) {
                }
            }
            return false;
        }

        private bool TryLoadQtConfig()
        {
            if (File.Exists(PresetsPath))
                Presets = JObject.Parse(File.ReadAllText(PresetsPath));
            else
                Presets = NullPresets.DeepClone() as JObject;
            if (File.Exists(UserPresetsPath))
                UserPresets = JObject.Parse(File.ReadAllText(UserPresetsPath));
            else
                UserPresets = NullPresets.DeepClone() as JObject;

            return Presets?["vendor"]?["qt-project.org/Presets"] != null
                || UserPresets?["vendor"]?["qt-project.org/Presets"] != null;
        }

        private bool SaveIfRequired()
        {
            var isDirty = false;
            var records = Presets.Descendants()
                .Union(UserPresets.Descendants())
                .Append(Presets)
                .Append(UserPresets)
                .Select(x => new
                {
                    Self = x as JObject,
                    Info = RecordInfo(x as JObject)
                })
                .Select(x => new
                {
                    x.Self,
                    x.Info,
                    Checksum = x.Info?.Value["checksum"]
                })
                .Where(x => x.Info != null)
                .ToList();
            foreach (var record in records) {
                var oldChecksum = record.Checksum?.Value<string>();
                var newChecksum = EvalChecksum(record.Self);
                if (oldChecksum == newChecksum)
                    continue;
                isDirty = true;
                record.Info.Value["checksum"] = newChecksum;
            }
            if (isDirty) {
                File.WriteAllText(PresetsPath, Presets.ToString(Formatting.Indented));
                File.WriteAllText(UserPresetsPath, UserPresets.ToString(Formatting.Indented));
            }
            return isDirty;
        }

        private bool IsCompatible()
        {
            return !File.Exists(SettingsPath);
        }

        private static bool IsProjectFile(string path)
        {
            return ProjectFileNames.Contains(Path.GetFileName(path));
        }

        private bool IsAutoConfigurable()
        {
            var configs = Presets?["configurePresets"]?.Cast<JObject>();
            return configs == null || configs.All(x => x["hidden"] is JValue y && y.Value<bool>());
        }
    }
}
