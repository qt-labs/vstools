// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;

namespace QtVsTools.QrcEditor
{
    /// <summary>
    /// Command line: QrcEditor [-theme dark|light] [file.qrc]
    /// </summary>
    internal sealed class CommandLine
    {
        public string FilePath { get; private set; }
        public bool DarkTheme { get; private set; }

        public static CommandLine Parse(IReadOnlyList<string> args)
        {
            var result = new CommandLine();
            for (var i = 0; i < args.Count; i++) {
                var arg = args[i];
                if (string.Equals(arg, "-theme", StringComparison.OrdinalIgnoreCase)) {
                    if (++i < args.Count)
                        result.DarkTheme = string.Equals(args[i], "dark",
                            StringComparison.OrdinalIgnoreCase);
                } else if (!arg.StartsWith("-", StringComparison.Ordinal)
                    && result.FilePath == null) {
                    result.FilePath = arg;
                }
            }
            return result;
        }
    }
}
