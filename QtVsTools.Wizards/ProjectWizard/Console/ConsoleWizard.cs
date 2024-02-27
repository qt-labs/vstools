/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using QtVsTools.Common;

    public class ConsoleWizard : ProjectTemplateWizard
    {
        LazyFactory Lazy { get; } = new();

        protected override Options TemplateType => Options.Application | Options.ConsoleSystem;

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                DefaultModules = new List<string> { "QtCore" }
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Console Application Wizard")
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
                    return "Console Application project not available for the "
                        + $"'{config.Target}' target.";
                }
            }
            return string.Empty;
        }
    }
}
