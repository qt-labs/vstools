/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using Microsoft.Win32;

namespace QtVsTools.Core.Common
{
    public static partial class Utils
    {
        public static void CopyRegistryKeys(string sourcePath, string destinationPath)
        {
            using var sourceKey = Registry.CurrentUser.OpenSubKey(sourcePath);
            using var destinationKey = Registry.CurrentUser.CreateSubKey(destinationPath);

            // Copy values
            foreach (var valueName in sourceKey?.GetValueNames() ?? Array.Empty<string>()) {
                if (sourceKey?.GetValue(valueName) is {} value)
                    destinationKey?.SetValue(valueName, value);
            }

            // Recursively copy sub keys
            foreach (var subKeyName in sourceKey?.GetSubKeyNames() ?? Array.Empty<string>()) {
                var subKeyPath = $"{sourcePath}\\{subKeyName}";
                CopyRegistryKeys(subKeyPath, $"{destinationPath}\\{subKeyName}");
            }
        }

        public static void MoveRegistryKeys(string sourcePath, string destinationPath)
        {
            // Copy keys and values
            CopyRegistryKeys(sourcePath, destinationPath);

            // Delete source keys recursively
            Registry.CurrentUser.DeleteSubKeyTree(sourcePath, false);
        }

        public static bool GetBoolValue(this RegistryKey key, string name, bool defaultValue = false)
        {
            var value = key.GetValue(name, defaultValue)?.ToString();
            if (int.TryParse(value, out var intValue))
                return intValue != 0;
            if (bool.TryParse(value, out var boolValue))
                return boolValue;
            return false;
        }
    }
}
