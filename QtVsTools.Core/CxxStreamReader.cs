/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO;

namespace QtVsTools.Core
{
    /// <summary>
    /// StreamReader for C++ files.
    /// Removes comments, takes care of strings and skips empty lines.
    /// </summary>
    class CxxStreamReader : IDisposable
    {
        private enum State
        {
            Normal, Comment, String
        }
        private State state = State.Normal;
        private readonly StreamReader sr;
        private string partialLine;
        bool disposed;

        int _lineNum;
        readonly string[] _lines;

        public CxxStreamReader(string[] lines)
        {
            _lines = lines;
        }

        public CxxStreamReader(string fileName)
        {
            sr = new StreamReader(fileName);
        }

        ~CxxStreamReader()
        {
            Dispose(false);
        }

        public void Close()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing && sr != null)
                sr.Dispose();

            disposed = true;
        }

        public string ReadLine(bool removeStrings = false)
        {
            string line;
            if (sr != null) {
                do {
                    line = sr.ReadLine();
                    if (line == null)
                        return null;
                    line = ProcessString(line, removeStrings);
                } while (line.Length == 0);
            } else {
                do {
                    if (_lineNum >= _lines.Length)
                        return null;
                    line = ProcessString(_lines[_lineNum++], removeStrings);
                } while (line.Length == 0);
            }
            return line;
        }

        private string ProcessString(string line, bool removeStrings)
        {
            switch (state) {
            case State.Normal: {
                    var lineCopy = line;
                    line = string.Empty;
                    for (int i = 0, j = 1; i < lineCopy.Length; ++i, ++j) {
                        if (lineCopy[i] == '/' && j < lineCopy.Length) {
                            if (lineCopy[j] == '*') {
                                // C style comment detected
                                var endIdx = lineCopy.IndexOf("*/", j + 1, StringComparison.Ordinal);
                                if (endIdx >= 0) {
                                    i = endIdx + 1;
                                    j = i + 1;
                                    continue;
                                }
                                state = State.Comment;
                                break;
                            }
                            if (lineCopy[j] == '/') {
                                // C++ style comment detected
                                break;
                            }
                        } else if (lineCopy[i] == '"') {
                            // start of a string detected
                            var endIdx = j - 1;
                            do {
                                endIdx = lineCopy.IndexOf('"', endIdx + 1);
                            } while (endIdx >= 0 && lineCopy[endIdx - 1] == '\\');

                            if (endIdx < 0) {
                                if (lineCopy.EndsWith("\\", StringComparison.Ordinal)) {
                                    partialLine = line;
                                    if (!removeStrings)
                                        partialLine += lineCopy.Substring(i);
                                    state = State.String;
                                } else {
                                    state = State.Normal;
                                }
                                line = string.Empty;
                                break;
                            }
                            if (!removeStrings)
                                line += lineCopy.Substring(i, endIdx - i + 1);
                            i = endIdx;
                            j = i + 1;
                            continue;
                        }
                        line += lineCopy[i];
                    }
                }
                break;
            case State.Comment: {
                    var idx = line.IndexOf("*/", StringComparison.Ordinal);
                    if (idx >= 0) {
                        state = State.Normal;
                        line = line.Substring(idx + 2);
                    } else {
                        line = string.Empty;  // skip line
                    }
                }
                break;
            case State.String: {
                    var lineCopy = line;
                    line = string.Empty;
                    var endIdx = -1;
                    do {
                        endIdx = lineCopy.IndexOf('"', endIdx + 1);
                    } while (endIdx >= 0 && lineCopy[endIdx - 1] == '\\');
                    if (endIdx < 0) {
                        if (!removeStrings)
                            partialLine += lineCopy;
                    } else {
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
