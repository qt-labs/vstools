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
        public Hashtable Entries { get; private set; }
        public string QMakeSpecDirectory { get; private set; }

        public QMakeConf(VersionInformation versionInfo, QMakeQuery qmakeQuery = null)
        {
            Entries = new Hashtable();
            QMakeSpecDirectory = Path.Combine(versionInfo.qtDir, "mkspecs", "default");
            var qmakeConf = Path.Combine(QMakeSpecDirectory, "qmake.conf");

            // Starting from Qt5 beta2 there is no more "\\mkspecs\\default" folder available
            // To find location of "qmake.conf" there is a need to run "qmake -query" command
            // This is what happens below.
            if (!File.Exists(qmakeConf)) {
                if (qmakeQuery == null)
                    qmakeQuery = new QMakeQuery(versionInfo);

                string qtPrefix = qmakeQuery["QT_INSTALL_PREFIX"];
                if (string.IsNullOrEmpty(qtPrefix))
                    throw new QtVSException("qmake error: no value for QT_INSTALL_PREFIX");

                string qtArchData = qmakeQuery["QT_INSTALL_ARCHDATA"];
                if (string.IsNullOrEmpty(qtArchData))
                    throw new QtVSException("qmake error: no value for QT_INSTALL_ARCHDATA");

                string qmakeXSpec = qmakeQuery["QMAKE_XSPEC"];
                if (string.IsNullOrEmpty(qtArchData))
                    throw new QtVSException("qmake error: no value for QMAKE_XSPEC");

                qmakeConf = Path.Combine(qtPrefix, qtArchData, "mkspecs", qmakeXSpec, "qmake.conf");

                if (!File.Exists(qmakeConf))
                    throw new QtVSException("qmake.conf expected at " + qmakeConf + " not found");
            }

            ParseFile(qmakeConf);
        }

        private void ParseFile(string filename)
        {
            var fileInfo = new FileInfo(filename);
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
                        var op = "=";
                        if (line[pos - 1] == '+' || line[pos - 1] == '-')
                            op = line[pos - 1] + "=";

                        var lineKey = line.Substring(0, pos - op.Length + 1).Trim();
                        var lineValue = ExpandVariables(line.Substring(pos + 1).Trim());

                        if (op == "+=") {
                            Entries[lineKey] += " " + lineValue;
                        } else if (op == "-=") {
                            foreach (var remval in lineValue.Split(' ', '\t'))
                                RemoveValue(lineKey, remval);
                        } else {
                            Entries[lineKey] = lineValue;
                        }
                    } else if (line.StartsWith("include", StringComparison.Ordinal)) {
                        pos = line.IndexOf('(');
                        var posEnd = line.LastIndexOf(')');
                        if (pos > 0 && pos < posEnd) {
                            var filenameToInclude = line.Substring(pos + 1, posEnd - pos - 1);
                            var saveCurrentDir = Environment.CurrentDirectory;
                            Environment.CurrentDirectory = fileInfo.Directory.FullName;
                            var fileInfoToInclude = new FileInfo(filenameToInclude);
                            if (fileInfoToInclude.Exists)
                                ParseFile(fileInfoToInclude.FullName);
                            Environment.CurrentDirectory = saveCurrentDir;
                        }
                    }
                    line = streamReader.ReadLine();
                }
                streamReader.Close();

                RemoveValue("QMAKE_LFLAGS_CONSOLE", "@QMAKE_SUBSYSTEM_SUFFIX@");
                RemoveValue("QMAKE_LFLAGS_WINDOWS", "@QMAKE_SUBSYSTEM_SUFFIX@");
            }
        }

        private string ExpandVariables(string value)
        {
            var pos = value.IndexOf("$$", StringComparison.Ordinal);
            while (pos != -1) {
                var startPos = pos + 2;
                var endPos = startPos;

                if (value[startPos] != '[') { // at the moment no handling of qmake internal variables
                    for (; endPos < value.Length; ++endPos) {
                        if ((Char.IsPunctuation(value[endPos]) && value[endPos] != '_')
                            || Char.IsWhiteSpace(value[endPos])) {
                            break;
                        }
                    }
                    if (endPos > startPos) {
                        var varName = value.Substring(startPos, endPos - startPos);
                        var varValue = (Entries[varName] ?? string.Empty).ToString();
                        value = value.Substring(0, pos) + varValue + value.Substring(endPos);
                        endPos = pos + varValue.Length;
                    }
                }

                pos = value.IndexOf("$$", endPos, StringComparison.Ordinal);
            }
            return value;
        }

        private void RemoveValue(string key, string valueToRemove)
        {
            var pos = -1;
            if (!Entries.Contains(key))
                return;

            var value = Entries[key].ToString();
            do {
                pos = value.IndexOf(valueToRemove, StringComparison.Ordinal);
                if (pos >= 0)
                    value = value.Remove(pos, valueToRemove.Length);
            } while (pos >= 0);
            Entries[key] = value;
        }
    }
}
