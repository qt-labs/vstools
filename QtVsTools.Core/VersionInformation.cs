/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace QtVsTools.Core
{
    using Common;
    using MsBuild;

    [DebuggerDisplay("QtDir = {QtDir}, Version = {Major}.{Minor}.{Patch}")]
    public class VersionInformation
    {
        public string QtDir { get; }

        public string Namespace { get; }
        public Platform Platform { get; }

        public uint Major { get; private set; }
        public uint Minor { get; private set; }
        public uint Patch { get; private set; }

        public bool IsWinRt { get; }
        public string LibExecs { get; }
        public string InstallPrefix { get; }
        public string QtInstallDocs { get; }

        public string VsPlatformName { get; }
        public string QMakeSpecDirectory { get; }
        public string GetQMakeConfEntry(string entryName) => qmakeConf[entryName];

        private string targetMachine;
        public string TargetMachine
        {
            get
            {
                if (!string.IsNullOrEmpty(targetMachine))
                    return targetMachine;

                // Get VS project settings
                try {
                    var tempProData = new StringBuilder();
                    tempProData.AppendLine("SOURCES = main.cpp");

                    var modules = QtModules.Instance.GetAvailableModules(Major)
                        .Where(mi => mi.Selectable).ToList();

                    foreach (var mi in modules) {
                        tempProData.AppendLine(
                            string.Format("qtHaveModule({0}): HEADERS += {0}.h", mi.proVarQT));
                    }

                    var randomName = Path.GetRandomFileName();
                    var tempDir = Path.Combine(Path.GetTempPath(), randomName);
                    Directory.CreateDirectory(tempDir);

                    var tempPro = Path.Combine(tempDir, $"{randomName}.pro");
                    File.WriteAllText(tempPro, tempProData.ToString());

                    var qmake = new QMakeImport(this, tempPro, disableWarnings: true);
                    if (qmake.Run(setVCVars: true) == 0) {
                        var tempVcxproj = Path.Combine(tempDir, $"{randomName}.vcxproj");
                        var msbuildProj = MsBuildProjectReaderWriter.Load(tempVcxproj);

                        Directory.Delete(tempDir, true);

                        targetMachine = msbuildProj.GetProperty("Link", "TargetMachine");
                    }
                } catch (Exception exception) {
                    exception.Log();
                    targetMachine = null;
                }

                return targetMachine;
            }
        }

        public static VersionInformation GetOrAddByName(string name)
        {
            try {
                if (!string.IsNullOrEmpty(name))
                    return GetOrAddByPath(QtVersionManager.GetInstallPath(name));
            } catch (Exception exception) {
                exception.Log();
            }

            return null;
        }

        public static VersionInformation GetOrAddByPath(string dir)
        {
            try {
                dir ??= Environment.GetEnvironmentVariable("QTDIR");
                dir = new FileInfo(dir?.TrimEnd('\\', '/', ' ') ?? "").FullName;
            } catch {
                return null;
            }

            if (!Directory.Exists(dir))
                return null;

            CacheSemaphore.Wait();
            try {
                var vi = Cache.AddOrUpdate(
                    dir,
                    _ => // Add value factory
                        new VersionInformation(dir),
                    (key, value) => // Update value factory
                    {
                        if (string.IsNullOrEmpty(value.QtDir))
                            value = new VersionInformation(key);
                        return value;
                    });
                return vi;
            } catch (Exception exception) {
                exception.Log();
                return null;
            } finally {
                CacheSemaphore.Release();
            }
        }

        public static implicit operator System.Version(VersionInformation v) =>
            new((int)v.Major, (int)v.Minor, (int)v.Patch);

        private readonly QMakeConf qmakeConf;
        private static readonly SemaphoreSlim CacheSemaphore = new(1, 1);
        private static readonly ConcurrentDictionary<string, VersionInformation> Cache
            = new(Utils.CaseIgnorer);

        private VersionInformation(string qtDir)
        {
            QtDir = qtDir;
            try {
                var qtConfig = new QtConfig(qtDir);
                Platform = qtConfig.Platform;
                Namespace = qtConfig.Namespace;
                VsPlatformName = Platform switch
                {
                    Platform.x86 => "Win32",
                    Platform.x64 => "x64",
                    Platform.arm64 => "ARM64",
                    _ => null
                };

                var qmakeQuery = new QMakeQuery(qtDir);
                var success = SetVersionComponents(qmakeQuery["QT_VERSION"]);
                if (!success && !SetVersionComponents(qtConfig.VersionString)) {
                    QtDir = null;
                    return;
                }
                LibExecs = qmakeQuery["QT_INSTALL_LIBEXECS"];
                QtInstallDocs = qmakeQuery["QT_INSTALL_DOCS"];
                InstallPrefix = qmakeQuery["QT_INSTALL_PREFIX"];
                IsWinRt = qmakeQuery["QMAKE_XSPEC"].StartsWith("winrt");

                qmakeConf = new QMakeConf(qmakeQuery);
                QMakeSpecDirectory = qmakeConf?.QMakeSpecDirectory;
            } catch (Exception exception) {
                exception.Log();
                QtDir = null;
            }
        }

        private bool SetVersionComponents(string version)
        {
            if (string.IsNullOrEmpty(version))
                return false;
            var versionParts = version.Split('.');
            if (versionParts.Length != 3)
                return false;

            Major = uint.Parse(versionParts[0]);
            Minor = uint.Parse(versionParts[1]);
            Patch = uint.Parse(versionParts[2]);

            return true;
        }
    }
}
