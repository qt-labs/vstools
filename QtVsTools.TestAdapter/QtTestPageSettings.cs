// ************************************************************************************************
// Copyright (C) 2024 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
// ************************************************************************************************

using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace QtVsTools.TestAdapter
{
    using Common;
    using Core;
    using QtVsTools.Core.Common;

    using static QtVsTools.Core.Resources;

    public sealed class QtTestPageSettings : SettingsBase<QtTestPage>
    {
        public static QtTestPageSettings Instance => StaticLazy.Get(() => Instance, () => new());

        public override void SaveSettings()
        {
            using var key = Registry.CurrentUser.CreateSubKey(TestAdapterSettingsPath);
            if (key == null)
                return;

            try {
                InitializedEvent.Wait();
                foreach (var kvp in Settings) {
                    switch (kvp.Value) {
                    case null:
                        key.DeleteValue(kvp.Key, false);
                        break;
                    case bool kvpValue:
                        key.SetValue(kvp.Key, kvpValue ? 1 : 0);
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
            using var registry = Registry.CurrentUser.OpenSubKey(TestAdapterSettingsPath);

            var loadedSettings = new Dictionary<string, object>();
            var properties = typeof(QtTestPage).GetProperties();

            foreach (var property in properties) {
                if (!Attribute.IsDefined(property, typeof(SettingsAttribute)))
                    continue;
                if (TryGetAttributeKey(property, out (string Key, object Default) tuple))
                    loadedSettings.Add(tuple.Key, registry?.GetValue(tuple.Key) ?? tuple.Default);
            }

            return loadedSettings;
        }

        private QtTestPageSettings() => Initialize();

        private static LazyFactory StaticLazy { get; } = new();
    }
}
