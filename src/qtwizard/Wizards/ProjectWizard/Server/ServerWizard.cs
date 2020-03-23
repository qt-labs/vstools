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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using EnvDTE;
using QtProjectLib;
using QtVsTools.Common;

namespace QtVsTools.Wizards.ProjectWizard
{
    using static EnumExt;

    public class ServerWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType => Options.DynamicLibrary | Options.GUISystem;

        enum NewClass
        {
            [String("classname")] ClassName,
            [String("sourcefilename")] SourceFileName,
            [String("headerfilename")] HeaderFileName,
            [String("include")] Include,
        }

        enum NewActiveQtProject
        {
            [String("pro_name")] Name,
            [String("uifilename")] UiFileName,
            [String("ui_hdr")] UiHeaderName,
        }

        WizardData _WizardData;
        protected override WizardData WizardData => _WizardData
            ?? (_WizardData = new WizardData
            {
                DefaultModules = new List<string> { "QtCore", "QtGui", "QtWidgets", "QtAxServer" }
            });

        WizardWindow _WizardWindow;
        protected override WizardWindow WizardWindow => _WizardWindow
            ?? (_WizardWindow = new WizardWindow(title: "Qt ActiveQt Server Wizard")
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
                    Header = @"Welcome to the Qt GUI Application Wizard",
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
                if (config.Target.EqualTo(ProjectTargets.WindowsStore)) {
                    return string.Format(
                        "ActiveQt Server project not available for the '{0}' target.",
                        config.Target);
                }
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
            include.AppendLine(string.Format("#include \"{0}\"", WizardData.ClassHeaderFile));
            if (UsePrecompiledHeaders)
                include.AppendLine(string.Format("#include \"{0}\"", PrecompiledHeader.Include));
            Parameter[NewClass.Include] = FormatParam(include);

            Parameter[NewActiveQtProject.UiHeaderName] = string.Format("ui_{0}.h",
                Path.GetFileNameWithoutExtension(WizardData.UiFile));

            Parameter[NewActiveQtProject.Name] = WizardData.LowerCaseFileNames
                ? Parameter[NewProject.SafeName].ToLower()
                : Parameter[NewProject.SafeName];
        }

        protected override void OnProjectGenerated(Project project)
        {
            var qtProject = QtProject.Create(project);
            qtProject.AddActiveQtBuildStep("1.0", Parameter[NewProject.SafeName] + ".def");
        }
    }
}
