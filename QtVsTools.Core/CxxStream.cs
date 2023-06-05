/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core
{
    /// <summary>
    /// StreamReader for C++ files.
    /// Removes comments, takes care of strings and skips empty lines.
    /// </summary>
    public class CxxStream
    {
        public static bool ContainsNotCommented(VCFile file, string str,
            StringComparison comparisonType, bool suppressStrings)
        {
            return ContainsNotCommented(file, new[] { str }, comparisonType, suppressStrings);
        }

        public static bool ContainsNotCommented(VCFile file, string[] searchStrings,
            StringComparison comparisonType, bool suppressStrings)
        {
            // Small optimization, we first read the whole content as a string and look for the
            // search strings. Once we found at least one, ...
            var found = false;
            var content = string.Empty;
            try {
                using var sr = new StreamReader(file.FullPath);
                content = sr.ReadToEnd();
                sr.Close();

                if (searchStrings.Any(key => content.IndexOf(key, comparisonType) >= 0))
                    found = true;
            } catch (Exception exception) {
                exception.Log();
            }

            if (!found)
                return false;

            // ... we will start parsing the file again to see if the actual string is commented
            // or not. The combination of string.IndexOf(...) and string.Split(...) seems to be
            // way faster then reading the file line by line.
            found = false;
            try {
                var cxxSr = new CxxStream(content.Split(new[] { "\n", "\r\n" },
                    StringSplitOptions.RemoveEmptyEntries));
                while (!found && cxxSr.ReadLine(suppressStrings) is {} strLine) {
                    if (searchStrings.Any(str => strLine.IndexOf(str, comparisonType) != -1))
                        found = true;
                }
            } catch (Exception exception) {
                exception.Log();
            }
            return found;
        }

        private enum State
        {
            Normal, Comment, String
        }
        private State state = State.Normal;
        private string partialLine;

        private int lineNumber;
        private readonly string[] lines;

        private CxxStream(string[] lines)
        {
            this.lines = lines;
        }

        private string ReadLine(bool removeStrings = false)
        {
            string line;
            do {
                if (lineNumber >= lines.Length)
                    return null;
                line = ProcessString(lines[lineNumber++], removeStrings);
            } while (line.Length == 0);
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
                break;
            }
            case State.Comment: {
                var idx = line.IndexOf("*/", StringComparison.Ordinal);
                if (idx >= 0) {
                    state = State.Normal;
                    line = line.Substring(idx + 2);
                } else {
                    line = string.Empty; // skip line
                }
                break;
            }
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
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
            }
            return line;
        }
    }
}
