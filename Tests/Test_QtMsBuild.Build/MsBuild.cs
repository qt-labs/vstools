/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Evaluation;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public static class MsBuild
    {
        public static ProjectCollection ProjectCollection { get; } = Get();
        public static Logger Log { get; private set; }

        private static ProjectCollection Get()
        {
            var msbuild = new ProjectCollection(ToolsetDefinitionLocations.None);
            Log = new Logger(msbuild);
            msbuild.RemoveAllToolsets();
            var props = new Dictionary<string, string>
            {
                { "VsInstallRoot", Properties.VsInstallRoot },
                { "VCTargetsPath", Properties.VCTargetsPath }
            };
            var toolset = new Toolset("Current", Properties.MSBuildToolsPath, props, msbuild, null);
            msbuild.AddToolset(toolset);
            return msbuild;
        }

        public static Project Evaluate(string path, params (string name, string value)[] globals)
        {
            return ProjectCollection.LoadProject(
                path, globals.ToDictionary(x => x.name, x => x.value), "Current");
        }

        public static Project Evaluate(XmlReader xml, params (string name, string value)[] globals)
        {
            return ProjectCollection.LoadProject(
                xml, globals.ToDictionary(x => x.name, x => x.value), null);
        }

        public static bool Run(string workDir, params string[] args)
        {
            var msbuildStartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                FileName = Path.Combine(Properties.MSBuildToolsPath, "MSBuild.exe"),
                Arguments = string.Join(" ", args
                    .Where(arg => arg is { Length: > 0})
                    .Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = workDir
            };
            msbuildStartInfo.EnvironmentVariables["VsInstallRoot"] = Properties.VsInstallRoot;
            msbuildStartInfo.EnvironmentVariables["VCTargetsPath"] = Properties.VCTargetsPath;

            var msbuildProc = new Process
            {
                StartInfo = msbuildStartInfo
            };
            msbuildProc.OutputDataReceived += (sender, ev) => Debug.WriteLine(ev.Data);
            msbuildProc.ErrorDataReceived += (sender, ev) => Debug.WriteLine(ev.Data);
            if (!msbuildProc.Start())
                return false;
            msbuildProc.BeginOutputReadLine();
            msbuildProc.BeginErrorReadLine();
            msbuildProc.WaitForExit();
            return msbuildProc.ExitCode == 0;
        }
    }
}
