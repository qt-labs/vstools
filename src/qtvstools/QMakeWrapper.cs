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
        private string qtdir;
        private bool valid;
        private bool flat;
        private string[] sources;
        private string[] headers;
        private string[] resources;
        private string[] forms;

        public QMakeWrapper()
        {
        }

        public void setQtDir(string path)
        {
            qtdir = path;
        }

        public bool readFile(string filePath)
        {
            string output;
            try {
                string exeFilePath = Vsix.Instance.QMakeFileReaderPath;
                if (!System.IO.File.Exists(exeFilePath))
                    return false;

                Process process = new Process();
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = exeFilePath;
                process.StartInfo.Arguments = shellQuote(qtdir) + ' ' + shellQuote(filePath);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                if (!process.Start())
                    return false;
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                XmlReader reader = new XmlTextReader(new System.IO.StringReader(output));
                reader.ReadToFollowing("content");
                valid = stringToBool(reader.GetAttribute("valid"));
                flat = stringToBool(reader.GetAttribute("flat"));
                sources = readFileElements(reader, "SOURCES");
                headers = readFileElements(reader, "HEADERS");
                resources = readFileElements(reader, "RESOURCES");
                forms = readFileElements(reader, "FORMS");
            } catch {
                return false;
            }
            return true;
        }

        public bool isValid()
        {
            return valid;
        }

        public bool isFlat()
        {
            return flat;
        }

        public string[] sourceFiles()
        {
            return sources;
        }

        public string[] headerFiles()
        {
            return headers;
        }

        public string[] resourceFiles()
        {
            return resources;
        }

        public string[] formFiles()
        {
            return forms;
        }

        private static bool stringToBool(string str)
        {
            return str == "true";
        }

        private static string[] readFileElements(XmlReader reader, string tag)
        {
            List<string> fileNames = new List<string>();
            if (reader.ReadToFollowing(tag)) {
                if (reader.ReadToDescendant("file")) {
                    do {
                        string fname = reader.ReadString();
                        fileNames.Add(fname);
                    } while (reader.ReadToNextSibling("file"));
                }
            }
            return fileNames.ToArray();
        }

        private static string shellQuote(string filePath)
        {
            return filePath.Contains(" ")
                ? ('"' + filePath + '"')
                : filePath;
        }
    }
}
