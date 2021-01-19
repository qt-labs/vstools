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

#region Task TaskName="Expand"

#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK Expand
/////////////////////////////////////////////////////////////////////////////////////////////////
// Expand a list of items, taking additional metadata from a base item and from a template item.
// Parameters:
//      in ITaskItem[] Items:    items to expand
//      in ITaskItem   BaseItem: base item from which the expanded items derive
//      in ITaskItem   Template = null: (optional) template containing metadata to add / update
//     out ITaskItem[] Result:   list of new items
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class Expand
    {
        public static bool Execute(
        #region Parameters
            out Microsoft.Build.Framework.ITaskItem[] Result,
            Microsoft.Build.Framework.ITaskItem[] Items,
            Microsoft.Build.Framework.ITaskItem BaseItem,
            Microsoft.Build.Framework.ITaskItem Template = null)
        #endregion
        {
            #region Code
            Result = new ITaskItem[] { };
            var reserved = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "AccessedTime", "CreatedTime", "DefiningProjectDirectory",
                "DefiningProjectExtension", "DefiningProjectFullPath", "DefiningProjectName",
                "Directory", "Extension", "Filename", "FullPath", "Identity", "ModifiedTime",
                "RecursiveDir", "RelativeDir", "RootDir",
            };
            var newItems = new List<ITaskItem>();
            foreach (var item in Items) {
                var newItem = new TaskItem(item);
                if (BaseItem != null)
                    BaseItem.CopyMetadataTo(newItem);
                var itemExt = newItem.GetMetadata("Extension");
                if (!string.IsNullOrEmpty(itemExt))
                    newItem.SetMetadata("Suffix", itemExt.Substring(1));
                if (Template != null) {
                    var metadataNames = Template.MetadataNames
                        .Cast<string>().Where(x => !reserved.Contains(x));
                    foreach (var metadataName in metadataNames) {
                        var metadataValue = Template.GetMetadata(metadataName);
                        newItem.SetMetadata(metadataName,
                            Regex.Replace(metadataValue, @"(%<)(\w+)(>)",
                            match => newItem.GetMetadata(match.Groups[2].Value)));
                    }
                }
                newItems.Add(newItem);
            }
            Result = newItems.ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
