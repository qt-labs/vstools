// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public static class MsBuild
    {
        private static string QtMsBuildPath =>
            Path.Combine(Properties.SolutionDir, "QtMsBuild", "QtMsBuild");

        private static string MsBuildExePath =>
            Path.Combine(Properties.MSBuildToolsPath, "MSBuild.exe");

        public static bool Run(string workDir, params string[] args)
        {
            var msbuildProc = new Process { StartInfo = CreateStartInfo(workDir, args) };
            msbuildProc.OutputDataReceived += (_, ev) => Debug.WriteLine(ev.Data);
            msbuildProc.ErrorDataReceived += (_, ev) => Debug.WriteLine(ev.Data);
            if (!msbuildProc.Start())
                return false;
            msbuildProc.BeginOutputReadLine();
            msbuildProc.BeginErrorReadLine();
            msbuildProc.WaitForExit();
            return msbuildProc.ExitCode == 0;
        }

        public static string GetProperty(string workDir, string projectPath, string propertyName,
            params string[] args)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                throw new ArgumentNullException(nameof(projectPath));
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            var allArgs = new List<string>
            {
                projectPath,
                "-nologo",
                "-verbosity:quiet",
                $"-getProperty:{propertyName}"
            };
            if (args is { Length: > 0 })
                allArgs.AddRange(args);

            var (exitCode, stdOut, stdErr) = RunWithOutput(workDir, allArgs.ToArray());
            if (exitCode == 0)
                return stdOut.Trim(' ', '\r', '\n', '\t');

            // we should not end up here in normal circumstances
            throw new InvalidOperationException($"msbuild -getProperty:{propertyName} "
                + $"failed: {stdErr}{stdOut}");
        }

        private static (int ExitCode, string StdOut, string StdErr) RunWithOutput(
            string workDir, params string[] args)
        {
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            var msbuildProc = new Process { StartInfo = CreateStartInfo(workDir, args) };
            msbuildProc.OutputDataReceived += (_, ev) =>
            {
                if (ev.Data != null)
                    stdOut.AppendLine(ev.Data);
            };
            msbuildProc.ErrorDataReceived += (_, ev) =>
            {
                if (ev.Data != null)
                    stdErr.AppendLine(ev.Data);
            };
            if (!msbuildProc.Start())
                throw new InvalidOperationException("Could not start MSBuild.exe.");
            msbuildProc.BeginOutputReadLine();
            msbuildProc.BeginErrorReadLine();
            msbuildProc.WaitForExit();

            return (msbuildProc.ExitCode, stdOut.ToString(), stdErr.ToString());
        }

        private static ProcessStartInfo CreateStartInfo(string workDir, params string[] args)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                FileName = MsBuildExePath,
                Arguments = string.Join(" ", (args ?? Array.Empty<string>())
                    .Where(arg => arg is { Length: > 0 })
                    .Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = workDir,
                EnvironmentVariables =
                {
                    ["VsInstallRoot"] = Properties.VsInstallRoot,
                    ["VCTargetsPath"] = Properties.VCTargetsPath,
                    ["QtMsBuild"] = QtMsBuildPath
                }
            };
            return startInfo;
        }
    }
}
