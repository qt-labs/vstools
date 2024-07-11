/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
                if (ExcludeValues != null && ExcludeValues.Length > 0) {
                    var varListValue = varValue.Split(';');
                    var includedValues = new List<string>();
                    var excludedValuesByType = ExcludeValues.GroupBy(x => x.EndsWith("*"));
                    var excludedExactValues = excludedValuesByType
                        .Where(x => !x.Key).SelectMany(x => x);
                    var excludedPrefixes = excludedValuesByType
                        .Where(x => x.Key).SelectMany(x => x)
                        .Select(x => x.Substring(0, x.Length - 1));
                    foreach (var value in varListValue) {
                        if (excludedExactValues
                            .Any(x => string.Equals(value, x, StringComparison.OrdinalIgnoreCase))) {
                            continue;
                        }
                        if (excludedPrefixes
                            .Any(x => value.StartsWith(x, StringComparison.OrdinalIgnoreCase))) {
                            continue;
                        }
                        includedValues.Add(value);
                    }
                    varValue = string.Join(";", includedValues);
                }
                var outVar = new TaskItem(varName.Trim());
                outVar.SetMetadata("Value", varValue.Trim());
                result.Add(outVar);
            }
            OutVars = result.ToArray();
            #endregion

            return true;
        }
    }
}
#endregion
