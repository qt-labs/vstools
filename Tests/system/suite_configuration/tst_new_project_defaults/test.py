############################################################################
#
# Copyright (C) 2023 The Qt Company Ltd.
# Contact: https://www.qt.io/licensing/
#
# This file is part of the Qt VS Tools.
#
# $QT_BEGIN_LICENSE:GPL-EXCEPT$
# Commercial License Usage
# Licensees holding valid commercial Qt licenses may use this file in
# accordance with the commercial license agreement provided with the
# Software or, alternatively, in accordance with the terms contained in
# a written agreement between you and The Qt Company. For licensing terms
# and conditions see https://www.qt.io/terms-conditions. For further
# information use the contact form at https://www.qt.io/contact-us.
#
# GNU General Public License Usage
# Alternatively, this file may be used under the terms of the GNU
# General Public License version 3 as published by the Free Software
# Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
# included in the packaging of this file. Please review the following
# information to ensure the GNU General Public License requirements will
# be met: https://www.gnu.org/licenses/gpl-3.0.html.
#
# $QT_END_LICENSE$
#
############################################################################

# -*- coding: utf-8 -*-

source("../../shared/testsection.py")
source("../../shared/utils.py")
source("../shared/scripts/config_utils.py")

import os
import sys

import names


def getExpectedName(templateName):
    if templateName == "Qt ActiveQt Server":
        return "ActiveQtServer"
    elif templateName == "Qt Designer Custom Widget":
        return "QtDesignerWidget"
    elif templateName == "Qt Empty Application":
        return "QtApplication"
    else:
        return templateName.replace(" ", "")


def testNewProjectDialog(version, templateName, expectedName):
    test.compare(waitForObjectExists(names.project_template_name_Label).text, templateName,
                 'Does the "Configure your new project" dialog show the right template name?')
    projectName = waitForObjectExists(names.microsoft_Visual_Studio_Project_name_Edit).text
    solutionName = waitForObjectExists(names.solutionNameText_Edit).text
    test.verify(projectName.startswith(expectedName),
                "Project name is based on template name?")
    test.verify(solutionName.startswith(expectedName),
                "Solution name is based on template name?")
    test.compare(projectName, solutionName, "Project name and solution name are the same?")
    projectLocation = waitForObjectExists(names.comboBox_Edit).text
    if version != "2019":
        test.compare(waitForObjectExists(names.outputPathTextBlock_Label).text,
                     'Project will be created in "%s"'
                     % os.path.join(projectLocation, solutionName, projectName, ""))
    return projectName


def testWizardPage1(expectedText, templateName):
    test.compare(waitForObjectExists(names.qt_Wizard_Window, 40000).text,
                 templateName + " Wizard", "Check wizard's title")
    test.verify(waitForObject(names.qt_Wizard_Welcome_Label).text.startswith(expectedText),
                "Check beginning of wizard's text on first page")


def testWizardPage2(expectedText, qtDirs):
    test.verify(waitForObject(names.qt_Wizard_Welcome_Label).text.startswith(expectedText),
                "Check beginning of wizard's text on second page")
    test.compare(waitForObjectExists(names.ProjectModel_ComboBox).nativeObject.Text,
                 "Qt Visual Studio Project (Qt/MSBuild)")
    configTable = waitForObjectExists(names.qt_ConfigTable)
    if test.compare(configTable.rowCount, 2) and test.compare(configTable.columnCount, 6):
        tableCell = {"container":names.qt_ConfigTable, "type":"TableCell"}
        test.compare(waitForObjectExists({"container":tableCell | {"row":0, "column":0},
                                          "type":"Edit"}).text, "Debug")
        test.compare(waitForObjectExists({"container":tableCell | {"row":1, "column":0},
                                          "type":"Edit"}).text, "Release")
        selectedQtVersion = waitForObjectExists(names.comboBox_Edit).text
        test.verify(selectedQtVersion in [x["name"] for x in qtDirs],
            "Is the selected Qt version '%s' in configured Qt versions?" % selectedQtVersion)


def testWizardPage3(expectedText, projectName):
    test.verify(waitForObject(names.qt_Wizard_Welcome_Label).text.startswith(expectedText),
                "Check beginning of wizard's text on third page")
    test.compare(waitForObjectExists(names.qt_Wizard_Class_Name_Edit).text,
                 projectName)
    test.compare(waitForObjectExists(names.qt_Wizard_Header_h_file_Edit).text,
                 projectName + ".h")
    test.compare(waitForObjectExists(names.qt_Wizard_Source_cpp_file_Edit).text,
                 projectName + ".cpp")


def main():
    qtDirs = readQtDirs()
    if not qtDirs:
        test.fatal("No Qt versions known", "Did you set SQUISH_VSTOOLS_QTDIRS correctly?")
        return
    version = startAppGetVersion()
    if not version:
        return
    if not configureQtVersions(version, qtDirs):
        closeMainWindow()
        return

    mouseClick(waitForObject(globalnames.file_MenuItem))
    mouseClick(waitForObject(names.pART_Popup_New_MenuItem))
    mouseClick(waitForObject(names.pART_Popup_Project_MenuItem))
    expand(waitForObject(names.project_type_filter_ComboBox))
    mouseClick(waitForObject(names.qt_ComboBoxItem))
    listView = waitForObject(names.microsoft_VS_TemplateList_ListView)
    for i in range(1, listView.itemCount - 1):  # itemCount is number of shown items + 2
        listItem = names.templateList_ListViewItem | {"occurrence": i}
        templateName = waitForObjectExists({"container": listItem, "type": "Label"}).text
        with TestSection(templateName):
            mouseClick(waitForObject(listItem))
            expectedName = getExpectedName(templateName)
            clickButton(waitForObject(names.microsoft_Visual_Studio_Next_Button))
            projectName = testNewProjectDialog(version, templateName, expectedName)
            clickButton(waitForObject(names.microsoft_Visual_Studio_Create_Button))
            try:
                expectedText = "Welcome to the %s Wizard" % templateName
                # work around issue fixed in
                # https://codereview.qt-project.org/c/qt-labs/vstools/+/473682
                if templateName == "Qt Designer Custom Widget":
                    expectedText = "Welcome to the Qt Custom Designer Widget"
                testWizardPage1(expectedText, templateName)
                clickButton(waitForObject(names.qt_Wizard_Next_Button))
                testWizardPage2(expectedText, qtDirs)
                if templateName in ["Qt ActiveQt Server", "Qt Class Library",
                                    "Qt Designer Custom Widget", "Qt Widgets Application"]:
                    clickButton(waitForObject(names.qt_Wizard_Next_Button))
                    testWizardPage3(expectedText, projectName)
                else:
                    test.verify(not findObject(names.qt_Wizard_Next_Button).enabled)
            except:
                eInfo = sys.exc_info()
                test.fatal("Exception caught", "%s: %s" % (eInfo[0].__name__, eInfo[1]))
            finally:
                # Cannot finish because of SQUISH-15876
                clickButton(waitForObject(names.qt_Wizard_Cancel_Button))

        clickButton(waitForObject(names.microsoft_Visual_Studio_Back_Button))
    clickButton(waitForObject(names.microsoft_Visual_Studio_Close_Button))

    clearQtVersions(version)
    closeMainWindow()
