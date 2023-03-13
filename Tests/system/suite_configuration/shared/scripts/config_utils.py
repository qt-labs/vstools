####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

import os

import names


def readQtDirs():
    dirString = os.getenv("SQUISH_VSTOOLS_QTDIRS")
    if not dirString:
        return []
    uniquePaths = set()
    qtDirs = []
    for current in dirString.split(";"):
        loweredPath = current.lower()
        if loweredPath in uniquePaths:
            continue
        uniquePaths.add(loweredPath)
        qtDirs.append({"path": current,
                       "name": current.rsplit(":")[-1].strip("\\").replace("\\", "_")})
    return qtDirs


def typeToEdit(editId, text):
    edit = waitForObject(editId)
    mouseClick(edit)
    type(edit, text)
    waitFor("waitForObject(editId).text == text")


def tableCell(col, row):
    return {"column": col, "container": names.dataGrid_Table, "row": row, "type": "TableCell"}


def tableCellEdit(col, row):
    return {"container": tableCell(col, row), "type": "Edit"}


def configureQtVersions(msvsVersion, qtDirs, withTests=False):
    openVsToolsMenu(msvsVersion)
    mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
    tableRows = waitForObjectExists(names.dataGrid_Table).rowCount
    if withTests:
        test.compare(tableRows, 1,
                     "The table should have exactly one line before adding Qt versions.")
    if tableRows != 1:
        test.fatal("Unexpected table shown. Probably there is either an unexpected configuration "
                   "or the UI changed.")
        clickButton(waitForObject(names.options_Cancel_Button))
        return False
    for i, qtDir in enumerate(qtDirs):
        mouseClick(waitForObject(tableCell(1, i)))
        typeToEdit(tableCellEdit(3, i), qtDir["path"])
        typeToEdit(tableCellEdit(1, i), qtDir["name"])
        if withTests:
            test.compare(waitForObjectExists(names.dataGrid_Table).rowCount, i + 2,
                         "The table should have %d lines after adding %d Qt versions."
                         % (i + 2, i + 1))
    clickButton(waitForObject(names.options_OK_Button))
    waitFor("not object.exists(names.options_Dialog)")
    return True


def clearQtVersions(msvsVersion):
    openVsToolsMenu(msvsVersion)
    mouseClick(waitForObject(names.pART_Popup_Qt_Versions_MenuItem))
    qtVersionCount = waitForObjectExists(names.dataGrid_Table).rowCount - 1
    for i in range(qtVersionCount):
        clickButton(waitForObject({"container": tableCell(1, i),
                                   "text": "", "type": "Button"}))
    snooze(1)
    clickButton(waitForObject(names.options_OK_Button))
    waitFor("not object.exists(names.options_Dialog)")
