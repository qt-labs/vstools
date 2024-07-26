/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

namespace QtVsTools.Core
{
    /// <summary>
    /// Summary description for Resources.
    /// </summary>
    public static class Resources
    {
        public const string RegistryRoot = @"SOFTWARE\QtProject\QtVsTools";
        public static string RegistrySuffix { get; set; } = "";
        public static string RegistryPath => RegistryRoot + RegistrySuffix;
        public static string SettingsRegistryPath => RegistryPath + @"\Settings";
        public static string VersionsRegistryPath => RegistryPath + @"\Versions";
        public static string TestAdapterSettingsPath => RegistryPath + @"\TestAdapter";

        public const string ObsoleteRegistryPath = @"SOFTWARE\Digia";
    }
}
