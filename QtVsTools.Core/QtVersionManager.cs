/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Win32;

namespace QtVsTools.Core
{
    using MsBuild;

    public static partial class Instances
    {
        public static QtVersionManager VersionManager => QtVersionManager.The();
    }

    /// <summary>
    /// Summary description for QtVersionManager.
    /// </summary>
    public class QtVersionManager
    {
        private static QtVersionManager instance;
        private readonly string regVersionPath;
        private readonly string strVersionKey;
        private Hashtable versionCache;

        protected QtVersionManager()
        {
            strVersionKey = "Versions";
            regVersionPath = Resources.registryVersionPath;
        }

        private static readonly EventWaitHandle packageInit = new(false, EventResetMode.ManualReset);
        private static EventWaitHandle packageInitDone;

        public static QtVersionManager The(EventWaitHandle initDone = null)
        {
            if (initDone == null) {
                packageInit.WaitOne();
                packageInitDone.WaitOne();
            } else {
                packageInitDone = initDone;
                packageInit.Set();
            }

            return instance ??= new QtVersionManager();
        }

        public VersionInformation GetVersionInfo(string name)
        {
            if (name == null)
                return null;
            if (name == "$(DefaultQtVersion)")
                name = GetDefaultVersion();
            versionCache ??= new Hashtable();

            if (versionCache[name] is VersionInformation vi)
                return vi;

            var qtdir = GetInstallPath(name);
            versionCache[name] = vi = VersionInformation.Get(qtdir);
            if (vi != null)
                vi.name = name;
            return vi;
        }

        public string[] GetVersions()
        {
            return GetVersions(Registry.CurrentUser);
        }

        public string[] GetVersions(RegistryKey root)
        {
            var key = root.OpenSubKey("SOFTWARE\\" + Resources.registryRootPath, false);
            if (key == null)
                return new string[] { };
            var versionKey = key.OpenSubKey(strVersionKey, false);
            return versionKey?.GetSubKeyNames() ?? new string[] { };
        }

        /// <summary>
        /// Check if all Qt versions are valid and readable.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns>true, if there are one or more invalid Qt version</returns>
        public bool HasInvalidVersions(out string errorMessage, out bool defaultVersionInvalid)
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

                if (string.IsNullOrEmpty(path) || !QMake.Exists(path)) {
                    errorMessage += $" * {version} in {path}\n";
                    defaultVersionInvalid |= version == defaultVersion;
                }
            }

            if (!string.IsNullOrEmpty(errorMessage)) {
                errorMessage = "These Qt version are inaccessible:\n"
                    + errorMessage
                    + "Make sure that you have read access to all files in your Qt directories.";
            }

            return errorMessage != null;
        }

        public void SetLatestQtVersionAsDefault()
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
                if (defaultVersion == null || defaultVersion < version) {
                    defaultName = tmp.Key;
                    defaultVersion = version;
                }
            }
            SaveDefaultVersion(defaultName);
        }

        public string GetInstallPath(string version)
        {
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            return GetInstallPath(version, Registry.CurrentUser);
        }

        public string GetInstallPath(string version, RegistryKey root)
        {
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion(root);
            if (version == "$(QTDIR)")
                return Environment.GetEnvironmentVariable("QTDIR");

            var key = root.OpenSubKey("SOFTWARE\\" + Resources.registryRootPath, false);
            var versionKey = key?
                .OpenSubKey(strVersionKey + Path.DirectorySeparatorChar + version, false);
            return versionKey?.GetValue("InstallDir") as string;
        }

        public string GetInstallPath(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var version = GetProjectQtVersion(project);
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            return version == null ? null : GetInstallPath(version);
        }

        public bool SaveVersion(string versionName, string path, bool checkPath = true)
        {
            var verName = versionName?.Trim().Replace(@"\", "_");
            if (string.IsNullOrEmpty(verName))
                return false;
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
                    return false;
                }
            }

            var rootKeyPath = "SOFTWARE" + Path.DirectorySeparatorChar + Resources.registryRootPath;
            using (var key = Registry.CurrentUser.CreateSubKey(rootKeyPath)) {
                if (key == null) {
                    Messages.Print(
                        "ERROR: root registry key creation failed");
                    return false;
                }
                var versionKeyPath = strVersionKey + Path.DirectorySeparatorChar + verName;
                using (var versionKey = key.CreateSubKey(versionKeyPath)) {
                    if (versionKey == null) {
                        Messages.Print(
                            "ERROR: version registry key creation failed");
                        return false;
                    }
                    versionKey.SetValue("InstallDir", dir);
                }
            }
            return true;
        }

        public bool HasVersion(string versionName)
        {
            if (string.IsNullOrEmpty(versionName))
                return false;
            return Registry.CurrentUser.OpenSubKey(Path.Combine("SOFTWARE", regVersionPath,
                versionName), false) != null;
        }

        public void RemoveVersion(string versionName)
        {
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE" + Path.DirectorySeparatorChar
                + regVersionPath, true);
            if (key == null)
                return;
            key.DeleteSubKey(versionName);
            key.Close();
        }

        internal bool IsVersionAvailable(string version)
        {
            var versionAvailable = false;
            var versions = GetVersions();
            foreach (var ver in versions) {
                if (version == ver) {
                    versionAvailable = true;
                    break;
                }
            }
            return versionAvailable;
        }

        public bool SaveProjectQtVersion(EnvDTE.Project project, string version)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return SaveProjectQtVersion(project, version, project.ConfigurationManager.ActiveConfiguration.PlatformName);
        }

        public bool SaveProjectQtVersion(EnvDTE.Project project, string version, string platform)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!IsVersionAvailable(version) && version != "$(DefaultQtVersion)")
                return false;

            if (ProjectFormat.GetVersion(project) >= ProjectFormat.Version.V3) {
                if (project.Object is VCProject vcPro) {
                    foreach (VCConfiguration3 config in (IVCCollection)vcPro.Configurations) {
                        config.SetPropertyValue(Resources.projLabelQtSettings, true,
                            "QtInstall", version);
                    }
                    return true;
                }
                return false;
            }
            var key = "Qt5Version " + platform;
            if (!project.Globals.VariableExists[key] || project.Globals[key].ToString() != version)
                project.Globals[key] = version;
            if (!project.Globals.VariablePersists[key])
                project.Globals.VariablePersists[key] = true;
            return true;
        }

        public string GetProjectQtVersion(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE.Configuration config = null;
            try {
                config = project.ConfigurationManager.ActiveConfiguration;
            } catch {
                // Accessing the ActiveConfiguration property throws an exception
                // if there's an "unconfigured" platform in the Solution platform combo box.
                config = project.ConfigurationManager.Item(1);
            }
            var version = GetProjectQtVersion(project, config);

            if (version == null && project.Globals.VariablePersists["Qt5Version"]) {
                version = (string)project.Globals["Qt5Version"];
                ExpandEnvironmentVariablesInQtVersion(ref version);
                return VerifyIfQtVersionExists(version) ? version : null;
            }
            return version;
        }

        public string GetProjectQtVersion(EnvDTE.Project project, EnvDTE.Configuration config)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ProjectFormat.GetVersion(project) >= ProjectFormat.Version.V3)
                return QtProject.GetPropertyValue(project, config, "QtInstall");

            var key = "Qt5Version " + config.PlatformName;
            if (!project.Globals.VariablePersists[key])
                return null;
            var version = (string)project.Globals[key];
            ExpandEnvironmentVariablesInQtVersion(ref version);
            return VerifyIfQtVersionExists(version) ? version : null;
        }

        private static void ExpandEnvironmentVariablesInQtVersion(ref string version)
        {
            if (version is not "$(QTDIR)" and not "$(DefaultQtVersion)") {
                // Make it possible to specify the version name
                // via an environment variable
                var regExp =
                    new System.Text.RegularExpressions.Regex("\\$\\((?<VarName>\\S+)\\)");
                var match = regExp.Match(version);
                if (match.Success) {
                    var env = match.Groups["VarName"].Value;
                    version = Environment.GetEnvironmentVariable(env);
                }
            }
        }

        public string GetDefaultVersion()
        {
            return GetDefaultVersion(Registry.CurrentUser);
        }

        public string GetDefaultVersion(RegistryKey root)
        {
            string defaultVersion = null;
            try {
                var key = root.OpenSubKey("SOFTWARE\\" + regVersionPath, false);
                if (key != null)
                    defaultVersion = (string)key.GetValue("DefaultQtVersion");
            } catch {
                Messages.DisplayWarningMessage("Cannot load the default Qt version.");
            }

            if (defaultVersion == null) {
                MergeVersions();
                var key = root.OpenSubKey("SOFTWARE\\" + regVersionPath, false);
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

        public string GetDefaultVersionString()
        {
            string defaultVersion = null;
            try {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + regVersionPath, false);
                if (key != null)
                    defaultVersion = key.GetValue("DefaultQtVersion") as string;
            } catch {
                Messages.Print("Cannot read the default Qt version from registry.");
            }
            return defaultVersion ?? Path.GetFileName(Environment.GetEnvironmentVariable("QTDIR"));
        }

        public bool SaveDefaultVersion(string version)
        {
            if (version == "$(DefaultQtVersion)")
                return false;
            var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + regVersionPath);
            if (key == null)
                return false;
            key.SetValue("DefaultQtVersion", version);
            return true;
        }

        private void MergeVersions()
        {
            var hkcuVersions = GetVersions();
            var hklmVersions = GetVersions(Registry.LocalMachine);

            var hkcuInstDirs = new string[hkcuVersions.Length];
            for (var i = 0; i < hkcuVersions.Length; ++i)
                hkcuInstDirs[i] = GetInstallPath(hkcuVersions[i]);
            var hklmInstDirs = new string[hklmVersions.Length];
            for (var i = 0; i < hklmVersions.Length; ++i)
                hklmInstDirs[i] = GetInstallPath(hklmVersions[i], Registry.LocalMachine);

            for (var i = 0; i < hklmVersions.Length; ++i) {
                if (hklmInstDirs[i] == null)
                    continue;

                var found = false;
                for (var j = 0; j < hkcuInstDirs.Length; ++j) {
                    if (hkcuInstDirs[j] != null
                        && hkcuInstDirs[j].ToLower() == hklmInstDirs[i].ToLower()) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    for (var j = 0; j < hkcuVersions.Length; ++j) {
                        if (hkcuVersions[j] != null
                            && hkcuVersions[j] == hklmVersions[i]) {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        SaveVersion(hklmVersions[i], hklmInstDirs[i]);
                }
            }
        }

        internal bool VerifyIfQtVersionExists(string version)
        {
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            if (string.IsNullOrEmpty(version))
                return false;

            var regExp = new System.Text.RegularExpressions.Regex("\\$\\(.*\\)");
            return regExp.IsMatch(version) || Directory.Exists(GetInstallPath(version));

        }
    }
}
