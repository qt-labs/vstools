/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="HostTranslatePaths" Condition = "'$(ApplicationType)' != 'Linux'"

#region Using
using System.Linq;
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
    public static class HostTranslatePaths_Windows
    {
        public static bool Execute(
        #region Parameters
            Microsoft.Build.Framework.ITaskItem[] Items,
            out Microsoft.Build.Framework.ITaskItem[] Result,
            System.String[] Names = null)
        #endregion
        {
            #region Code
            Result = Items.ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
