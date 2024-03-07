/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Win32;

namespace QtVsTools.Core
{
    using MsBuild;
    using static Common.Utils;

    /// <summary>
    /// Summary description for QtVersionManager.
    /// </summary>
    public static class QtVersionManager
    {
        private const string VersionsKey = "Versions";
        private const string RegistryVersionsPath = Resources.RegistryRootPath + "\\" + VersionsKey;

        public static string[] GetVersions()
        {
            var key = Registry.CurrentUser.OpenSubKey(Resources.RegistryRootPath, false);
            if (key == null)
                return Array.Empty<string>();
            var versionKey = key.OpenSubKey(VersionsKey, false);
            return versionKey?.GetSubKeyNames() ?? Array.Empty<string>();
        }

        public static string GetInstallPath(string version)
        {
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            if (version == "$(QTDIR)")
                return Environment.GetEnvironmentVariable("QTDIR");
            if (string.IsNullOrEmpty(version))
                return null;
            var key = Registry.CurrentUser.OpenSubKey(Resources.RegistryRootPath, false);
            var versionKey = key?.OpenSubKey(Path.Combine(VersionsKey, version), false);
            return versionKey?.GetValue("InstallDir") as string;
        }

        public static string GetInstallPath(MsBuildProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var version = project?.QtVersion;
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            return version == null ? null : GetInstallPath(version);
        }

        /// <summary>
        /// Sanitizes the provided version name by removing leading and trailing whitespaces,
        /// replacing backslashes and spaces with underscores.
        /// </summary>
        /// <param name="name">The version name to be sanitized.</param>
        /// <returns>A sanitized version of the input name.</returns>
        public static string SanitizeVersionName(string name)
        {
            return name?.Trim().Replace('\\', '_').Replace(' ', '_');
        }

        public static void SaveVersion(string versionName, string path, bool checkPath = true)
        {
            var verName = SanitizeVersionName(versionName);
            if (string.IsNullOrEmpty(verName))
                return;
            var dir = string.Empty;
            if (verName != "$(QTDIR)") {
                DirectoryInfo di;
                try {
                    di = new DirectoryInfo(path);
                } catch {
                    di = null;
                }
                if (di?.Exists == true) {
                    dir = di.FullName;
                } else if (!checkPath) {
                    dir = path;
                } else {
                    return;
                }
            }

            using var key = Registry.CurrentUser.CreateSubKey(Resources.RegistryRootPath);
            if (key == null) {
                Messages.Print("ERROR: root registry key creation failed");
                return;
            }

            using var versionKey = key.CreateSubKey(Path.Combine(VersionsKey, verName));
            if (versionKey == null) {
                Messages.Print("ERROR: version registry key creation failed");
            } else {
                versionKey.SetValue("InstallDir", dir);
            }
        }

        public static bool HasVersion(string versionName)
        {
            if (string.IsNullOrEmpty(versionName))
                return false;
            return Registry.CurrentUser.OpenSubKey(Path.Combine(RegistryVersionsPath, versionName),
                false) != null;
        }

        public static void RemoveVersion(string versionName)
        {
            var key = Registry.CurrentUser.OpenSubKey(RegistryVersionsPath, true);
            if (key == null)
                return;
            key.DeleteSubKey(versionName);
            key.Close();
        }

        private static bool IsVersionAvailable(string version)
        {
            return GetVersions().Any(ver => version == ver);
        }

        public static void SaveProjectQtVersion(MsBuildProject project, string version)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!IsVersionAvailable(version) && version != "$(DefaultQtVersion)")
                return;

            if (project?.VcProject.Configurations is not IVCCollection configurations)
                return;

            foreach (VCConfiguration3 config in configurations)
                config.SetPropertyValue("QtSettings", true, "QtInstall", version);
        }

        public static string GetDefaultVersion()
        {
            string defaultVersion = null;
            try {
                var key = Registry.CurrentUser.OpenSubKey(RegistryVersionsPath, false);
                if (key != null)
                    defaultVersion = (string)key.GetValue("DefaultQtVersion");
            } catch {
                Messages.DisplayWarningMessage("Cannot load the default Qt version.");
            }

            if (defaultVersion == null) {
                var key = Registry.CurrentUser.OpenSubKey(RegistryVersionsPath, false);
                if (key != null) {
                    var versions = GetVersions();
                    if (versions is {Length: > 0})
                        defaultVersion = versions[versions.Length - 1];
                    if (defaultVersion != null)
                        SaveDefaultVersion(defaultVersion);
                }
                if (defaultVersion == null) {
                    // last fallback... try QTDIR
                    var qtDir = Environment.GetEnvironmentVariable("QTDIR");
                    if (string.IsNullOrEmpty(qtDir))
                        return null;
                    var name = Path.GetFileName(qtDir);
                    SaveVersion(name, Path.GetFullPath(qtDir));
                    if (SaveDefaultVersion(name))
                        defaultVersion = name;
                }
            }
            return VersionExists(defaultVersion) ? defaultVersion : null;
        }

        public static string GetDefaultVersionInstallPath()
        {
            try {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryVersionsPath, false);
                var defaultVersion = key?.GetValue("DefaultQtVersion") as string;

                if (string.IsNullOrEmpty(defaultVersion))
                    return Path.GetFileName(Environment.GetEnvironmentVariable("QTDIR"));

                var versionKey = key.OpenSubKey(defaultVersion, false);
                return versionKey?.GetValue("InstallDir") as string;
            } catch {
                Messages.Print("Cannot read the default Qt version from registry.");
            }
            return null;
        }

        public static bool SaveDefaultVersion(string version)
        {
            if (version == "$(DefaultQtVersion)")
                return false;
            var key = Registry.CurrentUser.CreateSubKey(RegistryVersionsPath);
            if (key == null)
                return false;

            version = SanitizeVersionName(version);
            if (string.IsNullOrEmpty(version))
                return false;
            key.SetValue("DefaultQtVersion", version);
            return true;
        }

        public static bool VersionExists(string version)
        {
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            if (string.IsNullOrEmpty(version))
                return false;

            var regExp = new System.Text.RegularExpressions.Regex(@"\$\(.*\)");
            return regExp.IsMatch(version) || Directory.Exists(GetInstallPath(version));
        }

        public static void MoveRegisteredQtVersions()
        {
            const string keyName = @"HKEY_CURRENT_USER\SOFTWARE\Digia";
            const string valueName = "Copied";
            if (Registry.GetValue(keyName, valueName, null) != null)
                return;

            // TODO v3.2.0: Use MoveRegistryKeys and delete source keys
            CopyRegistryKeys("SOFTWARE\\Digia", Resources.RegistryRootPath);
            MoveRegistryKeys(Resources.RegistryRootPath + "\\Qt5VS2017",
                Resources.RegistryPackagePath);

            Registry.SetValue(keyName, valueName, "");
        }
    }
}
