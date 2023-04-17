/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace QtVsTools.Wizards.ProjectWizard
{
    using QtVsTools.Common;
    using Wizards.Common;

    using static QtVsTools.Common.EnumExt;

    public class LibraryWizard : ProjectTemplateWizard
    {
        LazyFactory Lazy { get; } = new();

        protected override Options TemplateType => Options.GUISystem
            | (WizardData.CreateStaticLibrary ? Options.StaticLibrary : Options.DynamicLibrary);

        enum NewLibClass
        {
            [String("classname")] ClassName,
            [String("sourcefilename")] SourceFileName,
            [String("headerfilename")] HeaderFileName,
            [String("include")] Include,
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
            var safeprojectname = Parameter[NewProject.SafeName];
            safeprojectname = Regex.Replace(safeprojectname, @"[^a-zA-Z0-9_]", string.Empty);
            safeprojectname = Regex.Replace(safeprojectname, @"^[\d-]*\s*", string.Empty);
            var result = new Util.ClassNameValidationRule().Validate(safeprojectname, null);
            if (result != ValidationResult.ValidResult)
                safeprojectname = @"QtClassLibrary";

            WizardData.ClassName = safeprojectname;
            WizardData.ClassHeaderFile = safeprojectname + @".h";
            WizardData.ClassSourceFile = safeprojectname + @".cpp";
        }

        protected override void BeforeTemplateExpansion()
        {
            Parameter[NewLibClass.ClassName] = WizardData.ClassName;
            Parameter[NewLibClass.HeaderFileName] = WizardData.ClassHeaderFile;
            Parameter[NewLibClass.SourceFileName] = WizardData.ClassSourceFile;

            var include = new StringBuilder();
            if (UsePrecompiledHeaders)
                include.AppendLine($"#include \"{PrecompiledHeader.Include}\"");
            include.AppendLine($"#include \"{WizardData.ClassHeaderFile}\"");
            Parameter[NewLibClass.Include] = FormatParam(include);

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
