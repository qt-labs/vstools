/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core
{
    using Common;

    public static class VcFilterExtensions
    {
        public static bool IsInFilter(this VCFile vcFile, FakeFilter fakeFilter)
        {
            try {
                return vcFile.project is VCProject vcProject
                 && IsInFilter(vcFile, vcProject.FilterFromGuid(fakeFilter));
            } catch { return false; }
        }

        public static bool IsInFilter(this VCFile vcFile, VCFilter filter)
        {
            var item = vcFile as VCProjectItem;
            while (item is {Parent: not null, Kind: not "VCProject"}) {
                item = item.Parent as VCProjectItem;
                if (item?.Kind != "VCFilter")
                    continue;
                if (item is not VCFilter {UniqueIdentifier: {} uniqueIdentifier})
                    continue;
                if (string.Equals(uniqueIdentifier, filter?.UniqueIdentifier, Utils.IgnoreCase))
                    return true;
            }
            return false;
        }

        public static void DeleteAndRemoveFromFilter(this VCFile vcFile, FakeFilter fakeFilter)
        {
            if (vcFile.project is not VCProject vcProject)
                return;
            DeleteAndRemoveFromFilter(vcFile,
                vcProject.FilterFromGuid(fakeFilter) ?? vcProject.FilterFromName(fakeFilter));
        }


        public static void DeleteAndRemoveFromFilter(this VCFile vcFile, VCFilter filter)
        {
            var fullPath = vcFile?.FullPath ?? "";
            try {
                if (vcFile.IsInFilter(filter)) {
                    filter.RemoveFile(vcFile);
                    Utils.DeleteFile(fullPath);
                }
            } catch { }

            if (filter.Filters is not IVCCollection subFilters)
                return;
            for (var i = subFilters.Count; i > 0; i--) {
                try {
                    DeleteAndRemoveFromFilter(vcFile, subFilters.Item(i) as VCFilter);
                } catch { }
            }
        }

        public static void MoveToFilter(this VCFile vcFile, FakeFilter fakeFilter)
        {
            if (vcFile is not { project: VCProject vcProject })
                return;
            if (vcFile.IsInFilter(fakeFilter))
                return;

            if (vcProject.Filters is not IVCCollection filters)
                return;

            foreach (VCFilter filter in filters) {
                if (!vcFile.IsInFilter(filter))
                    continue;

                // We need to get the path early, since removing a VCFile from an
                // filter disposes the object and we will get an disposed exception.
                var fullPath = vcFile.FullPath;

                // Only try to move the file if we can find the right filter.
                if (vcProject.FilterFromGuid(fakeFilter) is {} newFilter) {
                    filter.RemoveFile(vcFile);
                    if (newFilter.CanAddFile(fullPath))
                        newFilter.AddFile(fullPath);
                }
                break;
            }
        }

        public static VCFilter FilterFromName(this VCProject vcProject, FakeFilter fakeFilter)
        {
            try {
                if (vcProject.Filters is not IVCCollection filters)
                    return null;
                return filters.Cast<VCFilter>().FirstOrDefault(filter =>
                    string.Equals(filter.Name, fakeFilter.Name, Utils.IgnoreCase));
            } catch {
                throw new KeyNotFoundException("Cannot find filter.");
            }
        }

        public static VCFilter FilterFromGuid(this VCProject vcProject, FakeFilter fakeFilter)
        {
            try {
                if (vcProject.Filters is not IVCCollection filters)
                    return null;
                return filters.Cast<VCFilter>().FirstOrDefault(filter => string.Equals(
                    filter.UniqueIdentifier, fakeFilter.UniqueIdentifier, Utils.IgnoreCase));
            } catch {
                throw new KeyNotFoundException("Cannot find filter.");
            }
        }
    }
}
