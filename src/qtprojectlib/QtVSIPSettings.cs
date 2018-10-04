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

using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Win32;
using QtProjectLib.QtMsBuild;
using System;
using System.Collections;

namespace QtProjectLib
{
    public static class QtVSIPSettings
    {
        static Hashtable mocDirCache = new Hashtable();
        static Hashtable uicDirCache = new Hashtable();
        static Hashtable rccDirCache = new Hashtable();

        public static bool GetDisableAutoMocStepsUpdate()
        {
            return GetBoolValue(Resources.disableAutoMocStepsUpdateKeyword, false);
        }

        public static void SaveDisableAutoMocStepsUpdate(bool b)
        {
            SetBoolValue(Resources.disableAutoMocStepsUpdateKeyword, b);
        }

        public static string GetUicDirectory(EnvDTE.Project project)
        {
            return GetDirectory(project, Resources.uicDirKeyword);
        }

        public static void SaveUicDirectory(EnvDTE.Project project, string directory)
        {
            if (directory == null)
                SaveDirectory(project, Resources.uicDirKeyword, GetDirectory(project, Resources.uicDirKeyword));
            else
                SaveDirectory(project, Resources.uicDirKeyword, directory);
        }

        public static string GetMocDirectory()
        {
            return GetDirectory(Resources.mocDirKeyword);
        }

        public static string GetMocDirectory(EnvDTE.Project project)
        {
            return GetDirectory(project, Resources.mocDirKeyword);
        }

        public static string GetMocDirectory(
            EnvDTE.Project project,
            string configName,
            string platformName, VCFile vCFile)
        {
            string filePath = null;
            if (vCFile != null)
                filePath = vCFile.FullPath;
            return GetMocDirectory(project, configName, platformName, filePath);
        }

        public static string GetMocDirectory(
            EnvDTE.Project project,
            string configName,
            string platformName,
            string filePath = null)
        {
            var dir = GetDirectory(project, Resources.mocDirKeyword);
            if (!string.IsNullOrEmpty(configName)
                && !string.IsNullOrEmpty(platformName))
                HelperFunctions.ExpandString(ref dir, project, configName, platformName, filePath);
            return dir;
        }

        public static bool HasDifferentMocFilePerConfig(EnvDTE.Project project)
        {
            var mocDir = GetMocDirectory(project);
            return mocDir.Contains("$(ConfigurationName)");
        }

        public static bool HasDifferentMocFilePerPlatform(EnvDTE.Project project)
        {
            var mocDir = GetMocDirectory(project);
            return mocDir.Contains("$(PlatformName)");
        }

        public static string GetMocOptions()
        {
            return GetOption(Resources.mocOptionsKeyword);
        }

        public static string GetMocOptions(EnvDTE.Project project)
        {
            return GetOption(project, Resources.mocOptionsKeyword);
        }

        public static bool GetLUpdateOnBuild(EnvDTE.Project project)
        {
            return GetBoolValue(project, Resources.lupdateKeyword);
        }

        public static string GetLUpdateOptions()
        {
            return GetOption(Resources.lupdateOptionsKeyword);
        }

        public static string GetLUpdateOptions(EnvDTE.Project project)
        {
            return GetOption(project, Resources.lupdateOptionsKeyword);
        }

        public static string GetLReleaseOptions()
        {
            return GetOption(Resources.lreleaseOptionsKeyword);
        }

        public static string GetLReleaseOptions(EnvDTE.Project project)
        {
            return GetOption(project, Resources.lreleaseOptionsKeyword);
        }

        public static bool GetAskBeforeCheckoutFile()
        {
            return GetBoolValue(Resources.askBeforeCheckoutFileKeyword, true);
        }

        public static void SaveAskBeforeCheckoutFile(bool value)
        {
            SetBoolValue(Resources.askBeforeCheckoutFileKeyword, value);
        }

        public static bool GetDisableCheckoutFiles()
        {
            return GetBoolValue(Resources.disableCheckoutFilesKeyword, false);
        }

        public static void SaveDisableCheckoutFiles(bool value)
        {
            SetBoolValue(Resources.disableCheckoutFilesKeyword, value);
        }

        public static void SaveMocDirectory(EnvDTE.Project project, string directory)
        {
            if (directory == null)
                SaveDirectory(project, Resources.mocDirKeyword, GetDirectory(project, Resources.mocDirKeyword));
            else
                SaveDirectory(project, Resources.mocDirKeyword, directory);
        }

        public static void SaveMocOptions(EnvDTE.Project project, string options)
        {
            if (options == null)
                options = GetMocOptions();
            SaveOption(project, Resources.mocOptionsKeyword, options);
        }

        public static void SaveMocOptions(string options)
        {
            SaveOption(Resources.mocOptionsKeyword, options);
        }

        public static void SaveLUpdateOnBuild(EnvDTE.Project project)
        {
            SetBoolValue(project, Resources.lupdateKeyword, GetLUpdateOnBuild());
        }

        public static void SaveLUpdateOnBuild(EnvDTE.Project project, bool value)
        {
            SetBoolValue(project, Resources.lupdateKeyword, value);
        }

        public static void SaveLUpdateOptions(EnvDTE.Project project, string options)
        {
            if (options == null)
                options = GetLUpdateOptions();

            SaveOption(project, Resources.lupdateOptionsKeyword, options);
        }

        public static void SaveLUpdateOptions(string options)
        {
            SaveOption(Resources.lupdateOptionsKeyword, options);
        }

        public static void SaveLReleaseOptions(EnvDTE.Project project, string options)
        {
            if (options == null)
                options = GetLReleaseOptions();
            SaveOption(project, Resources.lreleaseOptionsKeyword, options);
        }

        public static void SaveLReleaseOptions(string options)
        {
            SaveOption(Resources.lreleaseOptionsKeyword, options);
        }

        public static string GetRccDirectory(EnvDTE.Project project)
        {
            return GetDirectory(project, Resources.rccDirKeyword);
        }

        public static void SaveRccDirectory(string dir)
        {
            SaveDirectory(Resources.rccDirKeyword, dir);
        }

        public static void SaveRccDirectory(EnvDTE.Project project, string directory)
        {
            if (directory == null)
                SaveDirectory(project, Resources.rccDirKeyword, GetDirectory(project, Resources.rccDirKeyword));
            else
                SaveDirectory(project, Resources.rccDirKeyword, directory);
        }

        private static string GetDirectory(string type)
        {
            try {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
                if (key != null) {
                    var path = (string) key.GetValue(type, null);
                    if (path != null)
                        return HelperFunctions.NormalizeRelativeFilePath(path);
                }
            } catch { }
            if (type == Resources.mocDirKeyword)
                return Resources.generatedFilesDir + "\\$(ConfigurationName)";
            return Resources.generatedFilesDir;
        }

        private static string GetOption(string type)
        {
            try {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
                if (key != null) {
                    var opt = (string) key.GetValue(type, null);
                    if (opt != null)
                        return opt;
                }
            } catch { }
            return null;
        }

        public static bool GetLUpdateOnBuild()
        {
            return GetBoolValue(Resources.lupdateKeyword, false);
        }

        public static string GetRccDirectory()
        {
            return GetDirectory(Resources.rccDirKeyword);
        }

        public static string GetUicDirectory()
        {
            return GetDirectory(Resources.uicDirKeyword);
        }

        private static string GetDirectory(EnvDTE.Project project, string type)
        {
            // check for directory in following order:
            // - stored in project
            // - stored in cache
            // - retrieve from moc/uic steps
            // - globally defined default directory
            // - fallback on hardcoded directory
            if (project != null) {
                if (project.Globals.get_VariablePersists(type))
                    return HelperFunctions.NormalizeRelativeFilePath((string) project.Globals[type]);

                try {
                    if (type == Resources.mocDirKeyword && mocDirCache.Contains(project.FullName))
                        return (string) mocDirCache[project.FullName];
                    if (type == Resources.uicDirKeyword && uicDirCache.Contains(project.FullName))
                        return (string) uicDirCache[project.FullName];
                    if (type == Resources.rccDirKeyword && rccDirCache.Contains(project.FullName))
                        return (string) rccDirCache[project.FullName];

                    QtCustomBuildTool tool = null;
                    string configName = null;
                    string platformName = null;
                    var vcpro = (VCProject) project.Object;
                    foreach (VCFile vcfile in (IVCCollection) vcpro.Files) {
                        var name = vcfile.Name;
                        if ((type == Resources.mocDirKeyword && HelperFunctions.IsHeaderFile(name))
                            || (type == Resources.mocDirKeyword && HelperFunctions.IsMocFile(name))
                            || (type == Resources.uicDirKeyword && HelperFunctions.IsUicFile(name))
                            || (type == Resources.rccDirKeyword && HelperFunctions.IsQrcFile(name))) {
                            foreach (VCFileConfiguration config in (IVCCollection) vcfile.FileConfigurations) {
                                tool = new QtCustomBuildTool(config);
                                configName = config.Name.Remove(config.Name.IndexOf('|'));
                                var vcConfig = config.ProjectConfiguration as VCConfiguration;
                                var platform = vcConfig.Platform as VCPlatform;
                                platformName = platform.Name;
                                if (tool != null && (tool.CommandLine.IndexOf("moc.exe", StringComparison.OrdinalIgnoreCase) != -1
                                    || (tool.CommandLine.IndexOf("uic.exe", StringComparison.OrdinalIgnoreCase) != -1)
                                    || (tool.CommandLine.IndexOf("rcc.exe", StringComparison.OrdinalIgnoreCase) != -1)))
                                    break;
                                tool = null;
                            }

                            if (tool != null)
                                break;
                        }
                    }

                    if (tool != null) {
                        string dir = null;
                        var lastindex = tool.Outputs.LastIndexOf('\\');
                        if (tool.Outputs.LastIndexOf('/') > lastindex)
                            lastindex = tool.Outputs.LastIndexOf('/');

                        if (lastindex == -1)
                            dir = ".";
                        else
                            dir = tool.Outputs.Substring(0, lastindex);
                        dir = dir.Replace("\"", "");

                        if (type == Resources.mocDirKeyword) {
                            int index;
                            if ((index = dir.IndexOf(configName, StringComparison.OrdinalIgnoreCase)) != -1)
                                dir = dir.Replace(dir.Substring(index, configName.Length), "$(ConfigurationName)");
                            if ((index = dir.IndexOf(platformName, StringComparison.OrdinalIgnoreCase)) != -1)
                                dir = dir.Replace(dir.Substring(index, platformName.Length), "$(PlatformName)");

                            mocDirCache.Add(project.FullName, HelperFunctions.NormalizeRelativeFilePath(dir));
                        } else if (type == Resources.uicDirKeyword)
                            uicDirCache.Add(project.FullName, HelperFunctions.NormalizeRelativeFilePath(dir));
                        else if (type == Resources.rccDirKeyword)
                            rccDirCache.Add(project.FullName, HelperFunctions.NormalizeRelativeFilePath(dir));

                        cleanUpCache(project);

                        return HelperFunctions.NormalizeRelativeFilePath(dir);
                    }
                } catch { }
            }

            return GetDirectory(type);
        }

        private static string GetOption(EnvDTE.Project project, string type)
        {
            // check for directory in following order:
            // - stored in project
            // - globally defined default option
            // - empty options
            if (project != null && project.Globals.get_VariablePersists(type))
                return (string) project.Globals[type];
            return GetOption(type);
        }

        private static bool GetBoolValue(EnvDTE.Project project, string type)
        {
            // check for directory in following order:
            // - stored in project
            // - globally defined default option
            // - empty options
            if (project != null && project.Globals.get_VariablePersists(type))
                return Convert.ToInt32(project.Globals[type] as string) > 0;
            return GetBoolValue(type, false);
        }

        private static void SaveDirectory(EnvDTE.Project project, string type, string dir)
        {
            dir = HelperFunctions.NormalizeRelativeFilePath(dir);
            project.Globals[type] = dir;
            if (!project.Globals.get_VariablePersists(type))
                project.Globals.set_VariablePersists(type, true);

            cleanUpCache(project);
        }

        private static void SaveOption(EnvDTE.Project project, string type, string option)
        {
            project.Globals[type] = option;
            if (!project.Globals.get_VariablePersists(type))
                project.Globals.set_VariablePersists(type, true);
        }

        private static void SetBoolValue(EnvDTE.Project project, string type, bool value)
        {
            project.Globals[type] = Convert.ToInt32(value).ToString();
            if (!project.Globals.get_VariablePersists(type))
                project.Globals.set_VariablePersists(type, true);
        }

        public static void SaveUicDirectory(string dir)
        {
            SaveDirectory(Resources.uicDirKeyword, dir);
        }

        public static void SaveMocDirectory(string dir)
        {
            SaveDirectory(Resources.mocDirKeyword, dir);
        }

        public static void SaveLUpdateOnBuild(bool val)
        {
            SetBoolValue(Resources.lupdateKeyword, val);
        }

        public static void cleanUpCache(EnvDTE.Project project)
        {
            try {
                var mocEnumerator = mocDirCache.GetEnumerator();
                while (mocEnumerator.MoveNext()) {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string) mocEnumerator.Key)) {
                        mocDirCache.Remove(mocEnumerator.Key);
                        mocEnumerator = mocDirCache.GetEnumerator();
                    }
                }

                var uicEnumerator = uicDirCache.GetEnumerator();
                while (uicEnumerator.MoveNext()) {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string) uicEnumerator.Key)) {
                        uicDirCache.Remove(uicEnumerator.Key);
                        uicEnumerator = uicDirCache.GetEnumerator();
                    }
                }

                var rccEnumerator = rccDirCache.GetEnumerator();
                while (rccEnumerator.MoveNext()) {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string) rccEnumerator.Key)) {
                        rccDirCache.Remove(rccEnumerator.Key);
                        rccEnumerator = rccDirCache.GetEnumerator();
                    }
                }
            } catch { }
        }

        private static void SaveDirectory(string type, string dir)
        {
            dir = HelperFunctions.NormalizeRelativeFilePath(dir);
            var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (key == null)
                return;
            key.SetValue(type, dir);
        }

        private static void SaveOption(string type, string option)
        {
            var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (key == null)
                return;
            if (option == null)
                option = "";
            key.SetValue(type, option);
        }

        public static bool AutoUpdateUicSteps()
        {
            if (ValueExists("AutoUpdateUicSteps"))
                return GetBoolValue("AutoUpdateUicSteps", true);
            return GetBoolValue("AutoUpdateBuildSteps", true);
        }

        private static bool GetBoolValue(string key, bool defaultValue)
        {
            var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey == null)
                return defaultValue;
            return ((int) regKey.GetValue(key, defaultValue ? 1 : 0)) > 0;
        }

        private static bool ValueExists(string key)
        {
            var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey != null) {
                foreach (var s in regKey.GetValueNames()) {
                    if (s == key)
                        return true;
                }
            }
            return false;
        }

        private static void SetBoolValue(string key, bool val)
        {
            var regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey == null)
                return;
            regKey.SetValue(key, val ? 1 : 0);
        }

        public static bool GetQmlDebug(EnvDTE.Project project)
        {
            return QtProject.Create(project).QmlDebug;
        }

        public static void SaveQmlDebug(EnvDTE.Project project, bool enabled)
        {
            QtProject.Create(project).QmlDebug = enabled;
        }

        public static ushort GetQmlDebugPort(EnvDTE.Project project)
        {
            return QtProject.Create(project).QmlDebugPort;
        }

        public static void SaveQmlDebugPort(EnvDTE.Project project, ushort port)
        {
            QtProject.Create(project).QmlDebugPort = port;
        }
    }
}
