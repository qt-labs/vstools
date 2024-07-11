/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Core.Options
{
    using Core;
    using QtVsTools.Common;
    using VisualStudio;
    using static Common.Utils;
    using static HelperFunctions;

    public class QtVersionsPage : UIElementDialogPage
    {
        private static LazyFactory Lazy { get; } = new();

        private static QtVersionsTable VersionsTable => Lazy.Get(() =>
            VersionsTable, () => new QtVersionsTable());

        protected override UIElement Child => VersionsTable;

        public override void LoadSettingsFromStorage()
        {
            var versions = new ObservableCollection<QtVersion>();
            var defaultVersion = QtVersionManager.GetDefaultVersionName();
            foreach (var versionName in QtVersionManager.GetVersions()) {
                var versionPath = QtVersionManager.GetInstallPath(versionName);
                if (string.IsNullOrEmpty(versionPath))
                    continue;

                var host = BuildHost.Windows;
                var compiler = "msvc";
                if (versionPath.StartsWith("SSH:") || versionPath.StartsWith("WSL:")) {
                    var linuxPaths = versionPath.Split(':');
                    versionPath = linuxPaths[1];
                    host = linuxPaths[0] == "SSH" ? BuildHost.LinuxSSH : BuildHost.LinuxWSL;
                    compiler = "g++";
                    if (linuxPaths.Length > 2 && !string.IsNullOrEmpty(linuxPaths[2]))
                        compiler = linuxPaths[2];
                }

                var isDefault = versionName == defaultVersion;
                versions.Insert(isDefault ? 0 : versions.Count, new QtVersion
                {
                    IsDefault = isDefault,
                    InitialIsDefault = versionName == defaultVersion,
                    Name = versionName,
                    InitialName = versionName,
                    Path = versionPath,
                    InitialPath = FromNativeSeparators(NormalizePath(versionPath) ?? ""),
                    Host = host,
                    InitialHost = host,
                    Compiler = compiler,
                    InitialCompiler = compiler
                });
            }
            VersionsTable.QtVersions = versions;
        }

        public override void SaveSettingsToStorage()
        {
            static void RemoveVersion(string versionName)
            {
                try {
                    if (QtVersionManager.HasVersion(versionName))
                        QtVersionManager.RemoveVersion(versionName);
                } catch (Exception exception) {
                    exception.Log();
                }
            }

            foreach (var qtVersion in VersionsTable.RemovedQtVersions)
                RemoveVersion(qtVersion.InitialName);
            VersionsTable.RemovedQtVersions.Clear();

            foreach (var version in VersionsTable.QtVersions) {
                try {
                    if (version.State.HasFlag(State.HostModified) || version.State.HasFlag(State.CompilerModified)) {
                        if (version.Host != BuildHost.Windows) {
                            var access = version.Host == BuildHost.LinuxSSH ? "SSH" : "WSL";
                            var compiler = version.Compiler;
                            if (compiler == "g++")
                                compiler = string.Empty;
                            QtVersionManager.SaveVersion(version.Name,
                                $"{access}:{version.Path}:{compiler}", checkPath: false);
                        } else {
                            QtVersionManager.SaveVersion(version.Name, version.Path);
                        }
                        RemoveVersion(version.InitialName);
                        continue;
                    }

                    if (version.State.HasFlag(State.PathModified))
                        QtVersionManager.SaveVersion(version.Name, version.Path);

                    if (!version.State.HasFlag(State.NameModified))
                        continue;
                    try {
                        QtVersionManager.SaveVersion(version.Name, version.Path);
                    } catch (Exception exception) {
                        exception.Log();
                    }
                    RemoveVersion(version.InitialName);
                } catch (Exception exception) {
                    exception.Log();
                    RemoveVersion(version.Name);
                }
            }

            try {
                var defaultVersion =
                    VersionsTable.QtVersions.FirstOrDefault(v => v is { IsDefault: true });
                QtVersionManager.SaveDefaultVersion(defaultVersion?.Name ?? "");
            } catch (Exception exception) {
                exception.Log();
            }

            if (!Notifications.NoQtVersion.IsOpen || QtVersionManager.GetVersions()?.Any() != true)
                return;

            ThreadHelper.ThrowIfNotOnUIThread();
            Notifications.NoQtVersion.Close();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            var errorMessages = VersionsTable.GetErrorMessages().ToList();
            try {
                var versions = VersionsTable.QtVersions;
                foreach (var version in versions) {
                    if (!version.State.HasFlag(State.PathModified) || version.Host != BuildHost.Windows)
                        continue;

                    var versionPath = version.Path;
                    if (string.IsNullOrEmpty(versionPath))
                        continue;
                    if (string.Equals(Path.GetFileName(versionPath), "qmake.exe", IgnoreCase))
                        versionPath = Path.GetDirectoryName(versionPath);
                    if (string.Equals(Path.GetFileName(versionPath ?? ""), "bin", IgnoreCase))
                        versionPath = Path.GetDirectoryName(versionPath);

                    var versionInfo = VersionInformation.GetOrAddByPath(versionPath);
                    var generator = versionInfo?.GetQMakeConfEntry("MAKEFILE_GENERATOR");

                    if (generator is "MSVC.NET" or "MSBUILD")
                        continue;

                    var message = $"{version.Name} - Incompatible makefile generator: {generator}";
                    errorMessages.Add(message);
                    version.ErrorMessage = message;
                }
            } catch (Exception exception) {
                errorMessages.Add(exception.Message);
            }

            if (errorMessages.Any()) {
                VersionsTable.Focus();
                var distinctErrorMessages = errorMessages
                    .SelectMany(errMsg => errMsg.Split(new[] { Environment.NewLine },
                                            StringSplitOptions.RemoveEmptyEntries))
                    .Distinct()
                    .Select(message => " * " + message);
                var errorMessage = "Invalid Qt versions:" + Environment.NewLine
                    + Environment.NewLine + string.Join(Environment.NewLine, distinctErrorMessages);

                VsShellUtilities.ShowMessageBox(
                    VsServiceProvider.Instance as IServiceProvider,
                    errorMessage, null,
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
