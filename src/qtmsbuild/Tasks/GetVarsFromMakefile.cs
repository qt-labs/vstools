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
