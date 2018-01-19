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

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace QtProjectLib
{
    public class VersionInformation
    {
        //fields
        public string qtDir;
        public uint qtMajor; // X in version x.y.z
        public uint qtMinor; // Y in version x.y.z
        public uint qtPatch; // Z in version x.y.z
        public bool qt5Version = true;
        private QtConfig qtConfig;
        private QMakeConf qmakeConf;
        private string vsPlatformName;
        private static readonly Hashtable _cache = new Hashtable();

        public static VersionInformation Get(string qtDir)
        {
            qtDir = qtDir ?? Environment.GetEnvironmentVariable("QTDIR");
            if (qtDir == null)
                return null;

            qtDir = new FileInfo(qtDir).FullName.ToUpperInvariant();
            var versionInfo = _cache[qtDir] as VersionInformation;
            if (versionInfo == null) {
                versionInfo = new VersionInformation(qtDir);
                _cache.Add(qtDir, versionInfo);
            } else if (versionInfo.qtDir == null) {
                versionInfo = new VersionInformation(qtDir);
                _cache[qtDir] = versionInfo;
            }
            return versionInfo;
        }

        public static void Clear()
        {
             _cache.Clear();
        }

        private VersionInformation(string qtDirIn)
        {
            qtDir = qtDirIn;
            SetupPlatformSpecificData();

            // Find version number
            try {
                var qmakeQuery = new QMakeQuery(this);
                var strVersion = qmakeQuery.query("QT_VERSION");
                if (qmakeQuery.ErrorValue == 0 && strVersion.Length > 0) {
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
                qt5Version = (qtMajor == 5);

                try {
                    QtInstallDocs = qmakeQuery.query("QT_INSTALL_DOCS");
                } catch { }
            } catch {
                qtDir = null;
            }
        }

        public string QtInstallDocs
        {
            get; private set;
        }

        public string QMakeSpecDirectory
        {
            get { return qmakeConf.QMakeSpecDirectory; }
        }

        public bool IsStaticBuild()
        {
            if (qtConfig == null)
                qtConfig = new QtConfig(qtDir);
            return qtConfig.BuildType == BuildType.Static;
        }

        public string GetQMakeConfEntry(string entryName)
        {
            if (qmakeConf == null)
                qmakeConf = new QMakeConf(this);
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
            if (qmakeConf == null)
                qmakeConf = new QMakeConf(this); // TODO: Do we need this?
            vsPlatformName = (is64Bit()) ? @"x64" : @"Win32";
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

        internal static class NativeMethods
        {
            [DllImport("kernel32.dll",
                BestFitMapping = false,
                CharSet = CharSet.Auto,
                SetLastError = true)
            ]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GetBinaryType(string lpApplicationName, ref int lpBinaryType);
        }

        public bool is64Bit()
        {
            var fileToCheck = qtDir + "\\bin\\Qt5Core.dll";
            if (!File.Exists(fileToCheck))
                throw new QtVSException("Can't find " + fileToCheck);

            const ushort MAGIC_NUMBER_MZ = 0x5A4D;
            const uint FILE_HEADER_OFFSET = 0x3C;
            const uint PE_SIGNATURE = 0x4550;
            const ushort IMAGE_FILE_MACHINE_I386 = 0x014c;
            const ushort IMAGE_FILE_MACHINE_IA64 = 0x0200;
            const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;

            using (var b = new BinaryReader(File.Open(fileToCheck,
                FileMode.Open, FileAccess.Read, FileShare.Read))) {

                ushort magicNumber;
                try {
                    magicNumber = b.ReadUInt16();
                } catch {
                    throw new QtVSException("Error reading PE header: magic number");
                }
                if (magicNumber != MAGIC_NUMBER_MZ)
                    throw new QtVSException("Incorrect PE header format: magic number");

                uint fileHeaderOffset;
                try {
                    b.BaseStream.Seek(FILE_HEADER_OFFSET, SeekOrigin.Begin);
                    fileHeaderOffset = b.ReadUInt32();
                } catch {
                    throw new QtVSException("Error reading PE header: file header offset");
                }

                uint signature;
                try {
                    b.BaseStream.Seek(fileHeaderOffset, SeekOrigin.Begin);
                    signature = b.ReadUInt32();
                } catch {
                    throw new QtVSException("Error reading PE header: signature");
                }
                if (signature != PE_SIGNATURE)
                    throw new QtVSException("Incorrect PE header format: signature");

                ushort machine;
                try {
                    machine = b.ReadUInt16();
                } catch {
                    throw new QtVSException("Error reading PE header: machine");
                }
                switch (machine) {
                    case IMAGE_FILE_MACHINE_I386:
                        return false;
                    case IMAGE_FILE_MACHINE_IA64:
                    case IMAGE_FILE_MACHINE_AMD64:
                        return true;
                    default:
                        throw new QtVSException("Unknown executable format for " + fileToCheck);
                }
            }
        }

        public bool isWinRT()
        {
            var qmakeQuery = new QMakeQuery(this);
            string qmakeXSpec;
            try {
                qmakeXSpec = qmakeQuery.query("QMAKE_XSPEC");
            }
            catch {
                throw new QtVSException("Error starting qmake process");
            }

            if (qmakeQuery.ErrorValue != 0 && string.IsNullOrEmpty(qmakeXSpec))
                throw new QtVSException("Error: unexpected result of qmake query");

            return qmakeXSpec.StartsWith("winrt");
        }
    }
}
