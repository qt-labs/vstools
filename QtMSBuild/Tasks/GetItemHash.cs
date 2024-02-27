/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#region Task TaskName="GetItemHash"

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK GetItemHash
/////////////////////////////////////////////////////////////////////////////////////////////////
// Calculate an hash code (Deflate + Base64) for an item, given a list of metadata to use as key
// Parameters:
//      in ITaskItem Item: item for which the hash will be calculated
//      in string[]  Keys: list of names of the metadata to use as item key
//     out string    Hash: hash code (Base64 representation of Deflate'd UTF-8 item key)
#endregion

#region Using
using System;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class GetItemHash
    {
        public static bool Execute(
        #region Parameters
            Microsoft.Build.Framework.ITaskItem Item,
            System.String[] Keys,
            out System.String Hash)
        #endregion
        {
            #region Code
            var data = Encoding.UTF8.GetBytes(string.Concat(Keys.OrderBy(x => x)
                .Select(x => string.Format("[{0}={1}]", x, Item.GetMetadata(x))))
                .ToUpper());
            using (var dataZipped = new MemoryStream()) {
                using (var zip = new DeflateStream(dataZipped, CompressionLevel.Fastest))
                    zip.Write(data, 0, data.Length);
                Hash = Convert.ToBase64String(dataZipped.ToArray());
            }
            #endregion

            return true;
        }
    }
}
#endregion
