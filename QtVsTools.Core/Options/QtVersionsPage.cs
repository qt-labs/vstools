/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Core.Options
{
    using Common;
    using Core;
    using VisualStudio;
    using static QtVersionsTable;
    using static Utils;

    public class QtVersionsPage : UIElementDialogPage
    {
        static LazyFactory Lazy { get; } = new();

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
                    host = linuxPaths[0] == "SSH" ? BuildHost.LinuxSSH : BuildHost.LinuxWSL;
                    compiler = "g++";
                    if (linuxPaths.Length > 2 && !string.IsNullOrEmpty(linuxPaths[2]))
                        compiler = linuxPaths[2];
                }
                var defaultVersion = VersionManager.GetDefaultVersion();
                versions.Add(new Row
                {
                    IsDefault = versionName == defaultVersion,
                    VersionName = versionName,
                    InitialVersionName = versionName,
                    Path = versionPath,
                    Host = host,
                    Compiler = compiler,
                    State = State.Unknown
                });
            }
            VersionsTable.AddVersions(versions);
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
                    versions.FirstOrDefault(v => v is { IsDefault: true, State: not State.Removed })
                        ?? versions.FirstOrDefault(v => v.State != State.Removed);
                VersionManager.SaveDefaultVersion(defaultVersion?.VersionName ?? "");
            } catch (Exception exception) {
                exception.Log();
            }

            //if (Notifications.NoQtVersion.IsOpen && VersionManager.GetVersions()?.Any() == true)
            //    Notifications.NoQtVersion.Close();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            var errorMessages = VersionsTable.GetErrorMessages().ToList();

            try {
                var versions = VersionsTable.Versions;
                foreach (var version in versions) {
                    version.ErrorMessageOnApply = null;
                    if (!version.State.HasFlag(State.Modified) || version.Host != BuildHost.Windows)
                        continue;
                    if (!version.State.HasFlag((State)Column.Path))
                        continue;

                    var versionPath = version.Path;
                    if (Path.GetFileName(versionPath).Equals("qmake.exe", IgnoreCase))
                        versionPath = Path.GetDirectoryName(versionPath);
                    if (Path.GetFileName(versionPath ?? "").Equals("bin", IgnoreCase))
                        versionPath = Path.GetDirectoryName(versionPath);

                    var qtConfiguration = new QMakeConf(versionPath);
                    var generator = qtConfiguration.Entries["MAKEFILE_GENERATOR"].ToString();

                    if (generator is "MSVC.NET" or "MSBUILD")
                        continue;

                    errorMessages.Add($"{version.VersionName} - Unsupported makefile "
                      + $"generator: {generator}");
                    version.ErrorMessageOnApply = errorMessages.Last();
                }
            } catch (Exception exception) {
                errorMessages.Add(exception.Message);
            }

            if (errorMessages.Any()) {
                VersionsTable.Focus();
                var errorMessage = "Invalid Qt versions:\r\n"
                    + $"{string.Join("\r\n", errorMessages.Select(errMsg => " * " + errMsg))}";
                VsShellUtilities.ShowMessageBox(
                    VsServiceProvider.Instance as IServiceProvider,
                    errorMessage,
                    "Qt VS Tools",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
            } else {
                base.OnApply(e);
                Instances.CMake.ActiveProject?.CheckQtStatus();
            }
        }
    }
}
