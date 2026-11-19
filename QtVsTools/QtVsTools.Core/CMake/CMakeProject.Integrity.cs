// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace QtVsTools.Core.CMake
{
    public partial class CMakeProject
    {
        /// <summary>
        /// Track records (JObject instances) by identity because JToken equality is content
        /// based and mutable. Content-based tracking can lose entries when record content changes.
        /// </summary>
        private sealed class IdentityEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly IdentityEqualityComparer<T> Instance = new();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }

        private readonly HashSet<JObject> touchedRecords =
            new(IdentityEqualityComparer<JObject>.Instance);

        private bool HasUserModifiedPresets { get; set; }
        private bool PresetsModifiedNotified { get; set; }
        private bool ManagedPresetResetPending { get; set; }
        private string CurrentMismatchSignature { get; set; }
        private string IgnoredMismatchSignature { get; set; }

        private void ResetRecordTracking()
        {
            touchedRecords.Clear();
            HasUserModifiedPresets = false;
            CurrentMismatchSignature = null;
        }

        private void TouchRecord(JObject record)
        {
            if (record != null)
                touchedRecords.Add(record);
        }

        private bool IsRecordTouched(JObject record) =>
            record != null && touchedRecords.Contains(record);

        private bool IsRootPresetRecord(JObject record) =>
            ReferenceEquals(record, Presets) || ReferenceEquals(record, UserPresets);

        private JObject GetRootRecord(JToken record)
        {
            return record?.AncestorsAndSelf()
                .OfType<JObject>()
                .LastOrDefault(x => ReferenceEquals(x, Presets) || ReferenceEquals(x, UserPresets));
        }

        private static string EvalMismatchSignature(string recordType, string oldChecksum,
            string newChecksum) => $"{recordType}|{oldChecksum}|{newChecksum}";

        private bool IsCurrentMismatchIgnored()
        {
            if (string.IsNullOrEmpty(CurrentMismatchSignature))
                return false;
            return string.Equals(CurrentMismatchSignature, IgnoredMismatchSignature,
                StringComparison.Ordinal);
        }

        private void IgnoreCurrentMismatch()
        {
            IgnoredMismatchSignature = CurrentMismatchSignature;
            PresetsModifiedNotified = false;
        }

        private void ClearIgnoredMismatch() => IgnoredMismatchSignature = null;
    }
}
