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

using Microsoft.VisualStudio.Shell;
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
        private string vsPlatformName;
        private static readonly Hashtable _cache = new Hashtable();

        public static VersionInformation Get(string qtDir)
        {
            qtDir = qtDir ?? Environment.GetEnvironmentVariable("QTDIR");
            if (qtDir == null)
                return null;

            try {
                qtDir = new FileInfo(qtDir).FullName.ToUpperInvariant();
            } catch {
                return null;
            }
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

        readonly Dictionary<string, bool> _IsModuleAvailable;
        public bool IsModuleAvailable(string module)
        {
            return _IsModuleAvailable?[module] ?? false;
        }

        public string VC_MinimumVisualStudioVersion { get; }
        public string VC_ApplicationTypeRevision { get; }
        public string VC_WindowsTargetPlatformMinVersion { get; }
        public string VC_WindowsTargetPlatformVersion { get; }
        public string VC_Link_TargetMachine { get; }
        public string VC_PlatformToolset { get; }

        private VersionInformation(string qtDirIn)
        {
            qtDir = qtDirIn;

            ThreadHelper.ThrowIfNotOnUIThread();
            try {
                var qmakeQuery = new QMakeQuery(this);
                SetupPlatformSpecificData(qmakeQuery);

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

                var modules = QtModules.Instance.GetAvailableModules()
                    .Where((QtModule mi) => mi.Selectable);

                foreach (QtModule mi in modules) {
                    tempProData.AppendLine(string.Format(
                        "qtHaveModule({0}): HEADERS += {0}.h", mi.proVarQT));
                }

                var randomName = Path.GetRandomFileName();
                var tempDir = Path.Combine(Path.GetTempPath(), randomName);
                Directory.CreateDirectory(tempDir);

                var tempPro = Path.Combine(tempDir, string.Format("{0}.pro", randomName));
                File.WriteAllText(tempPro, tempProData.ToString());

                var qmake = new QMakeImport(this, tempPro);
                qmake.DisableWarnings = true;
                qmake.Run(setVCVars: true);

                var tempVcxproj = Path.Combine(tempDir, string.Format("{0}.vcxproj", randomName));
                var msbuildProj = MsBuildProject.Load(tempVcxproj);

                Directory.Delete(tempDir, recursive: true);

                var availableModules = msbuildProj.GetItems("ClInclude")
                    .Select((string s) => Path.GetFileNameWithoutExtension(s));

                _IsModuleAvailable = modules.ToDictionary(
                    (QtModule mi) => mi.proVarQT,
                    (QtModule mi) => availableModules.Contains(mi.proVarQT));

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

        public string LibInfix()
        {
            if (qtConfig == null)
                qtConfig = new QtConfig(qtDir);
            return qtConfig.LibInfix;
        }

        public string Namespace()
        {
            if (qtConfig == null)
                qtConfig = new QtConfig(qtDir);
            return qtConfig.Namespace;
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
        private void SetupPlatformSpecificData(QMakeQuery qmakeQuery)
        {
            if (qmakeConf == null)
                qmakeConf = new QMakeConf(this, qmakeQuery);
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

        public bool is64Bit()
        {
            if (qtConfig == null)
                qtConfig = new QtConfig(qtDir);
            return qtConfig.Is64Bit;
        }

        public bool isWinRT()
        {
            var qmakeQuery = new QMakeQuery(this);
            string qmakeXSpec;

            ThreadHelper.ThrowIfNotOnUIThread();
            try {
                qmakeXSpec = qmakeQuery["QMAKE_XSPEC"];
            } catch {
                throw new QtVSException("Error starting qmake process");
            }

            if (string.IsNullOrEmpty(qmakeXSpec))
                throw new QtVSException("Error: unexpected result of qmake query");

            return qmakeXSpec.StartsWith("winrt");
        }
    }
}
