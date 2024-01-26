/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="ListQrc"

#region Reference
//System.Xml
//System.Xml.Linq
#endregion

#region Using
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK ListQrc
/////////////////////////////////////////////////////////////////////////////////////////////////
// List resource paths in a QRC file.
// Parameters:
//      in string    QrcFilePath: path to QRC file
//     out string[]  Result: paths to files referenced in QRC
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class ListQrc
    {
        public static bool Execute(
        #region Parameters
            System.String QrcFilePath,
            out System.String[] Result)
        #endregion
        {
            #region Code
            Result = null;
            if (!File.Exists(QrcFilePath))
                return false;
            XDocument qrc = XDocument.Load(QrcFilePath, LoadOptions.SetLineInfo);
            IEnumerable<XElement> files = qrc
                .Element("RCC")
                .Elements("qresource")
                .Elements("file");
            Uri QrcPath = new Uri(QrcFilePath);
            Result = files
                .Select(x => new Uri(QrcPath, x.Value).LocalPath)
                .ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
