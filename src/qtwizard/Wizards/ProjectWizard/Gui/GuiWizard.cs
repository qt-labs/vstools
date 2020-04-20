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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using EnvDTE;
using QtProjectLib;
using QtVsTools.Common;

namespace QtVsTools.Wizards.ProjectWizard
{
    using static EnumExt;

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
        }

        WizardData _WizardData;
        protected override WizardData WizardData => _WizardData
            ?? (_WizardData = new WizardData
            {
                DefaultModules = new List<string> { "QtCore", "QtGui", "QtWidgets" }
            });

        WizardWindow _WizardWindow;
        protected override WizardWindow WizardWindow => _WizardWindow
            ?? (_WizardWindow = new WizardWindow(title: "Qt GUI Application Wizard")
            {
                new WizardIntroPage
                {
                    Data = WizardData,
                    Header = @"Welcome to the Qt GUI Application Wizard",
                    Message = @"This wizard generates a Qt GUI application project. The "
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
                    Header = @"Welcome to the Qt GUI Application Wizard",
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
                    Header = @"Welcome to the Qt GUI Application Wizard",
                    Message = @"This wizard generates a Qt GUI application project. The "
                        + @"application derives from QApplication and includes an empty "
                        + @"widget.",
                    PreviousButtonEnabled = true,
                    NextButtonEnabled = false,
                    FinishButtonEnabled = true,
                    CancelButtonEnabled = true
                }
            });

        List<ItemDef> _ExtraItems;
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
                className = @"QtGuiApplication";

            WizardData.ClassName = className;
            WizardData.BaseClass = @"QMainWindow";
            WizardData.ClassHeaderFile = className + @".h";
            WizardData.ClassSourceFile = className + @".cpp";
            WizardData.UiFile = WizardData.ClassName + @".ui";
            WizardData.QrcFile = WizardData.ClassName + @".qrc";
        }

        protected override void BeforeTemplateExpansion()
        {
            Parameter[NewClass.ClassName] = WizardData.ClassName;
            Parameter[NewClass.BaseClass] = WizardData.BaseClass;
            Parameter[NewClass.HeaderFileName] = WizardData.ClassHeaderFile;
            Parameter[NewClass.SourceFileName] = WizardData.ClassSourceFile;
            Parameter[NewGuiProject.UiFileName] = WizardData.UiFile;

            var include = new StringBuilder();
            include.AppendLine(string.Format("#include \"{0}\"", WizardData.ClassHeaderFile));
            if (UsePrecompiledHeaders)
                include.AppendLine(string.Format("#include \"{0}\"", PrecompiledHeader.Include));
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
                _ExtraItems.Add(new ItemDef
                {
                    ItemType = "None",
                    Include = Parameter[NewProject.SafeName] + ".ico",
                    Filter = "Resource Files"
                });
                var projectIcon = Path.Combine(
                    Parameter[NewProject.DestinationDirectory],
                    Parameter[NewProject.SafeName] + ".ico");
                var templateIcon = Path.Combine(
                    Parameter[NewProject.DestinationDirectory],
                    "gui.ico");
                if (!File.Exists(projectIcon)) {
                    File.Move(templateIcon, projectIcon);
                    File.SetAttributes(projectIcon,
                        File.GetAttributes(projectIcon) & (~FileAttributes.ReadOnly));
                }
                winRcFile.AppendLine(
                    string.Format("IDI_ICON1\t\tICON\t\tDISCARDABLE\t\"{0}.ico",
                        /*{0}*/ Parameter[NewProject.SafeName]));
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
        }

        protected override void OnProjectGenerated(Project project)
        {
            var qtProject = QtProject.Create(project);
            qtProject.CreateQrcFile(WizardData.ClassName, WizardData.QrcFile);

            IWizardConfiguration configWinRT = Configurations
                .Where(whereConfigTargetIsWindowsStore)
                .FirstOrDefault();

            if (configWinRT != null) {
                var versionInfo = VersionManager.GetVersionInfo(configWinRT.QtVersion);
                var projDir = Parameter[NewProject.DestinationDirectory];
                var qmakeTmpDir = Path.Combine(projDir, "qmake_tmp");
                Directory.CreateDirectory(qmakeTmpDir);

                var dummyPro = Path.Combine(qmakeTmpDir,
                    string.Format("{0}.pro", Parameter[NewProject.SafeName]));
                File.WriteAllText(dummyPro, "SOURCES = main.cpp\r\n");

                var qmake = new QMakeImport(versionInfo, dummyPro);
                qmake.Run();

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
