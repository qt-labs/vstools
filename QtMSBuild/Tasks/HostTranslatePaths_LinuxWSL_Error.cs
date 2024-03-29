/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#region Task TaskName="HostTranslatePaths" Condition="('$(VisualStudioVersion)' != '16.0' AND '$(VisualStudioVersion)' != '17.0') AND '$(ApplicationType)' == 'Linux' AND '$(PlatformToolset)' == 'WSL_1_0'"

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class HostTranslatePaths_LinuxWSL_Error
    {
        public static QtMSBuild.ITaskLoggingHelper Log { get; set; }

        public static bool Execute(
        #region Parameters
            Microsoft.Build.Framework.ITaskItem[] Items,
            out Microsoft.Build.Framework.ITaskItem[] Result,
            System.String[] Names = null)
        #endregion
        {
            #region Code
            Result = null;
            Log.LogError("Cross-compilation of Qt projects in WSL not supported.");
            return false;
            #endregion
        }
    }
}
#endregion
