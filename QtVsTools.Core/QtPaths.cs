/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QtVsTools.Core
{
    using Common;

    public class QtPaths : QtBuildTool<QtPaths>, IQueryProcess
    {
        public bool Version { get; set; }
        public bool Types { get; set; }
        public string Type { get; set; }
        public bool Paths { get; set; }
        public bool WritablePath { get; set; }
        public bool LocateDir { get; set; }
        public bool LocateFile { get; set; }
        public bool LocateFiles { get; set; }
        public bool Display { get; set; }

        public string Executable { get; set; }
        public bool FindExecutable { get; set; }

        public bool TestMode { get; set; }

        public string Query { get; set; }
        public string QueryFormat { get; set; } = "qmake";
        public string QtConf { get; set; }

        private StringBuilder stdOutput;
        public StringBuilder StdOutput => stdOutput ??= new StringBuilder();

        public QtPaths(string qtDir)
            : base(qtDir)
        {}

        protected override string ToolArgs
        {
            get
            {
                var args = new StringBuilder();
                if (Version)
                    args.Append(" --version");
                if (Types)
                    args.Append(" --types");
                if (Paths)
                    args.Append($" --paths {Type}");
                if (WritablePath)
                    args.Append($" --writable-path {Type}");
                if (LocateDir)
                    args.Append($" --locate-dir {Type}");
                if (LocateFile)
                    args.Append($" --locate-file {Type}");
                if (LocateFiles)
                    args.Append($" --locate-files {Type}");
                if (Display)
                    args.Append($" --display {Type}");

                if (FindExecutable)
                    args.Append($" --find-exe {Executable}");

                if (TestMode)
                    args.Append(" --testmode");

                if (!string.IsNullOrEmpty(Query))
                    args.Append($" --query {Query} --query-format {QueryFormat}");
                if (!string.IsNullOrEmpty(QtConf))
                    args.Append($" --qtconf {QtConf}");

                return args.ToString();
            }
        }

        protected override string WorkingDirectory => "";

        protected override void OutMsg(Process qmakeProc, string msg)
        {
            base.OutMsg(qmakeProc, msg);
            StdOutput.AppendLine(msg);
        }

        protected override Process CreateProcess()
        {
            // This function implements a workaround running qtpaths.bat from a Process. The
            // issue here is that QCommandLineParser checks if the executable is running from
            // a console and if it is not, it will show a message box in addition to dumping
            // the version information with fputs() to stdout. This is a bug in Qt, but we need
            // to work around the issue here.

            // only create the process if we want to know the version of qtpaths
            if (!Version)
                return base.CreateProcess();
            // only create the process if we want to know the version of qtpaths using a .bat file
            if (string.Equals(Path.GetExtension(ToolExe), ".exe", Utils.IgnoreCase))
                return base.CreateProcess();

            // open the .bat file, read the content, do some replacement and create the start info
            var batchFileDirectory = Path.GetDirectoryName(ToolExe);
            var batchFileLines = File.ReadAllLines(ToolExe);
            var qtPathsLine = batchFileLines.FirstOrDefault(line => line.Contains("qtpaths"));
            if (string.IsNullOrEmpty(qtPathsLine))
                return base.CreateProcess();

            var expandedLine = qtPathsLine.Replace("%~dp0", batchFileDirectory + "\\")
                .Replace("%*", ToolArgs);
            var commandParts = expandedLine.Split(new[] { ' ' }, 2 /* split in two parts */);
            var toolStartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                FileName = commandParts[0],
                Arguments = commandParts[1],
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
    }
}
