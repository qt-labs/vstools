/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using QtVsTools.Common;
    using static QtVsTools.Common.EnumExt;

    public class QuickWizard : ProjectTemplateWizard
    {
        LazyFactory Lazy { get; } = new();

        protected override Options TemplateType => Options.Application | Options.GUISystem;

        protected enum Qml
        {
            [String("qml_prefix")] Prefix
        }

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                DefaultModules = new List<string> { "QtQuick" }
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Quick Application Wizard")
            {
                new WizardIntroPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Quick Application Wizard",
                    Message = @"This wizard generates a Qt Quick application project."
                        + System.Environment.NewLine
                        + "Click Finish to create the project.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new ConfigPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Quick Application Wizard",
                    Message =
                            @"Setup the configurations you want to include in your project. "
                            + @"The recommended settings for this project are selected by default.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            });

        protected override void BeforeTemplateExpansion()
        {
            Parameter[Qml.Prefix] = Parameter[NewProject.SafeName].ToLower();
        }

        protected override void ExpandQtSettings(StringBuilder xml, IWizardConfiguration config)
        {
            base.ExpandQtSettings(xml, config);
            if (config.IsDebug)
                xml.AppendLine(@"<QtQMLDebugEnable>true</QtQMLDebugEnable>");
        }

        protected override CMakeConfigPreset ConfigureCMakePreset(IWizardConfiguration config)
        {
            var preset = base.ConfigureCMakePreset(config);
            if (config.IsDebug) {
                preset.CacheVariables.CxxFlags = "-DQT_QML_DEBUG";
                (preset.Environment ??= new())
                    .QmlDebugArgs = $"-qmljsdebugger=file:{{{Guid.NewGuid()}}},block";
            }
            return preset;
        }
    }
}
