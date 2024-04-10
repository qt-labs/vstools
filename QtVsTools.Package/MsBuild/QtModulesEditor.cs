/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace QtVsTools.Package.MsBuild
{
    using Core;
    using QtVsTools.Core.MsBuild;

    [Export(typeof(IPropertyPageUIValueEditor))]
    [ExportMetadata("Name", "QtModulesEditor")]
    [AppliesTo("IntegratedConsoleDebugging")]
    internal sealed class QtModulesEditor : IPropertyPageUIValueEditor
    {
        public async Task<object> EditValueAsync(
            IServiceProvider serviceProvider,
            IProperty ruleProperty,
            object currentValue)
        {
            await Task.Yield();

            var currentModules = currentValue as string;

            if (await ruleProperty.GetValueAsync() is IEnumerable<string> values) {
                var modulesToAdd = currentModules?.Split(';') ?? Array.Empty<string>();
                currentModules = string.Join(";", values.Union(modulesToAdd));
            }

            var qtSettings = ruleProperty.ContainingRule;
            var qtVersion = await qtSettings.GetPropertyValueAsync("QtInstall");

            var versionInfo = VersionInformation.GetOrAddByName(qtVersion)
                ?? VersionInformation.GetOrAddByName(QtVersionManager.GetDefaultVersion());
            if (versionInfo == null)
                return "";

            var modules = QtModules.Instance.GetAvailableModules(versionInfo.Major)
                .Where(x => !string.IsNullOrEmpty(x.proVarQT))
                .Select(x => new QtModulesPopup.Module
                {
                    Name = x.Name,
                    IsReadOnly = !x.Selectable,
                    QT = x.proVarQT.Split(' ').ToHashSet()
                })
                .ToList();

            HashSet<string> selectedQt = null;
            IEnumerable<string> extraQt = null;
            if (!string.IsNullOrEmpty(currentModules)) {
                selectedQt = currentModules
                    .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                extraQt = selectedQt.Except(modules.SelectMany(x => x.QT));

                foreach (var module in modules)
                    module.IsSelected = module.QT.Intersect(selectedQt).Count() == module.QT.Count;
            }

            var popup = new QtModulesPopup();
            popup.SetModules(modules.OrderBy(module => module.Name));

            if (popup.ShowModal().GetValueOrDefault()) {
                selectedQt = modules
                    .Where(x => x.IsSelected)
                    .SelectMany(x => x.QT)
                    .Union(extraQt ?? Enumerable.Empty<string>())
                    .ToHashSet();
            }
            return selectedQt?.Any() == true ? string.Join(";", selectedQt) : "";
        }
    }
}
