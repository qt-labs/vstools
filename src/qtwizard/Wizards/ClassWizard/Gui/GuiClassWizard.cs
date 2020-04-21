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
using QtVsTools.VisualStudio;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QtVsTools.Wizards.ClassWizard
{
    public class GuiClassWizard : IClassWizard
    {
        public WizardResult Run(EnvDTE.DTE dte, string name, string location)
        {
            var serviceProvider = new ServiceProvider(dte as IServiceProvider);
            var iVsUIShell = VsServiceProvider.GetService<SVsUIShell, IVsUIShell>();

            iVsUIShell.EnableModeless(0);

            try {
                System.IntPtr hwnd;
                iVsUIShell.GetDialogOwnerHwnd(out hwnd);

                try {
                    if (string.IsNullOrEmpty(name))
                        name = @"QtWidgetsClass";

                    data.ClassName = name;
                    data.BaseClass = @"QWidget";
                    data.ClassHeaderFile = name + @".h";
                    data.ClassSourceFile = name + @".cpp";
                    data.UiFile = data.ClassName + @".ui";

                    var wizard = new WizardWindow(new List<WizardPage> {
                        new WizardIntroPage {
                            Data = data,
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
                        new GuiClassPage {
                            Data = data,
                            Header = @"Welcome to the Qt Widgets Class Wizard",
                            Message = @"This wizard will add a new Qt Widgets class to your project. "
                                + @"The wizard creates a .h and .cpp file. It also creates a new "
                                + @"empty form.",
                            PreviousButtonEnabled = true,
                            NextButtonEnabled = false,
                            FinishButtonEnabled = true,
                            CancelButtonEnabled = true
                        }
                    })
                    {
                        Title = @"Qt Widgets Class Wizard"
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

                    var uiFile = AddProjectItemUi(location);
                    qtProject.AdjustWhitespace(uiFile);
                    qtProject.AddFileToProject(uiFile, Filters.FormFiles());

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
                @"QtCore", @"QtGui", @"QtWidgets"
            }
        };

        private const string MemberClassHeader =
            "#pragma once\r\n"
            + "\r\n"
            + "#include <%BASECLASS%>\r\n"
            + "#include \"%UI_HDR%\"\r\n"
            + "\r\n"
            + "%NAMESPACE_BEGIN%class %CLASS% : public %BASECLASS%\r\n"
            + "{\r\n"
            + "    Q_OBJECT\r\n"
            + "\r\n"
            + "public:\r\n"
            + "    %CLASS%(QWidget *parent = Q_NULLPTR);\r\n"
            + "    ~%CLASS%();\r\n"
            + "\r\n"
            + "private:\r\n"
            + "    Ui::%CLASS% ui;\r\n"
            + "};\r\n"
            + "%NAMESPACE_END%";

        private const string MemberClassSource =
            "#include \"%INCLUDE%\"\r\n"
            + "\r\n"
            + "%NAMESPACE_BEGIN%%CLASS%::%CLASS%(QWidget *parent)\r\n"
            + "    : %BASECLASS%(parent)\r\n"
            + "{\r\n"
            + "    ui.setupUi(this);\r\n"
            + "}\r\n"
            + "\r\n"
            + "%CLASS%::~%CLASS%()\r\n"
            + "{\r\n"
            + "}\r\n"
            + "%NAMESPACE_END%";

        private const string MemberPointerClassHeader =
            "#pragma once\r\n"
            + "\r\n"
            + "#include <%BASECLASS%>\r\n"
            + "namespace Ui { class %CLASS%; };\r\n"
            + "\r\n"
            + "%NAMESPACE_BEGIN%class %CLASS% : public %BASECLASS%\r\n"
            + "{\r\n"
            + "    Q_OBJECT\r\n"
            + "\r\n"
            + "public:\r\n"
            + "    %CLASS%(QWidget *parent = Q_NULLPTR);\r\n"
            + "    ~%CLASS%();\r\n"
            + "\r\n"
            + "private:\r\n"
            + "    Ui::%CLASS% *ui;\r\n"
            + "};\r\n"
            + "%NAMESPACE_END%";

        private const string MemberPointerClassSource =
            "#include \"%INCLUDE%\"\r\n"
            + "#include \"%UI_HDR%\"\r\n"
            + "\r\n"
            + "%NAMESPACE_BEGIN%%CLASS%::%CLASS%(QWidget *parent)\r\n"
            + "    : %BASECLASS%(parent)\r\n"
            + "{\r\n"
            + "    ui = new Ui::%CLASS%();\r\n"
            + "    ui->setupUi(this);\r\n"
            + "}\r\n"
            + "\r\n"
            + "%CLASS%::~%CLASS%()\r\n"
            + "{\r\n"
            + "    delete ui;\r\n"
            + "}\r\n"
            + "%NAMESPACE_END%";

        private const string InheritanceClassHeader =
            "#pragma once\r\n"
            + "\r\n"
            + "#include <%BASECLASS%>\r\n"
            + "#include \"%UI_HDR%\"\r\n"
            + "\r\n"
            + "%NAMESPACE_BEGIN%class %CLASS% : public %BASECLASS%, public Ui::%CLASS%\r\n"
            + "{\r\n"
            + "    Q_OBJECT\r\n"
            + "\r\n"
            + "public:\r\n"
            + "    %CLASS%(QWidget *parent = Q_NULLPTR);\r\n"
            + "    ~%CLASS%();\r\n"
            + "};\r\n"
            + "%NAMESPACE_END%";

        private const string InheritanceClassSource =
            "#include \"%INCLUDE%\"\r\n"
            + "\r\n"
            + "%NAMESPACE_BEGIN%%CLASS%::%CLASS%(QWidget *parent)\r\n"
            + "    : %BASECLASS%(parent)\r\n"
            + "{\r\n"
            + "    setupUi(this);\r\n"
            + "}\r\n"
            + "\r\n"
            + "%CLASS%::~%CLASS%()\r\n"
            + "{\r\n"
            + "}\r\n"
            + "%NAMESPACE_END%";

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
            var replaceUiHeader = true;
            var hppFile = Path.GetTempFileName();
            using (var tmp = new StreamWriter(hppFile)) {
                var content = string.Empty;
                switch (data.UiClassInclusion) {
                case UiClassInclusion.Member:
                    content = MemberClassHeader;
                    break;
                case UiClassInclusion.MemberPointer:
                    replaceUiHeader = false;
                    content = MemberPointerClassHeader;
                    break;
                case UiClassInclusion.MultipleInheritance:
                    content = InheritanceClassHeader;
                    break;
                }
                tmp.Write(content);
            }
            hppFile = QtProject.CopyFileToFolder(hppFile, location, data.ClassHeaderFile);

            QtProject.ReplaceTokenInFile(hppFile, "%CLASS%", data.ClassName);
            QtProject.ReplaceTokenInFile(hppFile, "%BASECLASS%", data.BaseClass);
            if (replaceUiHeader) {
                QtProject.ReplaceTokenInFile(hppFile, "%UI_HDR%", "ui_" + Path
                    .GetFileNameWithoutExtension(data.UiFile) + ".h");
            }

            return hppFile;
        }

        private string AddProjectItemCpp(string location, string pch)
        {
            var replaceUiHeader = false;
            var cppFile = Path.GetTempFileName();

            using (var tmp = new StreamWriter(cppFile)) {
                var content = string.Empty;
                switch (data.UiClassInclusion) {
                case UiClassInclusion.Member:
                    content = MemberClassSource;
                    break;
                case UiClassInclusion.MemberPointer:
                    replaceUiHeader = true;
                    content = MemberPointerClassSource;
                    break;
                case UiClassInclusion.MultipleInheritance:
                    content = InheritanceClassSource;
                    break;
                }
                tmp.Write(content);
            }
            cppFile = QtProject.CopyFileToFolder(cppFile, location, data.ClassSourceFile);

            if (!string.IsNullOrEmpty(pch))
                QtProject.ReplaceTokenInFile(cppFile, "%INCLUDE%", pch + "\"\r\n#include \"%INCLUDE%");

            QtProject.ReplaceTokenInFile(cppFile, "%INCLUDE%", data.ClassHeaderFile);
            QtProject.ReplaceTokenInFile(cppFile, "%CLASS%", data.ClassName);
            QtProject.ReplaceTokenInFile(cppFile, "%BASECLASS%", data.BaseClass);
            if (replaceUiHeader) {
                QtProject.ReplaceTokenInFile(cppFile, "%UI_HDR%", "ui_" + Path
                    .GetFileNameWithoutExtension(data.UiFile) + ".h");
            }

            return cppFile;
        }

        private string AddProjectItemUi(string location)
        {
            var uiFile = Path.GetTempFileName();
            using (var tmp = new StreamWriter(uiFile)) {
                tmp.Write(
                    "<UI version=\"4.0\" >\r\n"
                    + " <class>%CLASS%</class>\r\n"
                    + " <widget class=\"%BASECLASS%\" name=\"%CLASS%\" >\r\n"
                    + "  <property name=\"objectName\" >\r\n"
                    + "   <string notr=\"true\">%CLASS%</string>\r\n"
                    + "  </property>\r\n"
                    + "  <property name=\"geometry\" >\r\n"
                    + "   <rect>\r\n"
                    + "    <x>0</x>\r\n"
                    + "    <y>0</y>\r\n"
                    + "    <width>400</width>\r\n"
                    + "    <height>300</height>\r\n"
                    + "   </rect>\r\n"
                    + "  </property>\r\n"
                    + "  <property name=\"windowTitle\" >\r\n"
                    + "   <string>%CLASS%</string>\r\n"
                    + "  </property>%CENTRAL_WIDGET%\r\n"
                    + " </widget>\r\n"
                    + " <layoutDefault spacing=\"6\" margin=\"11\" />\r\n"
                    + " <pixmapfunction></pixmapfunction>\r\n"
                    + " <resources/>\r\n"
                    + " <connections/>\r\n"
                    + "</UI>\r\n");
            }
            uiFile = QtProject.CopyFileToFolder(uiFile, location, data.UiFile);

            QtProject.ReplaceTokenInFile(uiFile, "%CLASS%", data.ClassName);
            QtProject.ReplaceTokenInFile(uiFile, "%BASECLASS%", data.BaseClass);
            if (data.BaseClass == "QMainWindow") {
                QtProject.ReplaceTokenInFile(uiFile, "%CENTRAL_WIDGET%",
                    "\r\n  <widget class=\"QMenuBar\" name=\"menuBar\" />"
                    + "\r\n  <widget class=\"QToolBar\" name=\"mainToolBar\" />"
                    + "\r\n  <widget class=\"QWidget\" name=\"centralWidget\" />"
                    + "\r\n  <widget class=\"QStatusBar\" name=\"statusBar\" />");
            } else if (data.BaseClass == "QDockWidget") {
                QtProject.ReplaceTokenInFile(uiFile, "%CENTRAL_WIDGET%",
                    "\r\n  <widget class=\"QWidget\" name=\"widget\" />");
            } else {
                QtProject.ReplaceTokenInFile(uiFile, "%CENTRAL_WIDGET%", string.Empty);
            }

            return uiFile;
        }
    }
}
