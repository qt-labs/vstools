/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Core.MsBuild
{
    internal class VcPropertyStorageProvider : IPropertyStorageProvider
    {
        private string GetProperty(IVCRulePropertyStorage propertyStorage, string propertyName)
        {
            return propertyStorage?.GetUnevaluatedPropertyValue(propertyName) ?? "";
        }

        public string GetProperty(object obj, string itemType, string propertyName)
        {
            switch (obj) {
            case VCFileConfiguration { Tool: IVCRulePropertyStorage propertyStorage }:
                return GetProperty(propertyStorage, propertyName);
            case VCConfiguration vcConfiguration: {
                var ruleName = QtProject.GetRuleName(vcConfiguration, itemType);
                return GetProperty(vcConfiguration.Rules.Item(ruleName) as IVCRulePropertyStorage,
                    propertyName);
            }
            default:
                return "";
            }
        }

        private static bool SetProperty(IVCRulePropertyStorage storage, string name, string value)
        {
            if (storage?.GetUnevaluatedPropertyValue(name) != value)
                storage?.SetPropertyValue(name, value);
            return storage != null;
        }

        public bool SetProperty(object propertyStorage, string itemType, string propertyName,
            string propertyValue)
        {
            switch (propertyStorage) {
            case VCFileConfiguration { Tool: IVCRulePropertyStorage storage }:
                return SetProperty(storage, propertyName, propertyValue);
            case VCConfiguration vcConfiguration:
                var ruleName = QtProject.GetRuleName(vcConfiguration, itemType);
                return SetProperty(vcConfiguration.Rules.Item(ruleName) as IVCRulePropertyStorage,
                    propertyName, propertyValue);
            }
            return false;
        }

        private static bool DeleteProperty(IVCRulePropertyStorage propertyStorage, string name)
        {
            propertyStorage?.DeleteProperty(name);
            return propertyStorage != null;
        }

        public bool DeleteProperty(object propertyStorage, string itemType, string propertyName)
        {
            switch (propertyStorage) {
            case VCFileConfiguration { Tool: IVCRulePropertyStorage storage }:
                return DeleteProperty(storage, propertyName);
            case VCConfiguration vcConfiguration:
                var ruleName = QtProject.GetRuleName(vcConfiguration, itemType);
                return DeleteProperty(vcConfiguration.Rules.Item(ruleName) as IVCRulePropertyStorage,
                    propertyName);
            }
            return false;
        }

        public string GetConfigName(object propertyStorage)
        {
            return propertyStorage switch
            {
                VCFileConfiguration vcFileConfiguration => vcFileConfiguration.Name,
                VCConfiguration vcConfiguration => vcConfiguration.Name,
                _ => ""
            };
        }

        public string GetItemType(object propertyStorage)
        {
            return propertyStorage is VCFileConfiguration { File: VCFile vcFile }
                ? vcFile.ItemType : "";
        }

        public string GetItemName(object propertyStorage)
        {
            return propertyStorage is VCFileConfiguration { File: VCFile vcFile }
                ? vcFile.Name : "";
        }

        public object GetParentProject(object propertyStorage)
        {
            return propertyStorage switch
            {
                VCFileConfiguration { ProjectConfiguration: VCConfiguration projectConfiguration }
                    => projectConfiguration.project as VCProject,
                VCConfiguration storage => storage.project as VCProject,
                _ => null
            };
        }

        public object GetProjectConfiguration(object project, string configName)
        {
            var vcProject = project as VCProject;
            if (vcProject?.Configurations is not IVCCollection configurations)
                return null;

            foreach (VCConfiguration projConfig in configurations) {
                if (projConfig.Name == configName)
                    return projConfig;
            }
            return null;
        }

        public IEnumerable<object> GetItems(object project, string itemType, string configName = "")
        {
            var items = new List<VCFileConfiguration>();
            var vcProject = project as VCProject;
            if (vcProject?.GetFilesWithItemType(itemType) is not IVCCollection allItems)
                return items;

            foreach (VCFile vcFile in allItems) {
                if (vcFile.FileConfigurations is not IVCCollection fileConfigurations)
                    continue;
                foreach (VCFileConfiguration fileConfiguration in fileConfigurations) {
                    if (!string.IsNullOrEmpty(configName) && fileConfiguration.Name != configName)
                        continue;
                    items.Add(fileConfiguration);
                }
            }
            return items;
        }
    }
}
