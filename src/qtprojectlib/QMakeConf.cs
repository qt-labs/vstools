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
using System.Collections;
using System.IO;

namespace QtProjectLib
{
    public class QMakeConf
    {
        protected Hashtable mEntries = new Hashtable();
        private FileInfo fileInfo;

        public QMakeConf(VersionInformation versionInfo)
        {
            Init(versionInfo);
        }

        public enum InitType { InitQtInstallPath, InitQMakeConf }

        /// <param name="str">string for initialization</param>
        /// <param name="itype">determines the use of str</param>
        public QMakeConf(string str, InitType itype)
        {
            switch (itype) {
            case InitType.InitQtInstallPath:
                Init(new VersionInformation(str));
                break;
            case InitType.InitQMakeConf:
                Init(str);
                break;
            }
        }

        public string QMakeSpecDirectory { get; private set; }

        protected void Init(VersionInformation versionInfo)
        {
            QMakeSpecDirectory = Path.Combine(versionInfo.qtDir, "mkspecs", "default");
            var qmakeConf = Path.Combine(QMakeSpecDirectory, "qmake.conf");

            // Starting from Qt5 beta2 there is no more "\\mkspecs\\default" folder available
            // To find location of "qmake.conf" there is a need to run "qmake -query" command
            // This is what happens below.
            if (!File.Exists(qmakeConf)) {
                var qmakeQuery = new QMakeQuery(versionInfo);
                var qmakespecDir = qmakeQuery.query("QMAKE_XSPEC");

                if (qmakeQuery.ErrorValue == 0 && !string.IsNullOrEmpty(qmakespecDir)) {
                    QMakeSpecDirectory = Path.Combine(versionInfo.qtDir, "mkspecs", qmakespecDir);
                    qmakeConf = Path.Combine(QMakeSpecDirectory, "qmake.conf");
                }

                if (qmakeQuery.ErrorValue != 0 || !File.Exists(qmakeConf))
                    throw new QtVSException("qmake.conf expected at " + qmakeConf + " not found");
            }

            Init(qmakeConf);
        }

        protected void Init(string filename)
        {
            ParseFile(filename, ref mEntries);
        }

        public string Get(string key)
        {
            return (string) mEntries[key];
        }

        private void ParseFile(string filename, ref Hashtable entries)
        {
            fileInfo = new FileInfo(filename);
            if (fileInfo.Exists) {
                var streamReader = new StreamReader(filename);
                var line = streamReader.ReadLine();
                while (line != null) {
                    line = line.Trim();
                    var commentStartIndex = line.IndexOf('#');
                    if (commentStartIndex >= 0)
                        line = line.Remove(commentStartIndex);
                    var pos = line.IndexOf('=');
                    if (pos > 0) {
                        string op = "=";
                        if (line[pos - 1] == '+' || line[pos - 1] == '-')
                            op = line[pos - 1] + "=";

                        string lineKey;
                        string lineValue;
                        lineKey = line.Substring(0, pos - op.Length + 1).Trim();
                        lineValue = ExpandVariables(line.Substring(pos + 1).Trim(), entries);

                        if (op == "+=") {
                            entries[lineKey] += " " + lineValue;
                        } else if (op == "-=") {
                            foreach (string remval in lineValue.Split(new char[] { ' ', '\t' }))
                                RemoveValue(lineKey, remval, entries);
                        } else
                            entries[lineKey] = lineValue;
                    } else if (line.StartsWith("include", StringComparison.Ordinal)) {
                        pos = line.IndexOf('(');
                        var posEnd = line.LastIndexOf(')');
                        if (pos > 0 && pos < posEnd) {
                            var filenameToInclude = line.Substring(pos + 1, posEnd - pos - 1);
                            string saveCurrentDir = Environment.CurrentDirectory;
                            Environment.CurrentDirectory = fileInfo.Directory.FullName;
                            var fileInfoToInclude = new FileInfo(filenameToInclude);
                            if (fileInfoToInclude.Exists)
                                ParseFile(fileInfoToInclude.FullName, ref entries);
                            Environment.CurrentDirectory = saveCurrentDir;
                        }
                    }
                    line = streamReader.ReadLine();
                }
                streamReader.Close();

                RemoveQmakeSubsystemSuffix("QMAKE_LFLAGS_CONSOLE", ref entries);
                RemoveQmakeSubsystemSuffix("QMAKE_LFLAGS_WINDOWS", ref entries);
            }
        }

        static void RemoveQmakeSubsystemSuffix(string key, ref Hashtable hash)
        {
            if (hash.Contains(key))
                hash[key] = hash[key].ToString().Replace("@QMAKE_SUBSYSTEM_SUFFIX@", string.Empty);
        }

        private string ExpandVariables(string value, Hashtable entries)
        {
            var pos = value.IndexOf("$$", StringComparison.Ordinal);
            while (pos != -1) {
                int startPos = pos + 2;
                int endPos = startPos;

                if (value[startPos] != '[')  // at the moment no handling of qmake internal variables
                {
                    for (; endPos < value.Length; ++endPos) {
                        if ((Char.IsPunctuation(value[endPos]) && value[endPos] != '_')
                            || Char.IsWhiteSpace(value[endPos])) {
                            break;
                        }
                    }
                    if (endPos > startPos) {
                        var varName = value.Substring(startPos, endPos - startPos);
                        object varValueObj = entries[varName];
                        string varValue = "";
                        if (varValueObj != null) varValue = varValueObj.ToString();
                        value = value.Substring(0, pos) + varValue + value.Substring(endPos);
                        endPos = pos + varValue.Length;
                    }
                }

                pos = value.IndexOf("$$", endPos, StringComparison.Ordinal);
            }
            return value;
        }

        private void RemoveValue(string key, string valueToRemove, Hashtable entries)
        {
            int pos;
            if (!entries.Contains(key))
                return;

            var value = entries[key].ToString();
            do {
                pos = value.IndexOf(valueToRemove, StringComparison.Ordinal);
                if (pos >= 0)
                    value = value.Remove(pos, valueToRemove.Length);
            } while (pos >= 0);
            entries[key] = value;
        }
    }
}
