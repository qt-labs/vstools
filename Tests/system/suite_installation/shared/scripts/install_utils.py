####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

import globalnames


def openExtensionManager(version):
    mouseClick(waitForObject(globalnames.extensions_MenuItem))
    mouseClick(waitForObject(names.pART_Popup_Manage_Extensions_MenuItem))


def selectInstalledVsTools(version):
    openExtensionManager(version)
    mouseClick(waitForObject({"type": "TreeItem", "id": "Installed"}))
    try:
        vsToolsLabel = waitForObject(names.extensionManager_UI_InstalledExtItem_Qt_Label,
                                     5000)
    except:
        return None
    mouseClick(vsToolsLabel)
    return vsToolsLabel.text


def changesScheduledLabelExists():
    return object.exists(names.changes_scheduled_Label)


def readFile(filename):
    with open(filename, "r") as f:
        return f.read()


def readExpectedVsToolsVersion():
    expectedVersion = os.getenv("SQUISH_VSTOOLS_VERSION")
    if expectedVersion:
        return expectedVersion
    test.warning("No expected Qt VS Tools version set.",
                 "The environment variable SQUISH_VSTOOLS_VERSION is not set. Falling back to "
                 "reading the expected version from version.targets")
    try:
        return readFile("../../../../version.log")
    except:
        test.fatal("Can't read expected VS Tools version from sources.")
        return ""


def verifyVsToolsVersion():
    displayedVersion = waitForObjectExists(names.manage_Extensions_Version_Label).text
    expectedVersion = readExpectedVsToolsVersion()
    if expectedVersion:
        test.compare(displayedVersion, expectedVersion,
                     "Expected version of VS Tools is displayed?")
