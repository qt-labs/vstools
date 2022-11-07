/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Options
{
    using Common;
    using Core;
    using static QtVsTools.Options.QtVersionsTable;

    public class QtVersionsPage : UIElementDialogPage
    {
        static LazyFactory Lazy { get; } = new LazyFactory();

        QtVersionManager VersionManager => QtVersionManager.The();

        QtVersionsTable VersionsTable => Lazy.Get(() =>
            VersionsTable, () => new QtVersionsTable());

        protected override UIElement Child => VersionsTable;

        public override void LoadSettingsFromStorage()
        {
            var versions = new List<Row>();
            foreach (var versionName in VersionManager.GetVersions()) {
                var versionPath = VersionManager.GetInstallPath(versionName);
                if (string.IsNullOrEmpty(versionPath))
                    continue;

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
                versions.Add(new Row()
                {
                    IsDefault = (versionName == defaultVersion),
                    VersionName = versionName,
                    InitialVersionName = versionName,
                    Path = versionPath,
                    Host = host,
                    Compiler = compiler,
                    State = State.Unknown
                });
            }
            VersionsTable.UpdateVersions(versions);
        }

        public override void SaveSettingsToStorage()
        {
            void RemoveVersion(string versionName)
            {
                try {
                    if (VersionManager.HasVersion(versionName))
                        VersionManager.RemoveVersion(versionName);
                } catch (Exception exception) {
                    exception.Log();
                }
            }

            var versions = VersionsTable.Versions.ToList();
            foreach (var version in versions) {
                if (version.State.HasFlag(State.Removed))
                    RemoveVersion(version.VersionName);

                if (!version.State.HasFlag(State.Modified))
                    continue;

                try {
                    if (version.Host != BuildHost.Windows) {
                        string name = version.VersionName;
                        string access = version.Host == BuildHost.LinuxSSH ? "SSH" : "WSL";
                        string path = version.Path;
                        string compiler = version.Compiler;
                        if (compiler == "g++")
                            compiler = string.Empty;
                        VersionManager.SaveVersion(name, $"{access}:{path}:{compiler}",
                            checkPath: false);
                    } else {
                        if (version.State.HasFlag((State)Column.Path))
                            VersionManager.SaveVersion(version.VersionName, version.Path);
                    }

                    if (version.State.HasFlag((State)Column.VersionName)) {
                        try {
                            VersionManager.SaveVersion(version.VersionName, version.Path);
                        } catch (Exception exception) {
                            exception.Log();
                        }
                        RemoveVersion(version.InitialVersionName);
                    }
                } catch (Exception exception) {
                    exception.Log();
                    version.State = State.Removed;
                    RemoveVersion(version.VersionName);
                }
            }

            try {
                var defaultVersion =
                    versions.FirstOrDefault(v => v.IsDefault && v.State != State.Removed)
                        ?? versions.FirstOrDefault(v => v.State != State.Removed);
                VersionManager.SaveDefaultVersion(defaultVersion?.VersionName ?? "");
            } catch (Exception exception) {
                exception.Log();
            }

            if (Notifications.NoQtVersion.IsOpen && VersionManager.GetVersions()?.Any() == true)
                Notifications.NoQtVersion.Close();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            var errorMessages = VersionsTable.GetErrorMessages().ToList();

            try {
                var versions = VersionsTable.Versions;
                foreach (var version in versions) {
                    if (!version.State.HasFlag(State.Modified) || version.Host != BuildHost.Windows)
                        continue;
                    if (version.State.HasFlag((State)Column.Path)) {
                        var versionPath = version.Path;
                        var ignoreCase = StringComparison.OrdinalIgnoreCase;
                        if (Path.GetFileName(versionPath).Equals("qmake.exe", ignoreCase))
                            versionPath = Path.GetDirectoryName(versionPath);
                        if (Path.GetFileName(versionPath ?? "").Equals("bin", ignoreCase))
                            versionPath = Path.GetDirectoryName(versionPath);

                        QMakeConf qtConfiguration = new QMakeConf(versionPath);
                        var generator = qtConfiguration.Entries["MAKEFILE_GENERATOR"].ToString();

                        if (generator != "MSVC.NET" && generator != "MSBUILD")
                            errorMessages.Add($"Unsupported makefile generator used: {generator}");
                    }
                }
            } catch (Exception exception) {
                errorMessages.Add(exception.Message);
            }

            if (errorMessages.Any()) {
                VersionsTable.Focus();
                var errorMessage = "Invalid Qt versions:\r\n"
                    + $"{string.Join("\r\n", errorMessages.Select(errMsg => " * " + errMsg))}";
                VsShellUtilities.ShowMessageBox(
                    QtVsToolsPackage.Instance,
                    errorMessage,
                    "Qt VS Tools",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
            } else {
                base.OnApply(e);
            }
        }
    }
}
