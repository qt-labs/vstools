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

using System;
using System.Collections;
using System.IO;

namespace Nokia.QtProjectLib
{
    public class QMakeConf
    {
        protected Hashtable entries = new Hashtable();
        private FileInfo fileInfo = null;

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
            Init(filename);
        }

        protected void Init(string filename)
        {
            fileInfo = new FileInfo(filename);
            if (fileInfo.Exists)
            {
                StreamReader streamReader = new StreamReader(filename);
                string line = streamReader.ReadLine();
                while (line != null)
                {
                    ParseLine(line);
                    line = streamReader.ReadLine();
                }
                streamReader.Close();
            }
        }

        public string Get(string key)
        {
            return (string)entries[key];
        }

        private void ParseLine(string line)
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
                lineValue = ExpandVariables(line.Substring(pos + 1).Trim());

                if (op == "+=")
                {
                    entries[lineKey] += " " + lineValue;
                }
                else if (op == "-=")
                {
                    foreach (string remval in lineValue.Split(new char[] { ' ', '\t' }))
                        RemoveValue(lineKey, remval);
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
                    {
                        QMakeConf includeConf = new QMakeConf(fileInfoToInclude.FullName, InitType.InitQMakeConf);
                        foreach (string key in includeConf.entries.Keys)
                        {
                            if (entries.ContainsKey(key))
                                entries[key] += includeConf.entries[key].ToString();
                            else
                                entries[key] = includeConf.entries[key].ToString();
                        }
                    }
                    Environment.CurrentDirectory = saveCurrentDir;
                }
            }
        }

        private string ExpandVariables(string value)
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

        private void RemoveValue(string key, string valueToRemove)
        {
            int pos;
            if (entries.Contains(key))
            {
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
}
