/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace QtArchiveGen
{
    class Program
    {
        static int Main(string[] args)
        {
            Func<string, string> parseArgs = option => args.Where(s => s.StartsWith(option,
                StringComparison.Ordinal)).Select(s => s.Substring(option.Length)).FirstOrDefault();

            var EXIT_SUCCESS = 0;
            var EXIT_FAILURE = 1;
            try {
                using (var vsixToOpen = new FileStream(parseArgs("target="), FileMode.Open))
                using (var vsix = new ZipArchive(vsixToOpen, ZipArchiveMode.Update)) {

                    var vsixEntry = vsix.CreateEntry(Path.GetFileName(parseArgs("source=")));
                    if (vsixEntry == null)
                        return EXIT_FAILURE;

                    using (var sourceStream = new FileStream(parseArgs("source="), FileMode.Open))
                    using (var targetWriter = new BinaryWriter(vsixEntry.Open())) {
                        int bytesRead;
                        byte[] buffer = new byte[1024];
                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                            targetWriter.Write(buffer, 0, bytesRead);
                    }
                }
            } catch {
                return EXIT_FAILURE;
            }
            return EXIT_SUCCESS;
        }
    }
}
