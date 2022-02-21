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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace QtVsTools.Wizards.ItemWizard
{
    using Core;
    using Wizards.Common;
    using Wizards.ProjectWizard;
    using Wizards.Util;

    using static QtVsTools.Common.EnumExt;

    public sealed class WidgetsClassWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType => Options.GUISystem;

        enum NewClass
        {
            [String("safeitemname")] SafeItemName,
            [String("sourcefilename")] SourceFileName,
            [String("headerfilename")] HeaderFileName,
            [String("uifilename")] UiFileName
        }

        enum NewWidgetsItem
        {
            [String("classname")] ClassName,
            [String("baseclass")] BaseClass,
            [String("include")] Include,
            [String("qobject")] QObject,
            [String("ui_hdr")] UiHeaderName,
            [String("centralwidget")] CentralWidget,
            [String("forward_declare_class")] ForwardDeclClass,
            [String("multiple_inheritance")] MultipleInheritance,
            [String("ui_classname")] UiClassName,
            [String("member")] Member
        }

        enum Meta
        {
            [String("namespacebegin")] NamespaceBegin,
            [String("operator")] Operator,
            [String("asterisk")] Asterisk,
            [String("semicolon")] Semicolon,
            [String("new")] New,
            [String("delete")] Delete,
            [String("namespaceend")] NamespaceEnd
        }

        WizardData _WizardData;
        protected override WizardData WizardData => _WizardData
            ?? (_WizardData = new WizardData
            {
                InsertQObjectMacro = true,
                LowerCaseFileNames = false,
                UsePrecompiledHeader = false,
                DefaultModules = new List<string> { "core", "gui", "widgets" }
            });

        WizardWindow _WizardWindow;
        protected override WizardWindow WizardWindow => _WizardWindow
            ?? (_WizardWindow = new WizardWindow(title: "Qt Widgets Class Wizard")
            {
                new WizardIntroPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Widgets Class Wizard",
                    Message = @"This wizard will add a new Qt Widgets class to your project. "
                        + @"The wizard creates a .h and .cpp file. It also creates a new "
                        + @"empty form." + System.Environment.NewLine
                        + System.Environment.NewLine + "To continue, click Next.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new GuiPage
                {
                    Data = WizardData,
                    IsClassWizardPage = true,
                    Header = @"Welcome to the Qt Widgets Class Wizard",
                    Message = @"This wizard will add a new Qt Widgets class to your project. "
                        + @"The wizard creates a .h and .cpp file. It also creates a new "
                        + @"empty form.",
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
                className = @"QtWidgetsClass";

            WizardData.ClassName = className;
            WizardData.BaseClass = @"QMainWindow";
            WizardData.ClassHeaderFile = className + @".h";
            WizardData.ClassSourceFile = className + @".cpp";
            WizardData.UiFile = WizardData.ClassName + @".ui";
            WizardData.QrcFile = WizardData.ClassName + @".qrc";
            WizardData.UiClassInclusion = UiClassInclusion.Member;

            Parameter[NewWidgetsItem.ForwardDeclClass] = "";
            Parameter[NewWidgetsItem.MultipleInheritance] = "";
            Parameter[NewWidgetsItem.UiClassName] = "";
            Parameter[NewWidgetsItem.Member] = "ui";

            Parameter[Meta.Asterisk] ="";
            Parameter[Meta.Operator] = ".";
            Parameter[Meta.Semicolon] = ";";
            Parameter[Meta.New] = "";
            Parameter[Meta.Delete] = "";
        }

        protected override void BeforeTemplateExpansion()
        {
            Parameter[NewClass.SourceFileName] = WizardData.ClassSourceFile;
            Parameter[NewClass.HeaderFileName] = WizardData.ClassHeaderFile;
            Parameter[NewClass.UiFileName] = WizardData.UiFile;

            var array = WizardData.ClassName.Split(new[] { "::" },
                StringSplitOptions.RemoveEmptyEntries);
            var className = array.LastOrDefault();

            Parameter[NewWidgetsItem.ClassName] = className;
            Parameter[NewWidgetsItem.BaseClass] = WizardData.BaseClass;

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
            Parameter[NewWidgetsItem.Include] = FormatParam(include);

            Parameter[NewWidgetsItem.QObject] = WizardData.InsertQObjectMacro
                                                    ? "\r\n    Q_OBJECT\r\n" : "";

            Parameter[NewWidgetsItem.UiHeaderName] = string.Format("ui_{0}.h",
                Path.GetFileNameWithoutExtension(WizardData.UiFile));

            if (WizardData.BaseClass == "QMainWindow") {
                Parameter[NewWidgetsItem.CentralWidget] = FormatParam(
                      @"  <widget class=""QMenuBar"" name=""menuBar"" />"
                    + @"  <widget class=""QToolBar"" name=""mainToolBar"" />"
                    + @"  <widget class=""QWidget"" name=""centralWidget"" />"
                    + @"  <widget class=""QStatusBar"" name=""statusBar"" />"
                );
            }

            switch (WizardData.UiClassInclusion) {
            case UiClassInclusion.MemberPointer:
                Parameter[NewWidgetsItem.ForwardDeclClass] =
                    string.Format(
                          "\r\nQT_BEGIN_NAMESPACE\r\n"
                        + "namespace Ui {{ class {0}Class; }};\r\n"
                        + "QT_END_NAMESPACE\r\n", className
                    );
                Parameter[Meta.Asterisk] = "*";
                Parameter[Meta.Operator] = "->";
                Parameter[Meta.New] = string.Format("\r\n    , {0}(new Ui::{1}Class())",
                                                    Parameter[NewWidgetsItem.Member], className);
                Parameter[Meta.Delete] = string.Format("\r\n    delete {0};\r\n",
                                                       Parameter[NewWidgetsItem.Member]);
                goto case UiClassInclusion.Member;
            case UiClassInclusion.Member:
                Parameter[NewWidgetsItem.UiClassName] = string.Format("Ui::{0}Class", className);
                break;
            case UiClassInclusion.MultipleInheritance:
                Parameter[NewWidgetsItem.MultipleInheritance] =
                    string.Format(", public Ui::{0}Class", className);
                Parameter[NewWidgetsItem.Member] = "";
                Parameter[Meta.Operator] = "";
                Parameter[Meta.Semicolon] = "";
                break;
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
            ThreadHelper.ThrowIfNotOnUIThread();
            VCRulePropertyStorageHelper.SetQtModules(Dte, WizardData.DefaultModules);
        }

        public override void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            QtProject.AdjustWhitespace(Dte, projectItem.Properties.Item("FullPath").Value.ToString());
        }
    }
}
