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

#region Task TaskName="ListQrc"

#region Reference
//System.Xml
//System.Xml.Linq
#endregion

#region Using
using System;
using System.Collections.Generic;
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
