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
        public static string MSBuildToolsPath => ValueOf["MSBuildToolsPath"];
        public static string MSBuildToolsPath32 => ValueOf["MSBuildToolsPath32"];
        public static string MSBuildToolsPath64 => ValueOf["MSBuildToolsPath64"];
        public static string MSBuildSDKsPath => ValueOf["MSBuildSDKsPath"];
        public static string FrameworkSDKRoot => ValueOf["FrameworkSDKRoot"];
        public static string MSBuildRuntimeVersion => ValueOf["MSBuildRuntimeVersion"];
        public static string MSBuildFrameworkToolsPath => ValueOf["MSBuildFrameworkToolsPath"];
        public static string MSBuildFrameworkToolsPath32 => ValueOf["MSBuildFrameworkToolsPath32"];
        public static string MSBuildFrameworkToolsPath64 => ValueOf["MSBuildFrameworkToolsPath64"];
        public static string MSBuildFrameworkToolsPathArm64 => ValueOf["MSBuildFrameworkToolsPathArm64"];
        public static string MSBuildFrameworkToolsRoot => ValueOf["MSBuildFrameworkToolsRoot"];
        public static string SDK35ToolsPath => ValueOf["SDK35ToolsPath"];
        public static string SDK40ToolsPath => ValueOf["SDK40ToolsPath"];
        public static string WindowsSDK80Path => ValueOf["WindowsSDK80Path"];
        public static string VsInstallRoot => ValueOf["VsInstallRoot"];
        public static string MSBuildToolsRoot => ValueOf["MSBuildToolsRoot"];
        public static string MSBuildExtensionsPath => ValueOf["MSBuildExtensionsPath"];
        public static string MSBuildExtensionsPath32 => ValueOf["MSBuildExtensionsPath32"];
        public static string MSBuildExtensionsPath64 => ValueOf["MSBuildExtensionsPath64"];
        public static string RoslynTargetsPath => ValueOf["RoslynTargetsPath"];
        public static string VCTargetsPath => ValueOf["VCTargetsPath"];
        public static string VCTargetsPath14 => ValueOf["VCTargetsPath14"];
        public static string VCTargetsPath12 => ValueOf["VCTargetsPath12"];
        public static string VCTargetsPath11 => ValueOf["VCTargetsPath11"];
        public static string VCTargetsPath10 => ValueOf["VCTargetsPath10"];
        public static string AndroidTargetsPath => ValueOf["AndroidTargetsPath"];
        public static string iOSTargetsPath => ValueOf["iOSTargetsPath"];
        public static string VSToolsPath => ValueOf["VSToolsPath"];
        public static string SolutionDir => ValueOf["SolutionDir"];
        public static string Configuration => ValueOf["Configuration"];
        public static string Platform => ValueOf["Platform"];
    }
}
