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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QtProjectLib
{
    class MocCmdChecker
    {
        private Regex backslashRegEx = new Regex(@"\\+\.?\\+");
        private Regex endRegEx = new Regex(@"\\\.?$");

        private string NormalizePath(string path)
        {
            var s = path.ToLower().Trim();
            s = backslashRegEx.Replace(s, "\\");
            s = endRegEx.Replace(s, "");
            return s;
        }

        public string NewCmdLine(string cmdLine, string includes, string defines,
            string mocOptions, string mocFile, string newPchParameters,
            string outputFile)
        {
            string inputMocFile = ProjectMacros.Path;
            if (outputFile.ToLower().EndsWith(".moc"))
                inputMocFile = mocFile;
            var cmds = SplitIntoCommands(cmdLine);
            var mocPos = MocCommandPosition(cmds);
            if (mocPos < 0)
                return null;

            string mocCmd = cmds[mocPos];
            var defs = ExtractDefines(mocCmd);
            var incs = ExtractIncludes(mocCmd);
            var pchParameters = ExtractPCHOptions(mocCmd);
            var newIncludes = ExtractIncludes(includes);
            var newDefines = ExtractDefines(defines);

            bool equal = true;

            if (newDefines.Count == defs.Count) {
                foreach (string s in newDefines) {
                    if (defs.Contains(s)) {
                        defs.Remove(s);
                    } else {
                        equal = false;
                        break;
                    }
                }
            } else {
                equal = false;
            }

            equal = equal && newIncludes.Count == incs.Count;
            if (equal) {
                foreach (string s in newIncludes) {
                    if (incs.Contains(s)) {
                        incs.Remove(s);
                    } else {
                        equal = false;
                        break;
                    }
                }
            }

            equal = equal && pchParameters == newPchParameters;
            if (equal)
                return null;

            string newCmdLine = "";
            for (int i = 0; i < cmds.Length; ++i) {
                if (i == mocPos) {
                    newCmdLine = newCmdLine + "\"" + Resources.moc4Command + "\" "
                        + mocOptions
                        + " \"" + inputMocFile + "\" -o \"" + outputFile + "\""
                        + " " + defines + " " + includes;
                    if (!string.IsNullOrEmpty(newPchParameters) && !newCmdLine.Contains(newPchParameters))
                        newCmdLine += " " + newPchParameters;
                } else {
                    newCmdLine = newCmdLine + cmds[i];
                }
                if (i < cmds.Length - 1 && !newCmdLine.EndsWith("\r\n"))
                    newCmdLine = newCmdLine + "\r\n";
            }
            return newCmdLine;
        }

        private static string[] SplitIntoCommands(string cmdLine)
        {
            var cmds = cmdLine.Split(new string[] { "&&", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] res = new string[cmds.Length];
            for (int i = 0; i < cmds.Length; ++i)
                res[i] = cmds[i].Trim();
            return res;
        }

        private static int MocCommandPosition(string[] cmds)
        {
            int res = -1;
            var reg = new Regex(@"(\S*moc.exe|""\S+:\\\.*moc.exe"")");
            for (int i = 0; i < cmds.Length; ++i) {
                var m = reg.Match(cmds[i]);
                if (m.Success)
                    return i;
            }
            return res;
        }

        private static List<string> ExtractDefines(string cmdLine)
        {
            var reg = new Regex(@"-D(\S+)");
            var col = reg.Matches(cmdLine);
            var lst = new List<string>(col.Count);
            for (int i = 0; i < col.Count; ++i)
                lst.Add(col[i].Groups[1].ToString());
            return lst;
        }

        private List<string> ExtractIncludes(string cmdLine)
        {
            var reg = new Regex(@"-I([^\s""]+)|-I""([^""]+)""");
            var col = reg.Matches(cmdLine);
            var lst = new List<string>(col.Count);
            for (int i = 0; i < col.Count; ++i) {
                var s = col[i].Groups[1].ToString();
                if (s.Length != 0)
                    lst.Add(NormalizePath(s));
                else
                    lst.Add(NormalizePath(col[i].Groups[2].ToString()));
            }
            return lst;
        }

        private static string ExtractPCHOptions(string cmdLine)
        {
            var reg = new Regex(@"""-f(\S+)"" ""-f(\S+)");
            var col = reg.Matches(cmdLine);
            if (col.Count != 1)
                return null;
            return col[0].ToString();
        }
    }
}
