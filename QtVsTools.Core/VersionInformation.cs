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
using System.Text.RegularExpressions;
using System.Threading;

namespace QtVsTools.Core
{
    using Common;
    using MsBuild;

    [DebuggerDisplay("Name = {name}, Version = {qtMajor}.{qtMinor}.{qtPatch}")]
    public class VersionInformation
    {
        //fields
        public string name;
        public readonly string qtDir;
        public uint qtMajor; // X in version x.y.z
        public uint qtMinor; // Y in version x.y.z
        public uint qtPatch; // Z in version x.y.z
        private QtConfig qtConfig;
        private QMakeConf qmakeConf;
        private readonly QMakeQuery qmakeQuery;
        private string vsPlatformName;

        private static readonly SemaphoreSlim CacheSemaphore = new (1, 1);
        private static readonly ConcurrentDictionary<string, VersionInformation> Cache
            = new(Utils.CaseIgnorer);

        public static VersionInformation GetOrAddByName(string name)
        {
            try {
                if (!string.IsNullOrEmpty(name))
                    return GetOrAddByPath(QtVersionManager.GetInstallPath(name), name);
            } catch (Exception exception) {
                exception.Log();
            }
            return null;
        }

        public static VersionInformation GetOrAddByPath(string dir, string name = null)
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
                var vi = Cache.AddOrUpdate(dir,
                    _ =>  // Add value factory
                    {
                        var vi = new VersionInformation(dir);
                        if (string.IsNullOrEmpty(vi.name) && !string.IsNullOrEmpty(name))
                            vi.name = name;
                        return vi;
                    },
                    (key, value) => // Update value factory
                    {
                        if (string.IsNullOrEmpty(value.qtDir))
                            value = new VersionInformation(key);
                        if (string.IsNullOrEmpty(value.name) && !string.IsNullOrEmpty(name))
                            value.name = name;
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

        private string vcLinkTargetMachine;
        public string VC_Link_TargetMachine
        {
            get
            {
                if (!string.IsNullOrEmpty(vcLinkTargetMachine))
                    return vcLinkTargetMachine;

                // Get VS project settings
                try {
                    var tempProData = new StringBuilder();
                    tempProData.AppendLine("SOURCES = main.cpp");

                    var modules = QtModules.Instance.GetAvailableModules(qtMajor)
                        .Where(mi => mi.Selectable).ToList();

                    foreach (var mi in modules) {
                        tempProData.AppendLine(string.Format(
                            "qtHaveModule({0}): HEADERS += {0}.h", mi.proVarQT));
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

                        Directory.Delete(tempDir, recursive: true);

                        vcLinkTargetMachine = msbuildProj.GetProperty("Link", "TargetMachine");
                    }
                } catch (Exception exception) {
                    exception.Log();
                    vcLinkTargetMachine = null;
                }
                return vcLinkTargetMachine;
            }
        }

        private VersionInformation(string qtDirIn)
        {
            qtDir = qtDirIn;
            try {
                qmakeQuery = new QMakeQuery(qtDirIn);
                SetupPlatformSpecificData();

                // Find version number
                var strVersion = qmakeQuery["QT_VERSION"];
                if (!string.IsNullOrEmpty(strVersion)) {
                    var versionParts = strVersion.Split('.');
                    if (versionParts.Length != 3) {
                        qtDir = null;
                        return;
                    }
                    qtMajor = uint.Parse(versionParts[0]);
                    qtMinor = uint.Parse(versionParts[1]);
                    qtPatch = uint.Parse(versionParts[2]);
                } else {
                    var inF = new StreamReader(Locate_qglobal_h());
                    var rgxpVersion = new Regex("#define\\s*QT_VERSION\\s*0x(?<number>\\d+)", RegexOptions.Multiline);
                    var contents = inF.ReadToEnd();
                    inF.Close();
                    var matchObj = rgxpVersion.Match(contents);
                    if (!matchObj.Success) {
                        qtDir = null;
                        return;
                    }

                    strVersion = matchObj.Groups[1].ToString();
                    var version = Convert.ToUInt32(strVersion, 16);
                    qtMajor = version >> 16;
                    qtMinor = (version >> 8) & 0xFF;
                    qtPatch = version & 0xFF;
                }

                try {
                    QtInstallDocs = qmakeQuery["QT_INSTALL_DOCS"];
                } catch { }
            } catch {
                qtDir = null;
            }
        }

        public string QtInstallDocs
        {
            get;
        }

        public string QMakeSpecDirectory => qmakeConf.QMakeSpecDirectory;

        public string Namespace()
        {
            qtConfig ??= new QtConfig(qtDir);
            return qtConfig.Namespace;
        }

        public string GetQMakeConfEntry(string entryName)
        {
            qmakeConf ??= new QMakeConf(qtDir, qmakeQuery);
            return qmakeConf.Entries[entryName].ToString();
        }

        /// <summary>
        /// Returns the platform name in a way Visual Studio understands.
        /// </summary>
        public string GetVSPlatformName()
        {
            return vsPlatformName;
        }

        /// <summary>
        /// Read platform name from qmake.conf.
        /// </summary>
        private void SetupPlatformSpecificData()
        {
            qmakeConf ??= new QMakeConf(qtDir, qmakeQuery);
            vsPlatformName = platform() switch
            {
                Platform.x86 => "Win32",
                Platform.x64 => "x64",
                Platform.arm64 => "ARM64",
                _ => vsPlatformName
            };
        }

        private string Locate_qglobal_h()
        {
            string[] candidates = {qtDir + "\\include\\qglobal.h",
                                   qtDir + "\\src\\corelib\\global\\qglobal.h",
                                   qtDir + "\\include\\QtCore\\qglobal.h"};

            foreach (var filename in candidates) {
                if (File.Exists(filename)) {
                    // check whether we look at the real qglobal.h or just a "pointer"
                    var inF = new StreamReader(filename);
                    var rgxpVersion = new Regex("#include\\s+\"(.+global.h)\"", RegexOptions.Multiline);
                    var matchObj = rgxpVersion.Match(inF.ReadToEnd());
                    inF.Close();
                    if (!matchObj.Success)
                        return filename;

                    if (matchObj.Groups.Count >= 2) {
                        var origCurrentDirectory = Directory.GetCurrentDirectory();
                        Directory.SetCurrentDirectory(filename.Substring(0, filename.Length - 10));   // remove "\\qglobal.h"
                        var absIncludeFile = Path.GetFullPath(matchObj.Groups[1].ToString());
                        Directory.SetCurrentDirectory(origCurrentDirectory);
                        if (File.Exists(absIncludeFile))
                            return absIncludeFile;
                    }
                }
            }

            throw new FileNotFoundException("qglobal.h not found");
        }

        public Platform platform()
        {
            qtConfig ??= new QtConfig(qtDir);
            return qtConfig.Platform;
        }

        public bool isWinRT()
        {
            try {
                var qmakeXSpec = qmakeQuery["QMAKE_XSPEC"];
                return qmakeXSpec.StartsWith("winrt");
            } catch {
                throw new InvalidOperationException("Error: unexpected result of qmake query");
            }
        }

        public string InstallPrefix => qmakeQuery["QT_INSTALL_PREFIX"];

        public string LibExecs => qmakeQuery["QT_INSTALL_LIBEXECS"];
    }
}
