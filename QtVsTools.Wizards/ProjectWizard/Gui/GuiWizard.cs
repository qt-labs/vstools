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
using EnvDTE;

namespace QtVsTools.Wizards.ProjectWizard
{
    using Core;
    using Wizards.Common;

    using static QtVsTools.Common.EnumExt;

    public class GuiWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType => Options.Application | Options.GUISystem;

        readonly Func<IWizardConfiguration, bool> whereConfigTargetIsWindowsStore
            = (IWizardConfiguration config) => config.Target.EqualTo(ProjectTargets.WindowsStore);

        enum NewClass
        {
            [String("classname")] ClassName,
            [String("baseclass")] BaseClass,
            [String("sourcefilename")] SourceFileName,
            [String("headerfilename")] HeaderFileName,
            [String("include")] Include,
        }

        enum NewGuiProject
        {
            [String("centralwidget")] CentralWidget,
            [String("qrcfilename")] QrcFileName,
            [String("uifilename")] UiFileName,
            [String("ui_hdr")] UiHeaderName,
            [String("forward_declare_class")] ForwardDeclClass,
            [String("multiple_inheritance")] MultipleInheritance,
            [String("ui_classname")] UiClassName,
            [String("member")] Member,
        }

        enum Meta
        {
            [String("namespace")] Namespace,
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
                DefaultModules = new List<string> { "QtCore", "QtGui", "QtWidgets" }
            });

        WizardWindow _WizardWindow;
        protected override WizardWindow WizardWindow => _WizardWindow
            ?? (_WizardWindow = new WizardWindow(title: "Qt Widgets Application Wizard")
            {
                new WizardIntroPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Widgets Application Wizard",
                    Message = @"This wizard generates a Qt Widgets application project. The "
                        + @"application derives from QApplication and includes an empty "
                        + @"widget." + System.Environment.NewLine
                        + System.Environment.NewLine + "To continue, click Next.",
                    PreviousButtonEnabled = false,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new ConfigPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Widgets Application Wizard",
                    Message =
                            @"Setup the configurations you want to include in your project. "
                            + @"The recommended settings for this project are selected by default.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = true,
                    FinishButtonEnabled = false,
                    CancelButtonEnabled = true
                },
                new GuiPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Widgets Application Wizard",
                    Message = @"This wizard generates a Qt Widgets application project. The "
                        + @"application derives from QApplication and includes an empty "
                        + @"widget.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            });

        readonly List<ItemDef> _ExtraItems;
        protected override IEnumerable<ItemDef> ExtraItems => _ExtraItems;

        public GuiWizard()
        {
            _ExtraItems = new List<ItemDef>
            {
                new ItemDef
                {
                    ItemType = "AppxManifest",
                    Include = "Package.appxmanifest",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
                new ItemDef
                {
                    ItemType = "Image",
                    Include = "assets/logo_store.png",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
                new ItemDef
                {
                    ItemType = "Image",
                    Include = "assets/logo_620x300.png",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
                new ItemDef
                {
                    ItemType = "Image",
                    Include = "assets/logo_150x150.png",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
                new ItemDef
                {
                    ItemType = "Image",
                    Include = "assets/logo_44x44.png",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
            };
        }

        protected override void BeforeWizardRun()
        {
            var className = Parameter[NewProject.SafeName];
            className = Regex.Replace(className, @"[^a-zA-Z0-9_]", string.Empty);
            className = Regex.Replace(className, @"^[\d-]*\s*", string.Empty);
            var result = new Util.ClassNameValidationRule().Validate(className, null);
            if (result != ValidationResult.ValidResult)
                className = @"QtWidgetsApplication";

            WizardData.ClassName = className;
            WizardData.BaseClass = @"QMainWindow";
            WizardData.ClassHeaderFile = className + @".h";
            WizardData.ClassSourceFile = className + @".cpp";
            WizardData.UiFile = WizardData.ClassName + @".ui";
            WizardData.QrcFile = WizardData.ClassName + @".qrc";
            WizardData.UiClassInclusion = UiClassInclusion.Member;

            Parameter[NewGuiProject.ForwardDeclClass] = "";
            Parameter[NewGuiProject.MultipleInheritance] = "";
            Parameter[NewGuiProject.UiClassName] = "";
            Parameter[NewGuiProject.Member] = "ui";

            Parameter[Meta.Asterisk] ="";
            Parameter[Meta.Operator] = ".";
            Parameter[Meta.Semicolon] = ";";
            Parameter[Meta.New] = "";
            Parameter[Meta.Delete] = "";
        }

        protected override void BeforeTemplateExpansion()
        {
            var array = WizardData.ClassName.Split(new[] { "::" },
                StringSplitOptions.RemoveEmptyEntries);
            var className = array.LastOrDefault();

            Parameter[NewClass.ClassName] = className;
            Parameter[NewClass.BaseClass] = WizardData.BaseClass;
            Parameter[NewClass.HeaderFileName] = WizardData.ClassHeaderFile;
            Parameter[NewClass.SourceFileName] = WizardData.ClassSourceFile;
            Parameter[NewGuiProject.UiFileName] = WizardData.UiFile;

            var include = new StringBuilder();
            if (UsePrecompiledHeaders)
                include.AppendLine(string.Format("#include \"{0}\"", PrecompiledHeader.Include));
            include.AppendLine(string.Format("#include \"{0}\"", WizardData.ClassHeaderFile));
            Parameter[NewClass.Include] = FormatParam(include);

            Parameter[NewGuiProject.UiHeaderName] = string.Format("ui_{0}.h",
                Path.GetFileNameWithoutExtension(WizardData.UiFile));
            Parameter[NewGuiProject.QrcFileName] = WizardData.QrcFile;

            if (WizardData.BaseClass == "QMainWindow") {
                Parameter[NewGuiProject.CentralWidget] = FormatParam(@"
  <widget class=""QMenuBar"" name=""menuBar"" />
  <widget class=""QToolBar"" name=""mainToolBar"" />
  <widget class=""QWidget"" name=""centralWidget"" />
  <widget class=""QStatusBar"" name=""statusBar"" />");
            }

            StringBuilder winRcFile = new StringBuilder();

            if (WizardData.AddDefaultAppIcon) {
                var projectIcon = Path.Combine(
                    Parameter[NewProject.DestinationDirectory],
                    Parameter[NewProject.SafeName] + ".ico");
                var iconExists = File.Exists(projectIcon);
                if (!iconExists) {
                    try {
                        var uri =
                            new Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
                        var pkgInstallPath
                            = Path.GetDirectoryName(Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";
                        var templateIcon
                            = Path.Combine(pkgInstallPath, @"ProjectTemplates\VC\Qt\1033\gui\gui.ico");
                        File.Copy(templateIcon, projectIcon);
                        File.SetAttributes(projectIcon,
                            File.GetAttributes(projectIcon) & (~FileAttributes.ReadOnly));
                        iconExists = true;
                    }  catch (Exception /*ex*/) {
                        // Silently ignore any error, the project is working
                        // without icon too.
                    }
                }

                if (iconExists) {
                    _ExtraItems.Add(new ItemDef
                    {
                        ItemType = "None",
                        Include = Parameter[NewProject.SafeName] + ".ico",
                        Filter = "Resource Files"
                    });
                    winRcFile.AppendLine(
                        string.Format("IDI_ICON1\t\tICON\t\tDISCARDABLE\t\"{0}.ico\"",
                            /*{0}*/ Parameter[NewProject.SafeName]));
                }
            }

            if (winRcFile.Length > 0) {
                _ExtraItems.Add(new ItemDef
                {
                    ItemType = "ResourceCompile",
                    Include = Parameter[NewProject.SafeName] + ".rc",
                    Filter = "Resource Files"
                });
                File.WriteAllText(
                    Path.Combine(
                        Parameter[NewProject.DestinationDirectory],
                        Parameter[NewProject.SafeName] + ".rc"),
                    winRcFile.ToString());
            }

            switch (WizardData.UiClassInclusion) {
            case UiClassInclusion.MemberPointer:
                Parameter[NewGuiProject.ForwardDeclClass] =
                    string.Format(
                          "\r\nQT_BEGIN_NAMESPACE\r\n"
                        + "namespace Ui {{ class {0}Class; }};\r\n"
                        + "QT_END_NAMESPACE\r\n", className
                    );
                Parameter[Meta.Asterisk] = "*";
                Parameter[Meta.Operator] = "->";
                Parameter[Meta.New] = string.Format("\r\n    , {0}(new Ui::{1}Class())",
                                                    Parameter[NewGuiProject.Member], className);
                Parameter[Meta.Delete] = string.Format("\r\n    delete {0};\r\n",
                                                       Parameter[NewGuiProject.Member]);
                goto case UiClassInclusion.Member;
            case UiClassInclusion.Member:
                Parameter[NewGuiProject.UiClassName] = string.Format("Ui::{0}Class", className);
                break;
            case UiClassInclusion.MultipleInheritance:
                Parameter[NewGuiProject.MultipleInheritance] =
                    string.Format(", public Ui::{0}Class", className);
                Parameter[NewGuiProject.Member] = "";
                Parameter[Meta.Operator] = "";
                Parameter[Meta.Semicolon] = "";
                break;
            }

            string ns = "",  nsBegin = "", nsEnd = "";
            for (var i = 0; i < array.Length - 1; ++i) {
                ns += array[i] + "::";
                nsBegin += "namespace " + array[i] + " {\r\n";
                nsEnd = "} // namespace " + array[i] + "\r\n" + nsEnd;
            }
            Parameter[Meta.Namespace] = ns;
            Parameter[Meta.NamespaceBegin] = nsBegin;
            Parameter[Meta.NamespaceEnd] = nsEnd;
        }

        protected override void OnProjectGenerated(Project project)
        {
            IWizardConfiguration configWinRT = Configurations
                .Where(whereConfigTargetIsWindowsStore)
                .FirstOrDefault();

            if (configWinRT != null) {
                var projDir = Parameter[NewProject.DestinationDirectory];
                var qmakeTmpDir = Path.Combine(projDir, "qmake_tmp");
                Directory.CreateDirectory(qmakeTmpDir);

                var dummyPro = Path.Combine(qmakeTmpDir,
                    string.Format("{0}.pro", Parameter[NewProject.SafeName]));
                File.WriteAllText(dummyPro, "SOURCES = main.cpp\r\n");

                var qmake = new QMakeImport(configWinRT.QtVersion, dummyPro);
                qmake.Run(setVCVars: true);

                var qmakeAssetsDir = Path.Combine(qmakeTmpDir, "assets");
                var projAssetsDir = Path.Combine(projDir, "assets");
                if (Directory.Exists(qmakeAssetsDir)) {
                    if (Directory.Exists(projAssetsDir))
                        Directory.Delete(projAssetsDir, recursive: true);
                    Directory.Move(qmakeAssetsDir, projAssetsDir);
                }

                var manifestFile = Path.Combine(qmakeTmpDir, "Package.appxmanifest");
                if (File.Exists(manifestFile)) {
                    File.Move(manifestFile, Path.Combine(projDir, "Package.appxmanifest"));
                }

                Directory.Delete(qmakeTmpDir, recursive: true);
            }
        }
    }
}
