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
using System.IO;
using System.Text.RegularExpressions;

namespace QtVsTools.Core
{
    enum BuildType
    {
        Unknown,
        Static,
        Shared
    }

    /// <summary>
    /// A very simple reader for the qconfig.pri file.
    /// </summary>
    class QtConfig
    {
        public BuildType BuildType { get; }

        public string LibInfix { get; }

        public bool Is64Bit { get; }

        public string Namespace { get; }

        public uint VersionMajor { get; }
        public uint VersionMinor { get; }
        public uint VersionPatch { get; }

        public QtConfig(string qtdir)
        {
            LibInfix = string.Empty;

            var fi = new FileInfo(qtdir + "\\mkspecs\\qconfig.pri");
            if (!fi.Exists)
                fi = new FileInfo(qtdir + "\\..\\mkspecs\\qconfig.pri");
            if (!fi.Exists)
                return;

            var qConfig = File.ReadAllText(fi.FullName);

            var variableDef = new Regex(@"(\w+)\s*\{|(\})|([\w\.]+)\s*([\+\-]?\=)(.*)\n");
            var lastBlock = string.Empty;
            bool inBlock = false;
            foreach (Match match in variableDef.Matches(qConfig)) {
                var block = match.Groups[1].Value;
                var blockEnd = match.Groups[2].Value;
                var name = match.Groups[3].Value;
                var oper = match.Groups[4].Value;
                var data = match.Groups[5].Value;

                if (!string.IsNullOrEmpty(block)) {
                    inBlock = true;
                    if (block == "else" && !string.IsNullOrEmpty(lastBlock))
                        lastBlock = "!" + lastBlock;
                    else
                        lastBlock = block;
                } else if (!string.IsNullOrEmpty(blockEnd)) {
                    inBlock = false;
                    if (lastBlock.StartsWith("!"))
                        lastBlock = "";
                } else if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(data)
                    && (!inBlock || lastBlock == "!host_build")) {

                    data = data.Replace("\r", "").Trim();
                    if (name == "CONFIG") {
                        var values = data.Split(new char[] { ' ', '\t' },
                            StringSplitOptions.RemoveEmptyEntries);
                        foreach (var value in values) {
                            if (value == "static") {
                                BuildType = BuildType.Static;
                                break;
                            } else if (value == "shared") {
                                BuildType = BuildType.Shared;
                                break;
                            }
                        }
                    } else if (name == "QT_LIBINFIX") {
                        LibInfix = data;
                    } else if (name == "QT_ARCH") {
                        Is64Bit = (data == "x86_64");
                    } else if (name == "QT_NAMESPACE") {
                        Namespace = data;
                    } else if (name == "QT_MAJOR_VERSION") {
                        if (uint.TryParse(data, out uint versionMajor))
                            VersionMajor = versionMajor;
                    } else if (name == "QT_MINOR_VERSION") {
                        if (uint.TryParse(data, out uint versionMinor))
                            VersionMinor = versionMinor;
                    } else if (name == "QT_PATCH_VERSION") {
                        if (uint.TryParse(data, out uint versionPatch))
                            VersionPatch = versionPatch;
                    }
                }
            }
        }
    }
}
