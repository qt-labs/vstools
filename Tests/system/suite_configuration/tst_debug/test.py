####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

source("../shared/scripts/config_utils.py")

import shutil

import names
from newprojectdialog import NewProjectDialog
from testsection import TestSection

workDir = os.getenv("SQUISH_VSTOOLS_WORKDIR")
createdProjects = set()


def waitAndTestForStoppedDebugger(expectStopped=True):
    expectedState = "stopped" if expectStopped else "running"
    with TestSection("Verify %s state" % expectedState):
        continueButton = waitForObjectExists(names.continue_Button, 70000)
        test.verify(waitFor(lambda: continueButton.enabled == expectStopped, 8000),
                    "Is the debugger %s as expected?" % expectedState)
        test.compare(waitForObjectExists(names.thread_ComboBox).enabled, expectStopped,
                    "Is the 'Thread' combo box enabled?")
        test.compare(waitForObjectExists(names.stackFrame_ComboBox).enabled, expectStopped,
                    "Is the 'Stack Frame' combo box enabled?")
        test.compare(waitForObjectExists(names.breakAll_Button).enabled, not expectStopped,
                     "Is the 'Break All' button enabled?")
        test.verify(waitForObjectExists(names.stopDebugging_Button).enabled,
                    "Is the 'Stop Debugging' button enabled?")


def qtQuickAppContextNames():
    return filter(lambda name: name.startswith("QtQuickApplication"),
                  map(lambda ctx: ctx.name, applicationContextList()))


def qtQuickAppContextExists():
    return any(qtQuickAppContextNames())


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

    # By default, MSVS shows the "Output" view which contains a text edit with the same properties
    # as the code editor. Explicitly show a different view so the right editor will be used.
    mouseClick(waitForObject(names.view_MenuItem))
    mouseClick(waitForObject(names.view_Error_List_MenuItem))

    with NewProjectDialog() as dialog:
        dialog.filterForQtProjects()
        listedTemplates = list(dialog.getListedTemplates())
    # find list item for Qt Quick Application
    quickItem = next(filter(lambda templ: templ[1] == "Qt Quick Application", listedTemplates))[0]

    NewProjectDialog.open()
    mouseClick(waitForObject(quickItem))
    clickButton(waitForObject(names.microsoft_Visual_Studio_Next_Button))
    type(waitForObject(names.comboBox_Edit), workDir)
    waitFor(lambda: waitForObject(names.comboBox_Edit).text == workDir)
    projectName = waitForObjectExists(names.msvs_Project_name_Edit).text
    createdProjects.add(projectName)
    clickButton(waitForObject(names.microsoft_Visual_Studio_Create_Button))
    fixAppContext()
    clickButton(waitForObject(names.qt_Wizard_Next_Button))
    clickButton(waitForObject(names.qt_Wizard_Finish_Button))
    fixAppContext()

    doubleClick(waitForObjectItem(names.msvs_SolutionExplorer_List, "Source Files"))
    doubleClick(waitForObjectItem(names.msvs_SolutionExplorer_List, "main.cpp"))
    for _ in range(12): # move to line "engine.load(...)"
        type(waitForObject(names.msvs_WpfTextView_WPFControl), "<Down>")
    type(waitForObject(names.msvs_WpfTextView_WPFControl), "<F9>") # Toggle Breakpoint
    test.verify(not object.exists(names.continue_Button),
                "Continue Button doesn't exist?")
    type(waitForObject(names.msvs_WpfTextView_WPFControl), "<F5>") # Start debugging
    waitAndTestForStoppedDebugger()
    type(waitForObject(names.msvs_WpfTextView_WPFControl), "<F5>") # Continue
    waitFor(qtQuickAppContextExists, 30000)
    fixAppContext()
    waitAndTestForStoppedDebugger(False)
    # When stopping the app using MSVS' menu, Squish considers this a crashed AUT
    # Instead, close the app's window
    fixAppContext(projectName)
    type(waitForObject(names.qtQuickApplication_Window), "<Alt+F4>")
    waitFor(lambda: not qtQuickAppContextExists(), 5000)
    with TestSection("Verify finished state"):
        test.verify(waitFor(lambda: not object.exists(names.continue_Button), 5000),
                    "Continue Button doesn't exist anymore?")
        test.verify(not waitForObjectExists(names.thread_ComboBox).enabled,
                    "Is the 'Thread' combo box disabled?")
        test.verify(not waitForObjectExists(names.stackFrame_ComboBox).enabled,
                    "Is the 'Stack Frame' combo box disabled?")
    clearQtVersions(version)
    closeMainWindow()


def cleanup():
    if workDir:
        waitFor(lambda: len(applicationContextList()) == 0, 5000)
        for project in createdProjects:
            shutil.rmtree(os.path.join(workDir, project))
