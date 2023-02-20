/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core
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
        private readonly VCCLCompilerTool compilerTool;
        private readonly Object compilerObj;
        private readonly Type compilerType;

        public static CompilerToolWrapper Create(VCConfiguration config)
        {
            CompilerToolWrapper wrapper = null;
            try {
                wrapper = new CompilerToolWrapper(((IVCCollection)config.Tools)
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

        public List<string> PreprocessorDefinitions
        {
            get
            {
                var ppdefsstr = GetPreprocessorDefinitions();
                if (string.IsNullOrEmpty(ppdefsstr))
                    return new List<string>();

                var ppdefs = ppdefsstr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                return new List<string>(ppdefs);
            }
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

            var obj = compilerType.InvokeMember("UsePrecompiledHeader",
                BindingFlags.GetProperty, null, compilerObj, null);
            if (obj == null)
                return pchOption.pchNone;
            return (pchOption)obj;
        }

        public void SetUsePrecompiledHeader(pchOption value)
        {
            if (compilerTool != null) {
                compilerTool.UsePrecompiledHeader = value;
            } else {
                compilerType.InvokeMember(
                    "UsePrecompiledHeader",
                    BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { value });
            }
        }

        private void SetStringProperty(string name, string value)
        {
            compilerType.InvokeMember(
                name,
                BindingFlags.SetProperty,
                null,
                compilerObj,
                new object[] { value });
        }

        private string GetStringProperty(string name)
        {
            object obj;
            try {
                obj = compilerType.InvokeMember(name, BindingFlags.GetProperty,
                    null, compilerObj, null);
            } catch {
                obj = null;
            }
            if (obj == null)
                return string.Empty;
            return (string)obj;
        }

    }
}
