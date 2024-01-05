/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="HostExec" Condition="('$(VisualStudioVersion)' != '16.0' AND '$(VisualStudioVersion)' != '17.0') AND '$(ApplicationType)' == 'Linux' AND '$(PlatformToolset)' == 'WSL_1_0'"

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class HostExec_LinuxWSL_Error
    {
        public static QtMSBuild.ITaskLoggingHelper Log { get; set; }

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
            ExitCode = 1;
            Log.LogError("Cross-compilation of Qt projects in WSL not supported.");
            return false;
            #endregion
        }
    }
}
#endregion
