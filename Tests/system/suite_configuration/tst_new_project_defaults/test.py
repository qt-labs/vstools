############################################################################
#
# Copyright (C) 2024 The Qt Company Ltd.
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

source("../../shared/utils.py")
source("../shared/scripts/config_utils.py")

import os

import names


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
    testAllQtWizards(testNewProjectDialog, testWizardPage1, testWizardPage2, testWizardPage3)
