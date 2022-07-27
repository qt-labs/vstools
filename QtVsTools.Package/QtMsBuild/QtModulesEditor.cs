/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

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
