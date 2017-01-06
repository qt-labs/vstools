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

using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.VCProjectEngine;
using QtProjectLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace QtProjectWizard
{
    public class GuiWizard : IWizard
    {
        public void RunStarted(object automation, Dictionary<string, string> replacements,
            WizardRunKind runKind, object[] customParams)
        {
            var serviceProvider = new ServiceProvider(automation as IServiceProvider);
            var iVsUIShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

            iVsUIShell.EnableModeless(0);

            try {
                System.IntPtr hwnd;
                iVsUIShell.GetDialogOwnerHwnd(out hwnd);

                try {
                    var className = replacements["$safeprojectname$"];
                    className = Regex.Replace(className, @"[^a-zA-Z0-9_]", string.Empty);
                    className = Regex.Replace(className, @"^[\d-]*\s*", string.Empty);
                    var result = new ClassNameValidationRule().Validate(className, null);
                    if (result != ValidationResult.ValidResult)
                        className = @"QtGuiApplication";

                    data.ClassName = className;
                    data.BaseClass = @"QMainWindow";
                    data.ClassHeaderFile = className + @".h";
                    data.ClassSourceFile = className + @".cpp";
                    data.UiFile = data.ClassName + @".ui";
                    data.QrcFile = data.ClassName + @".qrc";

                    var wizard = new WizardWindow(new List<WizardPage> {
                        new IntroPage {
                            Data = data,
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
                        new ModulePage {
                            Data = data,
                            Header = @"Welcome to the Qt GUI Application Wizard",
                            Message = @"Select the modules you want to include in your project. The "
                                + @"recommended modules for this project are selected by default.",
                            PreviousButtonEnabled = true,
                            NextButtonEnabled = true,
                            FinishButtonEnabled = false,
                            CancelButtonEnabled = true
                        },
                        new GuiPage {
                            Data = data,
                            Header = @"Welcome to the Qt GUI Application Wizard",
                            Message = @"This wizard generates a Qt GUI application project. The "
                                + @"application derives from QApplication and includes an empty "
                                + @"widget.",
                            PreviousButtonEnabled = true,
                            NextButtonEnabled = false,
                            FinishButtonEnabled = data.DefaultModules.All(QtModuleInfo.IsInstalled),
                            CancelButtonEnabled = true
                        }
                    })
                    {
                        Title = @"Qt GUI Application Wizard"
                    };
                    WindowHelper.ShowModal(wizard, hwnd);
                    if (!wizard.DialogResult.HasValue || !wizard.DialogResult.Value)
                        throw new System.Exception("Unexpected wizard return value.");
                } catch (QtVSException exception) {
                    Messages.DisplayErrorMessage(exception.Message);
                    throw; // re-throw, but keep the original exception stack intact
                }

                var version = (automation as DTE).Version;
                replacements["$ToolsVersion$"] = version;

                var vm = QtVersionManager.The();
                var vi = VersionInformation.Get(vm.GetInstallPath(vm.GetDefaultVersion()));
                replacements["$Platform$"] = vi.GetVSPlatformName();

                replacements["$Keyword$"] = Resources.qtProjectKeyword;
                replacements["$ProjectGuid$"] = @"{B12702AD-ABFB-343A-A199-8E24837244A3}";
                replacements["$PlatformToolset$"] = version.Replace(".", string.Empty);

                replacements["$classname$"] = data.ClassName;
                replacements["$baseclass$"] = data.BaseClass;
                replacements["$sourcefilename$"] = data.ClassSourceFile;
                replacements["$headerfilename$"] = data.ClassHeaderFile;
                replacements["$uifilename$"] = data.UiFile;

                replacements["$precompiledheader$"] = string.Empty;
                replacements["$precompiledsource$"] = string.Empty;
                replacements["$DefaultApplicationIcon$"] = string.Empty;
                replacements["$centralwidget$"] = string.Empty;

                var strHeaderInclude = data.ClassHeaderFile;
                if (data.UsePrecompiledHeader) {
                    strHeaderInclude = "stdafx.h\"\r\n#include \"" + data.ClassHeaderFile;
                    replacements["$precompiledheader$"] = "<None Include=\"stdafx.h\" />";
                    replacements["$precompiledsource$"] = "<None Include=\"stdafx.cpp\" />";
                }

                replacements["$include$"] = strHeaderInclude;
                replacements["$ui_hdr$"] = "ui_" + Path.GetFileNameWithoutExtension(data.UiFile)
                    + ".h";
                replacements["$qrcfilename$"] = data.QrcFile;

                if (data.BaseClass == "QMainWindow") {
                    replacements["$centralwidget$"] =
                        "\r\n  <widget class=\"QMenuBar\" name=\"menuBar\" />"
                        + "\r\n  <widget class=\"QToolBar\" name=\"mainToolBar\" />"
                        + "\r\n  <widget class=\"QWidget\" name=\"centralWidget\" />"
                        + "\r\n  <widget class=\"QStatusBar\" name=\"statusBar\" />";
                }

                if (data.AddDefaultAppIcon)
                    replacements["$DefaultApplicationIcon$"] = "<None Include=\"gui.ico\" />";
            } catch {
                try {
                    Directory.Delete(replacements["$destinationdirectory$"]);
                    Directory.Delete(replacements["$solutiondirectory$"]);
                } catch { }

                iVsUIShell.EnableModeless(1);
                throw new WizardBackoutException();
            }

            iVsUIShell.EnableModeless(1);
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public void ProjectFinishedGenerating(Project project)
        {
            var qtProject = QtProject.Create(project);

            QtVSIPSettings.SaveUicDirectory(project, null);
            QtVSIPSettings.SaveMocDirectory(project, null);
            QtVSIPSettings.SaveMocOptions(project, null);
            QtVSIPSettings.SaveRccDirectory(project, null);
            QtVSIPSettings.SaveLUpdateOnBuild(project);
            QtVSIPSettings.SaveLUpdateOptions(project, null);
            QtVSIPSettings.SaveLReleaseOptions(project, null);

            var vm = QtVersionManager.The();
            var qtVersion = vm.GetDefaultVersion();
            var vi = VersionInformation.Get(vm.GetInstallPath(qtVersion));
            if (vi.GetVSPlatformName() != "Win32")
                qtProject.SelectSolutionPlatform(vi.GetVSPlatformName());
            vm.SaveProjectQtVersion(project, qtVersion);

            qtProject.MarkAsQtProject("v1.0");
            qtProject.AddDirectories();

            var type = TemplateType.Application | TemplateType.GUISystem;
            qtProject.WriteProjectBasicConfigurations(type, data.UsePrecompiledHeader);

            qtProject.AddModule(QtModule.Main);
            foreach (var module in data.Modules)
                qtProject.AddModule(QtModules.Instance.ModuleIdByName(module));

            var vcProject = qtProject.VCProject;
            var files = vcProject.GetFilesWithItemType(@"None") as IVCCollection;
            foreach (var vcFile in files)
                vcProject.RemoveFile(vcFile);

            if (data.UsePrecompiledHeader) {
                qtProject.AddFileToProject(@"stdafx.cpp", Filters.SourceFiles());
                qtProject.AddFileToProject(@"stdafx.h", Filters.HeaderFiles());
            }

            qtProject.AddFileToProject(data.ClassSourceFile, Filters.SourceFiles());
            qtProject.AddFileToProject(data.ClassHeaderFile, Filters.HeaderFiles());
            qtProject.AddFileToProject(data.UiFile, Filters.FormFiles());
            var qrc = qtProject.CreateQrcFile(data.ClassName, data.QrcFile);
            qtProject.AddFileToProject(qrc, Filters.ResourceFiles());

            if (data.AddDefaultAppIcon) {
                try {
                    var icon = vcProject.ProjectDirectory + "\\" + vcProject.ItemName + ".ico";
                    if (!File.Exists(icon)) {
                        File.Move(vcProject.ProjectDirectory + "\\gui.ico", icon);
                        var attribs = File.GetAttributes(icon);
                        File.SetAttributes(icon, attribs & (~FileAttributes.ReadOnly));
                    }

                    var rcFile = vcProject.ProjectDirectory + "\\" + vcProject.ItemName + ".rc";
                    if (!File.Exists(rcFile)) {
                        FileStream fs = null;
                        try {
                            fs = File.Create(rcFile);
                            using (var sw = new StreamWriter(fs)) {
                                fs = null;
                                sw.WriteLine("IDI_ICON1\t\tICON\t\tDISCARDABLE\t\""
                                    + vcProject.ItemName + ".ico\"" + sw.NewLine);
                            }
                        } finally {
                            if (fs != null)
                                fs.Dispose();
                        }
                        vcProject.AddFile(rcFile);
                    }
                } catch { }
            }

            foreach (VCFile file in (IVCCollection) qtProject.VCProject.Files)
                qtProject.AdjustWhitespace(file.FullPath);

            qtProject.SetQtEnvironment(qtVersion);
            qtProject.Finish(); // Collapses all project nodes.
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
        }

        private readonly WizardData data = new WizardData
        {
            DefaultModules = new List<string> {
                @"QtCore", @"QtGui", @"QtWidgets"
            }
        };
    }
}
