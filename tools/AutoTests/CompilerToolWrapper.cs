/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia. For licensing terms and
** conditions see http://qt.digia.com/licensing. For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights. These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;

namespace CompilerToolSpace
{
    using Microsoft.VisualStudio.VCProjectEngine;

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
        private Object compilerObj;
        private Type compilerType;

        public CompilerToolWrapper(VCConfiguration config)
        {
            compilerTool = ((IVCCollection)config.Tools).Item("VCCLCompilerTool") as VCCLCompilerTool;
            if (compilerTool == null)
            {
                compilerObj = ((IVCCollection)config.Tools).Item("VCCLCompilerTool");
                compilerType = compilerObj.GetType();
            }
        }

        public CompilerToolWrapper(VCFileConfiguration config)
        {
            compilerTool = config.Tool as VCCLCompilerTool;
            if (compilerTool == null)
            {
                compilerObj = config.Tool;
                compilerType = compilerObj.GetType();
            }
        }

        public CompilerToolWrapper(VCPropertySheet sheet)
        {
            compilerTool = ((IVCCollection)sheet.Tools).Item("VCCLCompilerTool") as VCCLCompilerTool;
            if (compilerTool == null)
            {
                compilerObj = ((IVCCollection)sheet.Tools).Item("VCCLCompilerTool");
                compilerType = compilerObj.GetType();
            }
        }

        public bool IsCompilerToolNull()
        {
            return compilerTool == null;
        }

        public void SetAdditionalIncludeDirectories(string value)
        {
            if (compilerTool != null)
                compilerTool.AdditionalIncludeDirectories = value;
            else
                SetStringProperty("AdditionalIncludeDirectories", value);
        }

        public void AddPreprocessorDefinitions(string value)
        {
            SetPreprocessorDefinitions(GetPreprocessorDefinitions() + value);
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
            else
                return GetStringProperty("PreprocessorDefinitions");
        }

        public void AddAdditionalIncludeDirectories(string value)
        {
            string includes = GetAdditionalIncludeDirectories();
            if (includes != null && includes.Length > 0 && !includes.EndsWith(";"))
                includes += ";";
            SetAdditionalIncludeDirectories(includes + value);
        }

        public string GetAdditionalIncludeDirectories()
        {
            if (compilerTool != null)
                return compilerTool.AdditionalIncludeDirectories;
            else
                return GetStringProperty("AdditionalIncludeDirectories");
        }

        public string GetPrecompiledHeaderFile()
        {
            if (compilerTool != null)
                return compilerTool.PrecompiledHeaderFile;
            else
                return GetStringProperty("PrecompiledHeaderFile");
        }

        public string GetPrecompiledHeaderThrough()
        {
            if (compilerTool != null)
                return compilerTool.PrecompiledHeaderThrough;
            else
                return GetStringProperty("PrecompiledHeaderThrough");
        }

        public pchOption GetUsePrecompiledHeader()
        {
            if (compilerTool != null)
                return compilerTool.UsePrecompiledHeader;
            else
            {
                object obj = compilerType.InvokeMember("UsePrecompiledHeader", System.Reflection.BindingFlags.GetProperty, null, compilerObj, null);
                if (obj == null)
                    return pchOption.pchNone;
                else
                    return (pchOption)obj;
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

        public void SetRuntimeLibrary(runtimeLibraryOption value)
        {
            if (compilerTool != null)
                compilerTool.RuntimeLibrary = value;
            else
                compilerType.InvokeMember(
                    "RuntimeLibrary",
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { @value });
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

        public void SetWarningLevel(warningLevelOption value)
        {
            if (compilerTool != null)
                compilerTool.WarningLevel = value;
            else
                compilerType.InvokeMember(
                    "WarningLevel",
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    compilerObj,
                    new object[] { @value });
        }

        public void SetBufferSecurityCheck(bool value)
        {
            if (compilerTool != null)
                compilerTool.BufferSecurityCheck = value;
            else
                compilerType.InvokeMember(
                    "BufferSecurityCheck",
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
            object obj = compilerType.InvokeMember(name, System.Reflection.BindingFlags.GetProperty, null, compilerObj, null);
            if (obj == null)
                return "";
            else
                return (string)obj;
        }

    }
}
