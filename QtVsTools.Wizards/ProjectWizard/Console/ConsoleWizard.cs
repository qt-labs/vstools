/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using System.Collections.Generic;

namespace QtVsTools.Wizards.ProjectWizard
{
    using QtVsTools.Common;
    using Wizards.Common;

    public class ConsoleWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType => Options.Application | Options.ConsoleSystem;

        WizardData _WizardData;
        protected override WizardData WizardData => _WizardData
            ?? (_WizardData = new WizardData
            {
                DefaultModules = new List<string> { "QtCore" }
            });

        WizardWindow _WizardWindow;
        protected override WizardWindow WizardWindow => _WizardWindow
            ?? (_WizardWindow = new WizardWindow(title: "Qt Console Application Wizard")
            {
                new WizardIntroPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Console Application Wizard",
                    Message = @"This wizard generates a Qt console application "
                        + @"project. The application derives from QCoreApplication "
                        + @"and does not present a GUI." + System.Environment.NewLine
                        + System.Environment.NewLine + "To continue, click Next.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new ConfigPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Console Application Wizard",
                    Message =
                            @"Setup the configurations you want to include in your project. "
                            + @"The recommended settings for this project are selected by default.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true,
                    ValidateConfigs = ValidateConfigsForConsoleApp
                }
            });

        string ValidateConfigsForConsoleApp(IEnumerable<IWizardConfiguration> configs)
        {
            foreach (var config in configs) {
                if (config.Target.EqualTo(ProjectTargets.WindowsStore)) {
                    return string.Format(
                        "Console Application project not available for the '{0}' target.",
                        config.Target);
                }
            }
            return string.Empty;
        }
    }
}
