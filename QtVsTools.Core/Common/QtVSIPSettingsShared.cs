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
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Win32;
using QtVsTools.Core;

namespace QtVsTools.Common
{
    public static class QtVSIPSettingsShared
    {
        private static readonly Dictionary<string, string> mocDirCache
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> uicDirCache
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> rccDirCache
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string GetDirectory(EnvDTE.Project project, string type)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // check for directory in following order:
            // - stored in project
            // - stored in cache
            // - retrieve from moc/uic steps
            // - fall-back on hard-coded directory

            var fullName = project?.FullName;
            if (string.IsNullOrEmpty(fullName))
                return GetDirectory(type); // - fall-back on hard-coded directory

            if (project.Globals.get_VariablePersists(type)) // - stored in project
                return HelperFunctions.NormalizeRelativeFilePath(project.Globals[type] as string);

            switch (type) { // - stored in cache
            case Resources.mocDirKeyword:
                if (mocDirCache.ContainsKey(fullName))
                    return mocDirCache[fullName];
                break;
            case Resources.uicDirKeyword:
                if (uicDirCache.ContainsKey(fullName))
                    return uicDirCache[fullName];
                break;
            case Resources.rccDirKeyword:
                if (rccDirCache.ContainsKey(fullName))
                    return rccDirCache[fullName];
                break;
            default:
                return GetDirectory(type); // - fall-back on hard-coded directory
            }

            try {
                string configName = null;
                string platformName = null;
                QtCustomBuildTool tool = null;
                foreach (VCFile vcfile in (project.Object as VCProject).Files as IVCCollection) {
                    var name = vcfile?.Name;
                    if (string.IsNullOrEmpty(name))
                        continue;
                    if (!(HelperFunctions.IsHeaderFile(name) || HelperFunctions.IsMocFile(name)
                        || HelperFunctions.IsUicFile(name) || HelperFunctions.IsQrcFile(name)))
                        continue;

                    foreach (VCFileConfiguration config in vcfile?.FileConfigurations as IVCCollection) {
                        tool = new QtCustomBuildTool(config);
                        configName = config.Name.Remove(config.Name.IndexOf('|'));
                        var vcConfig = config.ProjectConfiguration as VCConfiguration;
                        platformName = (vcConfig.Platform as VCPlatform).Name;
                        var cmd = tool.CommandLine;
                        if (cmd.Contains("moc.exe") || cmd.Contains("uic.exe") || cmd.Contains("rcc.exe"))
                            break;
                        tool = null;
                    }

                    if (tool != null)
                        break;
                }

                if (tool == null)
                    return GetDirectory(type); // - fall-back on hard-coded directory

                var dir = ".";
                var lastindex = tool.Outputs.LastIndexOf(Path.DirectorySeparatorChar);
                if (tool.Outputs.LastIndexOf(Path.AltDirectorySeparatorChar) > lastindex)
                    lastindex = tool.Outputs.LastIndexOf(Path.AltDirectorySeparatorChar);

                if (lastindex != -1)
                    dir = tool.Outputs.Substring(0, lastindex);
                dir = dir.Replace("\"", "");

                if (type == Resources.mocDirKeyword) {
                    int index = dir.IndexOf(configName, StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                        dir = dir.Replace(dir.Substring(index, configName.Length), "$(ConfigurationName)");

                    index = dir.IndexOf(platformName, StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                        dir = dir.Replace(dir.Substring(index, platformName.Length), "$(PlatformName)");
                    dir = HelperFunctions.NormalizeRelativeFilePath(dir);

                    mocDirCache.Add(fullName, dir);
                } else if (type == Resources.uicDirKeyword) {
                    dir = HelperFunctions.NormalizeRelativeFilePath(dir);
                    uicDirCache.Add(fullName, dir);
                } else if (type == Resources.rccDirKeyword) {
                    dir = HelperFunctions.NormalizeRelativeFilePath(dir);
                    rccDirCache.Add(fullName, dir);
                } else {
                    dir = HelperFunctions.NormalizeRelativeFilePath(dir);
                }

                CleanUpCache(project);
                return dir; // - retrieved from moc/uic/rcc steps
            } catch { }
            return GetDirectory(type); // - fall-back on hard-coded directory
        }

        private const string registryPath = "SOFTWARE\\" + Resources.registryPackagePath;

        public static string GetDirectory(string type)
        {
            try {
                var key = Registry.CurrentUser.OpenSubKey(registryPath);
                if (key != null) {
                    if (key.GetValue(type, null) is string path)
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
                return project.Globals[type] as string;
            return GetOption(type);
        }

        public static string GetOption(string type)
        {
            try {
                var key = Registry.CurrentUser.OpenSubKey(registryPath);
                if (key != null) {
                    if (key.GetValue(type, null) is string opt)
                        return opt;
                }
            } catch { }
            return null;
        }

        public static bool GetBoolValue(string key, bool defaultValue)
        {
            var regKey = Registry.CurrentUser.OpenSubKey(registryPath);
            if (regKey == null)
                return defaultValue;
            return ((int)regKey.GetValue(key, defaultValue ? 1 : 0)) > 0;
        }

        public static bool ValueExists(string key)
        {
            var regKey = Registry.CurrentUser.OpenSubKey(registryPath);
            if (regKey != null) {
                foreach (var s in regKey.GetValueNames()) {
                    if (s == key)
                        return true;
                }
            }
            return false;
        }

        public static void CleanUpCache(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in HelperFunctions.ProjectsInSolution(project.DTE))
                projects.Add(p.FullName);

            mocDirCache.RemoveValues(projects);
            uicDirCache.RemoveValues(projects);
            rccDirCache.RemoveValues(projects);
        }

        static void RemoveValues(this Dictionary<string, string> cache, HashSet<string> projects)
        {
            foreach (var key in cache.Keys) {
                if (projects.Contains(key))
                    cache.Remove(key);
            }
        }
    }
}
