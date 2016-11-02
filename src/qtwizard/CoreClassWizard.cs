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

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using QtProjectLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QtProjectWizard
{
    public class CoreClassWizard : IClassWizard
    {
        public WizardResult Run(EnvDTE.DTE dte, string name, string location)
        {
            var serviceProvider = new ServiceProvider(dte as IServiceProvider);
            var iVsUIShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;

            iVsUIShell.EnableModeless(0);

            try {
                System.IntPtr hwnd;
                iVsUIShell.GetDialogOwnerHwnd(out hwnd);

                try {
                    if (string.IsNullOrEmpty(name))
                        name = @"QtClass";

                    data.ClassName = name;
                    data.BaseClass = @"QObject";
                    data.ConstructorSignature = "QObject *parent";
                    data.InsertQObjectMacro = true;
                    data.ClassHeaderFile = name + @".h";
                    data.ClassSourceFile = name + @".cpp";

                    var wizard = new WizardWindow(new List<WizardPage> {
                        new IntroPage {
                            Data = data,
                            Header = @"Welcome to the Qt Class Wizard",
                            Message = @"This wizard will add a new Qt5 class to your project. The "
                                + @"wizard creates a .h and .cpp file." + System.Environment.NewLine
                                + System.Environment.NewLine + "To continue, click Next.",
                            PreviousButtonEnabled = false,
                            NextButtonEnabled = true,
                            FinishButtonEnabled = false,
                            CancelButtonEnabled = true
                        },
                        new CoreClassPage {
                            Data = data,
                            Header = @"Welcome to the Qt Class Wizard",
                            Message = @"This wizard will add a new Qt5 class to your project. The "
                                + @"wizard creates a .h and .cpp file.",
                            PreviousButtonEnabled = true,
                            NextButtonEnabled = false,
                            FinishButtonEnabled = QtModuleInfo.IsModuleInstalled(@"QtCore"),
                            CancelButtonEnabled = true
                        }
                    })
                    {
                        Title = @"Qt Class Wizard"
                    };
                    WindowHelper.ShowModal(wizard, hwnd);
                    if (!wizard.DialogResult.HasValue || !wizard.DialogResult.Value)
                        throw new System.Exception("Unexpected wizard return value.");

                    var array = data.ClassName.Split(new[] { "::" }, System
                        .StringSplitOptions.RemoveEmptyEntries);
                    data.ClassName = array.LastOrDefault();

                    string nsBegin = string.Empty, nsEnd = string.Empty;
                    for (var i = 0; i < array.Length - 1; ++i) {
                        nsBegin += "namespace " + array[i] + " {\r\n";
                        nsEnd = "} // namespace " + array[i] + "\r\n" + nsEnd;
                    }

                    var pro = HelperFunctions.GetSelectedQtProject(dte);
                    if (pro == null)
                        throw new QtVSException("Can't find a selected project");

                    var qtProject = QtProject.Create(pro);

                    var hppFile = AddProjectItemHpp(location);
                    ReplaceNamespaceToken(hppFile, nsBegin, nsEnd);

                    qtProject.AdjustWhitespace(hppFile);
                    qtProject.AddFileToProject(hppFile, Filters.HeaderFiles());
                    VsShellUtilities.OpenDocument(serviceProvider, hppFile);

                    var pch = string.Empty;
                    if (qtProject.UsesPrecompiledHeaders())
                        pch = qtProject.GetPrecompiledHeaderThrough();

                    var cppFile = AddProjectItemCpp(location, pch);
                    ReplaceNamespaceToken(cppFile, nsBegin, nsEnd);

                    qtProject.AdjustWhitespace(cppFile);
                    qtProject.AddFileToProject(cppFile, Filters.SourceFiles());
                    VsShellUtilities.OpenDocument(serviceProvider, cppFile);

                } catch (QtVSException exception) {
                    Messages.DisplayErrorMessage(exception.Message);
                    throw; // re-throw, but keep the original exception stack intact
                }
            } catch {
                throw new WizardBackoutException();
            } finally {
                iVsUIShell.EnableModeless(1);
            }

            return WizardResult.Finished;
        }

        private readonly WizardData data = new WizardData
        {
            DefaultModules = new List<string> {
                @"QtCore"
            }
        };

        private static void ReplaceNamespaceToken(string file, string nsBegin, string nsEnd)
        {
            if (!string.IsNullOrEmpty(nsBegin))
                nsBegin += System.Environment.NewLine;
            QtProject.ReplaceTokenInFile(file, "%NAMESPACE_BEGIN%", nsBegin);

            if (!string.IsNullOrEmpty(nsEnd))
                nsEnd = System.Environment.NewLine + nsEnd;
            QtProject.ReplaceTokenInFile(file, "%NAMESPACE_END%", nsEnd);
        }

        private string AddProjectItemHpp(string location)
        {
            var hppFile = Path.GetTempFileName();
            using (var tmp = new StreamWriter(hppFile)) {
                tmp.Write("#pragma once\r\n\r\n"
                    + "%BASECLASSINCLUDE%%NAMESPACE_BEGIN%class %CLASS%%BASEDECL%\r\n"
                    + "{%Q_OBJECT%\r\n"
                    + "public:\r\n"
                    + "    %CLASS%(%CTORSIG%);\r\n"
                    + "    ~%CLASS%();\r\n"
                    + "};\r\n"
                    + "%NAMESPACE_END%");
            }
            hppFile = QtProject.CopyFileToFolder(hppFile, location, data.ClassHeaderFile);

            QtProject.ReplaceTokenInFile(hppFile, "%CLASS%", data.ClassName);
            QtProject.ReplaceTokenInFile(hppFile, "%BASECLASS%", data.BaseClass);

            if (string.IsNullOrEmpty(data.BaseClass)) {
                QtProject.ReplaceTokenInFile(hppFile, "%BASEDECL%", string.Empty);
                QtProject.ReplaceTokenInFile(hppFile, "%BASECLASSINCLUDE%", string.Empty);
                QtProject.ReplaceTokenInFile(hppFile, "%Q_OBJECT%", string.Empty);
                QtProject.ReplaceTokenInFile(hppFile, "%CTORSIG%", string.Empty);
            } else {
                QtProject.ReplaceTokenInFile(hppFile, "%BASEDECL%", " : public "
                    + data.BaseClass);
                QtProject.ReplaceTokenInFile(hppFile, "%BASECLASSINCLUDE%", "#include <"
                    + data.BaseClass + ">\r\n\r\n");
                if (data.InsertQObjectMacro)
                    QtProject.ReplaceTokenInFile(hppFile, "%Q_OBJECT%", "\r\n\tQ_OBJECT\r\n");
                QtProject.ReplaceTokenInFile(hppFile, "%CTORSIG%", data.ConstructorSignature);
            }

            return hppFile;
        }

        private string AddProjectItemCpp(string location, string pch)
        {
            var cppFile = Path.GetTempFileName();
            using (var tmp = new StreamWriter(cppFile)) {
                tmp.Write("#include \"%INCLUDE%\"\r\n\r\n"
                    + "%NAMESPACE_BEGIN%%CLASS%::%CLASS%(%CTORSIG%)%BASECLASS%\r\n"
                    + "{\r\n}\r\n\r\n"
                    + "%CLASS%::~%CLASS%()\r\n"
                    + "{\r\n}\r\n"
                    + "%NAMESPACE_END%");
            }
            cppFile = QtProject.CopyFileToFolder(cppFile, location, data.ClassSourceFile);

            if (!string.IsNullOrEmpty(pch))
                QtProject.ReplaceTokenInFile(cppFile, "%INCLUDE%", pch + "\"\r\n#include \"%INCLUDE%");

            QtProject.ReplaceTokenInFile(cppFile, "%INCLUDE%", data.ClassHeaderFile);
            QtProject.ReplaceTokenInFile(cppFile, "%CLASS%", data.ClassName);

            QtProject.ReplaceTokenInFile(cppFile, "%BASECLASS%", string.IsNullOrEmpty(data
                .ConstructorSignature) ? "" : "\r\n    : " + data.BaseClass + "(parent)");
            QtProject.ReplaceTokenInFile(cppFile, "%CTORSIG%", data.ConstructorSignature);

            return cppFile;
        }
    }
}
