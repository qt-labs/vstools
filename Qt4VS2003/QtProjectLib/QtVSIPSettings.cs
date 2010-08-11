/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

using System;
using Microsoft.Win32;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections;
using System.Drawing;

namespace Nokia.QtProjectLib
{
    public class QtVSIPSettings
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

        public static string GetMocDirectory(EnvDTE.Project project, string configName, string platformName)
        {
            string dir = GetDirectory(project, Resources.mocDirKeyword);
            if (!string.IsNullOrEmpty(configName))
                dir = dir.Replace("$(ConfigurationName)", configName);
            if (!string.IsNullOrEmpty(platformName))
                dir = dir.Replace("$(PlatformName)", platformName);
            return dir;
        }

        public static bool HasDifferentMocFilePerConfig(EnvDTE.Project project)
        {
            string mocDir = GetMocDirectory(project);
            return mocDir.Contains("$(ConfigurationName)");
        }

        public static bool HasDifferentMocFilePerPlatform(EnvDTE.Project project)
        {
            string mocDir = GetMocDirectory(project);
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
            {
                options = GetMocOptions();
            }
            SaveOption(project, Resources.mocOptionsKeyword, options);
        }

        public static void SaveLUpdateOnBuild(EnvDTE.Project project)
        {
            SetBoolValue(project, Resources.lupdateKeyword, GetLUpdateOnBuild());
        }

        public static void SaveLUpdateOnBuild(EnvDTE.Project project, bool value)
        {
            SetBoolValue(project, Resources.lupdateKeyword, value);
        }

        public static void SaveMocOptions(string options)
        {
            SaveOption(Resources.mocOptionsKeyword, options);
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
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
                if (key != null)
                {
                    string path = (string)key.GetValue(type, null);
                    if (path != null)
                        return HelperFunctions.NormalizeRelativeFilePath(path);
                }
            }
            catch { }
            if (type == Resources.mocDirKeyword)
                return Resources.generatedFilesDir + "\\$(ConfigurationName)";
            else
                return Resources.generatedFilesDir;
        }

        private static string GetOption(string type)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
                if (key != null)
                {
                    string opt = (string)key.GetValue(type, null);
                    if (opt != null)
                        return opt;
                }
            }
            catch { }
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
            if (project != null)
            {
                if (project.Globals.get_VariablePersists(type))
                {
                    return HelperFunctions.NormalizeRelativeFilePath((string)project.Globals[type]);
                }
                else
                {
                    try
                    {
                        if (type == Resources.mocDirKeyword && mocDirCache.Contains(project.FullName))
                            return (string)mocDirCache[project.FullName];
                        else if (type == Resources.uicDirKeyword && uicDirCache.Contains(project.FullName))
                            return (string)uicDirCache[project.FullName];
                        else if (type == Resources.rccDirKeyword && rccDirCache.Contains(project.FullName))
                            return (string)rccDirCache[project.FullName];

                        VCCustomBuildTool tool = null;
                        string configName = null;
                        string platformName = null;
                        VCProject vcpro = (VCProject)project.Object;
                        foreach(VCFile vcfile in (IVCCollection)vcpro.Files)
                        {
                            if ((type == Resources.mocDirKeyword &&
                                (HelperFunctions.HasHeaderFileExtension(vcfile.Name) || vcfile.Name.ToLower().EndsWith(".moc")))
                                || (type == Resources.uicDirKeyword && vcfile.Name.ToLower().EndsWith(".ui"))
                                || (type == Resources.rccDirKeyword && vcfile.Name.ToLower().EndsWith(".qrc")))
                            {
                                foreach (VCFileConfiguration config in (IVCCollection)vcfile.FileConfigurations)
                                {
                                    tool = HelperFunctions.GetCustomBuildTool(config);
                                    configName = config.Name.Remove(config.Name.IndexOf('|'));
                                    VCConfiguration vcConfig = config.ProjectConfiguration as VCConfiguration;
                                    VCPlatform platform = vcConfig.Platform as VCPlatform;
                                    platformName = platform.Name;
                                    if (tool != null && (tool.CommandLine.ToLower().IndexOf("moc.exe") != -1
                                        || (tool.CommandLine.ToLower().IndexOf("uic.exe") != -1)
                                        || (tool.CommandLine.ToLower().IndexOf("rcc.exe") != -1)))
                                        break;
                                    tool = null;
                                }

                                if (tool != null)
                                    break;
                            }
                        }

                        if (tool != null)
                        {
                            string dir = null;
                            int lastindex = tool.Outputs.LastIndexOf('\\');
                            if (tool.Outputs.LastIndexOf('/') > lastindex)
                                lastindex = tool.Outputs.LastIndexOf('/');

                            if (lastindex == -1)
                                dir = ".";
                            else 
                                dir = tool.Outputs.Substring(0, lastindex);
                            dir = dir.Replace("\"","");

                            if (type == Resources.mocDirKeyword)
                            {
                                int index;
                                if ((index = dir.ToLower().IndexOf(configName.ToLower())) != -1)
                                    dir = dir.Replace(dir.Substring(index, configName.Length), "$(ConfigurationName)");
                                if ((index = dir.ToLower().IndexOf(platformName.ToLower())) != -1)
                                    dir = dir.Replace(dir.Substring(index, platformName.Length), "$(PlatformName)");

                                mocDirCache.Add(project.FullName, HelperFunctions.NormalizeRelativeFilePath(dir));
                            }
                            else if (type == Resources.uicDirKeyword)
                                uicDirCache.Add(project.FullName, HelperFunctions.NormalizeRelativeFilePath(dir));
                            else if (type == Resources.rccDirKeyword)
                                rccDirCache.Add(project.FullName, HelperFunctions.NormalizeRelativeFilePath(dir));

                            cleanUpCache(project);

                            return HelperFunctions.NormalizeRelativeFilePath(dir);
                        }
                    }
                    catch {}
                }
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
            {
                return (string)project.Globals[type];
            }
            return GetOption(type);
        }

        private static bool GetBoolValue(EnvDTE.Project project, string type)
        {
            // check for directory in following order:
            // - stored in project
            // - globally defined default option
            // - empty options
            if (project != null && project.Globals.get_VariablePersists(type))
            {
                string valueString = (string)project.Globals[type];
                int val = Convert.ToInt32(valueString);
                bool v = val > 0 ? true : false;
                return v;
            }
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
            try
            {
                IDictionaryEnumerator mocEnumerator = mocDirCache.GetEnumerator();
                while ( mocEnumerator.MoveNext() )
                {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string)mocEnumerator.Key))
                    {
                        mocDirCache.Remove(mocEnumerator.Key);
                        mocEnumerator = mocDirCache.GetEnumerator();
                    }
                }

                IDictionaryEnumerator uicEnumerator = uicDirCache.GetEnumerator();
                while ( uicEnumerator.MoveNext() )
                {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string)uicEnumerator.Key))
                    {
                        uicDirCache.Remove(uicEnumerator.Key);
                        uicEnumerator = uicDirCache.GetEnumerator();
                    }
                }

                IDictionaryEnumerator rccEnumerator = rccDirCache.GetEnumerator();
                while (rccEnumerator.MoveNext())
                {
                    if (!HelperFunctions.IsProjectInSolution(project.DTE, (string)rccEnumerator.Key))
                    {
                        rccDirCache.Remove(rccEnumerator.Key);
                        rccEnumerator = rccDirCache.GetEnumerator();
                    }
                }
            }
            catch { }
        }

        private static void SaveDirectory(string type, string dir)
        {
            dir = HelperFunctions.NormalizeRelativeFilePath(dir);
            RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (key == null)
                return;
            key.SetValue(type, dir);
        }

        private static void SaveOption(string type, string option)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (key == null)
                return;
            if (option == null)
                option = "";
            key.SetValue(type, option);
        }

        public static Size Size()
        {
            Size s = new Size();
            s.Width = GetGridValue("gridX", 10);
            s.Height = GetGridValue("gridY", 10);
            return s;
        }
        
        public static void SetSize(Size s)
        {
            SetGridValue("gridX", s.Width);
            SetGridValue("gridY", s.Height);
        }

        public static bool Visible()
        {
            return GetBoolValue("visible", true);
        }

        public static void SetVisible(bool visible)
        {
            SetBoolValue("visible", visible);
        }

        public static bool AutoRunUic()
        {
            return GetBoolValue("AutoRunUic4", true);
        }

        public static void SetAutoRunUic(bool autoRun)
        {
            SetBoolValue("AutoRunUic4", autoRun);
        }

        public static bool AutoUpdateRccSteps()
        {
            return GetBoolValue("AutoUpdateRccSteps", true);
        }

        public static void SetAutoUpdateRccSteps(bool autoUpdate)
        {
            SetBoolValue("AutoUpdateRccSteps", autoUpdate);
        }

        public static bool AutoUpdateMocSteps()
        {
            if (ValueExists("AutoUpdateMocSteps"))
                return GetBoolValue("AutoUpdateMocSteps", true);
            return GetBoolValue("AutoUpdateBuildSteps", true);
        }

        public static void SetAutoUpdateMocSteps(bool autoUpdate)
        {
            SetBoolValue("AutoUpdateMocSteps", autoUpdate);
        }

        public static bool AutoUpdateUicSteps()
        {
            if (ValueExists("AutoUpdateUicSteps"))
                return GetBoolValue("AutoUpdateUicSteps", true);
            return GetBoolValue("AutoUpdateBuildSteps", true);
        }

        public static void SetAutoUpdateUicSteps(bool autoUpdate)
        {
            SetBoolValue("AutoUpdateUicSteps", autoUpdate);
        }

        private static bool GetBoolValue(string key, bool defaultValue)
        {
            bool v;
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey != null)
            {
                int val = (int)regKey.GetValue(key, defaultValue ? (object)1 : (object)0);
                v = val > 0 ? true : false;
            } 
            else 
            {
                v = defaultValue;
            }
            return v;
        }

        private static bool ValueExists(string key)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey != null) 
            {
                foreach (string s in regKey.GetValueNames())
                    if (s == key)
                        return true;
            }
            return false;
        }

        private static void SetBoolValue(string key, bool val)
        {
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey == null)
                return;
            regKey.SetValue(key, val ? 1 : 0);
        }

        private static int GetGridValue(string key, int defaultValue)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey != null)
            {
                try
                {
                    int val = Convert.ToInt32((regKey.GetValue(key, defaultValue)));
                    if (val <= 0 || val > 100)
                        return defaultValue;
                    else
                        return val;
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }
            else
            {
                return defaultValue;
            }
        }

        private static void SetGridValue(string key, int val)
        {
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Resources.registryPackagePath);
            if (regKey == null)
                return;
            regKey.SetValue(key, val.ToString());
        }
    }
}
