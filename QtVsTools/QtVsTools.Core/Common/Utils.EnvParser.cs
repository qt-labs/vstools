// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;

namespace QtVsTools.Core.Common
{
    using Env = Dictionary<string, string>;

    public static partial class Utils
    {
        /// <summary>
        /// Parses environment variables, one 'NAME=VALUE' per line. Names are compared
        /// case-insensitively; the last value wins if a name occurs more than once. Lines without
        /// a name are skipped. Returns null if there is no variable.
        /// </summary>
        public static Env ParseEnvironment(string envString)
        {
            Env env = null;
            var lines = (envString ?? string.Empty).Split(new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;
                env ??= new Env(CaseIgnorer);
                env[line.Substring(0, separator)] = line.Substring(separator + 1);
            }
            return env;
        }
    }
}
