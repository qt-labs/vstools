/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="HostExec" Condition="('$(VisualStudioVersion)' == '16.0' OR '$(VisualStudioVersion)' == '17.0') AND '$(ApplicationType)' == 'Linux' AND '$(PlatformToolset)' == 'WSL_1_0'"

#region Reference
//$(VCTargetsPath)\Application Type\Linux\1.0\Microsoft.Build.Linux.Tasks.dll
#endregion

#region Using
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK HostExec
///  * Linux build over WSL
/////////////////////////////////////////////////////////////////////////////////////////////////
// Run command in build host.
// Parameters:
//     in string      Command: Command to run on the build host
//     in string      RedirectStdOut: Path to file to receive redirected output messages
//                      * can be NUL to discard messages
//     in string      RedirectStdErr: Path to file to receive redirected error messages
//                      * can be NUL to discard messages
//                      * can be STDOUT to apply the same redirection as output messages
//     in string      WorkingDirectory: Path to directory where command will be run
//     in ITaskItem[] Inputs: List of local -> host path mappings for command inputs
//                      * item format: cf. HostTranslatePaths task
//     in ITaskItem[] Outputs: List of host -> local path mappings for command outputs
//                      * item format: cf. HostTranslatePaths task
//     in string      RemoteTarget: Set by ResolveRemoteDir in SSH mode; null otherwise
//     in string      RemoteProjectDir: Set by ResolveRemoteDir in SSH mode; null otherwise
//     in bool        IgnoreExitCode: Set flag to disable build error if command failed
//    out int         ExitCode: status code at command exit
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class HostExec_LinuxWSL
    {
        public static QtMSBuild.ITaskLoggingHelper Log { get; set; }
        public static IBuildEngine BuildEngine { get; set; }
        public static ITaskHost HostObject { get; set; }

        public static bool Execute(
        #region Parameters
            System.String Command,
            out System.Int32 ExitCode,
            System.String Message = null,
            System.String RedirectStdOut = null,
            System.String RedirectStdErr = null,
            System.String WorkingDirectory = null,
            Microsoft.Build.Framework.ITaskItem[] Inputs = null,
            Microsoft.Build.Framework.ITaskItem[] Outputs = null,
            System.String RemoteTarget = null,
            System.String RemoteProjectDir = null,
            System.Boolean IgnoreExitCode = false)
        #endregion
        {
#if (VS2019 || VS2022)
            #region Code
            if (!string.IsNullOrEmpty(Message))
                Log.LogMessage(MessageImportance.High, Message);
            Command = "(" + Command + ")";
            if (RedirectStdOut == "NUL" || RedirectStdOut == "/dev/null")
                Command += " 1> /dev/null";
            else if (!string.IsNullOrEmpty(RedirectStdOut))
                Command += " 1> " + RedirectStdOut;
            if (RedirectStdErr == "NUL" || RedirectStdErr == "/dev/null")
                Command += " 2> /dev/null";
            else if (RedirectStdErr == "STDOUT")
                Command += " 2>&1";
            else if (!string.IsNullOrEmpty(RedirectStdErr))
                Command += " 2> " + RedirectStdErr;

            var createDirs = new List<string>();
            if (Inputs != null) {
                createDirs.AddRange(Inputs
                    .Select(x => string.Format("\x24(dirname {0})", x.GetMetadata("Value"))));
            }
            if (Outputs != null) {
                createDirs.AddRange(Outputs
                    .Select(x => string.Format("\x24(dirname {0})", x.GetMetadata("Value"))));
            }
            if (!string.IsNullOrEmpty(WorkingDirectory)) {
                createDirs.Add(WorkingDirectory);
                Command = string.Format("cd {0}; {1}", WorkingDirectory, Command);
            }
            if (createDirs.Any()) {
                Command = string.Format("{0}; {1}",
                    string.Join("; ", createDirs.Select(x => string.Format("mkdir -p {0}", x))),
                    Command);
            }

            var taskExec = new Microsoft.Build.Linux.WSL.Tasks.ExecuteCommand
            {
                BuildEngine = BuildEngine,
                HostObject = HostObject,
                ProjectDir = @"$(ProjectDir)",
                IntermediateDir = @"$(IntDir)",
                WSLPath = @"$(WSLPath)",
                Command = Command
            };
            Log.LogMessage("\r\n==== HostExec: Microsoft.Build.Linux.WSL.Tasks.ExecuteCommand");
            Log.LogMessage("ProjectDir: {0}", taskExec.ProjectDir);
            Log.LogMessage("IntermediateDir: {0}", taskExec.IntermediateDir);
            Log.LogMessage("WSLPath: {0}", taskExec.WSLPath);
            Log.LogMessage("Command: {0}", taskExec.Command);

            bool ok = taskExec.Execute();
            Log.LogMessage("== {0} ExitCode: {1}\r\n", ok ? "OK" : "FAIL", taskExec.ExitCode);

            ExitCode = taskExec.ExitCode;
            if (!ok && !IgnoreExitCode) {
                Log.LogError("Host command failed.");
                return false;
            }
            #endregion
#else
            ExitCode = 0;
#endif
            return true;
        }
    }
}
#endregion
