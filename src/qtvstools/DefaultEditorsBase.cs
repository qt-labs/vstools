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

using Microsoft.Win32;

namespace QtVsTools
{
    // Base class to support writing default editor values to registry
    public class DefaultEditorsBase
    {
        private const string registryBasePath = @"SOFTWARE\Microsoft\VisualStudio\{0}";
        private const string newProjectTemplates = @"\NewProjectTemplates\TemplateDirs\{0}\/1";

        private const string linguist = @"Qt Linguist";
        private const string designer = @"Qt Designer";
        private const string qrcEditor = @"Qt Resource Editor";

        private readonly string guid;
        private readonly string appWrapper;
        private readonly string qrcEditorName;

        /// <summary>
        /// Write default editor values to registry for VS 2013 if add-in is installed. Applies
        /// both to Qt4 and Qt5 version of the add-in. TODO: Remove if we drop Visual Studio 2013.
        /// </summary>
        public void WriteAddinRegistryValues()
        {
            var basePath = string.Format(registryBasePath, @"12.0");
            var projectTemplates = basePath + string.Format(newProjectTemplates, guid);

            var addinInstallPath = GetAddinInstallPath(GetCUKey(projectTemplates, false));
            if (string.IsNullOrEmpty(addinInstallPath))
                addinInstallPath = GetAddinInstallPath(GetLMKey(projectTemplates, false));
            WriteRegistryValues(basePath, addinInstallPath);
        }

        /// <summary>
        /// Write default editor values to registry for Visual Studio 2013 and above. Uses the VSIX
        /// install path.
        /// </summary>
        public void WriteVsixRegistryValues()
        {
            if (Vsix.Instance.Dte != null) {
                var basePath = string.Format(registryBasePath, Vsix.Instance.Dte.Version)
#if DEBUG
                    + @"Exp"
#endif
                ;
                WriteRegistryValues(basePath, Vsix.Instance.PkgInstallPath);
            }
        }

        protected DefaultEditorsBase(string uid, string wrapper, string editor)
        {
            guid = uid;
            appWrapper = wrapper;
            qrcEditorName = editor;
        }

        // Get add-in installation path using a registry key
        private static string GetAddinInstallPath(RegistryKey key)
        {
            if (key == null)
                return null;

            var templatesDirPath = key.GetValue(@"TemplatesDir") as string;
            if (string.IsNullOrEmpty(templatesDirPath))
                return null;

            return templatesDirPath.Substring(0, templatesDirPath.IndexOf(@"\projects\",
                System.StringComparison.Ordinal));
        }

        // Get/create registry key under HKCU
        private static RegistryKey GetCUKey(string key_path, bool writable)
        {
            var key = Registry.CurrentUser.OpenSubKey(key_path, writable);
            if (key == null && writable)
                key = Registry.CurrentUser.CreateSubKey(key_path);
            return key;
        }

        // Get/create registry key under HKLM
        private static RegistryKey GetLMKey(string key_path, bool writable)
        {
            var key = Registry.LocalMachine.OpenSubKey(key_path, writable);
            if (key == null && writable)
                key = Registry.LocalMachine.CreateSubKey(key_path);
            return key;
        }

        private void WriteRegistryValues(string basePath, string installPath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(installPath))
                return;

            installPath += @"\";
            WriteCustomTypeEditor(basePath + @"\Default Editors\ts", linguist);
            var key = GetCUKey(basePath + @"\Default Editors\ts\" + linguist, true);
            key.SetValue(@"", installPath + appWrapper);

            WriteCustomTypeEditor(basePath + @"\Default Editors\ui", designer);
            key = GetCUKey(basePath + @"\Default Editors\ui\" + designer, true);
            key.SetValue(@"", installPath + appWrapper);

            WriteCustomTypeEditor(basePath + @"\Default Editors\qrc", qrcEditor);
            key = GetCUKey(basePath + @"\Default Editors\qrc\" + qrcEditor, true);
            key.SetValue(@"", installPath + qrcEditorName);
        }

        private static void WriteCustomTypeEditor(string path, string customEditor)
        {
            var key = Registry.CurrentUser.OpenSubKey(path, true);
            if (key == null) {
                key = Registry.CurrentUser.CreateSubKey(path);
                key.SetValue(@"Custom", customEditor);
                key.SetValue(@"Type", 0x00000002, RegistryValueKind.DWord);
            }
        }
    }

    // Default editor handling for Qt4 add-in

    // Default editor handling for Qt5 add-in

    // Default editor handling for Qt VS Tools
}
