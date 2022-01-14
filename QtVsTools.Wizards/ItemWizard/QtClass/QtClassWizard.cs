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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using EnvDTE;
using QtVsTools.Common;
using QtVsTools.Core;
using QtVsTools.Wizards.Common;
using QtVsTools.Wizards.ProjectWizard;

namespace QtVsTools.Wizards.ItemWizard
{
    using static EnumExt;

    public sealed class QtClassWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType => Options.ConsoleSystem;

        enum NewClass
        {
            [String("safeitemname")] SafeItemName,
            [String("sourcefilename")] SourceFileName,
            [String("headerfilename")] HeaderFileName
        }

        enum NewQtItem
        {
            [String("classname")] ClassName,
            [String("baseclass")] BaseClass,
            [String("include")] Include,
            [String("qobject")] QObject,
            [String("baseclassdecl")] BaseClassDecl,
            [String("signature")] Signature,
            [String("baseclassinclude")] BaseClassInclude,
            [String("baseclasswithparent")] BaseClassWithParent
        }

        enum Meta
        {
            [String("namespacebegin")] NamespaceBegin,
            [String("namespaceend")] NamespaceEnd
        }

        WizardData _WizardData;
        protected override WizardData WizardData => _WizardData
            ?? (_WizardData = new WizardData
            {
                InsertQObjectMacro = true,
                LowerCaseFileNames = false,
                UsePrecompiledHeader = false,
                DefaultModules = new List<string> { "QtCore" }
            });

        WizardWindow _WizardWindow;
        protected override WizardWindow WizardWindow => _WizardWindow
            ?? (_WizardWindow = new WizardWindow(title: "Qt Class Wizard")
            {
                new WizardIntroPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Class Wizard",
                    Message = @"This wizard will add a new Qt class to your project. The "
                        + @"wizard creates a .h and .cpp file." + System.Environment.NewLine
                        + System.Environment.NewLine + "To continue, click Next.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new QtClassPage {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Class Wizard",
                    Message = @"This wizard will add a new Qt class to your project. The "
                        + @"wizard creates a .h and .cpp file.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            });

        protected override void BeforeWizardRun()
        {
            var className = Parameter[NewClass.SafeItemName];
            className = Regex.Replace(className, @"[^a-zA-Z0-9_]", string.Empty);
            className = Regex.Replace(className, @"^[\d-]*\s*", string.Empty);
            var result = new Util.ClassNameValidationRule().Validate(className, null);
            if (result != ValidationResult.ValidResult)
                className = @"QtClass";

            WizardData.ClassName = className;
            WizardData.BaseClass = @"QObject";
            WizardData.ClassHeaderFile = className + @".h";
            WizardData.ClassSourceFile = className + @".cpp";
            WizardData.ConstructorSignature = "QObject *parent";

            Parameter[NewQtItem.QObject] = "";
            Parameter[NewQtItem.BaseClassDecl] = "";
            Parameter[NewQtItem.Signature] = "";
            Parameter[NewQtItem.BaseClassInclude] = "";
            Parameter[NewQtItem.BaseClassWithParent] = "";
        }

        protected override void BeforeTemplateExpansion()
        {
            Parameter[NewClass.SourceFileName] = WizardData.ClassSourceFile;
            Parameter[NewClass.HeaderFileName] = WizardData.ClassHeaderFile;

            var array = WizardData.ClassName.Split(new[] { "::" },
                StringSplitOptions.RemoveEmptyEntries);
            var className = array.LastOrDefault();
            var baseClass = WizardData.BaseClass;

            Parameter[NewQtItem.ClassName] = className;
            Parameter[NewQtItem.BaseClass] = baseClass;

            var include = new StringBuilder();
            var pro = HelperFunctions.GetSelectedQtProject(Dte);
            if (pro != null) {
                var qtProject = QtProject.Create(pro);
                if (qtProject != null && qtProject.UsesPrecompiledHeaders()) {
                    include.AppendLine(string.Format("#include \"{0}\"", qtProject
                        .GetPrecompiledHeaderThrough()));
                }
            }
            include.AppendLine(string.Format("#include \"{0}\"", WizardData.ClassHeaderFile));
            Parameter[NewQtItem.Include] = FormatParam(include);

            if (!string.IsNullOrEmpty(baseClass)) {
                Parameter[NewQtItem.QObject] = WizardData.InsertQObjectMacro
                                                    ? "\r\n    Q_OBJECT\r\n" : "";
                Parameter[NewQtItem.BaseClassDecl] = " : public " + baseClass;
                Parameter[NewQtItem.Signature] = WizardData.ConstructorSignature;
                Parameter[NewQtItem.BaseClassInclude] = "#include <" + baseClass + ">\r\n\r\n";
                Parameter[NewQtItem.BaseClassWithParent] = string.IsNullOrEmpty(WizardData
                    .ConstructorSignature) ? "" : "\r\n    : " + baseClass + "(parent)";
            }

            string nsBegin = string.Empty, nsEnd = string.Empty;
            for (var i = 0; i < array.Length - 1; ++i) {
                nsBegin += "namespace " + array[i] + " {\r\n";
                nsEnd = "} // namespace " + array[i] + "\r\n" + nsEnd;
            }
            Parameter[Meta.NamespaceBegin] = nsBegin;
            Parameter[Meta.NamespaceEnd] = nsEnd;
        }

        protected override void Expand()
        {
            // do not call the base class method here
        }

        public override void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            QtProject.AdjustWhitespace(Dte, projectItem.Properties.Item("FullPath").Value.ToString());
        }
    }
}
