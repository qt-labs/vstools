####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

source("../../shared/utils.py")
source("../shared/scripts/config_utils.py")

import names


def main():
    qtDirs = readQtDirs()
    if not qtDirs:
        test.fatal("No Qt versions known", "Did you set SQUISH_VSTOOLS_QTDIRS correctly?")
        return
    version = startAppGetVersion()
    if not version:
        return
    test.verify(checkSelectQtLabel(),
                "Warning about having to select a Qt version is being shown?")
    if not configureQtVersions(version, qtDirs, True):
        closeMainWindow()
        return
    test.verify(not checkSelectQtLabel(),
                "Warning about having to select a Qt version disappeared?")
    # Sort qtDirs by name because that's the order in which they'll be displayed
    qtDirs.sort(key=lambda dir: dir["name"].lower())
    # Check and remove the Qt versions, but cancel changes, then repeat and click "OK"
    for closeButton in [names.options_Cancel_Button, names.options_OK_Button]:
        openVsToolsMenu(version)
        mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
        if test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, len(qtDirs) + 1,
                        "The table should have %d lines after adding Qt versions."
                        % (len(qtDirs) + 1)):
            for i, qtDir in enumerate(qtDirs):
                test.compare(waitForObject(tableCellEdit(1, i)).text, qtDir["name"],
                             "Is the Qt version's name shown as entered?")
                test.compare(waitForObject(tableCellEdit(3, i)).text, qtDir["path"],
                             "Is the Qt version's path shown as entered?")
                clickButton(waitForObject({"container": tableCell(1, i),
                                           "text": "", "type": "Button"}))
        snooze(1)
        clickButton(waitForObject(closeButton))
        waitFor("not object.exists(names.options_Dialog)")
    # Check that Qt versions were removed
    openVsToolsMenu(version)
    mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
    if test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, 1,
                    "The table should have exactly one line after removing all Qt versions."):

        # test handling of invalid directory
        def testErrorMessage(nameEntered):
            clickButton(waitForObject(names.options_OK_Button))
            dialogText = waitForObjectExists(names.msvs_Qt_VS_Tools_Invalid_Qt_versions).text
            test.verify(("Name cannot be empty" in dialogText) ^ nameEntered)
            test.verify("Cannot find qmake.exe" in dialogText)
            clickButton(waitForObject(names.microsoft_Visual_Studio_OK_Button))

        nonExistingDir = "C:\\this\does\\not\\exist"
        while os.path.exists(nonExistingDir):
            nonExistingDir += "x"
        mouseClick(waitForObject(tableCell(1, 0)))
        typeToEdit(tableCellEdit(3, 0), nonExistingDir)
        testErrorMessage(False)
        typeToEdit(tableCellEdit(1, 0), "some name")
        testErrorMessage(True)
    clickButton(waitForObject(names.options_Cancel_Button))
    waitFor("not object.exists(names.options_Dialog)")
    closeMainWindow()


def checkSelectQtLabel():
    try:
        waitForObjectExists(names.You_must_select_a_Qt_version_Label, 5000)
        return True
    except:
        return False
