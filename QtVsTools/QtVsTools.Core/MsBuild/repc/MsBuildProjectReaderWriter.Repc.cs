// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    public partial class MsBuildProjectReaderWriter
    {
        private List<XElement> ConvertRepcCustomBuilds(
            MsBuildProjectContainer qtMsBuild,
            IEnumerable<XElement> configurations,
            string projDir,
            IEnumerable<CustomBuildEval> cbEvals,
            IReadOnlyDictionary<string, List<XElement>> projItemsByPath,
            IReadOnlyDictionary<string, List<XElement>> filterItemsByPath)
        {
            var repcCustomBuilds = GetCustomBuilds(QtRepc.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configurations, repcCustomBuilds,
                QtRepc.ToolExecName, QtRepc.ItemTypeName,
                new ItemCommandLineReplacement[] { })) {
                return null;
            }

            foreach (var qtRepc in repcCustomBuilds.Elements(ns + QtRepc.ItemTypeName)) {
                var itemName = (string)qtRepc.Attribute("Include");
                var configName = (string)qtRepc.Attribute("ConfigName");

                // remove items with generated files
                RemoveGeneratedFiles(projDir, cbEvals, configName, itemName,
                    projItemsByPath, filterItemsByPath);

                // set properties
                qtMsBuild.SetItemProperty(qtRepc,
                    QtRepc.Property.ExecutionDescription, "Repc'ing %(Identity)...");
                qtMsBuild.SetItemProperty(qtRepc,
                    QtRepc.Property.InputFile, "%(FullPath)");
            }

            return repcCustomBuilds;
        }
    }
}
