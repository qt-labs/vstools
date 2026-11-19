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
