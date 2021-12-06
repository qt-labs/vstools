/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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

#region Task TaskName="HostExec" Condition="'$(ApplicationType)' != 'Linux'"

#region Reference
//$(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll
//$(MSBuildToolsPath)\Microsoft.Build.Utilities.Core.dll
#endregion

#region Using
using Microsoft.Build.Framework;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK HostTranslatePaths
///  * Local (Windows) build
/////////////////////////////////////////////////////////////////////////////////////////////////
// Translate local (Windows) paths to build host paths. This could be a Linux host for cross
// compilation, or a simple copy (i.e. "no-op") when building in Windows.
// Input and output items are in the form:
//    <...>
//      <Item>...</Item>
//      <Name>...</Name>
//      <Value>...</Value>
//    </...>
// where <Item> is the local path, <Name> is a filter criteria identifier matched with the Names
// parameter, and <Value> is set to the host path in output items (for input items <Value> must
// be equal to <Item>).
// Parameters:
//      in ITaskItem[] Items:  input items with local paths
//      in string[]    Names:  filter criteria; unmatched items will simply be copied (i.e. no-op)
//     out ITaskItem[] Result: output items with translated host paths
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class HostExec_Windows
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
            #region Code
            if (!string.IsNullOrEmpty(Message))
                Log.LogMessage(MessageImportance.High, Message);
            Command = "(" + Command + ")";
            if (RedirectStdOut == "NUL" || RedirectStdOut == "/dev/null")
                Command += " 1> NUL";
            else if (!string.IsNullOrEmpty(RedirectStdOut))
                Command += " 1> " + RedirectStdOut;
            if (RedirectStdErr == "NUL" || RedirectStdErr == "/dev/null")
                Command += " 2> NUL";
            else if (RedirectStdErr == "STDOUT")
                Command += " 2>&1";
            else if (!string.IsNullOrEmpty(RedirectStdErr))
                Command += " 2> " + RedirectStdErr;

            var taskExec = new Microsoft.Build.Tasks.Exec()
            {
                BuildEngine = BuildEngine,
                HostObject = HostObject,
                WorkingDirectory = WorkingDirectory,
                Command = Command,
                IgnoreExitCode = IgnoreExitCode,
            };

            Log.LogMessage("\r\n==== HostExec: Microsoft.Build.Tasks.Exec");
            Log.LogMessage("WorkingDirectory: {0}", taskExec.WorkingDirectory);
            Log.LogMessage("Command: {0}", taskExec.Command);

            bool ok = taskExec.Execute();
            Log.LogMessage("== {0} ExitCode: {1}\r\n", ok ? "OK" : "FAIL", taskExec.ExitCode);

            ExitCode = taskExec.ExitCode;
            if (!ok)
                return false;
            #endregion

            return true;
        }
    }
}
#endregion
