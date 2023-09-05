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

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace QtVsTools
{
    public class QMakeWrapper
    {
        public string QtDir { get; set; }

        public bool IsFlat { get; private set; }
        private bool IsValid { get; set; }

        public string[] SourceFiles { get; private set; }
        public string[] HeaderFiles { get; private set; }
        public string[] ResourceFiles { get; private set; }
        public string[] FormFiles { get; private set; }

        public bool ReadFile(string filePath)
        {
            string output;
            try {
                var exeFilePath = QtVsToolsLegacyPackage.Instance.QMakeFileReaderPath;
                if (!System.IO.File.Exists(exeFilePath))
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

                System.IO.StringReader stringReader = null;
                try {
                    stringReader = new System.IO.StringReader(output);
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
                    if (stringReader != null)
                        stringReader.Dispose();
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
