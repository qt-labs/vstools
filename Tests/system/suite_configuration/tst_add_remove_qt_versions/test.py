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
    startApp()
    test.verify(checkSelectQtLabel(),
                "Warning about having to select a Qt version is being shown?")
    if not configureQtVersions(qtDirs, True):
        closeMainWindow()
        return
    test.verify(not checkSelectQtLabel(),
                "Warning about having to select a Qt version disappeared?")
    # Sort qtDirs by name because that's the order in which they'll be displayed
    qtDirs.sort(key=lambda dir: dir["name"].lower())
    # Check and remove the Qt versions, but cancel changes, then repeat and click "OK"
    for closeButton in [names.options_Cancel_Button, names.options_OK_Button]:
        openVsToolsMenu()
        mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
        if test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, len(qtDirs),
                        "The table should have %d lines after adding Qt versions."
                        % (len(qtDirs))):
            for i, qtDir in enumerate(qtDirs):
                # from the table, Squish returns the text without the underscores
                # although is is being displayed completely
                expectedNameInTable = qtDir["name"].replace("_", "")
                expectedPathInTable = qtDir["path"].replace("_", "")
                test.compare(waitForObject(tableCellEdit(1, i)).text, expectedNameInTable,
                             "Is the Qt version's name shown in table as entered?")
                test.compare(waitForObject(tableCellEdit(2, i)).text, expectedPathInTable,
                             "Is the Qt version's path shown in table as entered?")
                mouseClick(waitForObject(tableCell(1, i)))
                test.compare(waitForObject(names.name_Edit).text, qtDir["name"],
                             "Is the Qt version's name shown in edit as entered?")
                test.compare(waitForObject(names.location_Edit).text, qtDir["path"],
                             "Is the Qt version's path shown in edit as entered?")
            for _ in qtDirs:
                clickButton(waitForObject(names.remove_Button))
        snooze(1)
        clickButton(waitForObject(closeButton))
        waitFor(lambda: not object.exists(names.options_Dialog))
    # Check that Qt versions were removed
    openVsToolsMenu()
    mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
    if test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, 0,
                    "The table should have zero lines after removing all Qt versions."):

        # test handling of invalid directory
        def testErrorMessage(nameEntered):
            clickButton(waitForObject(names.options_OK_Button))
            dialogText = waitForObjectExists(names.msvs_Qt_VS_Tools_Invalid_Qt_versions).text
            test.verify(("Name cannot be empty" in dialogText) ^ nameEntered)
            test.verify("Cannot find qmake.exe" in dialogText)
            clickButton(waitForObject(globalnames.microsoft_Visual_Studio_OK_Button))

        nonExistingDir = "C:\\this\does\\not\\exist"
        while os.path.exists(nonExistingDir):
            nonExistingDir += "x"
        clickButton(waitForObject(names.add_Button))
        typeToEdit(names.location_Edit, nonExistingDir)
        testErrorMessage(False)
        typeToEdit(names.name_Edit, "some name")
        testErrorMessage(True)
    clickButton(waitForObject(names.options_Cancel_Button))
    waitFor(lambda: not object.exists(names.options_Dialog))
    closeMainWindow()


def checkSelectQtLabel():
    try:
        waitForObjectExists(names.You_must_select_a_Qt_version_Label, 5000)
        return True
    except:
        return False
