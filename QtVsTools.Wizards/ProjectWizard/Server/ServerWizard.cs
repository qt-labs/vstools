/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using EnvDTE;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using Core;
    using Core.MsBuild;
    using QtVsTools.Common;

    using static QtVsTools.Common.EnumExt;

    public class ServerWizard : ProjectTemplateWizard
    {
        LazyFactory Lazy { get; } = new();

        protected override Options TemplateType => Options.DynamicLibrary | Options.GUISystem;

        enum NewClass
        {
            [String("classname")] ClassName,
            [String("sourcefilename")] SourceFileName,
            [String("headerfilename")] HeaderFileName,
            [String("include")] Include
        }

        enum NewActiveQtProject
        {
            [String("pro_name")] Name,
            [String("uifilename")] UiFileName,
            [String("ui_hdr")] UiHeaderName
        }

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                DefaultModules = new List<string> { "QtCore", "QtGui", "QtWidgets", "QtAxServer" }
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt ActiveQt Server Wizard")
            {
                new WizardIntroPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt ActiveQt Server Wizard",
                    Message = @"This wizard generates a Qt ActiveQt server project. It "
                        + @"creates a simple ActiveQt widget with the required files."
                        + System.Environment.NewLine + System.Environment.NewLine
                        + "To continue, click Next.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new ConfigPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt ActiveQt Server Wizard",
                    Message =
                            @"Setup the configurations you want to include in your project. "
                            + @"The recommended settings for this project are selected by default.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true,
                    ValidateConfigs = ValidateConfigsForActiveQtServer
                },
                new ServerPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt ActiveQt Server Wizard",
                    Message = @"This wizard generates a Qt ActiveQt server project. It "
                        + @"creates a simple ActiveQt widget with the required files.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            });

        string ValidateConfigsForActiveQtServer(IEnumerable<IWizardConfiguration> configs)
        {
            foreach (var config in configs) {
                if (config.Target.EqualTo(ProjectTargets.WindowsStore))
                    return $"ActiveQt Server project not available for the '{config.Target}' target.";
            }
            return string.Empty;
        }

        protected override void BeforeWizardRun()
        {
            // midl.exe does not support spaces in project name. Fails while generating the
            // IDL file (library attribute), e.g. 'library Active QtServer1Lib' is illegal.
            if (Parameter[NewProject.SafeName].Contains(" "))
                throw new QtVSException("Project name shall not contain spaces.");

            var className = Parameter[NewProject.SafeName];
            className = Regex.Replace(className, @"[^a-zA-Z0-9_]", string.Empty);
            className = Regex.Replace(className, @"^[\d-]*\s*", string.Empty);
            var result = new Util.ClassNameValidationRule().Validate(className, null);
            if (result != ValidationResult.ValidResult)
                className = @"ActiveQtServer";

            WizardData.ClassName = className;
            WizardData.ClassHeaderFile = className + @".h";
            WizardData.ClassSourceFile = className + @".cpp";
            WizardData.UiFile = WizardData.ClassName + @".ui";
        }

        protected override void BeforeTemplateExpansion()
        {
            Parameter[NewClass.ClassName] = WizardData.ClassName;
            Parameter[NewClass.HeaderFileName] = WizardData.ClassHeaderFile;
            Parameter[NewClass.SourceFileName] = WizardData.ClassSourceFile;
            Parameter[NewActiveQtProject.UiFileName] = WizardData.UiFile;

            var include = new StringBuilder();
            if (UsePrecompiledHeaders)
                include.AppendLine($"#include \"{PrecompiledHeader.Include}\"");
            include.AppendLine($"#include \"{WizardData.ClassHeaderFile}\"");
            Parameter[NewClass.Include] = FormatParam(include);

            Parameter[NewActiveQtProject.UiHeaderName] =
                $"ui_{Path.GetFileNameWithoutExtension(WizardData.UiFile)}.h";

            Parameter[NewActiveQtProject.Name] = WizardData.LowerCaseFileNames
                ? Parameter[NewProject.SafeName].ToLower()
                : Parameter[NewProject.SafeName];
        }

        protected override void OnProjectGenerated(Project project)
        {
            if (QtProject.GetOrAdd(project) is {} qtProject)
                qtProject.AddActiveQtBuildStep("1.0", Parameter[NewProject.SafeName] + ".def");
        }
    }
}
