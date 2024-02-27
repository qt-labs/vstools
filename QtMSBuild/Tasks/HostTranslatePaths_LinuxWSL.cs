/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#region Task TaskName="HostTranslatePaths" Condition="('$(VisualStudioVersion)' == '16.0' OR '$(VisualStudioVersion)' == '17.0') AND '$(ApplicationType)' == 'Linux' AND '$(PlatformToolset)' == 'WSL_1_0'"

#region Reference
//$(VCTargetsPath)\Application Type\Linux\1.0\liblinux.dll
#endregion

#region Using
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using liblinux.IO;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK HostTranslatePaths
///  * Linux build over WSL
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
    public static class HostTranslatePaths_LinuxWSL
    {
        public static bool Execute(
        #region Parameters
            Microsoft.Build.Framework.ITaskItem[] Items,
            out Microsoft.Build.Framework.ITaskItem[] Result,
            System.String[] Names = null)
        #endregion
        {
#if (VS2019 || VS2022)
            #region Code
            Result = new ITaskItem[] { };
            var newItems = new List<ITaskItem>();
            foreach (var item in Items) {
                string itemName = item.GetMetadata("Name");
                string itemValue = item.GetMetadata("Value");
                if (Names.Contains(itemName)) {
                    if (Path.IsPathRooted(itemValue) && !itemValue.StartsWith("/"))
                        itemValue = PathUtils.TranslateWindowsPathToWSLPath(itemValue);
                    else
                        itemValue = itemValue.Replace(@"\", "/");
                }
                newItems.Add(new TaskItem(item.ItemSpec,
                    new Dictionary<string, string>
                    {
                      { "Item",  item.GetMetadata("Item") },
                      { "Name",  itemName },
                      { "Value", itemValue }
                    }));
            }
            Result = newItems.ToArray();
            #endregion
#else
            Result = null;
#endif
            return true;
        }
    }
}
#endregion
