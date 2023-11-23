/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public static class Properties
    {
        static Dictionary<string, string> PropEval = File.ReadAllLines("PropEval.csv")
            .Select(x => x.Split(';'))
            .ToDictionary(x => x.First(), x => string.Join(";", x.Skip(1)));
        public static IReadOnlyDictionary<string, string> ValueOf => PropEval;
        public static string MSBuildToolsPath => ValueOf["MSBuildToolsPath"];
        public static string VsInstallRoot => ValueOf["VsInstallRoot"];
        public static string VCTargetsPath => ValueOf["VCTargetsPath"];
        public static string SolutionDir => ValueOf["SolutionDir"];
    }
}
