/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
    /// For platforms other than Win32 the type VCCLCompilerTool is not
    /// available. So we have to use the reflection system to get and set the
    /// desired properties. This class should be the only place where
    /// VCCLCompilerTool is used. Using VCCLCompilerTool directly will break the
    /// VS integration for Win CE.
    internal class CompilerToolWrapper
    {
        private readonly VCCLCompilerTool compilerTool;
        private readonly object compilerObj;
        private readonly Type compilerType;

        public static CompilerToolWrapper Create(VCConfiguration config)
        {
            CompilerToolWrapper wrapper = null;
            try {
                wrapper = new CompilerToolWrapper(((IVCCollection)config.Tools)
                    .Item("VCCLCompilerTool"));
            } catch (Exception e) {
                e.Log();
            }
            return wrapper?.IsNull() == true ? null : wrapper;
        }

        public static CompilerToolWrapper Create(VCFileConfiguration config)
        {
            CompilerToolWrapper wrapper = null;
            try {
                wrapper = new CompilerToolWrapper(config.Tool);
            } catch (Exception e) {
                e.Log();
            }
            return wrapper?.IsNull() == true ? null : wrapper;
        }

        private CompilerToolWrapper(object tool)
        {
            compilerTool = tool as VCCLCompilerTool;
            if (compilerTool != null)
                return;
            compilerObj = tool;
            compilerType = compilerObj.GetType();
        }

        private bool IsNull() => compilerTool == null && compilerObj == null;

        public IEnumerable<string> PreprocessorDefinitions
        {
            get
            {
                var defines = GetPreprocessorDefinitions();
                return string.IsNullOrEmpty(defines)
                    ? Array.Empty<string>()
                    : defines.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public void SetPreprocessorDefinitions(string value)
        {
            if (compilerTool == null)
                SetProperty("PreprocessorDefinitions", value);
            else
                compilerTool.PreprocessorDefinitions = value;
        }

        public string GetPreprocessorDefinitions()
        {
            return compilerTool?.PreprocessorDefinitions
                ?? GetProperty<string>("PreprocessorDefinitions");
        }

        public string GetPrecompiledHeaderThrough()
        {
            return compilerTool?.PrecompiledHeaderThrough
                ?? GetProperty<string>("PrecompiledHeaderThrough");
        }

        public pchOption GetUsePrecompiledHeader()
        {
            return compilerTool?.UsePrecompiledHeader
                ?? GetProperty("UsePrecompiledHeader", pchOption.pchNone);
        }

        public void SetUsePrecompiledHeader(pchOption value)
        {
            if (compilerTool == null)
                SetProperty("UsePrecompiledHeader", value);
            else
                compilerTool.UsePrecompiledHeader = value;
        }

        private void SetProperty<T>(string name, T value)
        {
            compilerType.InvokeMember(name, BindingFlags.SetProperty, null, compilerObj,
                new object[] { value }
             );
        }

        private T GetProperty<T>(string name, T defaultValue = default)
        {
            try {
                var value = compilerType.InvokeMember(name, BindingFlags.GetProperty, null,
                    compilerObj, null);
                if (value != null)
                    return (T)value;
            } catch (Exception e) {
                e.Log();
            }
            return defaultValue;
        }
    }
}
