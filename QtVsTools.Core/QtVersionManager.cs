/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Win32;

namespace QtVsTools.Core
{
    using MsBuild;

    /// <summary>
    /// Summary description for QtVersionManager.
    /// </summary>
    public static class QtVersionManager
    {
        private const string VersionsKey = "Versions";
        private const string RegistryVersionsPath = Resources.RegistryRootPath + "\\" + VersionsKey;

        private static readonly SemaphoreSlim CacheSemaphore = new (1, 1);
        private static readonly ConcurrentDictionary<string, VersionInformation> VersionCache = new();

        public static VersionInformation GetVersionInfo(string name)
        {
            if (name == "$(DefaultQtVersion)")
                name = GetDefaultVersion();
            if (name == null)
                return null;

            if (VersionCache.TryGetValue(name, out var vi))
                return vi;

            CacheSemaphore.Wait();
            try {
                vi = VersionCache.GetOrAdd(name, VersionInformation.Get(GetInstallPath(name)));
                if (vi != null)
                    vi.name = name;
                return vi;
            } catch (Exception exception) {
                exception.Log();
                return null;
            } finally {
                CacheSemaphore.Release();
            }
        }

        public static string[] GetVersions()
        {
            var key = Registry.CurrentUser.OpenSubKey(Resources.RegistryRootPath, false);
            if (key == null)
                return Array.Empty<string>();
            var versionKey = key.OpenSubKey(VersionsKey, false);
            return versionKey?.GetSubKeyNames() ?? Array.Empty<string>();
        }

        /// <summary>
        /// Check if all Qt versions are valid and readable.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="defaultVersionInvalid"></param>
        /// <returns>true, if there are one or more invalid Qt version</returns>
        public static bool HasInvalidVersions(out string errorMessage, out bool defaultVersionInvalid)
        {
            var defaultVersion = GetDefaultVersionString();
            defaultVersionInvalid = string.IsNullOrEmpty(defaultVersion);

            errorMessage = null;
            foreach (var version in GetVersions()) {
                if (version == "$(DefaultQtVersion)")
                    continue;

                var path = GetInstallPath(version);
                if (path != null && (path.StartsWith("SSH:") || path.StartsWith("WSL:")))
                    continue;

                if (!string.IsNullOrEmpty(path) && QMake.Exists(path))
                    continue;
                errorMessage += $" * {version} in {path}\n";
                defaultVersionInvalid |= version == defaultVersion;
            }

            if (!string.IsNullOrEmpty(errorMessage)) {
                errorMessage = "These Qt version are inaccessible:\n"
                    + errorMessage
                    + "Make sure that you have read access to all files in your Qt directories.";
            }

            return errorMessage != null;
        }

        public static void SetLatestQtVersionAsDefault()
        {
            var validVersions = new Dictionary<string, System.Version>();
            foreach (var version in GetVersions()) {
                if (version == "$(DefaultQtVersion)")
                    continue;

                var path = GetInstallPath(version);
                if (!string.IsNullOrEmpty(path) && QMake.Exists(path))
                    validVersions[version] = new System.Version(new QtConfig(path).VersionString);
            }

            if (validVersions.Count <= 0)
                return;

            var defaultName = "";
            System.Version defaultVersion = null;
            foreach (var tmp in validVersions) {
                var version = tmp.Value;
                if (defaultVersion != null && defaultVersion >= version)
                    continue;
                defaultName = tmp.Key;
                defaultVersion = version;
            }
            SaveDefaultVersion(defaultName);
        }

        public static string GetInstallPath(string version)
        {
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            return GetInstallPath(version, Registry.CurrentUser);
        }

        private static string GetInstallPath(string version, RegistryKey root)
        {
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion(root);
            if (version == "$(QTDIR)")
                return Environment.GetEnvironmentVariable("QTDIR");

            var key = root.OpenSubKey(Resources.RegistryRootPath, false);
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

        public static void SaveVersion(string versionName, string path, bool checkPath = true)
        {
            var verName = versionName?.Trim().Replace(@"\", "_");
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
            return GetDefaultVersion(Registry.CurrentUser);
        }

        private static string GetDefaultVersion(RegistryKey root)
        {
            string defaultVersion = null;
            try {
                var key = root.OpenSubKey(RegistryVersionsPath, false);
                if (key != null)
                    defaultVersion = (string)key.GetValue("DefaultQtVersion");
            } catch {
                Messages.DisplayWarningMessage("Cannot load the default Qt version.");
            }

            if (defaultVersion == null) {
                var key = root.OpenSubKey(RegistryVersionsPath, false);
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
            return VerifyIfQtVersionExists(defaultVersion) ? defaultVersion : null;
        }

        private static string GetDefaultVersionString()
        {
            string defaultVersion = null;
            try {
                var key = Registry.CurrentUser.OpenSubKey(RegistryVersionsPath, false);
                if (key != null)
                    defaultVersion = key.GetValue("DefaultQtVersion") as string;
            } catch {
                Messages.Print("Cannot read the default Qt version from registry.");
            }
            return defaultVersion ?? Path.GetFileName(Environment.GetEnvironmentVariable("QTDIR"));
        }

        public static bool SaveDefaultVersion(string version)
        {
            if (version == "$(DefaultQtVersion)")
                return false;
            var key = Registry.CurrentUser.CreateSubKey(RegistryVersionsPath);
            if (key == null)
                return false;
            key.SetValue("DefaultQtVersion", version);
            return true;
        }

        private static bool VerifyIfQtVersionExists(string version)
        {
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            if (string.IsNullOrEmpty(version))
                return false;

            var regExp = new System.Text.RegularExpressions.Regex(@"\$\(.*\)");
            return regExp.IsMatch(version) || Directory.Exists(GetInstallPath(version));
        }
    }
}
