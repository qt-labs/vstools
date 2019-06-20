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
    using QtProjectLib;
    using QtProjectWizard;

    [Export(typeof(IPropertyPageUIValueEditor))]
    [ExportMetadata("Name", "QtModulesEditor")]
    [AppliesTo("IntegratedConsoleDebugging")]
    internal sealed class QtModulesEditor : IPropertyPageUIValueEditor
    {
        class Module
        {
            public string LibraryPrefix;
            public QtModuleInfo Info;
            public IEnumerable<string> Vars;
        }
        static Dictionary<string, Module> modules = null;

        public async Task<object> EditValueAsync(
            IServiceProvider serviceProvider,
            IProperty ruleProperty,
            object currentValue)
        {
            if (modules == null) {
                modules = QtModules.Instance.GetAvailableModuleInformation()
                    .Where(x => !string.IsNullOrEmpty(x.proVarQT))
                    .ToDictionary(x => x.LibraryPrefix, x => new Module
                    {
                        LibraryPrefix = x.LibraryPrefix,
                        Info = x,
                        Vars = x.proVarQT.Split(' ')
                    });
            }

            var currentModuleVars = currentValue.ToString().Split(';');
            var currentModules = modules.Values
                .Where(x => x.Vars.All(y => currentModuleVars.Contains(y)));

            await Vsix.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();

            var data = new WizardData
            {
                Modules = currentModules.Select(x => x.LibraryPrefix).ToList()
            };

            var pages = new List<WizardPage>
            {
                new ModulePage
                {
                    Data = data,
                    Header = @"Select Qt Modules",
                    Message = @"Select the modules you want to include in your project.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            };

            var wizard = new WizardWindow(pages) { Title = @"Qt Modules" };
            WindowHelper.ShowModal(wizard);
            if (!wizard.DialogResult.HasValue || !wizard.DialogResult.Value)
                return currentValue;

            var selectedModules = data.Modules
                .Select(x => modules[x])
                .Where(x => x != null)
                .SelectMany(x => x.Vars)
                .OrderBy(x => x);

            return string.Join(";", selectedModules);
        }
    }
}
