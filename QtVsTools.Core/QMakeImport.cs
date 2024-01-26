/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Process = System.Diagnostics.Process;

namespace QtVsTools.Core
{
    using QtVsTools.Common;
    using static Common.Utils;
    using static SyntaxAnalysis.RegExpr;

    public static class QMakeImport
    {
        private class QMakeProcess : QMake
        {
            private readonly VersionInformation qtVersion;

            public QMakeProcess(VersionInformation versionInfo, EnvDTE.DTE dte)
                : base(versionInfo.QtDir, dte)
            {
                qtVersion = versionInfo;
            }

            protected override void InfoStart(Process qmakeProc)
            {
                base.InfoStart(qmakeProc);
                InfoMsg(qmakeProc, $"Working Directory: {qmakeProc.StartInfo.WorkingDirectory}");
                InfoMsg(qmakeProc, qmakeProc.StartInfo.Arguments);
                if (!qmakeProc.StartInfo.EnvironmentVariables.ContainsKey("QMAKESPEC"))
                    return;
                var qmakeSpec = qmakeProc.StartInfo.EnvironmentVariables["QMAKESPEC"];
                if (qmakeSpec == qtVersion?.QMakeSpecDirectory)
                    return;
                InfoMsg(qmakeProc, "Variable QMAKESPEC overwriting Qt version QMAKESPEC.");
                InfoMsg(qmakeProc, $"Qt version QMAKESPEC: {qtVersion?.QMakeSpecDirectory}");
                InfoMsg(qmakeProc, $"Environment variable QMAKESPEC: {qmakeSpec}");
            }

            public override int Run()
            {
                using var qmakeProc = CreateProcess();
                if (qtVersion is null)
                    OutMsg(qmakeProc, "vcvars: Qt version may not be null");
                if (!SetVcVars(qmakeProc.StartInfo))
                    OutMsg(qmakeProc, "Error setting VC vars");
                return Run(qmakeProc);
            }

            private static LazyFactory StaticLazy { get; } = new();
            private static Parser EnvVarParser => StaticLazy.Get(() => EnvVarParser, () =>
            {
                var tokenName = new Token("name", (~Chars["=\r\n"]).Repeat(atLeast: 1));
                var tokenValuePart = new Token("value_part", (~Chars[";\r\n"]).Repeat(atLeast: 1));
                var tokenValue = new Token("value", (tokenValuePart | Chars[';']).Repeat())
                {
                    new Rule<List<string>>
                    {
                        Capture(_ => new List<string>()),
                        Update("value_part", (List<string> parts, string part) => parts.Add(part))
                    }
                };
                var tokenEnvVar = new Token("env_var", tokenName & "=" & tokenValue & LineBreak)
                {
                    new Rule<KeyValuePair<string, List<string>>>
                    {
                        Create("name", (string name)
                            => new KeyValuePair<string, List<string>>(name, null)),
                        Transform("value", (KeyValuePair<string, List<string>> prop, List<string> value)
                            => new KeyValuePair<string, List<string>>(prop.Key, value))
                    }
                };
                return tokenEnvVar.Render();
            });

            private bool SetVcVars(ProcessStartInfo startInfo)
            {
                if (string.IsNullOrEmpty(VcPath))
                    return false;

                // Select vcvars script according to host and target platforms
                var osIs64Bit = Environment.Is64BitOperatingSystem;
                string vcVarsCmd;
                switch (qtVersion?.Platform) {
                case Platform.x86:
                    vcVarsCmd = Path.Combine(VcPath, osIs64Bit
                            ? @"Auxiliary\Build\vcvarsamd64_x86.bat"
                            : @"Auxiliary\Build\vcvars32.bat");
                    break;
                case Platform.x64:
                    vcVarsCmd = Path.Combine(VcPath, osIs64Bit
                            ? @"Auxiliary\Build\vcvars64.bat"
                            : @"Auxiliary\Build\vcvarsx86_amd64.bat");
                    break;
                case Platform.arm64:
                    vcVarsCmd = Path.Combine(VcPath, osIs64Bit
                            ? @"Auxiliary\Build\vcvarsamd64_arm64.bat"
                            : @"Auxiliary\Build\vcvarsx86_arm64.bat");
                    if (!File.Exists(vcVarsCmd)) {
                        vcVarsCmd = Path.Combine(VcPath, osIs64Bit
                                ? @"Auxiliary\Build\vcvars64.bat"
                                : @"Auxiliary\Build\vcvarsx86_amd64.bat");
                    }
                    break;
                default:
                    Messages.Print(">>> vcvars: Cannot set path");
                    return false;
                }

                if (!File.Exists(vcVarsCmd)) {
                    Messages.Print(">>> vcvars: NOT FOUND");
                    return false;
                }

                // Run vcvars and print environment variables
                var stdOut = new StringBuilder();
                var command = $"/c \"{vcVarsCmd}\" && set";
                var comspecPath = Environment.GetEnvironmentVariable("COMSPEC");
                var vcVarsStartInfo = new ProcessStartInfo(comspecPath, command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                var process = Process.Start(vcVarsStartInfo);
                if (process == null)
                    return false;

                var vcVarsProcId = process.Id;
                Messages.Print($"--- vcvars({vcVarsProcId}): {vcVarsCmd}");
                process.OutputDataReceived += (_, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data))
                        return;
                    var data = e.Data.TrimEnd('\r', '\n');
                    if (!string.IsNullOrEmpty(data))
                        stdOut.Append($"{data}\r\n");
                };
                process.BeginOutputReadLine();
                process.WaitForExit();
                var ok = process.ExitCode == 0;
                process.Close();
                if (!ok)
                    return false;

                // Parse command output: copy environment variables to startInfo
                var envVars = EnvVarParser.Parse(stdOut.ToString())
                    .GetValues<KeyValuePair<string, List<string>>>("env_var")
                    .ToDictionary(envVar => envVar.Key, envVar => envVar.Value ?? new(), CaseIgnorer);
                foreach (var vcVar in envVars)
                    startInfo.Environment[vcVar.Key] = string.Join(";", vcVar.Value);

                // Warn if cl.exe is not in PATH
                var clPath = envVars["PATH"]
                    .Select(path => Path.Combine(path, "cl.exe"))
                    .FirstOrDefault(File.Exists);
                Messages.Print(string.IsNullOrEmpty(clPath)
                    ? $">>> vcvars({vcVarsProcId}): cl path NOT FOUND"
                    : $"--- vcvars({vcVarsProcId}): cl path: {clPath}");

                return true;
            }
        }

        public static string VcPath { get; set; }

        public static int Run(VersionInformation versionInfo, string proFilePath,
            bool recursiveRun = false, bool disableWarnings = false, EnvDTE.DTE dte = null)
        {
            versionInfo ??= VersionInformation.GetOrAddByName(QtVersionManager.GetDefaultVersion());
            var qmake = new QMakeProcess(versionInfo, dte) {
                ProFile = proFilePath,
                TemplatePrefix = "vc",
                Recursive = recursiveRun,
                DisableWarnings = disableWarnings,
                OutputFile = recursiveRun ? null : Path.ChangeExtension(proFilePath, ".vcxproj"),
                Vars = new Dictionary<string, string> {
                    {"QMAKE_INCDIR_QT", @"$(QTDIR)\include"},
                    {"QMAKE_LIBDIR", @"$(QTDIR)\lib"},
                    {"QMAKE_MOC", @"$(QTDIR)\bin\moc.exe"},
                    {"QMAKE_QMAKE", @"$(QTDIR)\bin\qmake.exe"}
                }
            };
            return qmake.Run();
        }
    }
}
