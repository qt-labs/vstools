/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#region Task TaskName="GetVarsFromMakefile"

#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Build.Utilities;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK GetVarsFromMakefile
/////////////////////////////////////////////////////////////////////////////////////////////////
//
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class GetVarsFromMakefile
    {
        public static bool Execute(
        #region Parameters
            System.String Makefile,
            System.String[] ExcludeValues,
            out Microsoft.Build.Framework.ITaskItem[] OutVars,
            Microsoft.Build.Framework.ITaskItem[] VarDefs = null)
        #endregion
        {
            #region Code
            var makefileVars = Regex.Matches(
                File.ReadAllText(Makefile),
                @"^(\w+)[^\=\r\n\S]*\=[^\r\n\S]*([^\r\n]+)[\r\n]",
                RegexOptions.Multiline)
                .Cast<Match>()
                .Where(x => x.Groups.Count > 2 && x.Groups[1].Success && x.Groups[2].Success
                    && !string.IsNullOrEmpty(x.Groups[1].Value))
                .GroupBy(x => x.Groups[1].Value)
                .ToDictionary(g => g.Key, g => g.Last().Groups[2].Value);
            OutVars = VarDefs
                .Where(x => makefileVars.ContainsKey(x.GetMetadata("Name")))
                .Select(x => new TaskItem(x.ItemSpec, new Dictionary<string, string>
                { {
                    "Value",
                    string.Join(";", Regex
                        .Matches(makefileVars[x.GetMetadata("Name")], x.GetMetadata("Pattern"))
                        .Cast<Match>()
                        .Select(y => Regex
                            .Replace(y.Value, x.GetMetadata("Pattern"), x.GetMetadata("Value")))
                        .Where(y => !string.IsNullOrEmpty(y)
                            && !ExcludeValues.Contains(y,
                                StringComparer.InvariantCultureIgnoreCase))
                        .ToHashSet())
                } }))
                .Where(x => !string.IsNullOrEmpty(x.GetMetadata("Value")))
                .ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
