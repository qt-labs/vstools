/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Add-in.
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
using System.Threading;

namespace Digia.Qt5ProjectLib
{
    public class QMakeConf
    {
        protected Hashtable mEntries = new Hashtable();
        private FileInfo fileInfo = null;
        private string qmakespecFolder = "";

        public QMakeConf(VersionInformation versionInfo)
        {
            Init(versionInfo);
        }

        public enum InitType { InitQtInstallPath, InitQMakeConf }

        /// <param name="str">string for initialization</param>
        /// <param name="itype">determines the use of str</param>
        public QMakeConf(string str, InitType itype)
        {
            switch (itype)
            {
                case InitType.InitQtInstallPath:
                    Init(new VersionInformation(str));
                    break;
                case InitType.InitQMakeConf:
                    Init(str);
                    break;
            }
        }

        protected void Init(VersionInformation versionInfo)
        {
            string filename = versionInfo.qtDir + "\\mkspecs\\default\\qmake.conf";
            fileInfo = new FileInfo(filename);

            // Starting from Qt5 beta2 there is no more "\\mkspecs\\default" folder available
            // To find location of "qmake.conf" there is a need to run "qmake -query" command
            // This is what happens below.
            if (!fileInfo.Exists)
            {
                QMakeQuery qmakeQuery = new QMakeQuery(versionInfo);
                qmakespecFolder = qmakeQuery.query("QMAKE_XSPEC");

                if (qmakeQuery.ErrorValue == 0 && qmakespecFolder.Length > 0)
                {
                    filename = versionInfo.qtDir + "\\mkspecs\\" + qmakespecFolder + "\\qmake.conf";
                    fileInfo = new FileInfo(filename);
                }

                if (qmakeQuery.ErrorValue != 0 || !fileInfo.Exists)
                    throw new QtVSException("qmake.conf expected at " +  filename + " not found");
            }

            Init(filename);
        }

        protected void Init(string filename)
        {
            ParseFile(filename, ref mEntries);
        }

        public string Get(string key)
        {
            return (string)mEntries[key];
        }

        private void ParseFile(string filename, ref Hashtable entries)
        {
            fileInfo = new FileInfo(filename);
            if (fileInfo.Exists)
            {
                StreamReader streamReader = new StreamReader(filename);
                string line = streamReader.ReadLine();
                while (line != null)
                {
                    line = line.Trim();
                    int commentStartIndex = line.IndexOf('#');
                    if (commentStartIndex >= 0)
                        line = line.Remove(commentStartIndex);
                    int pos = line.IndexOf('=');
                    if (pos > 0)
                    {
                        string op = "=";
                        if (line[pos - 1] == '+' || line[pos - 1] == '-')
                            op = line[pos - 1] + "=";

                        string lineKey;
                        string lineValue;
                        lineKey = line.Substring(0, pos - op.Length + 1).Trim();
                        lineValue = ExpandVariables(line.Substring(pos + 1).Trim(), entries);

                        if (op == "+=")
                        {
                            entries[lineKey] += " " + lineValue;
                        }
                        else if (op == "-=")
                        {
                            foreach (string remval in lineValue.Split(new char[] { ' ', '\t' }))
                                RemoveValue(lineKey, remval, entries);
                        }
                        else
                            entries[lineKey] = lineValue;
                    }
                    else if (line.StartsWith("include"))
                    {
                        pos = line.IndexOf('(');
                        int posEnd = line.LastIndexOf(')');
                        if (pos > 0 && pos < posEnd)
                        {
                            string filenameToInclude = line.Substring(pos + 1, posEnd - pos - 1);
                            string saveCurrentDir = Environment.CurrentDirectory;
                            Environment.CurrentDirectory = fileInfo.Directory.FullName;
                            FileInfo fileInfoToInclude = new FileInfo(filenameToInclude);
                            if (fileInfoToInclude.Exists)
                                ParseFile(fileInfoToInclude.FullName, ref entries);
                            Environment.CurrentDirectory = saveCurrentDir;
                        }
                    }
                    line = streamReader.ReadLine();
                }
                streamReader.Close();
            }
        }

        private string ExpandVariables(string value, Hashtable entries)
        {
            int pos = value.IndexOf("$$");
            while (pos != -1)
            {
                int startPos = pos + 2;
                int endPos = startPos;

                if (value[startPos] != '[')  // at the moment no handling of qmake internal variables
                {
                    for (; endPos < value.Length; ++endPos)
                    {
                        if ((Char.IsPunctuation(value[endPos]) && value[endPos] != '_')
                            || Char.IsWhiteSpace(value[endPos]))
                        {
                            break;
                        }
                    }
                    if (endPos > startPos)
                    {
                        string varName = value.Substring(startPos, endPos - startPos);
                        object varValueObj = entries[varName];
                        string varValue = "";
                        if (varValueObj != null) varValue = varValueObj.ToString();
                        value = value.Substring(0, pos) + varValue + value.Substring(endPos);
                        endPos = pos + varValue.Length;
                    }
                }

                pos = value.IndexOf("$$", endPos);
            }
            return value;
        }

        private void RemoveValue(string key, string valueToRemove, Hashtable entries)
        {
            int pos;
            if (!entries.Contains(key))
                return;

            string value = entries[key].ToString();
            do
            {
                pos = value.IndexOf(valueToRemove);
                if (pos >= 0)
                    value = value.Remove(pos, valueToRemove.Length);
            } while (pos >= 0);
            entries[key] = value;
        }
    }
}
