/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Wizards.ItemWizard
{
    using Common;
    using Core;
    using ProjectWizard;
    using QtVsTools.Common;
    using Util;
    using VisualStudio;

    using static QtVsTools.Common.EnumExt;

    public sealed class QtClassWizard : ProjectTemplateWizard
    {
        LazyFactory Lazy { get; } = new();

        protected override Options TemplateType => Options.ConsoleSystem;

        enum NewClass
        {
            [String("safeitemname")] SafeItemName,
            [String("sourcefilename")] SourceFileName,
            [String("headerfilename")] HeaderFileName,
            [String("rootname")] Rootname
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

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                InsertQObjectMacro = true,
                LowerCaseFileNames = false,
                UsePrecompiledHeader = false,
                DefaultModules = new List<string> { "core" }
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Class Wizard")
            {
                new WizardIntroPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Class Wizard",
                    Message = @"This wizard will add a new Qt class to your project. The "
                        + @"wizard creates a .h and .cpp file." + Environment.NewLine
                        + Environment.NewLine + "To continue, click Next.",
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
            var result = new ClassNameValidationRule().Validate(className, null);
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
            var qtProject = HelperFunctions.GetSelectedQtProject(Dte);
            if (qtProject?.UsesPrecompiledHeaders() == true)
                include.AppendLine($"#include \"{qtProject.GetPrecompiledHeaderThrough()}\"");

            include.AppendLine($"#include \"{WizardData.ClassHeaderFile}\"");
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
            ThreadHelper.ThrowIfNotOnUIThread();
            VCRulePropertyStorageHelper.SetQtModules(Dte, WizardData.DefaultModules);
        }

        public override void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem.Object is not VCFile vcFile)
                return;

            var fullPath = vcFile.FullPath;
            TextAndWhitespace.Adjust(Dte, fullPath);

            if (HelperFunctions.IsHeaderFile(fullPath)) {
                if (!string.IsNullOrEmpty(Path.GetDirectoryName(WizardData.ClassHeaderFile)))
                    vcFile.MoveToRelativePath(WizardData.ClassHeaderFile);
                vcFile.MoveToFilter(FakeFilter.HeaderFiles());
            }

            if (HelperFunctions.IsSourceFile(fullPath)) {
                if (!string.IsNullOrEmpty(Path.GetDirectoryName(WizardData.ClassSourceFile)))
                    vcFile.MoveToRelativePath(WizardData.ClassSourceFile);
                vcFile.MoveToFilter(FakeFilter.SourceFiles());
            }
        }

        public override void RunFinished()
        {
            var rootName = Parameter[NewClass.Rootname];
            if (!string.IsNullOrEmpty(rootName))
                VsEditor.Open(rootName + ".cpp");
        }
    }
}
