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
