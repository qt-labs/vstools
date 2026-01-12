// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QtVsTools.Core
{
    using Common;
    using MsBuild;

    [DebuggerDisplay("QtDir = {QtDir}, Version = {Major}.{Minor}.{Patch}")]
    public class VersionInformation
    {
        public string QtDir { get; }

        public string Namespace { get; private set; }
        public Platform Platform { get; private set; }

        public uint Major { get; private set; }
        public uint Minor { get; private set; }
        public uint Patch { get; private set; }

        public bool IsWinRt { get; private set; }
        public string LibExecs { get; private set; }
        public string InstallPrefix { get; private set; }
        public string QtInstallDocs { get; private set; }

        public string VsPlatformName { get; private set; }
        public string QMakeSpecDirectory { get; private set; }

        /// <summary>
        /// Retrieves the value of a variable from the qmake.conf file.
        /// </summary>
        /// <param name="entryName">The name of the variable.</param>
        /// <returns>
        /// The value of the variable specified by <paramref name="entryName" />,
        /// or <see langword="null" /> if the variable is not found.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="entryName" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="System.NullReferenceException">
        /// Thrown when the qmake configuration file has not been successfully loaded,
        /// causing the internal qmake configuration data to be <see langword="null" />.
        /// </exception>
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

                    if (QMakeImport.Run(this, tempPro, disableWarnings: true) == 0) {
                        var tempVcxproj = Path.Combine(tempDir, $"{randomName}.vcxproj");
                        var msbuildProj = MsBuildProjectReaderWriter.Load(tempVcxproj);

                        Utils.DeleteDirectory(tempDir, Utils.Option.Recursive);

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

            lock (CacheLock) {
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
                }
            }
        }

        public static implicit operator System.Version(VersionInformation v) =>
            new((int)v.Major, (int)v.Minor, (int)v.Patch);

        private readonly QMakeConf qmakeConf;
        private static readonly object CacheLock = new();
        private static readonly ConcurrentDictionary<string, VersionInformation> Cache
            = new(Utils.CaseIgnorer);

        private VersionInformation(string qtDir)
        {
            QtDir = null;
            try {
                if (TryInitialize(qtDir) is not { } localConf)
                    return;
                QtDir = qtDir;
                qmakeConf = localConf;
            } catch (Exception exception) {
                exception.Log();
            }
        }

        /// <summary>
        /// Initializes version information from the Qt installation.
        /// </summary>
        private QMakeConf TryInitialize(string qtDir)
        {
            var qtConfig = new QtConfig(qtDir);
            if (!QtQueryInfo.TryCreate(qtDir, out var queryInfo))
                return null;

            if (!TryParseVersion(queryInfo.QtVersion, out var parsedVersion)) {
                if (!TryParseVersion(qtConfig.VersionString, out parsedVersion))
                    return null;
            }

            var qmakeXSpec = queryInfo.QMakeXSpec;
            var qmakeConfLocal = new QMakeConf(queryInfo);

            // Reject non-MSVC generators for Windows mkspecs to fail closed.
            if (qmakeXSpec.StartsWith("win", StringComparison.OrdinalIgnoreCase)) {
                var generator = qmakeConfLocal["MAKEFILE_GENERATOR"];
                if (generator is not ("MSVC.NET" or "MSBUILD"))
                    return null;
            }

            Namespace = qtConfig.Namespace;
            Platform = qtConfig.Platform;
            VsPlatformName = Platform switch
            {
                Platform.x86 => "Win32",
                Platform.x64 => "x64",
                Platform.arm64 => "ARM64",
                _ => null
            };

            Major = (uint)parsedVersion.Major;
            Minor = (uint)parsedVersion.Minor;
            Patch = (uint)parsedVersion.Build;

            LibExecs = queryInfo.LibExecs;
            QtInstallDocs = queryInfo.InstallDocs;
            InstallPrefix = queryInfo.InstallPrefix;
            IsWinRt = qmakeXSpec.StartsWith("winrt", StringComparison.OrdinalIgnoreCase);
            QMakeSpecDirectory = qmakeConfLocal.QMakeSpecDirectory;

            return qmakeConfLocal;
        }

        /// <summary>
        /// Parses a strict three-part Qt version string (Major.Minor.Build).
        /// </summary>
        private static bool TryParseVersion(string version, out System.Version parsed)
        {
            parsed = null;
            if (!System.Version.TryParse(version, out var result))
                return false;
            if (result.Build < 0 || result.Revision >= 0)
                return false;
            parsed = result;
            return true;
        }
    }
}
