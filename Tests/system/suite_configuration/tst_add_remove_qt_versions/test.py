####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
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
    # Add the Qt versions
    openVsToolsMenu(version)
    mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
    if not test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, 1,
                        "The table should have exactly one line before adding Qt versions."):
        test.fatal("Unexpected table shown. Probably there is either an unexpected configuration "
                   "or the UI changed.")
        clickButton(waitForObject(names.options_Cancel_Button))
        closeMainWindow()
        return
    for i, qtDir in enumerate(qtDirs):
        mouseClick(waitForObject(tableCell(1, i)))
        typeToEdit(tableCellEdit(3, i), qtDir["path"])
        typeToEdit(tableCellEdit(1, i), qtDir["name"])
        test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, i + 2,
                     "The table should have %d lines after adding %d Qt versions."
                     % (i + 2, i + 1))
    clickButton(waitForObject(names.options_OK_Button))
    waitFor("not object.exists(names.options_Dialog)")
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
    test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, 1,
                 "The table should have exactly one line after removing all Qt versions.")
    clickButton(waitForObject(names.options_OK_Button))
    waitFor("not object.exists(names.options_Dialog)")
    closeMainWindow()


def checkSelectQtLabel():
    try:
        waitForObjectExists(names.You_must_select_a_Qt_version_Label, 1000)
        return True
    except:
        return False


def tableCell(col, row):
    return {"column": col, "container": names.dataGrid_Table, "row": row, "type": "TableCell"}


def tableCellEdit(col, row):
    return {"container": tableCell(col, row), "type": "Edit"}


def typeToEdit(editId, text):
    edit = waitForObject(editId)
    mouseClick(edit)
    type(edit, text)
    waitFor("waitForObject(editId).text == text")
