####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

source("../../shared/utils.py")
source("../shared/scripts/config_utils.py")

import os

import names


def testNewProjectDialog(templateName, expectedName):
    projectName = waitForObjectExists(names.msvs_Project_name_Edit).text
    solutionName = waitForObjectExists(names.solutionNameText_Edit).text
    test.compare(waitForObjectExists(names.project_template_name_Label).text, templateName,
                 'Does the "Configure your new project" dialog show the right template name?')
    test.verify(projectName.startswith(expectedName),
                "Project name is based on template name?")
    test.verify(solutionName.startswith(expectedName),
                "Solution name is based on template name?")
    test.compare(projectName, solutionName, "Project name and solution name are the same?")
    projectLocation = waitForObjectExists(names.comboBox_Edit).text
    if getMsvsProductLine() != "2019":
        test.compare(waitForObjectExists(names.outputPathTextBlock_Label).text,
                     'Project will be created in "%s"'
                     % os.path.join(projectLocation, solutionName, projectName, ""))


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


def testWizardPage3(templateName, expectedText, projectName):
    test.verify(waitForObject(names.qt_Wizard_Welcome_Label).text.startswith(expectedText),
                "Check beginning of wizard's text on third page")
    test.compare(waitForObjectExists(names.qt_Wizard_Class_Name_Edit).text,
                 projectName)
    if templateName != "Qt Test Application":
        test.compare(waitForObjectExists(names.qt_Wizard_Header_h_file_Edit).text,
                     projectName + ".h")
    test.compare(waitForObjectExists(names.qt_Wizard_Source_cpp_file_Edit).text,
                 projectName + ".cpp")


def main():
    testAllQtWizards(testNewProjectDialog, testWizardPage1, testWizardPage2, testWizardPage3)
