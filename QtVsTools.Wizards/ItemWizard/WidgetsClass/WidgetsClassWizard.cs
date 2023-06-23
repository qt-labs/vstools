/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools.Wizards.ItemWizard
{
    using Common;
    using Core;
    using ProjectWizard;
    using QtVsTools.Common;
    using Util;

    using static QtVsTools.Common.EnumExt;

    public sealed class WidgetsClassWizard : ProjectTemplateWizard
    {
        LazyFactory Lazy { get; } = new();

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

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                InsertQObjectMacro = true,
                LowerCaseFileNames = false,
                UsePrecompiledHeader = false,
                DefaultModules = new List<string> { "core", "gui", "widgets" }
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Widgets Class Wizard")
            {
                new WizardIntroPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Widgets Class Wizard",
                    Message = @"This wizard will add a new Qt Widgets class to your project. "
                        + @"The wizard creates a .h and .cpp file. It also creates a new "
                        + @"empty form." + Environment.NewLine
                        + Environment.NewLine + "To continue, click Next.",
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
            var result = new ClassNameValidationRule().Validate(className, null);
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
            var qtProject = HelperFunctions.GetSelectedQtProject(Dte);
            if (qtProject?.UsesPrecompiledHeaders() == true)
                include.AppendLine($"#include \"{qtProject.GetPrecompiledHeaderThrough()}\"");

            include.AppendLine($"#include \"{WizardData.ClassHeaderFile}\"");
            Parameter[NewWidgetsItem.Include] = FormatParam(include);

            Parameter[NewWidgetsItem.QObject] = WizardData.InsertQObjectMacro
                                                    ? "\r\n    Q_OBJECT\r\n" : "";

            Parameter[NewWidgetsItem.UiHeaderName] =
                $"ui_{Path.GetFileNameWithoutExtension(WizardData.UiFile)}.h";

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
                Parameter[NewWidgetsItem.ForwardDeclClass] = "\r\nQT_BEGIN_NAMESPACE\r\n"
                    + $"namespace Ui {{ class {className}Class; }};\r\n" + "QT_END_NAMESPACE\r\n";
                Parameter[Meta.Asterisk] = "*";
                Parameter[Meta.Operator] = "->";
                Parameter[Meta.New] =
                    $"\r\n    , {Parameter[NewWidgetsItem.Member]}(new Ui::{className}Class())";
                Parameter[Meta.Delete] = $"\r\n    delete {Parameter[NewWidgetsItem.Member]};\r\n";
                goto case UiClassInclusion.Member;
            case UiClassInclusion.Member:
                Parameter[NewWidgetsItem.UiClassName] = $"Ui::{className}Class";
                break;
            case UiClassInclusion.MultipleInheritance:
                Parameter[NewWidgetsItem.MultipleInheritance] = $", public Ui::{className}Class";
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
            TextAndWhitespace.Adjust(Dte, projectItem.Properties.Item("FullPath").Value.ToString());
        }
    }
}
