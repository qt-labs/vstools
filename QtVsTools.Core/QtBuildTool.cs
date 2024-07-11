/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace QtVsTools.Core
{
    using static Common.Utils;

    public abstract class QtBuildTool<T> where T : QtBuildTool<T>
    {
        private string QtDir { get; }

        protected QtBuildTool(string qtDir)
        {
            Debug.Assert(!string.IsNullOrEmpty(qtDir));
            QtDir = qtDir;
        }

        protected abstract string ToolArgs { get; }
        protected abstract string WorkingDirectory { get; }

        protected virtual string ToolExe
        {
            get
            {
                var pathsPath = Path.Combine(QtDir, "bin", $"{typeof(T).Name}.exe");
                if (!File.Exists(pathsPath))
                    pathsPath = Path.Combine(QtDir, $"{typeof(T).Name}.exe");
                if (!File.Exists(pathsPath))
                    pathsPath = Path.Combine(QtDir, "bin", $"{typeof(T).Name}.bat");
                if (!File.Exists(pathsPath))
                    pathsPath = Path.Combine(QtDir, $"{typeof(T).Name}.bat");
                return pathsPath;
            }
        }

        protected virtual Process CreateProcess()
        {
            var toolStartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                FileName = ToolExe,
                Arguments = ToolArgs,
                WorkingDirectory = WorkingDirectory,
                EnvironmentVariables =
                {
                    ["QTDIR"] = QtDir
                }
            };

            var toolProc = new Process
            {
                StartInfo = toolStartInfo
            };
            toolProc.OutputDataReceived += (_, ev) => OutMsg(toolProc, ev.Data);
            toolProc.ErrorDataReceived += (_, ev) => ErrMsg(toolProc, ev.Data);

            return toolProc;
        }

        protected virtual void OutMsg(Process toolProc, string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                Messages.Print($"+++ {typeof(T).Name}({toolProc.Id}): {msg}");
        }

        protected virtual void ErrMsg(Process toolProc, string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                Messages.Print($">>> {typeof(T).Name}({toolProc.Id}): {msg}");
        }

        protected virtual void InfoMsg(Process toolProc, string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                Messages.Print($"--- {typeof(T).Name}({toolProc.Id}): {msg}");
        }

        protected virtual void InfoStart(Process toolProc)
        {
            InfoMsg(toolProc, $"Started {toolProc.StartInfo.FileName}");
        }

        protected virtual void InfoExit(Process toolProc)
        {
            InfoMsg(toolProc, $"Exit code {toolProc.ExitCode} "
                + $"({(toolProc.ExitTime - toolProc.StartTime).TotalMilliseconds:0.##} msecs)");
        }

        public virtual int Run()
        {
            using var toolProc = CreateProcess();
            return Run(toolProc);
        }

        protected virtual int Run(Process toolProc)
        {
            var exitCode = -1;
            try {
                if (toolProc.Start()) {
                    InfoStart(toolProc);
                    toolProc.BeginOutputReadLine();
                    toolProc.BeginErrorReadLine();
                    toolProc.WaitForExit();
                    exitCode = toolProc.ExitCode;
                    InfoExit(toolProc);
                }
            } catch (Exception exception) {
                exception.Log();
            }
            return exitCode;
        }

        public static bool Exists(string path)
        {
            if (path == null)
                return false;

            var possibleToolPaths = new[] {
                // Path points to tool executable or batch file
                path,
                // Path points to folder containing tool executable or batch file
                Path.Combine(path, $"{typeof(T).Name}.exe"),
                Path.Combine(path, $"{typeof(T).Name}.bat"),
                // Path points to folder containing bin\tool executable or batch file
                Path.Combine(path, "bin", $"{typeof(T).Name}.exe"),
                Path.Combine(path, "bin", $"{typeof(T).Name}.bat")
            };
            return possibleToolPaths.Where(File.Exists).Select(Path.GetFileName)
                .Any(file => string.Equals(file, $"{typeof(T).Name}.exe", IgnoreCase)
                    || string.Equals(file, $"{typeof(T).Name}.bat", IgnoreCase));
        }
    }
}
