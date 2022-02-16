/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace QtVsTools.Core
{
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
            ThreadHelper.ThrowIfNotOnUIThread();

            strVersionKey = "Versions";
            regVersionPath = Resources.registryVersionPath;
            RefreshVersionNames();
        }

        void RefreshVersionNames()
        {
            var rootKeyPath = "SOFTWARE\\" + Resources.registryRootPath;
            try {
                using (var rootKey = Registry.CurrentUser.OpenSubKey(rootKeyPath, true))
                using (var versionsKey = rootKey.OpenSubKey(strVersionKey, true)) {
                    versionsKey.SetValue("VersionNames", string.Join(";", GetVersions()));
                }

            } catch (Exception e) {
                ThreadHelper.ThrowIfNotOnUIThread();
                Messages.Print(e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace);
            }
        }

        private static readonly EventWaitHandle packageInit = new EventWaitHandle(false, EventResetMode.ManualReset);
        private static EventWaitHandle packageInitDone = null;

        public static QtVersionManager The(EventWaitHandle initDone = null)
        {
            if (initDone == null) {
                packageInit.WaitOne();
                packageInitDone.WaitOne();
            } else {
                packageInitDone = initDone;
                packageInit.Set();
            }

            if (instance == null)
                instance = new QtVersionManager();
            return instance;
        }

        public VersionInformation GetVersionInfo(string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (name == "$(DefaultQtVersion)")
                name = GetDefaultVersion();
            if (name == null)
                return null;
            if (versionCache == null)
                versionCache = new Hashtable();

            var vi = versionCache[name] as VersionInformation;
            if (vi == null) {
                var qtdir = GetInstallPath(name);
                versionCache[name] = vi = VersionInformation.Get(qtdir);
                if (vi != null)
                    vi.name = name;
            }
            return vi;
        }

        public VersionInformation GetVersionInfo(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetVersionInfo(GetProjectQtVersion(project));
        }

        public string[] GetVersions()
        {
            return GetVersions(Registry.CurrentUser);
        }

        public string GetQtVersionFromInstallDir(string qtDir)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (qtDir == null)
                return null;

            qtDir = qtDir.ToLower();
            var versions = GetVersions();
            foreach (var version in versions) {
                var installPath = GetInstallPath(version);
                if (installPath == null)
                    continue;
                if (installPath.ToLower() == qtDir)
                    return version;
            }

            return null;
        }

        public string[] GetVersions(RegistryKey root)
        {
            var key = root.OpenSubKey("SOFTWARE\\" + Resources.registryRootPath, false);
            if (key == null)
                return new string[] { };
            var versionKey = key.OpenSubKey(strVersionKey, false);
            if (versionKey == null)
                return new string[] { };
            return versionKey.GetSubKeyNames();
        }

        /// <summary>
        /// Check if all Qt versions are valid and readable.
        /// </summary>
        /// Also sets the default Qt version to the newest version, if needed.
        /// <param name="errorMessage"></param>
        /// <returns>true, if we found an invalid version</returns>
        public bool HasInvalidVersions(out string errorMessage)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var validVersions = new Dictionary<string, QtConfig>();
            var invalidVersions = new List<string>();

            foreach (var v in GetVersions()) {
                if (v == "$(DefaultQtVersion)")
                    continue;

                var vPath = GetInstallPath(v);
                if (string.IsNullOrEmpty(vPath)) {
                    invalidVersions.Add(v);
                    continue;
                }

                if (vPath.StartsWith("SSH:") || vPath.StartsWith("WSL:"))
                    continue;

                var qmakePath = Path.Combine(vPath, "bin", "qmake.exe");
                if (!File.Exists(qmakePath))
                    qmakePath = Path.Combine(vPath, "qmake.exe");
                if (!File.Exists(qmakePath)) {
                    invalidVersions.Add(v);
                    continue;
                }

                validVersions[v] = new QtConfig(vPath);
            }

            if (invalidVersions.Count > 0) {
                errorMessage = "These Qt version are inaccessible:\n";
                foreach (var invalidVersion in invalidVersions)
                    errorMessage += invalidVersion + " in " + GetInstallPath(invalidVersion) + "\n";
                errorMessage += "Make sure that you have read access to all files in your Qt directories.";

                // Is the default Qt version invalid?
                var isDefaultQtVersionInvalid = false;
                var defaultQtVersionName = GetDefaultVersion();
                if (string.IsNullOrEmpty(defaultQtVersionName)) {
                    isDefaultQtVersionInvalid = true;
                } else {
                    foreach (var name in invalidVersions) {
                        if (name == defaultQtVersionName) {
                            isDefaultQtVersionInvalid = true;
                            break;
                        }
                    }
                }

                // find the newest valid Qt version that can be used as default version
                if (isDefaultQtVersionInvalid && validVersions.Count > 0) {
                    QtConfig defaultQtVersionConfig = null;
                    foreach (var vNameConfig in validVersions) {
                        var vName = vNameConfig.Key;
                        var v = vNameConfig.Value;
                        if (defaultQtVersionConfig == null) {
                            defaultQtVersionConfig = v;
                            defaultQtVersionName = vName;
                            continue;
                        }
                        if (defaultQtVersionConfig.VersionMajor < v.VersionMajor ||
                               (defaultQtVersionConfig.VersionMajor == v.VersionMajor && (defaultQtVersionConfig.VersionMinor < v.VersionMinor ||
                                   (defaultQtVersionConfig.VersionMinor == v.VersionMinor && defaultQtVersionConfig.VersionPatch < v.VersionPatch)))) {
                            defaultQtVersionConfig = v;
                            defaultQtVersionName = vName;
                        }
                    }
                    if (defaultQtVersionConfig != null)
                        SaveDefaultVersion(defaultQtVersionName);
                }

                return true;
            }
            errorMessage = null;
            return false;
        }

        public string GetInstallPath(string version)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            return GetInstallPath(version, Registry.CurrentUser);
        }

        public string GetInstallPath(string version, RegistryKey root)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion(root);
            if (version == "$(QTDIR)")
                return Environment.GetEnvironmentVariable("QTDIR");

            var key = root.OpenSubKey("SOFTWARE\\" + Resources.registryRootPath, false);
            if (key == null)
                return null;
            var versionKey = key.OpenSubKey(strVersionKey + "\\" + version, false);
            if (versionKey == null)
                return null;
            return (string)versionKey.GetValue("InstallDir");
        }

        public string GetInstallPath(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var version = GetProjectQtVersion(project);
            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            if (version == null)
                return null;
            return GetInstallPath(version);
        }

        public bool SaveVersion(string versionName, string path, bool checkPath = true)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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

            string rootKeyPath = "SOFTWARE\\" + Resources.registryRootPath;
            string versionKeyPath = strVersionKey + "\\" + verName;
            using (var key = Registry.CurrentUser.CreateSubKey(rootKeyPath)) {
                if (key == null) {
                    Messages.Print(
                        "ERROR: root registry key creation failed");
                    return false;
                }
                using (var versionKey = key.CreateSubKey(versionKeyPath)) {
                    if (versionKey == null) {
                        Messages.Print(
                            "ERROR: version registry key creation failed");
                        return false;
                    }
                    versionKey.SetValue("InstallDir", dir);
                }
            }
            RefreshVersionNames();
            return true;
        }

        public void RemoveVersion(string versionName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + regVersionPath, true);
            if (key == null)
                return;
            key.DeleteSubKey(versionName);
            key.Close();
            RefreshVersionNames();
        }

        private bool IsVersionAvailable(string version)
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
            if (!IsVersionAvailable(version) && version != "$(DefaultQtVersion)")
                return false;

            ThreadHelper.ThrowIfNotOnUIThread();

            if (QtProject.GetFormatVersion(project) >= Resources.qtMinFormatVersion_Settings) {
                var vcPro = project.Object as VCProject;
                if (vcPro == null)
                    return false;
                foreach (VCConfiguration3 config in (IVCCollection)vcPro.Configurations) {
                    config.SetPropertyValue(Resources.projLabelQtSettings, true,
                        "QtInstall", version);
                }
                return true;
            }
            var key = "Qt5Version " + platform;
            if (!project.Globals.get_VariableExists(key) || project.Globals[key].ToString() != version)
                project.Globals[key] = version;
            if (!project.Globals.get_VariablePersists(key))
                project.Globals.set_VariablePersists(key, true);
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

            if (version == null && project.Globals.get_VariablePersists("Qt5Version")) {
                version = (string)project.Globals["Qt5Version"];
                ExpandEnvironmentVariablesInQtVersion(ref version);
                return VerifyIfQtVersionExists(version) ? version : null;
            }

            if (version == null)
                version = GetSolutionQtVersion(project.DTE.Solution);

            return version;
        }

        public string GetProjectQtVersion(EnvDTE.Project project, EnvDTE.Configuration config)
        {
            if (QtProject.GetFormatVersion(project) >= Resources.qtMinFormatVersion_Settings)
                return QtProject.GetPropertyValue(project, config, "QtInstall");

            ThreadHelper.ThrowIfNotOnUIThread();

            var key = "Qt5Version " + config.PlatformName;
            if (!project.Globals.get_VariablePersists(key))
                return null;
            var version = (string)project.Globals[key];
            ExpandEnvironmentVariablesInQtVersion(ref version);
            return VerifyIfQtVersionExists(version) ? version : null;
        }

        public string GetProjectQtVersion(EnvDTE.Project project, string platform)
        {
            if (QtProject.GetFormatVersion(project) >= Resources.qtMinFormatVersion_Settings)
                return GetProjectQtVersion(project);

            ThreadHelper.ThrowIfNotOnUIThread();

            var key = "Qt5Version " + platform;
            if (!project.Globals.get_VariablePersists(key))
                return null;
            var version = (string)project.Globals[key];
            ExpandEnvironmentVariablesInQtVersion(ref version);
            return VerifyIfQtVersionExists(version) ? version : null;
        }

        private static void ExpandEnvironmentVariablesInQtVersion(ref string version)
        {
            if (version != "$(QTDIR)" && version != "$(DefaultQtVersion)") {
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

        public bool SaveSolutionQtVersion(EnvDTE.Solution solution, string version)
        {
            if (!IsVersionAvailable(version) && version != "$(DefaultQtVersion)")
                return false;

            ThreadHelper.ThrowIfNotOnUIThread();

            solution.Globals["Qt5Version"] = version;
            if (!solution.Globals.get_VariablePersists("Qt5Version"))
                solution.Globals.set_VariablePersists("Qt5Version", true);
            return true;
        }

        public string GetSolutionQtVersion(EnvDTE.Solution solution)
        {
            if (solution == null)
                return null;

            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution.Globals.get_VariableExists("Qt5Version")) {
                var version = (string)solution.Globals["Qt5Version"];
                return VerifyIfQtVersionExists(version) ? version : null;
            }

            return null;
        }

        public string GetDefaultVersion()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetDefaultVersion(Registry.CurrentUser);
        }

        public string GetDefaultVersion(RegistryKey root)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string defaultVersion = null;
            try {
                var key = root.OpenSubKey("SOFTWARE\\" + regVersionPath, false);
                if (key != null)
                    defaultVersion = (string)key.GetValue("DefaultQtVersion");
            } catch {
                Messages.DisplayWarningMessage(SR.GetString("QtVersionManager_CannotLoadQtVersion"));
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            if (defaultVersion == null) {
                MergeVersions();
                var key = root.OpenSubKey("SOFTWARE\\" + regVersionPath, false);
                if (key != null) {
                    var versions = GetVersions();
                    if (versions != null && versions.Length > 0)
                        defaultVersion = versions[versions.Length - 1];
                    if (defaultVersion != null)
                        SaveDefaultVersion(defaultVersion);
                }
                if (defaultVersion == null) {
                    // last fallback... try QTDIR
                    var qtDir = Environment.GetEnvironmentVariable("QTDIR");
                    if (qtDir == null)
                        return null;
                    var d = new DirectoryInfo(qtDir);
                    SaveVersion(d.Name, d.FullName);
                    if (SaveDefaultVersion(d.Name))
                        defaultVersion = d.Name;
                }
            }
            return VerifyIfQtVersionExists(defaultVersion) ? defaultVersion : null;
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
            ThreadHelper.ThrowIfNotOnUIThread();

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

        private bool VerifyIfQtVersionExists(string version)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (version == "$(DefaultQtVersion)")
                version = GetDefaultVersion();
            if (!string.IsNullOrEmpty(version)) {
                var regExp =
                    new System.Text.RegularExpressions.Regex("\\$\\(.*\\)");
                if (regExp.IsMatch(version))
                    return true;
                return Directory.Exists(GetInstallPath(version));
            }

            return false;
        }
    }
}
