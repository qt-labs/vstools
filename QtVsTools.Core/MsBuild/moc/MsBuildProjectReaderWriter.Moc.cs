// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    using static HelperFunctions;

    using static Common.Utils;

    public partial class MsBuildProjectReaderWriter
    {
        private List<XElement> ConvertMocCustomBuilds(
            MsBuildProjectContainer qtMsBuild,
            IEnumerable<XElement> configurations,
            string projDir,
            IEnumerable<CustomBuildEval> cbEvals,
            IReadOnlyDictionary<string, List<XElement>> projItemsByPath,
            IReadOnlyDictionary<string, List<XElement>> filterItemsByPath,
            ICollection<XElement> mocDisableDynamicSource)
        {
            var mocCustomBuilds = GetCustomBuilds(QtMoc.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configurations, mocCustomBuilds,
                QtMoc.ToolExecName, QtMoc.ItemTypeName,
                new ItemCommandLineReplacement[]
                {
                    (item, cmdLine) => cmdLine.Replace(
                            $@"\moc_{Path.GetFileNameWithoutExtension(item)}.cpp",
                        @"\moc_%(Filename).cpp", IgnoreCase)
                    .Replace($" -o moc_{Path.GetFileNameWithoutExtension(item)}.cpp",
                        @" -o $(ProjectDir)\moc_%(Filename).cpp", IgnoreCase),

                    (item, cmdLine) => cmdLine.Replace(
                            $@"\{Path.GetFileNameWithoutExtension(item)}.moc",
                        @"\%(Filename).moc", IgnoreCase)
                    .Replace($" -o {Path.GetFileNameWithoutExtension(item)}.moc",
                        @" -o $(ProjectDir)\%(Filename).moc", IgnoreCase)
                })) {
                return null;
            }

            foreach (var qtMoc in mocCustomBuilds.Elements(ns + QtMoc.ItemTypeName)) {
                var itemName = (string)qtMoc.Attribute("Include");
                var configName = (string)qtMoc.Attribute("ConfigName");

                //remove items with generated files
                var hasGeneratedFiles = RemoveGeneratedFiles(
                    projDir, cbEvals, configName, itemName,
                    projItemsByPath, filterItemsByPath);

                //set properties
                qtMsBuild.SetItemProperty(qtMoc,
                    QtMoc.Property.ExecutionDescription, "Moc'ing %(Identity)...");

                // CollectJson runs in a separate moc mode that consumes JSON inputs,
                // so we must preserve/restore the input list and avoid OutputJson.
                var collectJsonValue = qtMsBuild.GetPropertyChangedValue(
                    QtMoc.Property.CollectJson, itemName, configName);
                if (string.IsNullOrEmpty(collectJsonValue))
                    collectJsonValue = qtMoc.Element(ns + nameof(QtMoc.Property.CollectJson))?.Value;

                var isCollectJson = string.Equals(collectJsonValue, "true", IgnoreCase);
                if (isCollectJson) {
                    ConfigureCollectJsonMocForQmlTypeRegistrar(qtMsBuild, qtMoc, itemName,
                        configName, cbEvals);
                } else {
                    qtMsBuild.SetItemProperty(qtMoc, QtMoc.Property.InputFile, "%(FullPath)");
                }

                if (!IsSourceFile(itemName)) {
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.DynamicSource, "output");
                    if (!hasGeneratedFiles)
                        mocDisableDynamicSource.Add(qtMoc);
                } else {
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.DynamicSource, "input");
                }
                var includePath = qtMsBuild.GetPropertyChangedValue(
                    QtMoc.Property.IncludePath, itemName, configName);
                if (!string.IsNullOrEmpty(includePath)) {
                    qtMsBuild.SetItemProperty(qtMoc,
                        QtMoc.Property.IncludePath, AddGeneratedFilesPath(includePath));
                }
            }

            return mocCustomBuilds;
        }
    }
}
