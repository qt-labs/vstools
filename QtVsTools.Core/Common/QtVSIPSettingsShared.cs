/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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

using System;
using System.Collections;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Win32;
using QtVsTools.Core;

namespace QtVsTools.Common
{
    public static class QtVSIPSettingsShared
    {
        private static readonly Hashtable mocDirCache = new Hashtable();
        private static readonly Hashtable uicDirCache = new Hashtable();
        private static readonly Hashtable rccDirCache = new Hashtable();

        public static string GetDirectory(EnvDTE.Project project, string type)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // check for directory in following order:
            // - stored in project
            // - stored in cache
            // - retrieve from moc/uic steps
            // - globally defined default directory
            // - fallback on hardcoded directory
            if (project != null) {
                if (project.Globals.get_VariablePersists(type))
                    return HelperFunctions.NormalizeRelativeFilePath((string)project.Globals[type]);

                try {
                    if (type == Resources.mocDirKeyword && mocDirCache.Contains(project.FullName))
                        return (string)mocDirCache[project.FullName];
                    if (type == Resources.uicDirKeyword && uicDirCache.Contains(project.FullName))
                        return (string)uicDirCache[project.FullName];
                    if (type == Resources.rccDirKeyword && rccDirCache.Contains(project.FullName))
                        return (string)rccDirCache[project.FullName];

                    QtCustomBuildTool tool = null;
                    string configName = null;
                    string platformName = null;
                    var vcpro = (VCProject)project.Object;
                    foreach (VCFile vcfile in (IVCCollection)vcpro.Files) {
                        var name = vcfile.Name;
                        if ((type == Resources.mocDirKeyword && HelperFunctions.IsHeaderFile(name))
                            || (type == Resources.mocDirKeyword && HelperFunctions.IsMocFile(name))
                            || (type == Resources.uicDirKeyword && HelperFunctions.IsUicFile(name))
                            || (type == Resources.rccDirKeyword && HelperFunctions.IsQrcFile(name))) {
                            foreach (VCFileConfiguration config in (IVCCollection)vcfile.FileConfigurations) {
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

                        CleanUpCache(project);

                        return HelperFunctions.NormalizeRelativeFilePath(dir);
                    }
                } catch { }
            }

            return GetDirectory(type);
        }

        public static string GetDirectory(string type)
        {
            try {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
                if (key != null) {
                    var path = (string)key.GetValue(type, null);
                    if (path != null)
                        return HelperFunctions.NormalizeRelativeFilePath(path);
                }
            } catch { }
            if (type == Resources.mocDirKeyword)
                return Resources.generatedFilesDir + "\\$(ConfigurationName)";
            return Resources.generatedFilesDir;
        }

        public static string GetOption(EnvDTE.Project project, string type)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // check for directory in following order:
            // - stored in project
            // - globally defined default option
            // - empty options
            if (project != null && project.Globals.get_VariablePersists(type))
                return (string)project.Globals[type];
            return GetOption(type);
        }

        public static string GetOption(string type)
        {
            try {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
                if (key != null) {
                    var opt = (string)key.GetValue(type, null);
                    if (opt != null)
                        return opt;
                }
            } catch { }
            return null;
        }

        public static bool GetBoolValue(EnvDTE.Project project, string type)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // check for directory in following order:
            // - stored in project
            // - globally defined default option
            // - empty options
            if (project != null && project.Globals.get_VariablePersists(type))
                return Convert.ToInt32(project.Globals[type] as string) > 0;
            return GetBoolValue(type, false);
        }

        public static bool GetBoolValue(string key, bool defaultValue)
        {
            var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey == null)
                return defaultValue;
            return ((int)regKey.GetValue(key, defaultValue ? 1 : 0)) > 0;
        }

        public static bool ValueExists(string key)
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

        public static string GetProjectQtSetting(EnvDTE.Project project, string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vcProject = project.Object as VCProject;
            if (vcProject == null)
                return null;

            var vcConfigs = vcProject.Configurations as IVCCollection;
            if (vcConfigs == null)
                return null;

            var activeConfig = project.ConfigurationManager.ActiveConfiguration;
            if (activeConfig == null)
                return null;

            var activeConfigId = string.Format("{0}|{1}",
                activeConfig.ConfigurationName, activeConfig.PlatformName);

            var props = vcProject as IVCBuildPropertyStorage;
            if (props == null)
                return null;

            try {
                return props.GetPropertyValue(propertyName, activeConfigId, "ProjectFile");
            } catch {
                return null;
            }
        }

        public static void CleanUpCache(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                var mocEnumerator = mocDirCache.GetEnumerator();
                while (mocEnumerator.MoveNext()) {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string)mocEnumerator.Key)) {
                        mocDirCache.Remove(mocEnumerator.Key);
                        mocEnumerator = mocDirCache.GetEnumerator();
                    }
                }

                var uicEnumerator = uicDirCache.GetEnumerator();
                while (uicEnumerator.MoveNext()) {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string)uicEnumerator.Key)) {
                        uicDirCache.Remove(uicEnumerator.Key);
                        uicEnumerator = uicDirCache.GetEnumerator();
                    }
                }

                var rccEnumerator = rccDirCache.GetEnumerator();
                while (rccEnumerator.MoveNext()) {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string)rccEnumerator.Key)) {
                        rccDirCache.Remove(rccEnumerator.Key);
                        rccEnumerator = rccDirCache.GetEnumerator();
                    }
                }
            } catch { }
        }
    }
}
