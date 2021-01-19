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

#region Task TaskName="Flatten"

#region Using
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
                Metadata = new string[0];
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
