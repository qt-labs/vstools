// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QtVsTools.Core.MsBuild
{
    using static Common.Utils;

    public partial class MsBuildProjectReaderWriter
    {
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
    }
}
