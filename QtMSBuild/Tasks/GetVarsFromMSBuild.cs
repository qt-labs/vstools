/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
