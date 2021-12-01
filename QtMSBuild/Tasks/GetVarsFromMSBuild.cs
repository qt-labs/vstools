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

#region Task TaskName="GetVarsFromMSBuild"

#region Reference
//System.Xml
//System.Xml.Linq
//System.Xml.XPath.XDocument
#endregion

#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Utilities;
#endregion

#region Comment
/////////////////////////////////////////////////////////////////////////////////////////////////
/// TASK GetVarsFromMSBuild
/////////////////////////////////////////////////////////////////////////////////////////////////
//
#endregion

namespace QtVsTools.QtMsBuild.Tasks
{
    public static class GetVarsFromMSBuild
    {
        public static bool Execute(
        #region Parameters
            System.String Project,
            Microsoft.Build.Framework.ITaskItem[] VarDefs,
            System.String[] ExcludeValues,
            out Microsoft.Build.Framework.ITaskItem[] OutVars)
        #endregion
        {
            #region Code
            OutVars = null;
            var result = new List<Microsoft.Build.Framework.ITaskItem>();
            var projectXml = XDocument.Load(Project);
            projectXml.Descendants().ToList().ForEach(x => x.Name = x.Name.LocalName);
            foreach (var varDef in VarDefs) {
                string varName = varDef.GetMetadata("Name");
                string varXPath = varDef.GetMetadata("XPath");
                string varValue = "";
                var valueElement = projectXml.XPathSelectElement(varXPath);
                if (valueElement != null) {
                    var propertyName = valueElement.Name.LocalName;
                    varValue = Regex.Replace(valueElement.Value,
                        string.Format(@"[ ;][%$]\({0}\)$", propertyName),
                        string.Empty);
                }
                if (!ExcludeValues.Contains(varValue, StringComparer.InvariantCultureIgnoreCase)) {
                    var outVar = new TaskItem(varName.Trim());
                    outVar.SetMetadata("Value", varValue.Trim());
                    result.Add(outVar);
                }
            }
            OutVars = result.ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
