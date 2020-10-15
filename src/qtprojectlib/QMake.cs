/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using QtVsTools.VisualStudio;

namespace QtProjectLib
{
    public abstract class QMake
    {
        public Dictionary<string, string> Vars { get; protected set; }
        public string OutputFile { get; protected set; }
        public uint DebugLevel { get; protected set; }
        public string TemplatePrefix { get; protected set; }
        public bool Recursive { get; protected set; }
        public string ProFile { get; protected set; }
        public string Query { get; protected set; }
        public bool DisableWarnings { get; set; }

        protected VersionInformation QtVersion { get; private set; }
        protected EnvDTE.DTE Dte { get; private set; }

        public QMake(VersionInformation qtVersion, EnvDTE.DTE dte = null)
        {
            Debug.Assert(qtVersion != null);
            QtVersion = qtVersion;
            Dte = dte ?? VsServiceProvider.GetService<EnvDTE.DTE>();
        }

        protected virtual string QMakeExe
        {
            get
            {
                var qmakePath = Path.Combine(QtVersion.qtDir, "bin", "qmake.exe");
                if (!File.Exists(qmakePath))
                    qmakePath = Path.Combine(QtVersion.qtDir, "qmake.exe");
                return qmakePath;
            }
        }

        protected virtual string WorkingDirectory
        {
            get
            {
                return Path.GetDirectoryName(ProFile);
            }
        }

        string MakeRelative(string absolutePath)
        {
            var workDir = new Uri(Path.GetDirectoryName(ProFile) + Path.DirectorySeparatorChar);
            var path = new Uri(absolutePath);
            if (workDir.IsBaseOf(path)) {
                return workDir.MakeRelativeUri(path).OriginalString
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            } else {
                return absolutePath;
            }
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
            qmakeStartInfo.EnvironmentVariables["QTDIR"] = QtVersion.qtDir;

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
                Messages.PaneMessageSafe(Dte, msg, 5000);
        }

        protected virtual void ErrMsg(string msg)
        {
            if (Dte != null && !string.IsNullOrEmpty(msg))
                Messages.PaneMessageSafe(Dte, msg, 5000);
        }

        protected virtual void InfoMsg(string msg)
        {
            if (Dte != null && !string.IsNullOrEmpty(msg))
                Messages.PaneMessageSafe(Dte, msg, 5000);
        }

        protected virtual void InfoStart(Process qmakeProc)
        {
            InfoMsg(string.Format("--- qmake({0}): started {1}",
                qmakeProc.Id, qmakeProc.StartInfo.FileName));
        }

        protected virtual void InfoExit(Process qmakeProc)
        {
            InfoMsg(string.Format("--- qmake({0}): exit code {1} ({2:0.##} msecs)\r\n",
                qmakeProc.Id, qmakeProc.ExitCode,
                (qmakeProc.ExitTime - qmakeProc.StartTime).TotalMilliseconds));
        }

        public virtual int Run(bool setVCVars=false)
        {
            int exitCode = -1;
            using (var qmakeProc = CreateProcess()) {
                try {
                    if (setVCVars
                        && !HelperFunctions.SetVCVars(QtVersion, qmakeProc.StartInfo)) {
                        OutMsg("Error setting VC vars");
                    }
                    if (qmakeProc.Start()) {
                        InfoStart(qmakeProc);
                        qmakeProc.BeginOutputReadLine();
                        qmakeProc.BeginErrorReadLine();
                        qmakeProc.WaitForExit();
                        exitCode = qmakeProc.ExitCode;
                        InfoExit(qmakeProc);
                    }
                } catch (Exception e) {
                    ErrMsg(string.Format("Exception \"{0}\":\r\n{1}",
                        e.Message,
                        e.StackTrace));
                }
            }
            return exitCode;
        }
    }

    public class QMakeImport : QMake
    {
        public QMakeImport(
            VersionInformation qtVersion,
            string proFilePath,
            bool recursiveRun = false,
            EnvDTE.DTE dte = null)
        : base(qtVersion, dte)
        {
            ProFile = proFilePath;
            TemplatePrefix = "vc";
            if (recursiveRun)
                Recursive = true;
            else
                OutputFile = Path.ChangeExtension(proFilePath, ".vcxproj");
            Vars = new Dictionary<string, string>
            {
                { "QMAKE_INCDIR_QT", @"$(QTDIR)\include" },
                { "QMAKE_LIBDIR", @"$(QTDIR)\lib" },
                { "QMAKE_MOC", @"$(QTDIR)\bin\moc.exe" },
                { "QMAKE_QMAKE", @"$(QTDIR)\bin\qmake.exe" },
            };
        }

        protected override void InfoStart(Process qmakeProc)
        {
            base.InfoStart(qmakeProc);
            InfoMsg("--- qmake: Working Directory: " + qmakeProc.StartInfo.WorkingDirectory);
            InfoMsg("--- qmake: Arguments: " + qmakeProc.StartInfo.Arguments);
            if (qmakeProc.StartInfo.EnvironmentVariables.ContainsKey("QMAKESPEC")) {
                var qmakeSpec = qmakeProc.StartInfo.EnvironmentVariables["QMAKESPEC"];
                if (qmakeSpec != QtVersion.QMakeSpecDirectory) {
                    InfoMsg("--- qmake: Environment "
                        + "variable QMAKESPEC overwriting Qt version QMAKESPEC.");
                    InfoMsg("--- qmake: Qt version "
                        + "QMAKESPEC: " + QtVersion.QMakeSpecDirectory);
                    InfoMsg("--- qmake: Environment "
                        + "variable QMAKESPEC: " + qmakeSpec);
                }
            }
        }
    }
}
