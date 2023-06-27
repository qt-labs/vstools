// ************************************************************************************************
// Copyright (C) 2023 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
// ************************************************************************************************

using System.Collections.Generic;
using System.Linq;

namespace QtVsTools.Core.MsBuild
{
    public class MsBuildProjectContainer
    {
        private readonly IPropertyStorageProvider provider;
        public MsBuildProjectContainer(IPropertyStorageProvider provider)
        {
            this.provider = provider;
        }

        private string GetPropertyValueByName(object propertyStorage, string itemType,
            string propertyName)
        {
            return provider.GetProperty(propertyStorage, itemType, propertyName);
        }

        private bool SetPropertyValueByName(object propertyStorage, string itemType,
            string propertyName, string propertyValue)
        {
            return provider.SetProperty(propertyStorage, itemType, propertyName, propertyValue);
        }

        private bool DeletePropertyByName(object propertyStorage, string itemType,
            string propertyName)
        {
            return provider.DeleteProperty(propertyStorage, itemType, propertyName);
        }

        private class ItemPropertyChange
        {
            //key
            public string ConfigName;
            public string ItemTypeName;
            public string ItemName;
            public string PropertyName;

            //value
            public string PropertyValue;
            public object PropertyStorage;

            public void CopyFrom(ItemPropertyChange change)
            {
                ConfigName = change.ConfigName;
                ItemTypeName = change.ItemTypeName;
                ItemName = change.ItemName;
                PropertyName = change.PropertyName;
                PropertyValue = change.PropertyValue;
                PropertyStorage = change.PropertyStorage;
            }

            public bool IsMocSource => ItemTypeName == QtMoc.ItemTypeName
                && !HelperFunctions.IsHeaderFile(ItemName);

            public string GroupKey => string.Join(
                ",", ConfigName, ItemTypeName, PropertyName, PropertyValue, IsMocSource.ToString()
            );

            public string Key => string.Join(",", ConfigName, ItemTypeName, PropertyName, ItemName);
        }

        private IEnumerable<object> GetItems(string itemType, string configName = "")
        {
            return provider.GetItems(GetProject(), itemType, configName);
        }

        private int GetItemCount(string itemType, bool? isMocSource = null, string configName = "")
        {
            var items = GetItems(itemType, configName);
            if (!isMocSource.HasValue)
                return items.Count();

            if (!isMocSource.Value) {
                return items.Count(x => provider.GetItemType(x) != QtMoc.ItemTypeName
                 || HelperFunctions.IsHeaderFile(provider.GetItemName(x)));
            }

            return items.Count(x => provider.GetItemType(x) == QtMoc.ItemTypeName
             && !HelperFunctions.IsHeaderFile(provider.GetItemName(x)));
        }

        private object GetProjectConfiguration(string configName)
        {
            return provider.GetProjectConfiguration(GetProject(), configName);
        }

        private readonly Dictionary<string, ItemPropertyChange> itemPropertyChanges = new();
        private readonly Dictionary<string, List<ItemPropertyChange>> itemPropertyChangesGrouped = new();
        private bool pendingChanges;

        private void AddChange(ItemPropertyChange newChange)
        {
            if (itemPropertyChanges.TryGetValue(newChange.Key, out var oldChange)) {
                if (oldChange.GroupKey == newChange.GroupKey) {
                    oldChange.CopyFrom(newChange);
                    return;
                }
                RemoveChange(oldChange);
            }

            if (!itemPropertyChangesGrouped.TryGetValue(newChange.GroupKey, out var changeGroup)) {
                itemPropertyChangesGrouped.Add(
                    newChange.GroupKey,
                    changeGroup = new List<ItemPropertyChange>());
            }
            changeGroup.Add(newChange);
            itemPropertyChanges.Add(newChange.Key, newChange);
        }

        private void RemoveChange(ItemPropertyChange change)
        {
            if (itemPropertyChangesGrouped.TryGetValue(change.GroupKey, out var changeGroup)) {
                changeGroup.Remove(change);
                if (changeGroup.Count == 0)
                    itemPropertyChangesGrouped.Remove(change.GroupKey);
            }
            itemPropertyChanges.Remove(change.Key);
        }

        private object GetProject()
        {
            var change = itemPropertyChanges.Values.FirstOrDefault();
            return change == null ? null : provider.GetParentProject(change.PropertyStorage);
        }

        public bool BeginSetItemProperties()
        {
            if (pendingChanges)
                return false;
            itemPropertyChanges.Clear();
            itemPropertyChangesGrouped.Clear();
            pendingChanges = true;
            return true;
        }

        public bool SetItemPropertyByName(object propertyStorage, string propertyName,
            string propertyValue)
        {
            if (propertyStorage == null)
                return false;

            var configName = provider.GetConfigName(propertyStorage);
            var itemType = provider.GetItemType(propertyStorage);
            var itemName = provider.GetItemName(propertyStorage);

            var newChange = new ItemPropertyChange
            {
                ConfigName = configName,
                ItemTypeName = itemType,
                ItemName = itemName,
                PropertyName = propertyName,
                PropertyValue = propertyValue,
                PropertyStorage = propertyStorage
            };

            if (!pendingChanges) {
                if (!BeginSetItemProperties())
                    return false;
                AddChange(newChange);
                if (!EndSetItemProperties())
                    return false;
            } else {
                AddChange(newChange);
            }

            return true;
        }

        private void SetGroupItemProperties(IReadOnlyCollection<ItemPropertyChange> changeGroup)
        {
            //grouped by configName, itemTypeName, propertyName, propertyValue, isMocSource
            var firstChange = changeGroup.FirstOrDefault();
            if (firstChange == null)
                return;

            var configName = firstChange.ConfigName;
            var itemTypeName = firstChange.ItemTypeName;
            var propertyName = firstChange.PropertyName;
            var propertyValue = firstChange.PropertyValue;
            var isMocSource = firstChange.IsMocSource;
            var itemCount = GetItemCount(itemTypeName, isMocSource, configName);
            var groupCount = changeGroup.Count;
            var projConfig = GetProjectConfiguration(configName);

            if (!isMocSource && groupCount == itemCount) {
                //all items are setting the same value for this property
                // -> set at project level
                if (!SetPropertyValueByName(projConfig, itemTypeName, propertyName, propertyValue))
                    return;

                // -> remove old property from each item
                foreach (var change in changeGroup) {
                    if (!DeletePropertyByName(
                        change.PropertyStorage,
                        change.ItemTypeName,
                        change.PropertyName)) {
                        break;
                    }
                }
            } else {
                //different property values per item
                // -> set at each item
                foreach (var change in changeGroup) {
                    if (GetPropertyValueByName(projConfig, change.ItemTypeName,
                        change.PropertyName) == change.PropertyValue) continue;
                    if (!SetPropertyValueByName(change.PropertyStorage, change.ItemTypeName,
                        change.PropertyName, change.PropertyValue)) {
                        break;
                    }
                }
            }
        }

        public bool EndSetItemProperties()
        {
            if (!pendingChanges)
                return false;

            var changeGroupsNormal = itemPropertyChangesGrouped.Values
                .Where(x => x.Any() && !x.First().IsMocSource);
            foreach (var changeGroup in changeGroupsNormal)
                SetGroupItemProperties(changeGroup);

            var changeGroupsMocSource = itemPropertyChangesGrouped.Values
                .Where(x => x.Any() && x.First().IsMocSource);
            foreach (var changeGroup in changeGroupsMocSource)
                SetGroupItemProperties(changeGroup);

            itemPropertyChanges.Clear();
            itemPropertyChangesGrouped.Clear();
            pendingChanges = false;
            return true;
        }

        private string GetPropertyChangedValue(string configName, string itemTypeName,
            string itemName, string propertyName)
        {
            if (!pendingChanges)
                return null;

            var change = new ItemPropertyChange
            {
                ConfigName = configName,
                ItemTypeName = itemTypeName,
                ItemName = itemName,
                PropertyName = propertyName
            };
            return itemPropertyChanges.TryGetValue(change.Key, out change)
                ? change.PropertyValue : null;
        }

        public string GetPropertyChangedValue(QtMoc.Property property, string itemName,
            string configName)
        {
            return GetPropertyChangedValue(configName, QtMoc.ItemTypeName, itemName,
                property.ToString());
        }

        public bool SetCommandLine(string itemType, object propertyStorage, string commandLine,
            IVsMacroExpander macros)
        {
            return itemType switch
            {
                QtMoc.ItemTypeName => SetQtMocCommandLine(propertyStorage, commandLine, macros),
                QtRcc.ItemTypeName => SetQtRccCommandLine(propertyStorage, commandLine, macros),
                QtRepc.ItemTypeName => SetQtRepcCommandLine(propertyStorage, commandLine, macros),
                QtUic.ItemTypeName => SetQtUicCommandLine(propertyStorage, commandLine, macros),
                _ => false
            };
        }

        #region QtMoc

        private static QtMoc _qtMocInstance;
        public static QtMoc QtMocInstance => _qtMocInstance ??= new QtMoc();

        public bool SetItemProperty(object propertyStorage, QtMoc.Property property,
            string propertyValue)
        {
            return SetItemPropertyByName(propertyStorage, property.ToString(), propertyValue);
        }

        private bool SetQtMocCommandLine(object propertyStorage, string commandLine,
            IVsMacroExpander macros)
        {
            if (!QtMocInstance.ParseCommandLine(commandLine, macros, out var properties))
                return false;
            return properties.All(property => SetItemProperty(propertyStorage, property.Key,
                property.Value));
        }

        #endregion

        #region QtRcc

        private static QtRcc _qtRccInstance;

        private static QtRcc QtRccInstance => _qtRccInstance ??= new QtRcc();

        public bool SetItemProperty(object propertyStorage, QtRcc.Property property,
            string propertyValue)
        {
            return SetItemPropertyByName(propertyStorage, property.ToString(), propertyValue);
        }

        private bool SetQtRccCommandLine(object propertyStorage, string commandLine,
            IVsMacroExpander macros)
        {
            if (!QtRccInstance.ParseCommandLine(commandLine, macros, out var properties))
                return false;
            return properties.All(property => SetItemProperty(propertyStorage, property.Key,
                property.Value));
        }

        #endregion

        #region QtRepc

        private static QtRepc _qtRepcInstance;

        private static QtRepc QtRepcInstance => _qtRepcInstance ??= new QtRepc();

        public bool SetItemProperty(object propertyStorage, QtRepc.Property property,
            string propertyValue)
        {
            return SetItemPropertyByName(propertyStorage, property.ToString(), propertyValue);
        }

        private bool SetQtRepcCommandLine(object propertyStorage, string commandLine,
            IVsMacroExpander macros)
        {
            if (!QtRepcInstance.ParseCommandLine(commandLine, macros, out var properties))
                return false;
            return properties.All(property => SetItemProperty(propertyStorage, property.Key,
                property.Value));
        }

        #endregion

        #region QtUic

        private static QtUic _qtUicInstance;

        private static QtUic QtUicInstance => _qtUicInstance ??= new QtUic();

        public bool SetItemProperty(object propertyStorage, QtUic.Property property,
            string propertyValue)
        {
            return SetItemPropertyByName(propertyStorage, property.ToString(), propertyValue);
        }

        private bool SetQtUicCommandLine(object propertyStorage, string commandLine,
            IVsMacroExpander macros)
        {
            if (!QtUicInstance.ParseCommandLine(commandLine, macros, out var properties))
                return false;
            return properties.All(property => SetItemProperty(propertyStorage, property.Key,
                property.Value));
        }

        #endregion
    }
}
