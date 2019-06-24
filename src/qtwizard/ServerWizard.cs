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
using QtVsTools.VisualStudio;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;

namespace QtProjectWizard
{
    public class ServerWizard : IWizard
    {
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
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

            qtProject.MarkAsQtProject();
            qtProject.AddDirectories();

            var type = TemplateType.DynamicLibrary | TemplateType.GUISystem;
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
            qtProject.AddFileToProject(safeprojectname + @".rc", null);
            qtProject.AddFileToProject(safeprojectname + @".ico", null);
            qtProject.AddFileToProject(safeprojectname + @".def", Filters.SourceFiles());

            qtProject.AddActiveQtBuildStep(@"1.0", safeprojectname + @".def");

            foreach (VCFile file in (IVCCollection) qtProject.VCProject.Files)
                qtProject.AdjustWhitespace(file.FullPath);

            qtProject.SetQtEnvironment(qtVersion);
            qtProject.Finish(); // Collapses all project nodes.
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
        }

        public void RunStarted(object automation, Dictionary<string, string> replacements,
            WizardRunKind runKind, object[] customParams)
        {
            var serviceProvider = new ServiceProvider(automation as IServiceProvider);
            var iVsUIShell = VsServiceProvider.GetService<SVsUIShell, IVsUIShell>();

            iVsUIShell.EnableModeless(0);

            var versionMgr = QtVersionManager.The();
            var versionName = versionMgr.GetDefaultVersion();
            var versionInfo = VersionInformation.Get(versionMgr.GetInstallPath(versionName));
            if (versionInfo.isWinRT()) {
                MessageBox.Show(
                    string.Format(
                        "The Qt ActiveQt Server project type is not available\r\n" +
                        "for the currently selected Qt version ({0}).", versionName),
                    "Project Type Not Available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                iVsUIShell.EnableModeless(1);
                throw new WizardBackoutException();
            }

            try {
                System.IntPtr hwnd;
                iVsUIShell.GetDialogOwnerHwnd(out hwnd);

                try {
                    // midl.exe does not support spaces in project name. Fails while generating the
                    // IDL file (library attribute), e.g. 'library Active QtServer1Lib' is illegal.
                    if (replacements["$safeprojectname$"].Contains(" "))
                        throw new QtVSException("Project name shall not contain spaces.");

                    safeprojectname = replacements["$safeprojectname$"];
                    var result = new ClassNameValidationRule().Validate(safeprojectname, null);
                    if (result != ValidationResult.ValidResult)
                        safeprojectname = @"ActiveQtServer";

                    data.ClassName = safeprojectname;
                    data.ClassHeaderFile = safeprojectname + @".h";
                    data.ClassSourceFile = safeprojectname + @".cpp";
                    data.UiFile = data.ClassName + @".ui";

                    var wizard = new WizardWindow(new List<WizardPage> {
                        new IntroPage {
                            Data = data,
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
                        new ModulePage {
                            Data = data,
                            Header = @"Welcome to the Qt ActiveQt Server Wizard",
                            Message = @"Select the modules you want to include in your project. The "
                                + @"recommended modules for this project are selected by default.",
                            PreviousButtonEnabled = true,
                            NextButtonEnabled = true,
                            FinishButtonEnabled = false,
                            CancelButtonEnabled = true
                        },
                        new ServerPage {
                            Data = data,
                            Header = @"Welcome to the Qt ActiveQt Server Wizard",
                            Message = @"This wizard generates a Qt ActiveQt server project. It "
                                + @"creates a simple ActiveQt widget with the required files.",
                            PreviousButtonEnabled = true,
                            NextButtonEnabled = false,
                            FinishButtonEnabled = data.DefaultModules.All(QtModuleInfo.IsInstalled),
                            CancelButtonEnabled = true
                        }
                    })
                    {
                        Title = @"Qt ActiveQt Server Wizard"
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

                replacements["$Platform$"] = versionInfo.GetVSPlatformName();

                replacements["$Keyword$"] = Resources.qtProjectKeyword;
                replacements["$ProjectGuid$"] = @"{B12702AD-ABFB-343A-A199-8E24837244A3}";
                replacements["$PlatformToolset$"] = BuildConfig.PlatformToolset(version);

                replacements["$classname$"] = data.ClassName;
                replacements["$sourcefilename$"] = data.ClassSourceFile;
                replacements["$headerfilename$"] = data.ClassHeaderFile;
                replacements["$uifilename$"] = data.UiFile;

                replacements["$precompiledheader$"] = string.Empty;
                replacements["$precompiledsource$"] = string.Empty;
                var strHeaderInclude = data.ClassHeaderFile;
                if (data.UsePrecompiledHeader) {
                    strHeaderInclude = "stdafx.h\"\r\n#include \"" + data.ClassHeaderFile;
                    replacements["$precompiledheader$"] = "<None Include=\"stdafx.h\" />";
                    replacements["$precompiledsource$"] = "<None Include=\"stdafx.cpp\" />";
                }

                replacements["$include$"] = strHeaderInclude;
                replacements["$ui_hdr$"] = "ui_" + Path.GetFileNameWithoutExtension(data.UiFile)
                    + ".h";

                safeprojectname = data.LowerCaseFileNames ? safeprojectname.ToLower() : safeprojectname;
                replacements["$pro_name$"] = safeprojectname;

#if (VS2019 || VS2017 || VS2015)
                string versionWin10SDK = HelperFunctions.GetWindows10SDKVersion();
                if (!string.IsNullOrEmpty(versionWin10SDK)) {
                    replacements["$WindowsTargetPlatformVersion$"] = versionWin10SDK;
                    replacements["$isSet_WindowsTargetPlatformVersion$"] = "true";
                }
#endif
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

        private string safeprojectname;
        private readonly WizardData data = new WizardData
        {
            DefaultModules = new List<string> {
                @"QtCore", @"QtGui", @"QtWidgets", @"QtAxServer"
            }
        };
    }
}
