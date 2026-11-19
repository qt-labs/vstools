// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QtVsTools.Package.Editors
{
    internal class QtDesignerFileSniffer : IFileTypeSniffer
    {
        private static readonly Regex Regex = new(@"<\s*(?i:ui)\s+version\s*=\s*""\d+\.\d+""\s*>");

        public bool IsSupportedFile(string filePath)
        {
            try {
                return File.ReadLines(filePath).Take(3).Any(line => Regex.IsMatch(line.Trim()));
            } catch {
                return false;
            }
        }
    }
}
