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

#region Task TaskName="ParseVarDefs"

#region Using
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK ParseVarDefs
/////////////////////////////////////////////////////////////////////////////////////////////////
//
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class ParseVarDefs
    {
        public static bool Execute(
        #region Parameters
            System.String QtVars,
            out Microsoft.Build.Framework.ITaskItem[] OutVarDefs
            )
        #endregion
        {
            #region Code
            OutVarDefs = Regex.Matches(QtVars,
                @"\s*(\w+)\s*(?:;|=\s*(\w*)\s*(?:\/((?:\\.|[^;\/])*)\/((?:\\.|[^;\/])*)\/)?)?")
                .Cast<Match>()
                .Where((Match x) => x.Groups.Count > 4 && !string.IsNullOrEmpty(x.Groups[1].Value))
                .Select((Match x) => x.Groups
                      .Cast<Group>()
                      .Select((Group y) => !string.IsNullOrEmpty(y.Value) ? y.Value : null)
                      .ToArray())
                .Select((string[] x) => new TaskItem(x[1],
                    new Dictionary<string, string>
                    {
                        { "Name" ,    x[2] ?? x[1] },
                        { "Pattern" , x[3] ?? ".*" },
                        { "Value" ,   x[4] ?? "$0" },
                    }))
                .ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
