// ************************************************************************************************
// Copyright (C) 2024 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
// ************************************************************************************************

using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace QtVsTools.Core.Options
{
    using Common;
    using QtVsTools.Common;

    public sealed class QtOptionsPageSettings : SettingsBase<QtOptionsPage>
    {
        public static QtOptionsPageSettings Instance => StaticLazy.Get(() => Instance, () => new());

        public override void SaveSettings()
        {
            using var key = Registry.CurrentUser.CreateSubKey(Resources.PackageSettingsPath);
            if (key == null)
                return;

            try {
                InitializedEvent.Wait();
                foreach (var kvp in Settings) {
                    switch (kvp.Value) {
                    case null:
                        continue;
                    case bool kvpValue:
                        key.SetValue(kvp.Key, kvpValue ? 1 : 0);
                        break;
                    case QtOptionsPage.Timeout timeout:
                        key.SetValue(kvp.Key, Convert.ToInt32(timeout));
                        break;
                    default:
                        key.SetValue(kvp.Key, kvp.Value);
                        break;
                    }
                }
            } catch (Exception exception) {
                exception.Log();
            }
        }

        protected override Dictionary<string, object> LoadSettings()
        {
            using var registry = Registry.CurrentUser.OpenSubKey(Resources.PackageSettingsPath);

            var loadedSettings = new Dictionary<string, object>();
            var properties = typeof(QtOptionsPage).GetProperties();

            foreach (var property in properties) {
                if (!Attribute.IsDefined(property, typeof(SettingsAttribute)))
                    continue;
                if (TryGetAttributeKey(property, out (string Key, object Default) tuple))
                    loadedSettings.Add(tuple.Key, registry?.GetValue(tuple.Key) ?? tuple.Default);
            }

            return loadedSettings;
        }

        private QtOptionsPageSettings() => Initialize();

        private static LazyFactory StaticLazy { get; } = new();
    }
}
