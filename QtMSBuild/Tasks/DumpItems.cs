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

#region Task TaskName="DumpItems"

#region Reference
#endregion

#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK DumpItems
/////////////////////////////////////////////////////////////////////////////////////////////////
// Dump contents of items as a log message. The contents are formatted as XML.
// Parameters:
//      in string       ItemType:     type of the items; this is used as the parent node of each
//                                    item dump
//      in ITaskItem[]  Items:        items to dump
//      in bool         DumpReserved: include MSBuild reserved metadata in dump?
//      in string       Metadata:     list of names of metadata to include in dump; omit to
//                                    include all metadata
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class DumpItems
    {
        public static Microsoft.Build.Utilities.TaskLoggingHelper Log { get; set; }

        public static bool Execute(
        #region Parameters
            System.String ItemType,
            Microsoft.Build.Framework.ITaskItem[] Items,
            System.Boolean DumpReserved = false,
            System.String Metadata = null)
        #endregion
        {
            #region Code
            var reserved = new HashSet<string>
            {
                "AccessedTime", "CreatedTime", "DefiningProjectDirectory",
                "DefiningProjectExtension", "DefiningProjectFullPath", "DefiningProjectName",
                "Directory", "Extension", "Filename", "FullPath", "Identity", "ModifiedTime",
                "RecursiveDir", "RelativeDir", "RootDir",
            };
            if (Metadata == null)
                Metadata = "";
            var requestedNames = new HashSet<string>(Metadata.Split(new[] { ';' },
                StringSplitOptions.RemoveEmptyEntries));
            var itemXml = new StringBuilder();
            if (Items.Any()) {
                foreach (var item in Items) {
                    if (itemXml.Length > 0)
                        itemXml.Append("\r\n");
                    itemXml.AppendFormat("<{0} Include=\"{1}\"", ItemType, item.ItemSpec);
                    var names = item.MetadataNames.Cast<string>()
                        .Where(x => (DumpReserved || !reserved.Contains(x))
                        && (!requestedNames.Any() || requestedNames.Contains(x)))
                        .OrderBy(x => x);
                    if (names.Any()) {
                        itemXml.Append(">\r\n");
                        foreach (string name in names) {
                            if (!DumpReserved && reserved.Contains(name))
                                continue;
                            if (!item.MetadataNames.Cast<string>().Contains(name))
                                continue;
                            var value = item.GetMetadata(name);
                            if (!string.IsNullOrEmpty(value))
                                itemXml.AppendFormat("  <{0}>{1}</{0}>\r\n", name, value);
                            else
                                itemXml.AppendFormat("  <{0}/>\r\n", name);
                        }
                        itemXml.AppendFormat("</{0}>", ItemType);
                    } else {
                        itemXml.Append("/>");
                    }
                }
            } else {
                itemXml.AppendFormat("<{0}/>", ItemType);
            }
            Log.LogMessage(MessageImportance.High, itemXml.ToString());
            #endregion

            return true;
        }
    }
}
#endregion
