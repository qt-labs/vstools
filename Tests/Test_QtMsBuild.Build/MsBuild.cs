/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public static class MsBuild
    {
        public static ProjectCollection ProjectCollection { get; } = Get();
        public static Logger Log { get; private set; }

        public static Dictionary<string, string> GlobalProperties { get; private set; } = new()
        {
            { "MSBuildToolsPath32", Properties.MSBuildToolsPath32 },
            { "MSBuildToolsPath64", Properties.MSBuildToolsPath64 },
            { "MSBuildSDKsPath", Properties.MSBuildSDKsPath },
            { "MSBuildRuntimeVersion", Properties.MSBuildRuntimeVersion },
            { "MSBuildFrameworkToolsPath", Properties.MSBuildFrameworkToolsPath },
            { "MSBuildFrameworkToolsPath32", Properties.MSBuildFrameworkToolsPath32 },
            { "MSBuildFrameworkToolsPath64", Properties.MSBuildFrameworkToolsPath64 },
            { "MSBuildFrameworkToolsPathArm64", Properties.MSBuildFrameworkToolsPathArm64 },
            { "MSBuildFrameworkToolsRoot", Properties.MSBuildFrameworkToolsRoot },
            { "MSBuildToolsRoot", Properties.MSBuildToolsRoot },
            { "MSBuildExtensionsPath", Properties.MSBuildExtensionsPath },
            { "MSBuildExtensionsPath32", Properties.MSBuildExtensionsPath32 },
            { "MSBuildExtensionsPath64", Properties.MSBuildExtensionsPath64 },
            { "FrameworkSDKRoot", Properties.FrameworkSDKRoot },
            { "RoslynTargetsPath", Properties.RoslynTargetsPath },
            { "SDK35ToolsPath", Properties.SDK35ToolsPath },
            { "SDK40ToolsPath", Properties.SDK40ToolsPath },
            { "WindowsSDK80Path", Properties.WindowsSDK80Path },
            { "VSToolsPath", Properties.VSToolsPath },
            { "VsInstallRoot", Properties.VsInstallRoot },
            { "VCTargetsPath", Properties.VCTargetsPath },
            { "VCTargetsPath10", Properties.VCTargetsPath10 },
            { "VCTargetsPath11", Properties.VCTargetsPath11 },
            { "VCTargetsPath12", Properties.VCTargetsPath12 },
            { "VCTargetsPath14", Properties.VCTargetsPath14 },
            { "AndroidTargetsPath", Properties.AndroidTargetsPath },
            { "iOSTargetsPath", Properties.iOSTargetsPath },
            { "QtMsBuildToolsPath", Environment.CurrentDirectory },
            { "SkipImportNuGetProps", "true" },
            { "QtMsBuild", Path.Combine(Properties.SolutionDir, "QtMsBuild", "QtMsBuild") }
        };

        public static ProjectCollection Get()
        {
            var msbuild = new ProjectCollection();
            Log = new Logger(msbuild);
            msbuild.RemoveAllToolsets();
            var toolset = new Toolset("Current", Properties.MSBuildToolsPath, GlobalProperties, msbuild, null);
            msbuild.AddToolset(toolset);
            return msbuild;
        }

        private static Dictionary<string, string> MakeGlobals(params (string name, string value)[] globals)
        {
            globals ??= Array.Empty<(string, string)>();
            return GlobalProperties
                .Union(globals.ToDictionary(x => x.name, x => x.value))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public static Project Evaluate(string path, params (string name, string value)[] globals)
        {
            return new Project(path, MakeGlobals(globals), "Current", ProjectCollection, ProjectLoadSettings.RecordDuplicateButNotCircularImports);
        }

        public class Build
        {
            public ProjectInstance Project { get; set; }
            public BuildRequestData Request { get; set; }
            public BuildParameters Parameters { get; set; }
            public BuildResult Result { get; set; }
        }

        public static Build Prepare(Project project, params string[] targets)
        {
            var globals = project.GlobalProperties
                .Select(x => (x.Key, x.Value))
                .ToArray();
            return Prepare(project, globals, targets);
        }

        public static Build Prepare(Project project, (string name, string value)[] globals, params string[] targets)
        {
            var buildProject = new ProjectInstance(project.Xml, MakeGlobals(globals), "Current", ProjectCollection);
            return Prepare(buildProject, targets);
        }

        public static Build Prepare(string path, (string name, string value)[] globals, params string[] targets)
        {
            var buildProject = new ProjectInstance(path, MakeGlobals(globals), "Current", ProjectCollection);
            return Prepare(buildProject, targets);
        }

        private static Build Prepare(ProjectInstance buildProject, params string[] targets)
        {
            var buildParams = new BuildParameters(ProjectCollection)
            {
                Loggers = new[] { Log },
                LogInitialPropertiesAndItems = true,
                OnlyLogCriticalEvents = false,
                ResetCaches = true,
                DefaultToolsVersion = "Current"
            };
            var buildRequest = new BuildRequestData(
                buildProject, targets, hostServices: null,
                flags: BuildRequestDataFlags.ClearCachesAfterBuild
                    | BuildRequestDataFlags.SkipNonexistentTargets
                    | BuildRequestDataFlags.IgnoreMissingEmptyAndInvalidImports
                    | BuildRequestDataFlags.FailOnUnresolvedSdk
                    | BuildRequestDataFlags.ProvideProjectStateAfterBuild);
            return new()
            {
                Project = buildProject,
                Request = buildRequest,
                Parameters = buildParams,
                Result = null
            };
        }

        public static bool Run(Build build)
        {
            build.Result = BuildManager.DefaultBuildManager.Build(build.Parameters, build.Request);
            return build.Result.OverallResult == BuildResultCode.Success;
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
                    .Where(arg => arg is { Length: > 0 })
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
