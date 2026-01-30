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

        private List<XElement> ConvertQmlTypeRegistrarCustomBuilds(MsBuildProjectContainer qtMsBuild,
            IEnumerable<XElement> configurations, IReadOnlyCollection<CustomBuildEval> cbEvals,
            IReadOnlyDictionary<string, List<XElement>> projItemsByPath,
            IReadOnlyDictionary<string, List<XElement>> filterItemsByPath, string projDir)
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

        // Move all items of the given type into a dedicated ItemGroup (for clearer project layout).
        private void EnsureDedicatedItemGroup(string itemTypeName)
        {
            if (this[Files.Project].Xml.Root == null)
                return;

            var itemName = ns + itemTypeName;
            var itemGroups = this[Files.Project].Xml.Root
                .Elements(ns + "ItemGroup")
                .ToList();

            // Collect all items of the requested type, along with their current groups.
            var items = itemGroups
                .SelectMany(group => group.Elements(itemName)
                    .Select(item => new { Group = group, Item = item }))
                .ToList();

            if (items.Count == 0)
                return;

            // Reuse an existing group if it already contains only the requested item type.
            var targetGroup = itemGroups.FirstOrDefault(group =>
                group.Elements().All(e => e.Name == itemName) && group.Elements(itemName).Any());

            if (targetGroup == null) {
                var firstGroup = items.First().Group;
                targetGroup = new XElement(ns + "ItemGroup");
                firstGroup.AddBeforeSelf(targetGroup);
            }

            // Move items into the dedicated group.
            foreach (var entry in items.Where(entry => entry.Group != targetGroup)) {
                entry.Item.Remove();
                targetGroup.Add(entry.Item);
            }

            // Remove any now-empty groups.
            foreach (var group in itemGroups.Where(group => !group.Elements().Any()))
                group.Remove();
        }

        // Regroup filter entries for collect-json items into Generated Files filter.
        private void RegroupCollectJsonFilterItemsToGeneratedFiles()
        {
            var filtersProject = this[Files.Filters]?.Xml?.Elements(ns + "Project").ToList();
            if (filtersProject == null)
                return;

            const string generatedFilter = "Generated Files";

            // Build a set of QtMoc items that run in collect-json mode.
            var collectJsonItems = this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements(ns + QtMoc.ItemTypeName)
                .Where(x => x.Elements(ns + "CollectJson")
                    .Any(v => string.Equals(v.Value, "true", IgnoreCase)))
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            if (collectJsonItems.Count == 0)
                return;

            // Map file names to the current Include paths so we can update stale filter entries.
            var collectJsonByName = collectJsonItems
                .Select(x => new { Include = x, FileName = Path.GetFileName(x) })
                .Where(x => !string.IsNullOrEmpty(x.FileName))
                .GroupBy(x => x.FileName, StringComparer.InvariantCultureIgnoreCase)
                .Where(x => x.Count() == 1)
                .ToDictionary(x => x.Key, x => x.First().Include,
                    StringComparer.InvariantCultureIgnoreCase);

            // Retag matching filter entries to "Generated Files".
            var filterItems = filtersProject
                .Elements(ns + "ItemGroup")
                .Elements()
                .Where(x =>
                {
                    var include = x.Attribute("Include")?.Value ?? "";
                    if (collectJsonItems.Contains(include))
                        return true;
                    var fileName = Path.GetFileName(include);
                    return !string.IsNullOrEmpty(fileName)
                        && collectJsonByName.ContainsKey(fileName);
                })
                .ToList();

            foreach (var item in filterItems) {
                var include = item.Attribute("Include")?.Value ?? "";
                var fileName = Path.GetFileName(include);
                if (!string.IsNullOrEmpty(fileName)
                    && collectJsonByName.TryGetValue(fileName, out var updatedInclude)
                    && !string.Equals(include, updatedInclude, IgnoreCase)) {
                    item.SetAttributeValue("Include", updatedInclude);
                }

                // Ensure the filter item type matches the project item type.
                if (item.Name != ns + QtMoc.ItemTypeName)
                    item.Name = ns + QtMoc.ItemTypeName;

                if (item.Element(ns + "Filter") is { } filter)
                    filter.Value = generatedFilter;
            }

            // Remove any filter definitions that are no longer referenced.
            var referencedFilters = filtersProject
                .Elements(ns + "ItemGroup")
                .Elements()
                .Select(x => x.Element(ns + "Filter")?.Value)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            var filterDefinitions = filtersProject
                .Elements(ns + "ItemGroup")
                .Elements(ns + "Filter")
                .Where(x => !referencedFilters.Contains(x.Attribute("Include")?.Value ?? ""))
                .ToList();
            filterDefinitions.ForEach(x => x.Remove());
        }

        // Retarget root-level .cbt items to $(Configuration)\... without touching the filesystem.
        private void RetargetRootCbtIncludes()
        {
            var rootCbtItems = this[Files.Project].Xml
                .Elements(ns + "Project")
                .Elements(ns + "ItemGroup")
                .Elements()
                .Select(x => new { Element = x, Include = x.Attribute("Include")?.Value })
                .Where(x => !string.IsNullOrEmpty(x.Include)
                    && x.Include.EndsWith(".cbt", IgnoreCase)
                    && !x.Include.Contains("$(")
                    && string.Equals(Path.GetFileName(x.Include), x.Include, IgnoreCase))
                .ToList();

            if (rootCbtItems.Count == 0)
                return;

            // Retarget each distinct root-level include to a per-configuration path.
            foreach (var include in rootCbtItems
                .Select(x => x.Include)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(CaseIgnorer)) {
                var updatedInclude = @"$(Configuration)\" + include;
                foreach (var item in rootCbtItems.Where(x =>
                    string.Equals(x.Include, include, IgnoreCase)))
                    item.Element.SetAttributeValue("Include", updatedInclude);

                if (this[Files.Filters]?.Xml == null)
                    continue;

                // Keep the .filters file in sync, if present.
                var filterItems = this[Files.Filters].Xml
                    .Elements(ns + "Project")
                    .Elements(ns + "ItemGroup")
                    .Elements()
                    .Where(x => string.Equals(x.Attribute("Include")?.Value, include, IgnoreCase))
                    .ToList();
                filterItems.ForEach(x => x.SetAttributeValue("Include", updatedInclude));
            }
        }
    }
}
