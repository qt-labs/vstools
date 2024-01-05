// ************************************************************************************************
// Copyright (C) 2024 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
// ************************************************************************************************

using System.Collections.Generic;

namespace QtVsTools.Core.MsBuild
{
    public interface IPropertyStorageProvider
    {
        string GetProperty(
            object propertyStorage,
            string itemType,
            string propertyName);

        bool SetProperty(
            object propertyStorage,
            string itemType,
            string propertyName,
            string propertyValue);

        bool DeleteProperty(
            object propertyStorage,
            string itemType,
            string propertyName);

        string GetConfigName(
            object propertyStorage);

        string GetItemType(
            object propertyStorage);

        string GetItemName(
            object propertyStorage);

        object GetParentProject(
            object propertyStorage);

        object GetProjectConfiguration(
            object project,
            string configName);

        IEnumerable<object> GetItems(
            object project,
            string itemType,
            string configName = "");
    }
}
