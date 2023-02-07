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
