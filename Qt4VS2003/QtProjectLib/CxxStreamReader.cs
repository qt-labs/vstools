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
using System.IO;

namespace Digia.Qt5ProjectLib
{
    /// <summary>
    /// StreamReader for C++ files.
    /// Removes comments, takes care of strings and skips empty lines.
    /// </summary>
    class CxxStreamReader
    {
        private enum State
        {
            Normal, Comment, String
        }
        private State state = State.Normal;
        private StreamReader sr = null;
        private string partialLine = "";

        public CxxStreamReader(string fileName)
        {
            sr = new StreamReader(fileName);
        }

        public void Close()
        {
            sr.Close();
        }

        public string ReadLine()
        {
            return ReadLine(false);
        }

        public string ReadLine(bool removeStrings)
        {
            string line;
            do
            {
                line = sr.ReadLine();
                if (line == null)
                    return null;
                line = ProcessString(line, removeStrings);
            } while (line.Length == 0);
            return line;
        }

        private string ProcessString(string line, bool removeStrings)
        {
            switch (state)
            {
                case State.Normal:
                    {
                        string lineCopy = line;
                        line = "";
                        for (int i = 0, j = 1; i < lineCopy.Length; ++i, ++j)
                        {
                            if (lineCopy[i] == '/' && j < lineCopy.Length)
                            {
                                if (lineCopy[j] == '*')
                                {
                                    // C style comment detected
                                    int endIdx = lineCopy.IndexOf("*/", j + 1);
                                    if (endIdx >= 0)
                                    {
                                        i = endIdx + 1;
                                        j = i + 1;
                                        continue;
                                    }
                                    else
                                    {
                                        state = State.Comment;
                                        break;
                                    }
                                }
                                else if (lineCopy[j] == '/')
                                {
                                    // C++ style comment detected
                                    break;
                                }
                            }
                            else if (lineCopy[i] == '"')
                            {
                                // start of a string detected
                                int endIdx = j - 1;
                                do
                                {
                                    endIdx = lineCopy.IndexOf('"', endIdx + 1);
                                } while (endIdx >= 0 && lineCopy[endIdx - 1] == '\\');

                                if (endIdx < 0)
                                {
                                    if (lineCopy.EndsWith("\\"))
                                    {
                                        partialLine = line;
                                        if (!removeStrings)
                                            partialLine += lineCopy.Substring(i);
                                        state = State.String;
                                    }
                                    else
                                    {
                                        state = State.Normal;
                                    }
                                    line = "";
                                    break;
                                }
                                else
                                {
                                    if (!removeStrings)
                                        line += lineCopy.Substring(i, endIdx - i + 1);
                                    i = endIdx;
                                    j = i + 1;
                                    continue;
                                }
                            }
                            line += lineCopy[i];
                        }
                    }
                    break;
                case State.Comment:
                    {
                        int idx = line.IndexOf("*/");
                        if (idx >= 0)
                        {
                            state = State.Normal;
                            line = line.Substring(idx + 2);
                            break;
                        }
                        else
                        {
                            line = "";  // skip line
                        }
                    }
                    break;
                case State.String:
                    {
                        string lineCopy = line;
                        line = "";
                        int endIdx = -1;
                        do
                        {
                            endIdx = lineCopy.IndexOf('"', endIdx + 1);
                        } while (endIdx >= 0 && lineCopy[endIdx - 1] == '\\');
                        if (endIdx < 0)
                        {
                            if (!removeStrings)
                                partialLine += lineCopy;
                        }
                        else
                        {
                            state = State.Normal;
                            line = partialLine;
                            if (!removeStrings)
                                line += lineCopy.Substring(0, endIdx + 1);
                            line += ProcessString(lineCopy.Substring(endIdx + 1), removeStrings);
                        }
                    }
                    break;
            }
            return line;
        }
    }
}
