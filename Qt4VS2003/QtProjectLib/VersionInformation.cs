/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2011 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

namespace Nokia.QtProjectLib
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    public class VersionInformation
    {
        //fields
        public string qtDir = null;
        public uint   qtMajor = 0; // X in version x.y.z
        public uint   qtMinor = 0; // Y in version x.y.z
        public uint   qtPatch = 0; // Z in version x.y.z
        public bool   qt4Version = true;
        private QtConfig qtConfig = null;
        private QMakeConf qmakeConf = null;
        private string vsPlatformName = null;
        private bool qtWinCEVersion = false;
    
        public VersionInformation(string qtDirIn)
        {
            if (qtDirIn == null)
                qtDir = Environment.GetEnvironmentVariable("QTDIR");
            else
                qtDir = qtDirIn;

            if (qtDir == null)
                return;

            // make QtDir more consistent
            qtDir = new FileInfo(qtDir).FullName.ToLower();     // ### do we really need to convert qtDir to lower case?

            SetupPlatformSpecificData();

            // Find version number
            try 
            {
                StreamReader inF = new StreamReader(Locate_qglobal_h());
                Regex rgxpVersion = new Regex( "#define\\s*QT_VERSION\\s*0x(?<number>\\d+)", RegexOptions.Multiline );
                string contents = inF.ReadToEnd();
                inF.Close();
                Match matchObj = rgxpVersion.Match( contents );
                if (!matchObj.Success) 
                {
                    qtDir = null;
                    return;
                }

                string strVersion = matchObj.Groups[1].ToString();
                uint version = Convert.ToUInt32(strVersion, 16);
                qtMajor = version >> 16;
                qtMinor = (version >> 8) & 0xFF;
                qtPatch = version & 0xFF;

                if (qtMajor == 4)
                {
                    qt4Version = true;
                }
                else
                {
                    qt4Version = false;
                }
            } 
            catch(Exception /*e*/)
            {
                qtDir = null;
            }
        }

        public bool IsStaticBuild()
        {
            if (qtConfig == null)
                qtConfig = new QtConfig(qtDir);
            return qtConfig.IsStaticBuild;
        }

        public string GetSignatureFile()
        {
            if (qtConfig == null)
                qtConfig = new QtConfig(qtDir);
            return qtConfig.SignatureFile;
        }

        public string GetQMakeConfEntry(string entryName)
        {
            if (qmakeConf == null)
                qmakeConf = new QMakeConf(this);
            return qmakeConf.Get(entryName);
        }

        /// <summary>
        /// Returns the platform name in a way Visual Studio understands.
        /// </summary>
        public string GetVSPlatformName()
        {
            return vsPlatformName;
        }

        public bool IsWinCEVersion()
        {
            return qtWinCEVersion;
        }

        /// <summary>
        /// Read platform name and whether this is a WinCE version from qmake.conf.
        /// </summary>
        private void SetupPlatformSpecificData()
        {
            if (qmakeConf == null)
                qmakeConf = new QMakeConf(this);

            qtWinCEVersion = false;
            string ceSDK = qmakeConf.Get("CE_SDK");
            string ceArch = qmakeConf.Get("CE_ARCH");
            if (ceSDK != null && ceArch != null)
            {
                vsPlatformName = ceSDK + " (" + ceArch + ")";
                qtWinCEVersion = true;
            }
            else if (is64Bit())
                vsPlatformName = "x64";
            else
                vsPlatformName = "Win32";
        }

        private String Locate_qglobal_h()
        {
            string[] candidates = {qtDir + "\\include\\qglobal.h",
                                   qtDir + "\\src\\corelib\\global\\qglobal.h",
                                   qtDir + "\\include\\QtCore\\qglobal.h"};

            foreach (string filename in candidates)
            {
                if (File.Exists(filename))
                {
                    // check whether we look at the real qglobal.h or just a "pointer"
                    StreamReader inF = new StreamReader(filename);
                    Regex rgxpVersion = new Regex("#include\\s+\"(.+global.h)\"", RegexOptions.Multiline);
                    Match matchObj = rgxpVersion.Match(inF.ReadToEnd());
                    inF.Close();
                    if (!matchObj.Success)
                        return filename;

                    if (matchObj.Groups.Count >= 2)
                    {
                        string origCurrentDirectory = Directory.GetCurrentDirectory();
                        Directory.SetCurrentDirectory(filename.Substring(0, filename.Length - 10));   // remove "\\qglobal.h"
                        string absIncludeFile = Path.GetFullPath(matchObj.Groups[1].ToString());
                        Directory.SetCurrentDirectory(origCurrentDirectory);
                        if (File.Exists(absIncludeFile))
                            return absIncludeFile;
                    }
                }
            }

            throw new Qt4VS2003Exception("qglobal.h not found");
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetBinaryTypeA(string lpApplicationName, ref int lpBinaryType);

        public bool is64Bit()
        {
            // ### This does not work for x64 cross builds of Qt.
            // ### In that case qmake.exe is 32 bit but the DLLs are 64 bit.
            // ### So actually we should check QtCore4.dll / QtCored4.dll instead.
            // ### Unfortunately there's no Win API for checking the architecture of DLLs.
            // ### We must read the PE header instead.
            string fileToCheck = qtDir + "\\bin\\qmake.exe";
            if (!File.Exists(fileToCheck))
                throw new Qt4VS2003Exception("Can't find " + fileToCheck);

            const int SCS_32BIT_BINARY = 0;
            const int SCS_64BIT_BINARY = 6;
            int binaryType = 0;
            bool success = GetBinaryTypeA(fileToCheck, ref binaryType) != 0;
            if (!success)
                throw new Qt4VS2003Exception("GetBinaryTypeA failed");

            if (binaryType == SCS_32BIT_BINARY)
                return false;
            else if (binaryType == SCS_64BIT_BINARY)
                return true;

            throw new Qt4VS2003Exception("GetBinaryTypeA return unknown executable format for " + fileToCheck);
        }
    }
}
