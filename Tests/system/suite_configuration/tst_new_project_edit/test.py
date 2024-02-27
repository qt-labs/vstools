####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

source("../shared/scripts/config_utils.py")

import names

myProjectName = ""


def setNames(_, __, expectedName):
    global myProjectName
    projectNameEdit = waitForObjectExists(names.msvs_Project_name_Edit)
    myProjectName = "My%sProject" % expectedName
    type(projectNameEdit, myProjectName)
    solutionNameEdit = waitForObjectExists(names.solutionNameText_Edit)
    type(solutionNameEdit, "My%sSolution" % expectedName)


def testWizardPage3(_, __):
    classNameEdit = waitForObjectExists(names.qt_Wizard_Class_Name_Edit)
    headerEdit = waitForObjectExists(names.qt_Wizard_Header_h_file_Edit)
    sourceEdit = waitForObjectExists(names.qt_Wizard_Source_cpp_file_Edit)
    # Check that names are derived from project name
    test.compare(classNameEdit.text, myProjectName)
    test.compare(headerEdit.text, myProjectName + ".h")
    test.compare(sourceEdit.text, myProjectName + ".cpp")
    # Check that changing class name changes file names
    type(classNameEdit, "HereIs")
    changedClassName = "HereIs" + myProjectName
    waitFor("classNameEdit.text == changedClassName", 2000)
    test.compare(classNameEdit.text, changedClassName)
    test.compare(headerEdit.text, changedClassName + ".h")
    test.compare(sourceEdit.text, changedClassName + ".cpp")
    # Check that file names can be made lower case
    test.verify(not waitForObject(names.lower_case_file_names_CheckBox).checked)
    mouseClick(waitForObject(names.lower_case_file_names_CheckBox))
    test.compare(headerEdit.text, changedClassName.lower() + ".h")
    test.compare(sourceEdit.text, changedClassName.lower() + ".cpp")
    test.verify(waitForObject(names.lower_case_file_names_CheckBox).checked)
    # Check that file names can be set back to camel case
    mouseClick(waitForObject(names.lower_case_file_names_CheckBox))
    test.compare(headerEdit.text, changedClassName + ".h")
    test.compare(sourceEdit.text, changedClassName + ".cpp")
    test.verify(not waitForObject(names.lower_case_file_names_CheckBox).checked)

    # Check that the wizard can't proceed with empty values
    def clearAndRestoreEdit(edit):
        previousText = edit.text
        type(edit, "<Ctrl+a>")
        type(edit, "<Delete>")
        waitFor("edit.text == ''")
        test.verify(not waitForObjectExists(names.qt_Wizard_Finish_Button).enabled)
        type(edit, previousText)
        waitFor("edit.text == previousText")
        test.verify(waitForObjectExists(names.qt_Wizard_Finish_Button).enabled)

    clearAndRestoreEdit(sourceEdit)
    clearAndRestoreEdit(headerEdit)
    clearAndRestoreEdit(classNameEdit)


def main():
    testAllQtWizards(setNames, funcPage3=testWizardPage3)
