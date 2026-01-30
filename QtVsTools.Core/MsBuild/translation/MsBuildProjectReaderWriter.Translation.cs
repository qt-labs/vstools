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
        private List<XElement> ConvertQmCustomBuilds(
            MsBuildProjectContainer qtMsBuild,
            IEnumerable<XElement> configurations,
            string projDir,
            IEnumerable<CustomBuildEval> cbEvals,
            IReadOnlyDictionary<string, List<XElement>> projItemsByPath,
            IReadOnlyDictionary<string, List<XElement>> filterItemsByPath)
        {
            var qmCustomBuilds = GetCustomBuilds(QtLRelease.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configurations, qmCustomBuilds,
                QtLRelease.ToolExecName, QtLRelease.ItemTypeName,
                new ItemCommandLineReplacement[]
                {
                    (item, cmdLine) => cmdLine.Replace(
                        $@"{Path.GetFileNameWithoutExtension(item)}.ts",
                        @"%(Filename).ts", IgnoreCase)
                    .Replace(
                        $"{Path.GetFileNameWithoutExtension(item)}.qm",
                        @"%(Filename).qm", IgnoreCase)
                })) {
                return null;
            }

            foreach (var qtQm in qmCustomBuilds.Elements(ns + QtLRelease.ItemTypeName)) {
                var itemName = (string)qtQm.Attribute("Include");
                var configName = (string)qtQm.Attribute("ConfigName");

                // remove items with generated files
                RemoveGeneratedFiles(projDir, cbEvals, configName, itemName, projItemsByPath,
                    filterItemsByPath);

                // set properties
                qtMsBuild.SetItemProperty(qtQm, QtLRelease.Property.ReleaseDescription,
                    "lrelease %(Identity)");

                qtMsBuild.SetItemProperty(qtQm, QtLRelease.Property.BuildAction, "lrelease");
                qtMsBuild.SetItemProperty(qtQm, QtLRelease.Property.InputFile, "%(FullPath)");
            }

            return qmCustomBuilds;
        }
    }
}
