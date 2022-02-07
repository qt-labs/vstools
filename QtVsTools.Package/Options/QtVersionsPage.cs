/****************************************************************************
**
** Copyright (C) 2020 The Qt Company Ltd.
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
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using QtVsTools.Core;

namespace QtVsTools.Options
{
    public class QtVersionsPage : UIElementDialogPage
    {
        QtVersionManager VersionManager => QtVersionManager.The();

        QtVersionsTable _VersionsTable;
        QtVersionsTable VersionsTable => _VersionsTable
            ?? (_VersionsTable = new QtVersionsTable());

        protected override UIElement Child => VersionsTable;

        public override void LoadSettingsFromStorage()
        {
            var versions = new List<QtVersionsTable.Row>();
            foreach (var versionName in VersionManager.GetVersions()) {
                var versionPath = VersionManager.GetInstallPath(versionName);
                BuildHost host = BuildHost.Windows;
                string compiler = "msvc";
                if (versionPath.StartsWith("SSH:") || versionPath.StartsWith("WSL:")) {
                    var linuxPaths = versionPath.Split(':');
                    versionPath = linuxPaths[1];
                    if (linuxPaths[0] == "SSH")
                        host = BuildHost.LinuxSSH;
                    else
                        host = BuildHost.LinuxWSL;
                    compiler = "g++";
                    if (linuxPaths.Length > 2 && !string.IsNullOrEmpty(linuxPaths[2]))
                        compiler = linuxPaths[2];
                }
                var defaultVersion = VersionManager.GetDefaultVersion();
                versions.Add(new QtVersionsTable.Row()
                {
                    IsDefault = (versionName == defaultVersion),
                    VersionName = versionName,
                    Path = versionPath,
                    Host = host,
                    Compiler = compiler,
                });
            }
            VersionsTable.UpdateVersions(versions);
        }

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var versionName in VersionManager.GetVersions()) {
                try {
                    VersionManager.RemoveVersion(versionName);
                } catch (Exception exception) {
                    Messages.Print(
                        exception.Message + "\r\n\r\nStacktrace:\r\n" + exception.StackTrace);
                }
            }
            foreach (var version in VersionsTable.Versions) {
                try {
                    if (version.Host == BuildHost.Windows) {
                        var versionInfo = VersionInformation.Get(version.Path);
                        var generator = versionInfo.GetQMakeConfEntry("MAKEFILE_GENERATOR");
                        if (generator != "MSVC.NET" && generator != "MSBUILD")
                            throw new Exception(SR.GetString(
                                "AddQtVersionDialog_IncorrectMakefileGenerator", generator));
                        VersionManager.SaveVersion(version.VersionName, version.Path);
                    } else {
                        string name = version.VersionName;
                        string access =
                            (version.Host == BuildHost.LinuxSSH) ? "SSH" : "WSL";
                        string path = version.Path;
                        string compiler = version.Compiler;
                        if (compiler == "g++")
                            compiler = string.Empty;
                        path = string.Format("{0}:{1}:{2}", access, path, compiler);
                        VersionManager.SaveVersion(name, path, checkPath: false);
                    }
                } catch (Exception exception) {
                    Messages.Print(
                        exception.Message + "\r\n\r\nStacktrace:\r\n" + exception.StackTrace);
                }
            }
            try {
                var defaultVersion = VersionsTable.Versions
                    .Where(version => version.IsDefault)
                    .FirstOrDefault();
                if (defaultVersion != null)
                    VersionManager.SaveDefaultVersion(defaultVersion.VersionName);
            } catch (Exception exception) {
                Messages.Print(
                    exception.Message + "\r\n\r\nStacktrace:\r\n" + exception.StackTrace);
            }

            if (InfoBarMessages.NoQtVersion.IsOpen && VersionManager.GetVersions()?.Any() == true)
                InfoBarMessages.NoQtVersion.Close();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            var errorMessages = VersionsTable.GetErrorMessages();
            if (errorMessages == null || !errorMessages.Any()) {
                base.OnApply(e);
                return;
            }
            e.ApplyBehavior = ApplyKind.Cancel;
            VersionsTable.Focus();
            string errorMessage = string.Format("Invalid Qt versions:\r\n{0}",
                    string.Join("\r\n", errorMessages.Select(errMsg => " * " + errMsg)));
            VsShellUtilities.ShowMessageBox(QtVsToolsPackage.Instance,
                errorMessage, "Qt VS Tools", OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
