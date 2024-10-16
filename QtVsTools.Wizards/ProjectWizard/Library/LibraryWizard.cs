/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using Util;

    using static QtVsTools.Common.EnumExt;

    public class LibraryWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType => Options.GUISystem
            | (WizardData.CreateStaticLibrary ? Options.StaticLibrary : Options.DynamicLibrary);

        enum NewLibClass
        {
            [String("saveglobal")] GlobalHeader,
            [String("pro_lib_define")] LibDefine,
            [String("pro_lib_export")] LibExport,
            [String("cmake_static")] CMakeStatic
        }

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                DefaultModules = new List<string> { "QtCore" }
            });

        readonly List<string> LibExtraDefines = new();
        protected override IEnumerable<string> ExtraDefines => LibExtraDefines;

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Class Library Wizard")
            {
                new WizardIntroPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Class Library Wizard",
                    Message = @"This wizard generates a Qt Class Library project. The "
                        + @"resulting library is linked dynamically with Qt."
                        + System.Environment.NewLine + System.Environment.NewLine
                        + @"To continue, click Next.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new ConfigPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Class Library Wizard",
                    Message =
                            @"Setup the configurations you want to include in your project. "
                            + @"The recommended settings for this project are selected by default.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new LibraryClassPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Class Library Wizard",
                    Message = @"This wizard generates a Qt Class Library project. The "
                        + @"resulting library is linked dynamically with Qt.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            });

        protected override void BeforeWizardRun()
        {
            var safeprojectname = ClassNameValidationRule.SafeName(Parameter[NewProject.SafeName],
                "QtClassLibrary");

            WizardData.ClassName = safeprojectname;
            WizardData.ClassHeaderFile = safeprojectname + @".h";
            WizardData.ClassSourceFile = safeprojectname + @".cpp";
        }

        protected override void BeforeTemplateExpansion()
        {
            Parameter[NewClass.ClassName] = WizardData.ClassName;
            Parameter[NewClass.HeaderFileName] = WizardData.ClassHeaderFile;
            Parameter[NewClass.SourceFileName] = WizardData.ClassSourceFile;

            var include = new StringBuilder();
            if (UsePrecompiledHeaders)
                include.AppendLine($"#include \"{PrecompiledHeader.Include}\"");
            include.AppendLine($"#include \"{WizardData.ClassHeaderFile}\"");
            Parameter[NewClass.Include] = FormatParam(include);

            var safeprojectname = Parameter[NewProject.SafeName];
            Parameter[NewLibClass.GlobalHeader] = safeprojectname.ToLower();
            Parameter[NewLibClass.LibDefine] = safeprojectname.ToUpper() + "_LIB";
            Parameter[NewLibClass.LibExport] = safeprojectname.ToUpper() + "_EXPORT";

            LibExtraDefines.Add(Parameter[NewLibClass.LibDefine]);
            if (WizardData.CreateStaticLibrary) {
                LibExtraDefines.Add("BUILD_STATIC");
                Parameter[NewLibClass.CMakeStatic] = "STATIC";
            } else {
                Parameter[NewLibClass.CMakeStatic] = "SHARED";
            }
        }
    }
}
