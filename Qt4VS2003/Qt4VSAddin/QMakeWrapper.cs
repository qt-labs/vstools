/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Qt5VSAddin
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
            try
            {
                string exeFilePath = Connect.Instance().QMakeFileReaderPath;
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
            }
            catch
            {
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

        private bool stringToBool(string str)
        {
            return str == "true";
        }

        private string[] readFileElements(XmlReader reader, string tag)
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

        private string shellQuote(string filePath)
        {
            return filePath.Contains(" ")
                ? ('"' + filePath + '"')
                : filePath;
        }
    }
}
