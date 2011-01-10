/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
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
using System.IO;

namespace Nokia.QtProjectLib
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
            string line;
            do
            {
                line = sr.ReadLine();
                if (line == null)
                    return null;
                line = ProcessString(line);
            } while (line.Length == 0);
            return line;
        }

        private string ProcessString(string line)
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
                                    line += lineCopy.Substring(i);
                                    state = State.String;
                                    break;
                                }
                                else
                                {
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
                        int endIdx = line.IndexOf('"');
                        if (endIdx == 0)
                        {
                            state = State.Normal;
                            string lineCopy = line;
                            line = "\"";
                            line += ProcessString(lineCopy.Substring(1));
                        }
                        else if (endIdx > 0)
                        {
                            state = State.Normal;
                            string lineCopy = line;
                            line = line.Substring(0, endIdx + 1);
                            line += ProcessString(lineCopy.Substring(endIdx + 1));
                        }
                    }
                    break;
            }
            return line;
        }
    }
}
