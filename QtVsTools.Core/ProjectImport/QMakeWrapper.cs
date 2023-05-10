/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace QtVsTools.Core
{
    public class QMakeWrapper
    {
        public string QtDir { get; set; }
        public string PkgInstallPath { get; set; }

        public bool IsFlat { get; private set; }
        private bool IsValid { get; set; }

        public string[] SourceFiles { get; private set; }
        public string[] HeaderFiles { get; private set; }
        public string[] ResourceFiles { get; private set; }
        public string[] FormFiles { get; private set; }

        private string LocateHelperExecutable(string exeName)
        {
            if (!string.IsNullOrEmpty(PkgInstallPath) && File.Exists(PkgInstallPath + exeName))
                return PkgInstallPath + exeName;
            return null;
        }

        private string qMakeFileReaderPath;
        private string QMakeFileReaderPath
            => qMakeFileReaderPath ??= LocateHelperExecutable("QMakeFileReader.exe");

        public bool ReadFile(string filePath)
        {
            string output;
            try {
                var exeFilePath = QMakeFileReaderPath;
                if (!File.Exists(exeFilePath))
                    return false;

                using (var process = new Process()) {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = exeFilePath;
                    process.StartInfo.Arguments = ShellQuote(QtDir) + ' ' + ShellQuote(filePath);
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    if (!process.Start())
                        return false;
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }

                StringReader stringReader = null;
                try {
                    stringReader = new StringReader(output);
                    using (var reader = new XmlTextReader(stringReader)) {
                        stringReader = null;
                        reader.ReadToFollowing("content");
                        IsFlat = reader.GetAttribute("flat") == "true";
                        IsValid = reader.GetAttribute("valid") == "true";
                        SourceFiles = ReadFileElements(reader, "SOURCES");
                        HeaderFiles = ReadFileElements(reader, "HEADERS");
                        ResourceFiles = ReadFileElements(reader, "RESOURCES");
                        FormFiles = ReadFileElements(reader, "FORMS");
                    }
                } finally {
                    stringReader?.Dispose();
                }
            } catch {
                return false;
            }
            return true;
        }

        private static string ShellQuote(string filePath)
        {
            if (filePath.Contains(" "))
                return '"' + filePath + '"';
            return filePath;
        }

        private static string[] ReadFileElements(XmlReader reader, string tag)
        {
            var fileNames = new List<string>();
            if (reader.ReadToFollowing(tag)) {
                if (reader.ReadToDescendant("file")) {
                    do {
                        var fname = reader.ReadString();
                        fileNames.Add(fname);
                    } while (reader.ReadToNextSibling("file"));
                }
            }
            return fileNames.ToArray();
        }
    }
}
