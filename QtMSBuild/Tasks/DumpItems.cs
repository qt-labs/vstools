/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
        public static QtMSBuild.ITaskLoggingHelper Log { get; set; }

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
                "RecursiveDir", "RelativeDir", "RootDir"
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
                        .OrderBy(x => x)
                        .ToList();
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
