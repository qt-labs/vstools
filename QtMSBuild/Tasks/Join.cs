/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="Join"

#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK Join
/////////////////////////////////////////////////////////////////////////////////////////////////
// Combines two lists of items into a single list containing items with common metadata values.
// Example:
//     INPUT:
//         Left = <LeftItem><X>foo</X><Y>42</Y></LeftItem>
//                <LeftItem><X>sna</X><Y>99</Y></LeftItem>
//                <LeftItem><X>bar</X><Y>3.14159</Y></LeftItem>
//         Right = <RightItem><Z>foo</Z><Y>99</Y></RightItem>
//                 <RightItem><Z>sna</Z><Y>2.71828</Y></RightItem>
//                 <RightItem><Z>bar</Z><Y>42</Y></RightItem>
//                 <RightItem><Z>bar</Z><Y>99</Y></RightItem>
//     OUTPUT:
//         Result = <Item><X>foo</X><Y>42</Y><Z>bar</Z></Item>
//                  <Item><X>sna</X><Y>99</Y><Z>foo</Z></Item>
//                  <Item><X>sna</X><Y>99</Y><Z>bar</Z></Item>
// Parameters:
//            in ITaskItem[]  LeftItems: first list of items to join
//            in ITaskItem[] RightItems: second list of items to join
//           out ITaskItem[]     Result: resulting list of items with common metadata
//   optional in    string[]         On: join criteria, list of names of metadata to match in both
//                                       input lists; all metadata in the criteria must be matched;
//                                       by default, %(Identity) will be used as criteria; the
//                                       special value 'ROW_NUMBER' will match the position of items
//                                       (i.e. items with the same index in each input list).
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class Join
    {
        public static bool Execute(
        #region Parameters
            Microsoft.Build.Framework.ITaskItem[] LeftItems,
            Microsoft.Build.Framework.ITaskItem[] RightItems,
            out Microsoft.Build.Framework.ITaskItem[] Result,
            System.String[] On = null)
        #endregion
        {
            #region Code
            Result = new ITaskItem[] { };
            var resultItems = new List<ITaskItem>();
            List<string> matchFields = null;
            if (On != null)
                matchFields = new List<string>(On);
            if (matchFields == null || !matchFields.Any())
                matchFields = new List<string> { "Identity" };

            var reserved = new HashSet<string>
            {
                "AccessedTime", "CreatedTime", "DefiningProjectDirectory",
                "DefiningProjectExtension", "DefiningProjectFullPath", "DefiningProjectName",
                "Directory", "Extension", "Filename", "FullPath", "Identity", "ModifiedTime",
                "RecursiveDir", "RelativeDir", "RootDir"
            };

            for (int leftRowNum = 0; leftRowNum < LeftItems.Length; leftRowNum++) {
                for (int rightRowNum = 0; rightRowNum < RightItems.Length; rightRowNum++) {
                    var leftItem = LeftItems[leftRowNum];
                    var rightItem = RightItems[rightRowNum];
                    bool match = true;
                    foreach (string field in matchFields) {
                        if (field.Equals("ROW_NUMBER", StringComparison.OrdinalIgnoreCase)) {
                            match = leftRowNum == rightRowNum;
                        } else {
                            string leftField = leftItem.GetMetadata(field);
                            string rightField = rightItem.GetMetadata(field);
                            match = leftField.Equals(rightField, StringComparison.OrdinalIgnoreCase);
                        }
                        if (!match)
                            break;
                    }
                    if (match) {
                        var resultItem = new TaskItem(leftItem.ItemSpec);
                        foreach (string rightMetadata in rightItem.MetadataNames) {
                            if (!reserved.Contains(rightMetadata)) {
                                var metadataValue = rightItem.GetMetadata(rightMetadata);
                                if (!string.IsNullOrEmpty(metadataValue))
                                    resultItem.SetMetadata(rightMetadata, metadataValue);
                            }
                        }
                        foreach (string leftMetadata in leftItem.MetadataNames) {
                            if (!reserved.Contains(leftMetadata)) {
                                var metadataValue = leftItem.GetMetadata(leftMetadata);
                                if (!string.IsNullOrEmpty(metadataValue))
                                    resultItem.SetMetadata(leftMetadata, metadataValue);
                            }
                        }
                        resultItems.Add(resultItem);
                    }
                }
            }

            Result = resultItems.ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
