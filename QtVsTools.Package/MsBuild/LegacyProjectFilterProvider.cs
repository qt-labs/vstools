// ************************************************************************************************
// Copyright (C) 2023 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
// ************************************************************************************************

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools.Package.MsBuild
{
    using Core;
    using VisualStudio;

    [SolutionTreeFilterProvider(QtMenus.Package.GuidString, QtMenus.Package.LegacyProjectFilter)]
    internal class LegacyProjectFilterProvider : HierarchyTreeFilterProvider
    {
        private readonly IVsHierarchyItemCollectionProvider hierarchyCollectionProvider;

        [ImportingConstructor]
        public LegacyProjectFilterProvider(IVsHierarchyItemCollectionProvider provider)
        {
            hierarchyCollectionProvider = provider;
        }

        protected override HierarchyTreeFilter CreateFilter()
        {
            return new Filter(hierarchyCollectionProvider);
        }

        private sealed class Filter : HierarchyTreeFilter
        {
            private readonly IVsHierarchyItemCollectionProvider hierarchyCollectionProvider;

            public Filter(IVsHierarchyItemCollectionProvider provider)
            {
                hierarchyCollectionProvider = provider;
            }

            protected override async Task<IReadOnlyObservableSet> GetIncludedItemsAsync(
                IEnumerable<IVsHierarchyItem> rootItems)
            {
                var root = HierarchyUtilities.FindCommonAncestor(rootItems);
                var sourceItems = await hierarchyCollectionProvider.GetDescendantsAsync(
                    root.HierarchyIdentity.NestedHierarchy, CancellationToken);

                return await hierarchyCollectionProvider.GetFilteredHierarchyItemsAsync(
                    sourceItems, ShouldIncludeInFilter, CancellationToken);
            }

            private static bool ShouldIncludeInFilter(IVsHierarchyItem item)
            {
                if (item?.HierarchyIdentity is not {} identity)
                    return false;
                if (!HierarchyUtilities.IsProject(identity))
                    return false;
                if (VsShell.GetProject(identity.NestedHierarchy) is not {} vcProject)
                    return false;
                return MsBuildProjectFormat.GetVersion(vcProject) is
                    >= MsBuildProjectFormat.Version.V1 and < MsBuildProjectFormat.Version.Latest;
            }
        }
    }
}
