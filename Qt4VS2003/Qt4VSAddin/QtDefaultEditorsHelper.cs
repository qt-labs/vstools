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
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace Qt5VSAddin
{
    // Base class to support writing default editor values to registry
    public class DefaultEditorsBase
    {
        protected static string templatesDir = "TemplatesDir";
        protected static string path_template = "SOFTWARE\\Microsoft\\VisualStudio\\{0}.0\\NewProjectTemplates\\TemplateDirs\\{1}\\/1";
        protected static string linguist = "SOFTWARE\\Microsoft\\VisualStudio\\{0}.0\\Default Editors\\ts\\Qt Linguist";
        protected static string designer = "SOFTWARE\\Microsoft\\VisualStudio\\{0}.0\\Default Editors\\ui\\Qt Designer";
        protected static string qrc_editor = "SOFTWARE\\Microsoft\\VisualStudio\\{0}.0\\Default Editors\\qrc\\Qt Resource Editor";
        protected string addin_guid = null;
        protected string app_wrapper_name = null;
        protected string qrc_editor_name = null;

        // Check if add-in is installed in any supported VS version
        public bool IsAddinInstalled()
        {
            // Check VS2008, VS2010, VS2012 and VS2013
            return IsAddinInstalled(9) || IsAddinInstalled(10) || IsAddinInstalled(11) || IsAddinInstalled(12);
        }

        // Write default editor values to registry for all supported VS
        // versions if add-in is installed
        public void WriteRegistryValues()
        {
            if (IsAddinInstalled(9))
                WriteRegistryValues(9);
            if (IsAddinInstalled(10))
                WriteRegistryValues(10);
            if (IsAddinInstalled(11))
                WriteRegistryValues(11);
            if (IsAddinInstalled(12))
                WriteRegistryValues(12);
        }

        // Write default editor values to registry for given VS version
        //  9 == VS2008
        // 10 == VS2010
        // 11 == VS2012
        // 12 == VS2013
        protected void WriteRegistryValues(int vs_ver)
        {
            string inst_path = GetAddinInstallPath(vs_ver);

            string tmp = GetRegistryPath(linguist, vs_ver);
            RegistryKey key = GetCUKey(tmp, true);
            key.SetValue("", inst_path + "\\" + app_wrapper_name);

            tmp = GetRegistryPath(designer, vs_ver);
            key = GetCUKey(tmp, true);
            key.SetValue("", inst_path + "\\" + app_wrapper_name);

            tmp = GetRegistryPath(qrc_editor, vs_ver);
            key = GetCUKey(tmp, true);
            key.SetValue("", inst_path + "\\" + qrc_editor_name);
        }

        // Format incoming string to contain given VS version
        // 'path_template' must contain {0}
        protected string GetRegistryPath(string path_template, int vs_ver)
        {
            return string.Format(path_template, vs_ver);
        }

        // Get vs-addin template registry path
        protected string GetTemplateRegistryPath(int vs_ver)
        {
            return string.Format(path_template, vs_ver, addin_guid);
        }

        // Get add-in installation path using registry keys
        protected string GetInstallPath(RegistryKey cu_key, RegistryKey lm_key)
        {
            string inst_path = "";
            string addin_full_path = "";
            if (cu_key != null)
            {
                addin_full_path = (string)cu_key.GetValue(templatesDir);
            }
            if (string.IsNullOrEmpty(addin_full_path) && (lm_key != null))
                addin_full_path = (string)lm_key.GetValue(templatesDir);

            if (!string.IsNullOrEmpty(addin_full_path))
            {
                inst_path = addin_full_path.Substring(0, addin_full_path.IndexOf("\\projects\\"));
            }
            return inst_path;
        }

        // Get/create registry key under HKCU
        protected RegistryKey GetCUKey(string key_path, bool writable)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(key_path, writable);
            if (key == null && writable)
                key = Registry.CurrentUser.CreateSubKey(key_path);
            return key;
        }

        // Get/create registry key under HKLM
        protected RegistryKey GetLMKey(string key_path, bool writable)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(key_path, writable);
            if (key == null && writable)
                key = Registry.LocalMachine.CreateSubKey(key_path);
            return key;
        }

        // Get add-in installation path for given VS version
        protected string GetAddinInstallPath(int vs_ver)
        {
            // Registry path to check
            string addin_path = GetTemplateRegistryPath(vs_ver);
            // Try current user key
            RegistryKey cu_key = GetCUKey(addin_path, false);
            // Try local machine key
            RegistryKey lm_key = GetLMKey(addin_path, false);

            return GetInstallPath(cu_key, lm_key);
        }

        // Checks whether add-in is installed for given VS version
        protected bool IsAddinInstalled(int vs_ver)
        {
            string install_path = GetAddinInstallPath(vs_ver);
            return !string.IsNullOrEmpty(install_path);
        }
    }

    // Default editor handling for Qt4 add-in
    public class Qt4DefaultEditors : DefaultEditorsBase
    {
        public Qt4DefaultEditors()
        {
            // Set add-in spesific values
            addin_guid = "{6A7385B4-1D62-46e0-A4E3-AED4475371F0}";
            app_wrapper_name = "qtappwrapper.exe";
            qrc_editor_name = "qrceditor.exe";
        }
    }

    // Default editor handling for Qt5 add-in
    public class Qt5DefaultEditors : DefaultEditorsBase
    {
        public Qt5DefaultEditors()
        {
            // Set add-in spesific values
            addin_guid = "{C80C78C8-F64B-43df-9A53-96F7C44A1EB6}";
            app_wrapper_name = "qt5appwrapper.exe";
            qrc_editor_name = "q5rceditor.exe";
        }
    }
}
