/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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

namespace QtVsTools.Core
{
    public class QtModule
    {
        public string Name;
        public bool Selectable;
        public List<string> Defines = new List<string>();
        public string LibraryPrefix = string.Empty;
        public bool HasDLL = true;
        public List<string> AdditionalLibraries = new List<string>();
        public List<string> AdditionalLibrariesDebug = new List<string>();
        public List<string> IncludePath = new List<string>();
        public string proVarQT;
        public string proVarCONFIG;

        public string LibRelease
        {
            get
            {
                return
                    LibraryPrefix.StartsWith("Qt", StringComparison.Ordinal)
                        ? "Qt5" + LibraryPrefix.Substring(2) + ".lib"
                        : LibraryPrefix + ".lib";
            }
        }

        public string LibDebug
        {
            get
            {
                return
                    LibraryPrefix.StartsWith("Qt", StringComparison.Ordinal)
                        ? "Qt5" + LibraryPrefix.Substring(2) + "d.lib"
                        : LibraryPrefix + "d.lib";
            }
        }

        public QtModule(int id)
        {
            Id = id;
        }

        public int Id { get; } = -1;

        public List<string> GetIncludePath()
        {
            return IncludePath;
        }

        public List<string> GetLibs(bool isDebugCfg, VersionInformation vi)
        {
            return GetLibs(isDebugCfg, vi.IsStaticBuild(), vi.LibInfix());
        }

        public List<string> GetLibs(bool isDebugCfg, bool isStaticBuild, string libInfix)
        {
            // TODO: isStaticBuild is never used.
            var libs = new List<string>();
            var libName = LibraryPrefix;
            if (libName.StartsWith("Qt", StringComparison.Ordinal))
                libName = "Qt5" + libName.Substring(2);
            libName += libInfix;
            if (isDebugCfg)
                libName += "d";
            libName += ".lib";
            libs.Add(libName);
            libs.AddRange(GetAdditionalLibs(isDebugCfg));
            return libs;
        }

        private List<string> GetAdditionalLibs(bool isDebugCfg)
        {
            if (isDebugCfg && AdditionalLibrariesDebug.Count > 0)
                return AdditionalLibrariesDebug;
            return AdditionalLibraries;
        }

        public static bool IsInstalled(string moduleName)
        {
            var qtVersion = QtVersionManager.The().GetDefaultVersion();
            if (qtVersion == null) {
                throw new QtVSException("Unable to find a Qt build!" + Environment.NewLine
                                        + "To solve this problem specify a Qt build.");
            }

            var installPath = QtVersionManager.The().GetInstallPath(qtVersion);
            if (moduleName.StartsWith("Qt", StringComparison.Ordinal))
                moduleName = "Qt5" + moduleName.Substring(2);

            var qtVersionInfo = QtVersionManager.The().GetVersionInfo(qtVersion);
            var libPath = Path.Combine(installPath, "lib",
                string.Format("{0}{1}.lib", moduleName, qtVersionInfo.LibInfix()));

            return File.Exists(libPath);
        }
    }
}
