/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="Flatten"

#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK Flatten
/////////////////////////////////////////////////////////////////////////////////////////////////
// Destructure items into a "flat" list of metadata. The output is a list of (Name, Value) pairs,
// each corresponding to one item metadata. Semi-colon-separated lists will also expand to many
// items in the output list, with the metadata name shared among them.
// Example:
//     INPUT:
//         <QtMoc>
//           <InputFile>foo.h</InputFile>
//           <IncludePath>C:\FOO;D:\BAR</IncludePath>
//         </QtMoc>
//     OUTPUT:
//         <Result>
//           <Name>InputFile</Name>
//           <Value>foo.h</Value>
//         </Result>
//         <Result>
//           <Name>IncludePath</Name>
//           <Value>C:\FOO</Value>
//         </Result>
//         <Result>
//           <Name>IncludePath</Name>
//           <Value>D:\BAR</Value>
//         </Result>
// Parameters:
//      in ITaskItem[] Items:    list of items to flatten
//      in string[]    Metadata: names of metadata to look for; omit to include all metadata
//     out ITaskItem[] Result:   list of metadata from all items
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class Flatten
    {
        public static bool Execute(
        #region Parameters
            Microsoft.Build.Framework.ITaskItem[] Items,
            out Microsoft.Build.Framework.ITaskItem[] Result,
            System.String[] Metadata = null)
        #endregion
        {
            #region Code
            Result = new ITaskItem[] { };
            var reserved = new HashSet<string>
            {
                "AccessedTime", "CreatedTime", "DefiningProjectDirectory",
                "DefiningProjectExtension", "DefiningProjectFullPath", "DefiningProjectName",
                "Directory", "Extension", "Filename", "FullPath", "Identity", "ModifiedTime",
                "RecursiveDir", "RelativeDir", "RootDir",
            };
            if (Metadata == null)
                Metadata = Array.Empty<string>();
            var requestedNames = new HashSet<string>(Metadata.Where(x => !string.IsNullOrEmpty(x)));
            var newItems = new List<ITaskItem>();
            foreach (var item in Items) {
                var itemName = item.ItemSpec;
                var names = item.MetadataNames.Cast<string>().Where(x => !reserved.Contains(x)
                    && (!requestedNames.Any() || requestedNames.Contains(x)));
                foreach (string name in names) {
                    var values = item.GetMetadata(name).Split(';');
                    foreach (string value in values.Where(v => !string.IsNullOrEmpty(v))) {
                        newItems.Add(new TaskItem(string.Format("{0}={1}", name, value),
                            new Dictionary<string, string>
                            {
                                { "Item",  itemName },
                                { "Name",  name },
                                { "Value", value },
                            }));
                    }
                }
            }
            Result = newItems.ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
