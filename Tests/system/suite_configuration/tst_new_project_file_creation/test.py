####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

source("../shared/scripts/config_utils.py")

import os
import re
import shutil

from newprojectdialog import NewProjectDialog
import names


def fixAppContext():
    waitFor("len(applicationContextList()) > 1", 10000)
    appContexts = applicationContextList()
    if len(appContexts) == 1:  # Might have changed after waitFor()
        if appContexts[0].name != "devenv":
            test.fatal("The only application context is " + appContexts[0].name)
    else:
        for ctxt in appContexts:
            if ctxt.name == "devenv":
                setApplicationContext(ctxt)
                return
        test.fatal("There's no devenv application context, only: " + appContexts)


def testCompareRegex(text, pattern, message):
    regex = re.compile(pattern)
    test.verify(regex.match(text), '%s ("%s"/"%s")' % (message, text, pattern))


workDir = os.getenv("SQUISH_VSTOOLS_WORKDIR")
createdProjects = set()


def main():
    qtDirs = readQtDirs()
    if not qtDirs:
        test.fatal("No Qt versions known", "Did you set SQUISH_VSTOOLS_QTDIRS correctly?")
        return
    if not workDir:
        test.fatal("No directory for creating projects known",
                   "Did you set SQUISH_VSTOOLS_WORKDIR correctly?")
        return
    version = startAppGetVersion()
    if not version:
        return
    if not configureQtVersions(version, qtDirs):
        closeMainWindow()
        return

    with NewProjectDialog() as dialog:
        dialog.filterForQtProjects()
        listedTemplates = list(dialog.getListedTemplates())

    expectedFiles = {"Qt Designer Custom Widget": "^QtDesigner.*\.cpp$",
                     "Qt Console Application": "^main\.cpp$",
                     "Qt ActiveQt Server": "^ActiveQtServer\d*\.cpp$",
                     "Qt Quick Application": "^main\.qml$",
                     "Qt Empty Application": None,
                     "Qt Class Library": "^QtClassLibrary\d*\.cpp$",
                     "Qt Widgets Application": "^QtWidgets.*\.cpp$"}

    with NewProjectDialog() as dialog:
        for listItem, templateName in listedTemplates:
            with TestSection(templateName):
                if not templateName in expectedFiles:
                    test.warning("Template %s is not supported, skipping..." % templateName)
                    continue
                mouseClick(waitForObject(listItem))
                clickButton(waitForObject(names.microsoft_Visual_Studio_Next_Button))
                type(waitForObject(names.comboBox_Edit), workDir)
                waitFor("waitForObject(names.comboBox_Edit).text == workDir")
                createdProjects.add(waitForObjectExists(names.microsoft_Visual_Studio_Project_name_Edit).text)
                clickButton(waitForObject(names.microsoft_Visual_Studio_Create_Button))
                fixAppContext()
                clickButton(waitForObject(names.qt_Wizard_Next_Button))
                if templateName in ["Qt ActiveQt Server",
                                    "Qt Class Library",
                                    "Qt Designer Custom Widget",
                                    "Qt Widgets Application"]:
                    clickButton(waitForObject(names.qt_Wizard_Next_Button))
                clickButton(waitForObject(names.qt_Wizard_Finish_Button))
                fixAppContext()
                if expectedFiles[templateName]:
                    try:
                        testCompareRegex(waitForObjectExists(names.qt_cpp_Label).text,
                                         expectedFiles[templateName],
                                         "Was a file with an expected name opened?")
                    except:
                        test.fail("There was no expected file opened for %s" % templateName)
                else:
                    test.exception("waitForObjectExists(names.qt_cpp_Label)",
                                   "No file should be opened for %s" % templateName)
                mouseClick(waitForObject(globalnames.file_MenuItem))
                mouseClick(waitForObject(names.file_Close_Solution_MenuItem))
                # reopens the "New Project" dialog
                mouseClick(waitForObject(names.microsoft_Visual_Studio_Create_a_new_project_Label))
    clearQtVersions(version)
    closeMainWindow()


def cleanup():
    if workDir:
        for project in createdProjects:
            shutil.rmtree(os.path.join(workDir, project))
