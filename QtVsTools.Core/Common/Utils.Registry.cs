// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

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

        /// <summary>
        /// Sets a registry key to a specified value if it hasn't been set before, and marks it as
        /// changed.
        /// </summary>
        /// <param name="registryPath">The path to the registry key.</param>
        /// <param name="keyToChange">The name of the key to set.</param>
        /// <param name="newValue">The new value to set for the key.</param>
        /// <param name="valueKind">The kind of value to set (e.g., DWord, String).</param>
        /// <param name="indicatorKey">The name of the key to indicate the change has been made.</param>
        public static void SetRegistryKeyOnce(string registryPath, string keyToChange,
            object newValue, RegistryValueKind valueKind, string indicatorKey)
        {
            using var key = Registry.CurrentUser.OpenSubKey(registryPath, true)
                ?? Registry.CurrentUser.CreateSubKey(registryPath); // Key does not exist, create.

            // Check if the change indicator key exists and is set to true
            var changeIndicatorValue = key?.GetValue(indicatorKey);
            if (changeIndicatorValue != null && (int)changeIndicatorValue == 1)
                return;

            key?.SetValue(keyToChange, newValue, valueKind);
            key?.SetValue(indicatorKey, 1, RegistryValueKind.DWord);
        }
    }
}
