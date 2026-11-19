// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QtVsTools.Core.CMake
{
    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private static JProperty RecordInfo(JObject record)
        {
            return record?["vendor"]?
                .Children<JProperty>()
                .FirstOrDefault(x => x.Name.StartsWith("qt-project.org"));
        }

        private IEnumerable<JObject> GetRecords(JObject root, string recordType = null)
        {
            return root
                .Descendants()
                .Select(x => new
                {
                    Record = x as JObject,
                    Info = RecordInfo(x as JObject)
                })
                .Where(x => x.Record != null && x.Info != null
                    && (recordType == null || x.Info.Name == recordType))
                .Select(x => x.Record);
        }

        private string EvalChecksum(JObject record)
        {
            record = record?.DeepClone() as JObject;
            if (RecordInfo(record)?.Value is not JObject info)
                return string.Empty;
            info.Remove("checksum");
            var json = record?.ToString(Formatting.Indented, Array.Empty<JsonConverter>());
            var jsonUtf8 = Encoding.UTF8.GetBytes(json ?? string.Empty);
            using var sha1 = SHA1.Create();
            var sha1Data = sha1.ComputeHash(jsonUtf8);
            return System.Convert.ToBase64String(sha1Data);
        }

        private void VerifyChecksums(bool resetManagedPresets)
        {
            Presets ??= new JObject();
            UserPresets ??= new JObject();

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
                if (string.IsNullOrEmpty(oldChecksum))
                    continue;
                if (IsRootPresetRecord(record.Self))
                    continue;
                var newChecksum = EvalChecksum(record.Self);
                if (oldChecksum == newChecksum)
                    continue;
                HasUserModifiedPresets = true;
                CurrentMismatchSignature = EvalMismatchSignature(record.Info.Name, oldChecksum,
                    newChecksum);
                if (!resetManagedPresets)
                    break;
                var root = GetRootRecord(record.Self);
                record.Self.Remove();
                TouchRecord(root);
            }
        }
    }
}
