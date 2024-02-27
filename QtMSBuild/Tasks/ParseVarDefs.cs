/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

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
                .Where(x => x.Groups.Count > 4 && !string.IsNullOrEmpty(x.Groups[1].Value))
                .Select(x => x.Groups
                      .Cast<Group>()
                      .Select(y => !string.IsNullOrEmpty(y.Value) ? y.Value : null)
                      .ToArray())
                .Select(x => new TaskItem(x[1],
                    new Dictionary<string, string>
                    {
                        { "Name" ,    x[2] ?? x[1] },
                        { "Pattern" , x[3] ?? ".*" },
                        { "Value" ,   x[4] ?? "$0" }
                    }))
                .ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
