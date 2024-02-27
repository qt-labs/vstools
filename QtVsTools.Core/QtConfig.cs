/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.IO;
using System.Text.RegularExpressions;

namespace QtVsTools.Core
{
    public enum Platform
    {
        x86,
        x64,
        arm64
    }

    /// <summary>
    /// A very simple reader for the qconfig.pri file.
    /// </summary>
    internal class QtConfig
    {
        public Platform Platform { get; }
        public string Namespace { get; }
        public string VersionString { get; }

        public QtConfig(string qtDir)
        {
            var fi = new FileInfo(qtDir + "\\mkspecs\\qconfig.pri");
            if (!fi.Exists)
                fi = new FileInfo(qtDir + "\\..\\mkspecs\\qconfig.pri");
            if (!fi.Exists)
                return;

            var qConfig = File.ReadAllText(fi.FullName);

            var variableDef = new Regex(@"(\w+)\s*\{|(\})|([\w\.]+)\s*[\+\-]?\=(.*)\n");
            var lastBlock = "";
            var inBlock = false;
            foreach (Match match in variableDef.Matches(qConfig)) {
                var block = match.Groups[1].Value;
                var blockEnd = match.Groups[2].Value;
                var name = match.Groups[3].Value;
                var data = match.Groups[4].Value;

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
                    switch (name) {
                    case "QT_ARCH":
                        Platform = data switch {
                            "x86_64" => Platform.x64,
                            "arm64" => Platform.arm64,
                            _ => Platform.x86
                        };
                        break;
                    case "QT_NAMESPACE":
                        Namespace = data;
                        break;
                    case "QT_VERSION":
                        VersionString = data;
                        break;
                    }
                }
            }
        }
    }
}
