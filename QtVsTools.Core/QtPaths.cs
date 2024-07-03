/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Diagnostics;
using System.Text;

namespace QtVsTools.Core
{
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
    }
}
