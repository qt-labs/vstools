/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;

namespace QtVsTools.Core
{
    public class QMakeConf
    {
        private Dictionary<string, string> Properties { get; } = new();
        public string this[string name] => Properties.TryGetValue(name, out var value) ? value : null;

        public string QMakeSpecDirectory { get; }

        public QMakeConf(QtBuildToolQuery query)
        {
            var qtPrefix = query["QT_INSTALL_PREFIX"];
            if (string.IsNullOrEmpty(qtPrefix))
                throw new KeyNotFoundException("qmake error: no value for QT_INSTALL_PREFIX");
            var qtArchData = query["QT_INSTALL_ARCHDATA"];
            if (string.IsNullOrEmpty(qtArchData))
                throw new KeyNotFoundException("qmake error: no value for QT_INSTALL_ARCHDATA");
            var qmakeXSpec = query["QMAKE_XSPEC"];
            if (string.IsNullOrEmpty(qtArchData))
                throw new KeyNotFoundException("qmake error: no value for QMAKE_XSPEC");
            QMakeSpecDirectory = Path.Combine(qtPrefix, qtArchData, "mkspecs", qmakeXSpec);
            var qmakeConf = Path.Combine(QMakeSpecDirectory, "qmake.conf");
            if (!File.Exists(qmakeConf)) {
                // Check if this is a shadow build of Qt.
                qtPrefix = query["QT_INSTALL_PREFIX/src"];
                if (string.IsNullOrEmpty(qtPrefix))
                    throw new KeyNotFoundException("qmake error: no value for QT_INSTALL_PREFIX/src");
                qtArchData = query["QT_INSTALL_ARCHDATA/src"];
                if (string.IsNullOrEmpty(qtArchData))
                    throw new KeyNotFoundException("qmake error: no value for QT_INSTALL_ARCHDATA/src");
                QMakeSpecDirectory = Path.Combine(qtPrefix, qtArchData, "mkspecs", qmakeXSpec);
                qmakeConf = Path.Combine(QMakeSpecDirectory, "qmake.conf");
            }
            if (!File.Exists(qmakeConf))
                throw new FileNotFoundException("qmake.conf expected at " + qmakeConf + " not found");
            ParseFile(qmakeConf);
        }

        private void ParseFile(string fileName)
        {
            if (!File.Exists(fileName))
                return;
            using var streamReader = new StreamReader(fileName);
            while (streamReader.ReadLine() is {} line) {
                line = line.Trim();
                var commentStartIndex = line.IndexOf('#');
                if (commentStartIndex >= 0)
                    line = line.Remove(commentStartIndex);
                var pos = line.IndexOf('=');
                if (pos > 0)
                    ProcessKeyValue(line, pos);
                else if (line.StartsWith("include", StringComparison.Ordinal))
                    ProcessInclude(fileName, line);
            }
            streamReader.Close();
            RemoveValue("QMAKE_LFLAGS_CONSOLE", "@QMAKE_SUBSYSTEM_SUFFIX@");
            RemoveValue("QMAKE_LFLAGS_WINDOWS", "@QMAKE_SUBSYSTEM_SUFFIX@");
        }

        private void ProcessKeyValue(string line, int pos)
        {
            var op = "=";
            if (line[pos - 1] == '+' || line[pos - 1] == '-')
                op = line[pos - 1] + "=";

            var lineKey = line.Substring(0, pos - op.Length + 1).Trim();
            var lineValue = ExpandVariables(line.Substring(pos + 1).Trim());

            switch (op) {
            case "+=" when Properties.ContainsKey(lineKey):
                Properties[lineKey] += lineValue;
                break;
            case "-=":
                foreach (var value in lineValue.Split(' ', '\t'))
                    RemoveValue(lineKey, value);
                break;
            default:
                Properties[lineKey] = lineValue;
                break;
            }
        }

        private void ProcessInclude(string fileName, string line)
        {
            var pos = line.IndexOf('(');
            var posEnd = line.LastIndexOf(')');
            if (pos <= 0 || pos >= posEnd)
                return;
            var filenameToInclude = line.Substring(pos + 1, posEnd - pos - 1);
            var saveCurrentDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = new FileInfo(fileName).Directory?.FullName ?? "";
            var fileInfoToInclude = new FileInfo(filenameToInclude);
            if (fileInfoToInclude.Exists)
                ParseFile(fileInfoToInclude.FullName);
            Environment.CurrentDirectory = saveCurrentDir;
        }

        private string ExpandVariables(string value)
        {
            var pos = value.IndexOf("$$", StringComparison.Ordinal);
            while (pos != -1) {
                var startPos = pos + 2;
                var endPos = startPos;

                // at the moment no handling of qmake internal variables
                if (value[startPos] != '[') {
                    for (; endPos < value.Length; ++endPos) {
                        if ((char.IsPunctuation(value[endPos]) && value[endPos] != '_')
                            || char.IsWhiteSpace(value[endPos])) {
                            break;
                        }
                    }
                    if (endPos > startPos) {
                        var varName = value.Substring(startPos, endPos - startPos);
                        if (!Properties.TryGetValue(varName, out var varValue))
                            varValue = "";
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
            if (!Properties.TryGetValue(key, out var value))
                return;

            int pos;
            do {
                pos = value.IndexOf(valueToRemove, StringComparison.Ordinal);
                if (pos >= 0)
                    value = value.Remove(pos, valueToRemove.Length);
            } while (pos >= 0);
            Properties[key] = value;
        }
    }
}
