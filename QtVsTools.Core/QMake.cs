/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QtVsTools.Core
{
    using VisualStudio;
    using static Utils;

    public abstract class QMake
    {
        public Dictionary<string, string> Vars { get; set; }
        public string OutputFile { get; set; }
        private uint DebugLevel { get; set; }
        public string TemplatePrefix { get; set; }
        public bool Recursive { get; set; }
        public string ProFile { get; set; }
        public string Query { get; protected set; }
        public bool DisableWarnings { get; set; }

        private readonly string qtDir;

        private EnvDTE.DTE Dte { get; }

        protected QMake(string qtDir, EnvDTE.DTE dte = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(qtDir));
            this.qtDir = qtDir;
            Dte = dte ?? VsServiceProvider.GetService<EnvDTE.DTE>();
        }

        protected virtual string QMakeExe
        {
            get
            {
                var qmakePath = Path.Combine(qtDir, "bin", "qmake.exe");
                if (!File.Exists(qmakePath))
                    qmakePath = Path.Combine(qtDir, "qmake.exe");
                if (!File.Exists(qmakePath))
                    qmakePath = Path.Combine(qtDir, "bin", "qmake.bat");
                if (!File.Exists(qmakePath))
                    qmakePath = Path.Combine(qtDir, "qmake.bat");
                return qmakePath;
            }
        }

        protected virtual string WorkingDirectory => Path.GetDirectoryName(ProFile);

        string MakeRelative(string absolutePath)
        {
            var workDir = new Uri(Path.GetDirectoryName(ProFile) + Path.DirectorySeparatorChar);
            var path = new Uri(absolutePath);
            if (workDir.IsBaseOf(path))
                return HelperFunctions.ToNativeSeparator(workDir.MakeRelativeUri(path).OriginalString);
            return absolutePath;
        }

        protected virtual string QMakeArgs
        {
            get
            {
                var args = new StringBuilder();
                if (Vars != null) {
                    foreach (KeyValuePair<string, string> v in Vars) {
                        args.AppendFormat(" {0}={1}", v.Key, v.Value);
                    }
                }

                if (!string.IsNullOrEmpty(OutputFile))
                    args.AppendFormat(" -o \"{0}\"", MakeRelative(OutputFile));

                for (int i = 0; i < DebugLevel; ++i)
                    args.Append(" -d");

                if (!string.IsNullOrEmpty(TemplatePrefix))
                    args.AppendFormat(" -tp {0}", TemplatePrefix);

                if (Recursive)
                    args.Append(" -recursive");

                if (DisableWarnings)
                    args.Append(" -Wnone");

                if (!string.IsNullOrEmpty(ProFile))
                    args.AppendFormat(" \"{0}\"", MakeRelative(ProFile));

                if (!string.IsNullOrEmpty(Query))
                    args.AppendFormat(" -query {0}", Query);

                return args.ToString();
            }
        }

        protected virtual Process CreateProcess()
        {
            var qmakeStartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                FileName = QMakeExe,
                Arguments = QMakeArgs,
                WorkingDirectory = WorkingDirectory,
            };
            qmakeStartInfo.EnvironmentVariables["QTDIR"] = qtDir;

            var qmakeProc = new Process
            {
                StartInfo = qmakeStartInfo,
            };
            qmakeProc.OutputDataReceived += (sender, ev) => OutMsg(ev.Data);
            qmakeProc.ErrorDataReceived += (sender, ev) => ErrMsg(ev.Data);

            return qmakeProc;
        }

        protected virtual void OutMsg(string msg)
        {
            if (Dte != null && !string.IsNullOrEmpty(msg))
                Messages.Print(msg);
        }

        protected virtual void ErrMsg(string msg)
        {
            if (Dte != null && !string.IsNullOrEmpty(msg))
                Messages.Print(msg);
        }

        protected virtual void InfoMsg(string msg)
        {
            if (Dte != null && !string.IsNullOrEmpty(msg))
                Messages.Print(msg);
        }

        protected virtual void InfoStart(Process qmakeProc)
        {
            InfoMsg($"--- qmake({qmakeProc.Id}): started {qmakeProc.StartInfo.FileName}");
        }

        protected virtual void InfoExit(Process qmakeProc)
        {
            InfoMsg($"--- qmake({qmakeProc.Id}): exit code {qmakeProc.ExitCode} "
                + $"({(qmakeProc.ExitTime - qmakeProc.StartTime).TotalMilliseconds:0.##} msecs)\r\n");
        }

        public virtual int Run()
        {
            int exitCode = -1;
            using (var qmakeProc = CreateProcess()) {
                try {
                    if (qmakeProc.Start()) {
                        InfoStart(qmakeProc);
                        qmakeProc.BeginOutputReadLine();
                        qmakeProc.BeginErrorReadLine();
                        qmakeProc.WaitForExit();
                        exitCode = qmakeProc.ExitCode;
                        InfoExit(qmakeProc);
                    }
                } catch (Exception exception) {
                    exception.Log();
                }
            }
            return exitCode;
        }

        public static bool Exists(string path)
        {
            var possibleQMakePaths = new[] {
                // Path points to qmake.exe or qmake.bat
                path,
                // Path points to folder containing qmake.exe or qmake.bat
                Path.Combine(path, "qmake.exe"),
                Path.Combine(path, "qmake.bat"),
                // Path points to folder containing bin\qmake.exe or bin\qmake.bat
                Path.Combine(path, "bin", "qmake.exe"),
                Path.Combine(path, "bin", "qmake.bat"),
            };
            return possibleQMakePaths.Where(File.Exists).Select(Path.GetFileName)
                .Any(file => file.Equals("qmake.exe", IgnoreCase)
                  || file.Equals("qmake.bat", IgnoreCase));
        }
    }
}
