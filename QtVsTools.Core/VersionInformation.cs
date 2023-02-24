/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QtVsTools.Core
{
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
        private static readonly Hashtable _cache = new Hashtable();

        public static VersionInformation Get(string qtDir)
        {
            qtDir ??= Environment.GetEnvironmentVariable("QTDIR");
            if (qtDir == null)
                return null;

            try {
                qtDir = new FileInfo(qtDir).FullName.ToUpperInvariant();
            } catch {
                return null;
            }

            if (_cache[qtDir] is not VersionInformation versionInfo) {
                versionInfo = new VersionInformation(qtDir);
                _cache.Add(qtDir, versionInfo);
            } else if (versionInfo.qtDir == null) {
                versionInfo = new VersionInformation(qtDir);
                _cache[qtDir] = versionInfo;
            }
            return versionInfo;
        }

        public string VC_MinimumVisualStudioVersion { get; }
        public string VC_ApplicationTypeRevision { get; }
        public string VC_WindowsTargetPlatformMinVersion { get; }
        public string VC_WindowsTargetPlatformVersion { get; }
        public string VC_Link_TargetMachine { get; }
        private string VC_PlatformToolset { get; }

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
                return;
            }

            // Get VS project settings
            try {
                var tempProData = new StringBuilder();
                tempProData.AppendLine("SOURCES = main.cpp");

                var modules = QtModules.Instance.GetAvailableModules(qtMajor)
                    .Where(mi => mi.Selectable).ToList();

                foreach (QtModule mi in modules) {
                    tempProData.AppendLine(string.Format(
                        "qtHaveModule({0}): HEADERS += {0}.h", mi.proVarQT));
                }

                var randomName = Path.GetRandomFileName();
                var tempDir = Path.Combine(Path.GetTempPath(), randomName);
                Directory.CreateDirectory(tempDir);

                var tempPro = Path.Combine(tempDir, $"{randomName}.pro");
                File.WriteAllText(tempPro, tempProData.ToString());

                var qmake = new QMakeImport(this, tempPro, disableWarnings: true);
                qmake.Run(setVCVars: true);

                var tempVcxproj = Path.Combine(tempDir, $"{randomName}.vcxproj");
                var msbuildProj = MsBuildProject.Load(tempVcxproj);

                Directory.Delete(tempDir, recursive: true);

                VC_MinimumVisualStudioVersion =
                    msbuildProj.GetProperty("MinimumVisualStudioVersion");
                VC_ApplicationTypeRevision =
                    msbuildProj.GetProperty("ApplicationTypeRevision");
                VC_WindowsTargetPlatformVersion =
                    msbuildProj.GetProperty("WindowsTargetPlatformVersion");
                VC_WindowsTargetPlatformMinVersion =
                    msbuildProj.GetProperty("WindowsTargetPlatformMinVersion");
                VC_PlatformToolset =
                    msbuildProj.GetProperty("PlatformToolset");
                VC_Link_TargetMachine =
                    msbuildProj.GetProperty("Link", "TargetMachine");

            } catch (Exception e) {
                throw new QtVSException("Error reading VS project settings", e);
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
            switch (platform()) {
            case Platform.x86:
                vsPlatformName = "Win32";
                break;
            case Platform.x64:
                vsPlatformName = "x64";
                break;
            case Platform.arm64:
                vsPlatformName = "ARM64";
                break;
            }
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

            throw new QtVSException("qglobal.h not found");
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
                throw new QtVSException("Error: unexpected result of qmake query");
            }
        }

        public string InstallPrefix => qmakeQuery["QT_INSTALL_PREFIX"];
    }
}
