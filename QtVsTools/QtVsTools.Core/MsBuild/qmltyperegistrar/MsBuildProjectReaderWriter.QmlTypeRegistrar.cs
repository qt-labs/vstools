// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    using static Common.Utils;

    public partial class MsBuildProjectReaderWriter
    {
        // Preserve collect-json inputs and place outputs under the qmltyperegistrar int dir.
        private void ConfigureCollectJsonMocForQmlTypeRegistrar(MsBuildProjectContainer qtMsBuild,
            XElement qtMoc, string itemName, string configName, IEnumerable<CustomBuildEval> cbEvals)
        {
            // Preserve the JSON inputs from the original collect-json command
            // (the qmake custom build may carry them in the command or AdditionalInputs).
            var collectInputFile = qtMsBuild.GetPropertyChangedValue(QtMoc.Property.InputFile,
                itemName, configName);
            if (string.IsNullOrEmpty(collectInputFile)) {
                var cbEval = cbEvals.FirstOrDefault(x =>
                    string.Equals(x.ProjectConfig, configName, IgnoreCase)
                    && string.Equals(x.Identity, itemName, IgnoreCase));
                if (cbEval != null) {
                    using var evaluator = new MSBuildEvaluator(this[Files.Project]);
                    var tool = new QtMoc();
                    if (tool.ParseCommandLine(cbEval.Command, evaluator, out var properties)
                        && properties.TryGetValue(QtMoc.Property.InputFile, out var parsedInput)) {
                        collectInputFile = parsedInput;
                    }

                    if (string.IsNullOrEmpty(collectInputFile)
                        && !string.IsNullOrEmpty(cbEval.AdditionalInputs)) {
                        // Fallback: pick the first JSON input from AdditionalInputs.
                        collectInputFile = cbEval.AdditionalInputs
                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .FirstOrDefault(x => x.EndsWith(".json", IgnoreCase));
                    }
                }
            }

            if (!string.IsNullOrEmpty(collectInputFile)) {
                // Feed the JSON list through both InputFile and dependencies.
                qtMsBuild.SetItemProperty(qtMoc,
                    QtMoc.Property.InputFile, collectInputFile);
                qtMsBuild.SetItemProperty(qtMoc,
                    QtMoc.Property.AdditionalDependencies, collectInputFile);
            }

            // Place the collect-json marker alongside the qmltyperegistrar outputs.
            if (!string.IsNullOrEmpty(itemName)
                && itemName.EndsWith(".cbt", IgnoreCase)
                && !itemName.Contains("$(")
                && string.Equals(Path.GetFileName(itemName), itemName, IgnoreCase)) {
                var updatedInclude = @"$(QtIntDir)qmltyperegistrar\" + itemName;
                qtMoc.SetAttributeValue("Include", updatedInclude);
                qtMoc.Parent?.SetAttributeValue("Include", updatedInclude);
                if (this[Files.Filters]?.Xml != null) {
                    var filterItems = this[Files.Filters].Xml
                        .Elements(ns + "Project")
                        .Elements(ns + "ItemGroup")
                        .Elements(ns + QtMoc.ItemTypeName)
                        .Where(x => string.Equals(x.Attribute("Include")?.Value,
                            itemName, IgnoreCase))
                        .ToList();
                    filterItems.ForEach(x => x.SetAttributeValue("Include", updatedInclude));
                }
            }

            // Place the metatypes JSON output under the QtIntDir for qmltyperegistrar.
            var outputFile = qtMsBuild.GetPropertyChangedValue(QtMoc.Property.OutputFile, itemName,
                configName);
            if (!string.IsNullOrEmpty(outputFile)) {
                var outputDir = Path.GetDirectoryName(outputFile);
                var isProjectDir = string.Equals(outputDir, "$(ProjectDir)", IgnoreCase);
                var isProjectDirPrefixed = outputFile.StartsWith(@"$(ProjectDir)\", IgnoreCase)
                    || outputFile.StartsWith("$(ProjectDir)/", IgnoreCase);
                if (string.IsNullOrEmpty(outputDir) || isProjectDir || isProjectDirPrefixed) {
                    var fileName = Path.GetFileName(outputFile);
                    if (!string.IsNullOrEmpty(fileName)) {
                        qtMsBuild.SetItemProperty(qtMoc, QtMoc.Property.OutputFile,
                            @"$(QtIntDir)qmltyperegistrar\" + fileName);
                    }
                }
            }

            // Avoid inheriting OutputJson from project-level defaults.
            qtMsBuild.SetItemProperty(qtMoc, QtMoc.Property.OutputJson, "false");
        }

        private List<XElement> ConvertQmlTypeRegistrarCustomBuilds(
            MsBuildProjectContainer qtMsBuild,
            IEnumerable<XElement> configurations,
            string projDir,
            IEnumerable<CustomBuildEval> cbEvals,
            IReadOnlyDictionary<string, List<XElement>> projItemsByPath,
            IReadOnlyDictionary<string, List<XElement>> filterItemsByPath)
        {
            var qmlTypeRegistrarCustomBuilds = GetCustomBuilds(QtQmlTypeRegistrar.ToolExecName);
            if (!SetCommandLines(qtMsBuild, configurations, qmlTypeRegistrarCustomBuilds,
                QtQmlTypeRegistrar.ToolExecName, QtQmlTypeRegistrar.ItemTypeName,
                new ItemCommandLineReplacement[] { })) {
                return null;
            }

            foreach (var qtQmlTypeRegistrar in
                qmlTypeRegistrarCustomBuilds.Elements(ns + QtQmlTypeRegistrar.ItemTypeName)) {
                var itemName = (string)qtQmlTypeRegistrar.Attribute("Include");
                var configName = (string)qtQmlTypeRegistrar.Attribute("ConfigName");

                //remove items with generated files
                RemoveGeneratedFiles(projDir, cbEvals, configName, itemName,
                    projItemsByPath, filterItemsByPath);

                // Use the tool's parsed InputFile so the item identity matches the metatypes JSON.
                const string inputFileName = nameof(QtQmlTypeRegistrar.Property.InputFile);
                var inputFile = qtQmlTypeRegistrar.Element(ns + inputFileName)?.Value;
                if (string.IsNullOrEmpty(inputFile)) {
                    var cbEval = cbEvals.FirstOrDefault(x =>
                        string.Equals(x.ProjectConfig, configName, IgnoreCase)
                        && string.Equals(x.Identity, itemName, IgnoreCase));
                    if (cbEval != null) {
                        using var evaluator = new MSBuildEvaluator(this[Files.Project]);
                        var tool = new QtQmlTypeRegistrar();
                        if (tool.ParseCommandLine(cbEval.Command, evaluator, out var properties)
                            && properties.TryGetValue(
                                QtQmlTypeRegistrar.Property.InputFile, out var parsedInput)) {
                            inputFile = parsedInput;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(inputFile)) {
                    var inputDir = Path.GetDirectoryName(inputFile);
                    var isProjectDir = string.Equals(inputDir, "$(ProjectDir)", IgnoreCase);
                    var isProjectDirPrefixed = inputFile.StartsWith(@"$(ProjectDir)\", IgnoreCase)
                        || inputFile.StartsWith("$(ProjectDir)/", IgnoreCase);
                    if (string.IsNullOrEmpty(inputDir) || isProjectDir || isProjectDirPrefixed) {
                        var fileName = Path.GetFileName(inputFile);
                        if (!string.IsNullOrEmpty(fileName)) {
                            inputFile = @"$(QtIntDir)qmltyperegistrar\" + fileName;
                            qtMsBuild.SetItemProperty(qtQmlTypeRegistrar,
                                QtQmlTypeRegistrar.Property.InputFile, inputFile);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(inputFile)
                    && !string.Equals(inputFile, itemName, IgnoreCase)) {
                    // Keep project and filter includes in sync with the actual input file.
                    qtQmlTypeRegistrar.SetAttributeValue("Include", inputFile);
                    qtQmlTypeRegistrar.Parent?.SetAttributeValue("Include", inputFile);

                    var filterCustomBuild = (this[Files.Filters]?.Xml
                            ?.Elements(ns + "Project")
                            .Elements(ns + "ItemGroup")
                            .Elements(ns + "CustomBuild") ?? Array.Empty<XElement>())
                        .FirstOrDefault(filterItem =>
                            string.Equals(filterItem.Attribute("Include")?.Value,
                                itemName, IgnoreCase));
                    filterCustomBuild?.SetAttributeValue("Include", inputFile);
                }

                // Place qmake-generated outputs under the build output folder.
                var outputFile = qtMsBuild.GetPropertyChangedValue(
                    QtQmlTypeRegistrar.Property.OutputFile, itemName, configName);
                if (!string.IsNullOrEmpty(outputFile)
                    && !Path.IsPathRooted(outputFile)
                    && string.IsNullOrEmpty(Path.GetDirectoryName(outputFile))) {
                    qtMsBuild.SetItemProperty(qtQmlTypeRegistrar,
                        QtQmlTypeRegistrar.Property.OutputFile,
                        @"$(QtIntDir)qmltyperegistrar\" + outputFile);
                }

                var generateQmlTypes = qtMsBuild.GetPropertyChangedValue(
                    QtQmlTypeRegistrar.Property.GenerateQmlTypes, itemName, configName);
                if (!string.IsNullOrEmpty(generateQmlTypes)
                    && !Path.IsPathRooted(generateQmlTypes)
                    && string.IsNullOrEmpty(Path.GetDirectoryName(generateQmlTypes))) {
                    qtMsBuild.SetItemProperty(qtQmlTypeRegistrar,
                        QtQmlTypeRegistrar.Property.GenerateQmlTypes,
                        @"$(QtIntDir)qmltyperegistrar\" + generateQmlTypes);
                }

                //set properties
                qtMsBuild.SetItemProperty(qtQmlTypeRegistrar,
                    QtQmlTypeRegistrar.Property.ExecutionDescription,
                    "QmlTypeRegistrar %(Identity)...");
            }

            return qmlTypeRegistrarCustomBuilds;
        }
    }
}
