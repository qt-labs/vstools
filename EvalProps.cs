/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace QtVsTools
{
    public static class Properties
    {
        static Dictionary<string, string> PropEval = File.ReadAllLines("evalprops.csv")
            .Select(x => x.Split(';'))
            .ToDictionary(x => x.First(), x => string.Join(";", x.Skip(1)));
        public static IReadOnlyDictionary<string, string> ValueOf => PropEval;
        public static string Configuration => ValueOf["Configuration"];
        public static string MSBuildToolsPath => ValueOf["MSBuildToolsPath"];
        public static string SolutionDir => ValueOf["SolutionDir"];
        public static string VsInstallRoot => ValueOf["VsInstallRoot"];
        public static string VCTargetsPath => ValueOf["VCTargetsPath"];
    }
}
