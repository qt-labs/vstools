/****************************************************************************
**
** Copyright (C) 2019 The Qt Company Ltd.
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

#if VS2017 || VS2019
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace QtVsTools.QtMsBuild
{
    using QtVsTools.Core;

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
            await System.Threading.Tasks.Task.Yield();

            var modules = QtModules.Instance.GetAvailableModuleInformation()
                .Where(x => !string.IsNullOrEmpty(x.proVarQT))
                .Select(x => new QtModulesPopup.Module
                {
                    Id = (int)x.ModuleId,
                    Name = x.Name,
                    IsReadOnly = !x.Selectable,
                    QT = x.proVarQT.Split(' ').ToHashSet(),
                })
                .ToList();

            var allQT = modules.SelectMany(x => x.QT).ToHashSet();
            var selectedQT = currentValue.ToString().Split(';').ToHashSet();
            var extraQT = selectedQT.Except(allQT);

            foreach (var module in modules)
                module.IsSelected = module.QT.Intersect(selectedQT).Count() == module.QT.Count;

            var popup = new QtModulesPopup();
            popup.SetModules(modules);

            WindowHelper.ShowModal(popup);

            selectedQT = modules
                .Where(x => x.IsSelected)
                .SelectMany(x => x.QT)
                .Union(extraQT)
                .ToHashSet();

            return string.Join(";", selectedQT);
        }
    }
}
#endif
