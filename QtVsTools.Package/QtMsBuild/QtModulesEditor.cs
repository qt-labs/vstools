/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace QtVsTools.QtMsBuild
{
    using Core;

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

            var qtSettings = ruleProperty.ContainingRule;
            var qtVersion = await qtSettings.GetPropertyValueAsync("QtInstall");

            var vm = QtVersionManager.The();
            var versionInfo = vm.GetVersionInfo(qtVersion);
            if (versionInfo == null)
                versionInfo = vm.GetVersionInfo(vm.GetDefaultVersion());

            var modules = QtModules.Instance.GetAvailableModules(versionInfo.qtMajor)
                .Where(x => !string.IsNullOrEmpty(x.proVarQT))
                .Select(x => new QtModulesPopup.Module
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsReadOnly = !x.Selectable,
                    QT = x.proVarQT.Split(' ').ToHashSet(),
                })
                .ToList();

            HashSet<string> selectedQt = null;
            IEnumerable<string> extraQt = null;
            if (currentValue != null) {
                var allQt = modules.SelectMany(x => x.QT).ToHashSet();
                selectedQt = currentValue.ToString().Split(';').ToHashSet();
                extraQt = selectedQt.Except(allQt);

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
            return selectedQt == null ? "" : string.Join(";", selectedQt);
        }
    }
}
