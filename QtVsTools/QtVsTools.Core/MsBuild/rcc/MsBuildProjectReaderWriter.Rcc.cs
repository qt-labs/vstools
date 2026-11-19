// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    using static Common.Utils;

    public partial class MsBuildProjectReaderWriter
    {
        private List<XElement> ConvertRccCustomBuilds(
            MsBuildProjectContainer qtMsBuild,
            IEnumerable<XElement> configurations,
            string projDir,
            IEnumerable<CustomBuildEval> cbEvals,
            IReadOnlyDictionary<string, List<XElement>> projItemsByPath,
            IReadOnlyDictionary<string, List<XElement>> filterItemsByPath)
        {
            var rccCustomBuilds = GetCustomBuilds(QtRcc.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configurations, rccCustomBuilds,
                QtRcc.ToolExecName, QtRcc.ItemTypeName,
                new ItemCommandLineReplacement[]
                {
                    (item, cmdLine) => cmdLine.Replace(
                        $@"\qrc_{Path.GetFileNameWithoutExtension(item)}.cpp",
                        @"\qrc_%(Filename).cpp", IgnoreCase)
                    .Replace(
                        $" -o qrc_{Path.GetFileNameWithoutExtension(item)}.cpp",
                        @" -o $(ProjectDir)\qrc_%(Filename).cpp", IgnoreCase)
                })) {
                return null;
            }

            foreach (var qtRcc in rccCustomBuilds.Elements(ns + QtRcc.ItemTypeName)) {
                var itemName = (string)qtRcc.Attribute("Include");
                var configName = (string)qtRcc.Attribute("ConfigName");

                // remove items with generated files
                RemoveGeneratedFiles(projDir, cbEvals, configName, itemName,
                    projItemsByPath, filterItemsByPath);

                // set properties
                qtMsBuild.SetItemProperty(qtRcc,
                    QtRcc.Property.ExecutionDescription, "Rcc'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtRcc,
                    QtRcc.Property.InputFile, "%(FullPath)");
            }

            return rccCustomBuilds;
        }
    }
}
