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
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace QtProjectLib
{
    /// <summary>
    /// Wrapper for the VCCLCompilerTool class.
    /// </summary>
    /// For platforms other than Win32 the type VCCLCompilerTool is not available.
    /// See http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=220646&SiteID=1
    /// So we have to use the reflection system to get and set the desired properties.
    /// This class should be the only place where VCCLCompilerTool is used.
    /// Using VCCLCompilerTool directly will break the VS integration for Win CE.
    class CompilerToolWrapper
    {
        private VCCLCompilerTool compilerTool;
        private readonly Object compilerObj;
        private readonly Type compilerType;

        public static CompilerToolWrapper Create(VCConfiguration config)
        {
            CompilerToolWrapper wrapper = null;
            try {
                wrapper = new CompilerToolWrapper(((IVCCollection) config.Tools)
                    .Item("VCCLCompilerTool"));
            } catch {
            }

            return wrapper.IsNull() ? null : wrapper;
        }

        public static CompilerToolWrapper Create(VCFileConfiguration config)
        {
            CompilerToolWrapper wrapper = null;
            try {
                wrapper = new CompilerToolWrapper(config.Tool);
            } catch {
            }

            return wrapper.IsNull() ? null : wrapper;
        }

        public static CompilerToolWrapper Create(VCPropertySheet sheet)
        {
            CompilerToolWrapper wrapper = null;
            try {
                wrapper = new CompilerToolWrapper(((IVCCollection) sheet.Tools)
                    .Item("VCCLCompilerTool"));
            } catch {
            }

            return wrapper.IsNull() ? null : wrapper;
        }

        protected CompilerToolWrapper(object tool)
        {
            if (tool == null)
                return;

            compilerTool = tool as VCCLCompilerTool;
            if (compilerTool == null) {
                compilerObj = tool;
                compilerType = compilerObj.GetType();
            }
        }

        protected bool IsNull()
        {
            return compilerTool == null && compilerObj == null;
        }

        public string ObjectFile
        {
            set
            {
                if (compilerTool != null)
                    compilerTool.ObjectFile = value;
                else
                    SetStringProperty("ObjectFile", value);
            }
        }

        public string ProgramDataBaseFileName
        {
            set
            {
                if (compilerTool != null)
                    compilerTool.ProgramDataBaseFileName = value;
                else
                    SetStringProperty("ProgramDataBaseFileName", value);
            }
        }

        public List<string> AdditionalIncludeDirectories
        {
            get
            {
                var directories = GetAdditionalIncludeDirectories();
                if (directories == null)
                    return new List<string>();
                // double quotes are escaped
                directories = directories.Replace("\\\"", "\"");
                var dirArray = directories.Split(new char[] { ';', ',' }, StringSplitOptions
                    .RemoveEmptyEntries);
                var lst = new List<string>(dirArray);
                int i = 0;
                while (i < lst.Count) {
                    string item = lst[i];
                    if (item.StartsWith("\"", StringComparison.Ordinal) && item.EndsWith("\"", StringComparison.Ordinal)) {
                        item = item.Remove(0, 1);
                        item = item.Remove(item.Length - 1, 1);
                        lst[i] = item;
                    }

                    if (lst[i].Length > 0)
                        ++i;
                    else
                        lst.RemoveAt(i);
                }
                return lst;
            }

            set
            {
                if (value == null) {
                    SetAdditionalIncludeDirectories(null);
                    return;
                }
                string newDirectories = "";
                bool firstLoop = true;
                foreach (string dir in value) {
                    if (firstLoop)
                        firstLoop = false;
                    else
                        newDirectories += ";";

                    if (dir.IndexOfAny(new char[] { ' ', '\t' }) > 0 || !Path.IsPathRooted(dir))
                        newDirectories += "\"" + dir + "\"";
                    else
                        newDirectories += dir;
                }
                if (newDirectories != GetAdditionalIncludeDirectories())
                    SetAdditionalIncludeDirectories(newDirectories);
            }
        }

        public void SetAdditionalIncludeDirectories(string value)
        {
            // Prevent setting of empty substring, as they break the build
            value = value.Replace("\"\",", "");
            if (compilerTool != null)
                compilerTool.AdditionalIncludeDirectories = value;
            else
                SetStringProperty("AdditionalIncludeDirectories", value);
        }

        public List<string> PreprocessorDefinitions
        {
            get
            {
                var ppdefsstr = GetPreprocessorDefinitions();
                if (string.IsNullOrEmpty(ppdefsstr))
                    return new List<string>();

                var ppdefs = ppdefsstr.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                return new List<string>(ppdefs);
            }
        }

        /// <summary>
        /// Adds a single preprocessor definition.
        /// </summary>
        /// <param name="value"></param>
        public void AddPreprocessorDefinition(string value)
        {
            var preprocessorDefs = GetPreprocessorDefinitions();
            if (preprocessorDefs != null) {
                var definesArray = preprocessorDefs.Split(new char[] { ';', ',' },
                    StringSplitOptions.RemoveEmptyEntries);
                var definesList = new List<string>(definesArray);
                if (definesList.Contains(value))
                    return;
                if (preprocessorDefs.Length > 0
                    && !preprocessorDefs.EndsWith(";", StringComparison.Ordinal)
                    && !value.StartsWith(";", StringComparison.Ordinal)) {
                    preprocessorDefs += ";";
                }
            }
            preprocessorDefs += value;
            SetPreprocessorDefinitions(preprocessorDefs);
        }

        /// <summary>
        /// Removes a single preprocessor definition.
        /// </summary>
        /// <param name="value"></param>
        public void RemovePreprocessorDefinition(string value)
        {
            var preprocessorDefs = GetPreprocessorDefinitions();
            if (preprocessorDefs == null)
                return;

            var definesArray = preprocessorDefs.Split(new char[] { ';', ',' },
                StringSplitOptions.RemoveEmptyEntries);
            var definesList = new List<string>(definesArray);
            if (definesList == null || !definesList.Remove(value))
                return;
            preprocessorDefs = "";
            bool firstIteration = true;
            foreach (string define in definesList) {
                if (firstIteration)
                    firstIteration = false;
                else
                    preprocessorDefs += ';';
                preprocessorDefs += define;
            }
            NormalizePreprocessorDefinitions(ref preprocessorDefs);
            SetPreprocessorDefinitions(preprocessorDefs);
        }

        private static void NormalizePreprocessorDefinitions(ref string preprocessorDefs)
        {
            int idx = 0;
            while ((idx = preprocessorDefs.IndexOf(' ', idx)) != -1)
                preprocessorDefs = preprocessorDefs.Remove(idx, 1);

            preprocessorDefs = preprocessorDefs.Replace(',', ';');

            idx = 0;
            while ((idx = preprocessorDefs.IndexOf(";;", idx, StringComparison.Ordinal)) != -1)
                preprocessorDefs = preprocessorDefs.Remove(idx, 1);

            if (preprocessorDefs.EndsWith(";", StringComparison.Ordinal))
                preprocessorDefs = preprocessorDefs.Remove(preprocessorDefs.Length - 1);
        }

        public void SetPreprocessorDefinitions(string value)
        {
            if (compilerTool != null)
                compilerTool.PreprocessorDefinitions = value;
            else
                SetStringProperty("PreprocessorDefinitions", value);
        }

        public string GetPreprocessorDefinitions()
        {
            if (compilerTool != null)
                return compilerTool.PreprocessorDefinitions;
            return GetStringProperty("PreprocessorDefinitions");
        }

        public void AddAdditionalIncludeDirectories(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            bool directoryAdded = false;
            var directories = value.Split(new char[] { ';', ',' }, StringSplitOptions
                .RemoveEmptyEntries);
            List<string> lst = AdditionalIncludeDirectories;
            foreach (string directory in directories) {
                if (lst.Contains(directory))
                    continue;

                lst.Add(directory);
                directoryAdded = true;
            }

            if (directoryAdded)
                AdditionalIncludeDirectories = lst;
        }

        public string[] GetAdditionalIncludeDirectoriesList()
        {
            var includes = GetAdditionalIncludeDirectories()
                .Split(new char[] { ',', ';' });
            string[] fixedincludes;
            fixedincludes = new string[includes.Length];
            int i = 0;
            foreach (string include in includes) {
                string incl = include;
                if (incl.StartsWith("\\\"", StringComparison.Ordinal) && incl.EndsWith("\\\"", StringComparison.Ordinal)) {
                    incl = incl.Remove(0, 2);
                    incl = incl.Remove(incl.Length - 2, 2);
                }
                fixedincludes[i++] = incl;
            }
            return fixedincludes;
        }

        public string GetAdditionalIncludeDirectories()
        {
            if (compilerTool != null)
                return compilerTool.AdditionalIncludeDirectories;
            return GetStringProperty("AdditionalIncludeDirectories");
        }

        public string GetPrecompiledHeaderFile()
        {
            if (compilerTool != null)
                return compilerTool.PrecompiledHeaderFile;
            return GetStringProperty("PrecompiledHeaderFile");
        }

        public string GetPrecompiledHeaderThrough()
        {
            if (compilerTool != null)
                return compilerTool.PrecompiledHeaderThrough;
            return GetStringProperty("PrecompiledHeaderThrough");
        }

        public pchOption GetUsePrecompiledHeader()
        {
            if (compilerTool != null)
                return compilerTool.UsePrecompiledHeader;
            else {
                var obj = compilerType.InvokeMember("UsePrecompiledHeader",
                    System.Reflection.BindingFlags.GetProperty, null, compilerObj, null);
                if (obj == null)
                    return pchOption.pchNone;
                return (pchOption) obj;
            }
        }

        public void SetDebugInformationFormat(debugOption value)
        {
            if (compilerTool != null)
                compilerTool.DebugInformationFormat = value;
            else
                compilerType.InvokeMember(
                    "DebugInformationFormat",
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { @value });
        }

        public runtimeLibraryOption RuntimeLibrary
        {
            get
            {
                if (compilerTool != null)
                    return compilerTool.RuntimeLibrary;

                var obj = compilerType.InvokeMember("RuntimeLibrary", BindingFlags.GetProperty,
                    null, compilerObj, null);
                if (obj == null)
                    return runtimeLibraryOption.rtMultiThreaded;
                return (runtimeLibraryOption) obj;
            }

            set
            {
                if (compilerTool == null) {
                    compilerType.InvokeMember("RuntimeLibrary", BindingFlags.SetProperty,
                        null, compilerObj, new object[] { @value });
                } else {
                    compilerTool.RuntimeLibrary = value;
                }
            }
        }

        public void SetOptimization(optimizeOption value)
        {
            if (compilerTool != null)
                compilerTool.Optimization = value;
            else
                compilerType.InvokeMember(
                    "Optimization",
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { @value });
        }

        public void SetTreatWChar_tAsBuiltInType(bool value)
        {
            if (compilerTool != null)
                compilerTool.TreatWChar_tAsBuiltInType = value;
            else
                compilerType.InvokeMember(
                    "TreatWChar_tAsBuiltInType",
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { @value });
        }

        public void SetPrecompiledHeaderFile(string file)
        {
            if (compilerTool != null)
                compilerTool.PrecompiledHeaderFile = file;
            else
                compilerType.InvokeMember(
                    "PrecompiledHeaderFile",
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { @file });
        }

        public void SetPrecompiledHeaderThrough(string value)
        {
            if (compilerTool != null)
                compilerTool.PrecompiledHeaderThrough = value;
            else
                compilerType.InvokeMember(
                    "PrecompiledHeaderThrough",
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { @value });
        }

        public void SetUsePrecompiledHeader(pchOption value)
        {
            if (compilerTool != null)
                compilerTool.UsePrecompiledHeader = value;
            else
                compilerType.InvokeMember(
                    "UsePrecompiledHeader",
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { @value });
        }

        private void SetStringProperty(string name, string value)
        {
            compilerType.InvokeMember(
                name,
                System.Reflection.BindingFlags.SetProperty,
                null,
                compilerObj,
                new object[] { @value });
        }

        private string GetStringProperty(string name)
        {
            object obj;
            try {
                obj = compilerType.InvokeMember(name, System.Reflection.BindingFlags.GetProperty,
                    null, compilerObj, null);
            } catch {
                obj = null;
            }
            if (obj == null)
                return "";
            return (string) obj;
        }

    }
}
