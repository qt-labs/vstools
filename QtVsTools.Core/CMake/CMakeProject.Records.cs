/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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
            if (RecordInfo(record?.DeepClone() as JObject)?.Value is not JObject info)
                return string.Empty;
            info.Remove("checksum");
            var json = record.ToString(Formatting.Indented);
            var jsonUtf8 = Encoding.UTF8.GetBytes(json);
            using var sha1 = SHA1.Create();
            var sha1Data = sha1.ComputeHash(jsonUtf8);
            return System.Convert.ToBase64String(sha1Data);
        }

        private void VerifyChecksums()
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
                if (record.Checksum?.Value<string>() == EvalChecksum(record.Self))
                    continue;
                if (record.Self != Presets && record.Self != UserPresets)
                    record.Self.Remove();
            }
        }
    }
}
