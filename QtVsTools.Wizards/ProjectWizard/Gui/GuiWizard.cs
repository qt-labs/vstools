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

namespace QtVsTools.Wizards.ProjectWizard
{
    using Common;
    using Core;
    using QtVsTools.Common;
    using QtVsTools.Core.Common;

    using static QtVsTools.Common.EnumExt;

    public class GuiWizard : ProjectTemplateWizard
    {
        protected override Options TemplateType => Options.Application | Options.GUISystem;

        readonly Func<IWizardConfiguration, bool> whereConfigTargetIsWindowsStore
            = config => config.Target.EqualTo(ProjectTargets.WindowsStore);

        enum NewGuiProject
        {
            [String("centralwidget")] CentralWidget,
            [String("qrcfilename")] QrcFileName,
            [String("uiresources")] UiResources,
            [String("ui_hdr")] UiHeaderName,
            [String("forward_declare_class")] ForwardDeclClass,
            [String("multiple_inheritance")] MultipleInheritance,
            [String("ui_classname")] UiClassName,
            [String("member")] Member
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

        protected override WizardData WizardData => Lazy.Get(() =>
            WizardData, () => new WizardData
            {
                DefaultModules = new List<string> { "QtCore", "QtGui", "QtWidgets" }
            });

        protected override WizardWindow WizardWindow => Lazy.Get(() =>
            WizardWindow, () => new WizardWindow(title: "Qt Widgets Application Wizard")
            {
                new WizardIntroPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt Widgets Application Wizard",
                    Message = @"This wizard generates a Qt Widgets application project. The "
                        + @"application derives from QApplication and includes an empty "
                        + @"widget." + Environment.NewLine
                        + Environment.NewLine + "To continue, click Next.",
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

        readonly List<ItemDef> GuiExtraItems;
        protected override IEnumerable<ItemDef> ExtraItems => GuiExtraItems;

        public GuiWizard()
        {
            GuiExtraItems = new List<ItemDef>
            {
                new()
                {
                    ItemType = "AppxManifest",
                    Include = "Package.appxmanifest",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
                new()
                {
                    ItemType = "Image",
                    Include = "assets/logo_store.png",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
                new()
                {
                    ItemType = "Image",
                    Include = "assets/logo_620x300.png",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
                new()
                {
                    ItemType = "Image",
                    Include = "assets/logo_150x150.png",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                },
                new()
                {
                    ItemType = "Image",
                    Include = "assets/logo_44x44.png",
                    Filter = "Resource Files",
                    WhereConfig = whereConfigTargetIsWindowsStore
                }
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
            Parameter[NewClass.UiFileName] = WizardData.UiFile;

            var include = new StringBuilder();
            if (UsePrecompiledHeaders)
                include.AppendLine($"#include \"{PrecompiledHeader.Include}\"");
            include.AppendLine($"#include \"{WizardData.ClassHeaderFile}\"");
            Parameter[NewClass.Include] = FormatParam(include);

            Parameter[NewGuiProject.UiHeaderName] =
                $"ui_{Path.GetFileNameWithoutExtension(WizardData.UiFile)}.h";
            Parameter[NewGuiProject.QrcFileName] = WizardData.QrcFile;
            Parameter[NewGuiProject.UiResources] = WizardData.ProjectModel switch
            {
                WizardData.ProjectModels.CMake => "",
                _ => $@"
 <resources>
   <include location=""{WizardData.QrcFile}""/>
 </resources>
".Trim('\r', '\n')
            };

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
                            File.GetAttributes(projectIcon) & ~FileAttributes.ReadOnly);
                        iconExists = true;
                    }  catch (Exception /*ex*/) {
                        // Silently ignore any error, the project is working
                        // without icon too.
                    }
                }

                if (iconExists) {
                    GuiExtraItems.Add(new ItemDef
                    {
                        ItemType = "None",
                        Include = Parameter[NewProject.SafeName] + ".ico",
                        Filter = "Resource Files"
                    });
                    winRcFile.AppendLine(
                        $"IDI_ICON1\t\tICON\t\tDISCARDABLE\t\"{Parameter[NewProject.SafeName]}.ico\"");
                }
            }

            if (winRcFile.Length > 0) {
                GuiExtraItems.Add(new ItemDef
                {
                    ItemType = "ResourceCompile",
                    Include = Parameter[NewProject.SafeName] + ".rc",
                    Filter = "Resource Files"
                });
                File.WriteAllText(Path.Combine(Parameter[NewProject.DestinationDirectory],
                    Parameter[NewProject.ResourceFile] = Parameter[NewProject.SafeName] + ".rc"),
                    winRcFile.ToString());
            }

            switch (WizardData.UiClassInclusion) {
            case UiClassInclusion.MemberPointer:
                Parameter[NewGuiProject.ForwardDeclClass] = "\r\nQT_BEGIN_NAMESPACE\r\n"
                    + $"namespace Ui {{ class {className}Class; }};\r\n" + "QT_END_NAMESPACE\r\n";
                Parameter[Meta.Asterisk] = "*";
                Parameter[Meta.Operator] = "->";
                Parameter[Meta.New] =
                    $"\r\n    , {Parameter[NewGuiProject.Member]}(new Ui::{className}Class())";
                Parameter[Meta.Delete] = $"\r\n    delete {Parameter[NewGuiProject.Member]};\r\n";
                goto case UiClassInclusion.Member;
            case UiClassInclusion.Member:
                Parameter[NewGuiProject.UiClassName] = $"Ui::{className}Class";
                break;
            case UiClassInclusion.MultipleInheritance:
                Parameter[NewGuiProject.MultipleInheritance] = $", public Ui::{className}Class";
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

                var dummyPro = Path.Combine(qmakeTmpDir, $"{Parameter[NewProject.SafeName]}.pro");
                File.WriteAllText(dummyPro, "SOURCES = main.cpp\r\n");

                QMakeImport.Run(configWinRT.QtVersion, dummyPro);

                var qmakeAssetsDir = Path.Combine(qmakeTmpDir, "assets");
                var projAssetsDir = Path.Combine(projDir, "assets");
                if (Directory.Exists(qmakeAssetsDir)) {
                    Utils.DeleteDirectory(projAssetsDir, Utils.Option.Recursive);
                    Directory.Move(qmakeAssetsDir, projAssetsDir);
                }

                var manifestFile = Path.Combine(qmakeTmpDir, "Package.appxmanifest");
                if (File.Exists(manifestFile)) {
                    File.Move(manifestFile, Path.Combine(projDir, "Package.appxmanifest"));
                }

                Utils.DeleteDirectory(qmakeTmpDir, Utils.Option.Recursive);
            }
        }
    }
}
