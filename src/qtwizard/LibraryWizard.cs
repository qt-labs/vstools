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
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace QtProjectWizard
{
    public class LibraryWizard : IWizard
    {
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
            if (projectItem.FileCount >= 1) {
                var qtProject = QtProject.Create(projectItem.ContainingProject);
                for (short i = 0; i < projectItem.FileCount; ++i)
                    qtProject.AdjustWhitespace(projectItem.FileNames[i]);
            }
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
            var vi = new VersionInformation(vm.GetInstallPath(qtVersion));
            if (vi.GetVSPlatformName() != "Win32")
                qtProject.SelectSolutionPlatform(vi.GetVSPlatformName());
            vm.SaveProjectQtVersion(project, qtVersion);

            qtProject.MarkAsQtProject("v1.0");
            qtProject.AddDirectories();

            var type = TemplateType.GUISystem | (data.CreateStaticLibrary
                ? TemplateType.StaticLibrary : TemplateType.DynamicLibrary);
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

            qtProject.AddDefine(projectDefine, BuildConfig.Both);
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
            var iVsUIShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

            iVsUIShell.EnableModeless(0);

            try {
                System.IntPtr hwnd;
                iVsUIShell.GetDialogOwnerHwnd(out hwnd);

                var safeprojectname = replacements["$safeprojectname$"];
                safeprojectname = Regex.Replace(safeprojectname, @"[^a-zA-Z0-9_]", string.Empty);
                safeprojectname = Regex.Replace(safeprojectname, @"^[\d-]*\s*", string.Empty);
                var result = new ClassNameValidationRule().Validate(safeprojectname, null);
                if (result != ValidationResult.ValidResult)
                    safeprojectname = @"QtClassLibrary";

                try {
                    data.ClassName = safeprojectname;
                    data.ClassHeaderFile = safeprojectname + @".h";
                    data.ClassSourceFile = safeprojectname + @".cpp";

                    var wizard = new WizardWindow(new List<WizardPage> {
                        new IntroPage {
                            Data = data,
                            Header = @"Welcome to the Qt Class Library Wizard",
                            Message = @"This wizard generates a Qt Class Library project. The "
                                + @"resulting library is linked dynamically with Qt."
                                + System.Environment.NewLine + System.Environment.NewLine
                                + @"To continue, click Next.",
                            PreviousButtonEnabled = false,
                            NextButtonEnabled = true,
                            FinishButtonEnabled = false,
                            CancelButtonEnabled = true
                        },
                        new ModulePage {
                            Data = data,
                            Header = @"Welcome to the Qt Class Library Wizard",
                            Message = @"Select the modules you want to include in your project. The "
                                + @"recommended modules for this project are selected by default.",
                            PreviousButtonEnabled = true,
                            NextButtonEnabled = true,
                            FinishButtonEnabled = false,
                            CancelButtonEnabled = true
                        },
                        new LibraryClassPage {
                            Data = data,
                            Header = @"Welcome to the Qt Class Library Wizard",
                            Message = @"This wizard generates a Qt Class Library project. The "
                                + @"resulting library is linked dynamically with Qt.",
                            PreviousButtonEnabled = true,
                            NextButtonEnabled = false,
                            FinishButtonEnabled = QtModuleInfo.IsModuleInstalled(@"QtCore"),
                            CancelButtonEnabled = true
                        }
                    })
                    {
                        Title = @"Qt Class Library Wizard"
                    };
                    WindowHelper.ShowModal(wizard, hwnd);
                    if (!wizard.DialogResult.HasValue || !wizard.DialogResult.Value)
                        throw new System.Exception(@"Unexpected wizard return value.");
                } catch (QtVSException exception) {
                    Messages.DisplayErrorMessage(exception.Message);
                    throw; // re-throw, but keep the original exception stack intact
                }

                var version = (automation as DTE).Version;
                replacements["$ToolsVersion$"] = version;

                var vm = QtVersionManager.The();
                var vi = new VersionInformation(vm.GetInstallPath(vm.GetDefaultVersion()));
                replacements["$Platform$"] = vi.GetVSPlatformName();

                replacements["$Keyword$"] = Resources.qtProjectKeyword;
                replacements["$ProjectGuid$"] = @"{B12702AD-ABFB-343A-A199-8E24837244A3}";
                replacements["$PlatformToolset$"] = version.Replace(".", "");

                replacements["$classname$"] = data.ClassName;
                replacements["$sourcefilename$"] = data.ClassSourceFile;
                replacements["$headerfilename$"] = data.ClassHeaderFile;

                replacements["$precompiledheader$"] = string.Empty;
                replacements["$precompiledsource$"] = string.Empty;

                var strHeaderInclude = data.ClassHeaderFile;
                if (data.UsePrecompiledHeader) {
                    strHeaderInclude = "stdafx.h\"\r\n#include \"" + data.ClassHeaderFile;
                    replacements["$precompiledheader$"] = "<None Include=\"stdafx.h\" />";
                    replacements["$precompiledsource$"] = "<None Include=\"stdafx.cpp\" />";
                }

                replacements["$include$"] = strHeaderInclude;
                replacements["$saveglobal$"] = safeprojectname.ToLower();

                projectDefine = safeprojectname.ToUpper() + @"_LIB";
                replacements["$pro_lib_define$"] = projectDefine;
                replacements["$pro_lib_export$"] = safeprojectname.ToUpper() + @"_EXPORT";
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

        private string projectDefine;
        private readonly WizardData data = new WizardData
        {
            DefaultModules = new List<string> { @"QtCore" }
        };
    }
}
