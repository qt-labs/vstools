/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Win32;

namespace QtVsTools.Core
{
    using static Utils;

    public static class QtVSIPSettings
    {
        private static readonly Dictionary<string, string> MocDirCache = new(CaseIgnorer);
        private static readonly Dictionary<string, string> UicDirCache = new(CaseIgnorer);
        private static readonly Dictionary<string, string> RccDirCache = new(CaseIgnorer);


        public static string GetMocDirectory()
        {
            return GetDirectory(Resources.mocDirKeyword);
        }

        public static string GetMocDirectory(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetDirectory(project, Resources.mocDirKeyword);
        }

        public static bool HasDifferentMocFilePerConfig(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var mocDir = GetMocDirectory(project);
            return mocDir.Contains("$(ConfigurationName)");
        }

        public static bool HasDifferentMocFilePerPlatform(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var mocDir = GetMocDirectory(project);
            return mocDir.Contains("$(PlatformName)");
        }

        public static string GetRccDirectory()
        {
            return GetDirectory(Resources.rccDirKeyword);
        }

        public static string GetUicDirectory()
        {
            return GetDirectory(Resources.uicDirKeyword);
        }

        public static bool AutoUpdateUicSteps()
        {
            return GetBoolValue(ValueExists("AutoUpdateUicSteps")
                    ? "AutoUpdateUicSteps"
                    : "AutoUpdateBuildSteps",
                true);
        }

        private static string GetDirectory(EnvDTE.Project project, string type)
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

            if (project.Globals.VariablePersists[type]) // - stored in project
                return HelperFunctions.NormalizeRelativeFilePath(project.Globals[type] as string);

            switch (type) { // - stored in cache
            case Resources.mocDirKeyword:
                if (MocDirCache.ContainsKey(fullName))
                    return MocDirCache[fullName];
                break;
            case Resources.uicDirKeyword:
                if (UicDirCache.ContainsKey(fullName))
                    return UicDirCache[fullName];
                break;
            case Resources.rccDirKeyword:
                if (RccDirCache.ContainsKey(fullName))
                    return RccDirCache[fullName];
                break;
            default:
                return GetDirectory(type); // - fall-back on hard-coded directory
            }

            try {
                string configName = null;
                string platformName = null;
                QtCustomBuildTool tool = null;
                foreach (VCFile vcFile in (project.Object as VCProject).Files as IVCCollection) {
                    var name = vcFile?.Name;
                    if (string.IsNullOrEmpty(name))
                        continue;
                    if (!(HelperFunctions.IsHeaderFile(name) || HelperFunctions.IsMocFile(name)
                        || HelperFunctions.IsUicFile(name) || HelperFunctions.IsQrcFile(name)))
                        continue;

                    foreach (VCFileConfiguration config in vcFile.FileConfigurations as IVCCollection) {
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
                var lastIndex = tool.Outputs.LastIndexOf(Path.DirectorySeparatorChar);
                if (tool.Outputs.LastIndexOf(Path.AltDirectorySeparatorChar) > lastIndex)
                    lastIndex = tool.Outputs.LastIndexOf(Path.AltDirectorySeparatorChar);

                if (lastIndex != -1)
                    dir = tool.Outputs.Substring(0, lastIndex);
                dir = dir.Replace("\"", "");

                switch (type) {
                case Resources.mocDirKeyword: {
                    var index = dir.IndexOf(configName, IgnoreCase);
                    if (index != -1)
                        dir = dir.Replace(dir.Substring(index, configName.Length), "$(ConfigurationName)");

                    index = dir.IndexOf(platformName, IgnoreCase);
                    if (index != -1)
                        dir = dir.Replace(dir.Substring(index, platformName.Length), "$(PlatformName)");
                    dir = HelperFunctions.NormalizeRelativeFilePath(dir);

                    MocDirCache.Add(fullName, dir);
                    break;
                }
                case Resources.uicDirKeyword:
                    dir = HelperFunctions.NormalizeRelativeFilePath(dir);
                    UicDirCache.Add(fullName, dir);
                    break;
                case Resources.rccDirKeyword:
                    dir = HelperFunctions.NormalizeRelativeFilePath(dir);
                    RccDirCache.Add(fullName, dir);
                    break;
                default:
                    dir = HelperFunctions.NormalizeRelativeFilePath(dir);
                    break;
                }

                CleanUpCache(project);
                return dir; // - retrieved from moc/uic/rcc steps
            } catch { }
            return GetDirectory(type); // - fall-back on hard-coded directory
        }

        private const string RegistryPath = "SOFTWARE\\" + Resources.registryPackagePath;

        private static string GetDirectory(string type)
        {
            try {
                if (Registry.CurrentUser.OpenSubKey(RegistryPath) is {} key) {
                    if (key.GetValue(type, null) is string path)
                        return HelperFunctions.NormalizeRelativeFilePath(path);
                }
            } catch { }
            if (type == Resources.mocDirKeyword)
                return Resources.generatedFilesDir + "\\$(ConfigurationName)";
            return Resources.generatedFilesDir;
        }

        private static bool GetBoolValue(string key, bool defaultValue)
        {
            if (Registry.CurrentUser.OpenSubKey(RegistryPath) is {} regKey)
                return (int)regKey.GetValue(key, defaultValue ? 1 : 0) > 0;
            return defaultValue;
        }

        private static bool ValueExists(string key)
        {
            if (Registry.CurrentUser.OpenSubKey(RegistryPath) is {} regKey)
                return regKey.GetValueNames().Any(s => s == key);
            return false;
        }

        private static void CleanUpCache(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projects = new HashSet<string>(CaseIgnorer);
            foreach (var p in HelperFunctions.ProjectsInSolution(project.DTE))
                projects.Add(p.FullName);

            MocDirCache.RemoveValues(projects);
            UicDirCache.RemoveValues(projects);
            RccDirCache.RemoveValues(projects);
        }

        private static void RemoveValues(this Dictionary<string, string> cache,
            ICollection<string> projects)
        {
            foreach (var key in cache.Keys.Where(projects.Contains))
                cache.Remove(key);
        }
    }
}
