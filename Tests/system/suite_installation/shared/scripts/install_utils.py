####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

import globalnames


def openExtensionManager(version):
    mouseClick(waitForObject(globalnames.extensions_MenuItem))
    mouseClick(waitForObject(names.extensions_Manage_Extensions_MenuItem))


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


def testChangesScheduledLabel(timeout=5000):
    expectedMessages = {"Your changes will be scheduled.  The modifications will begin when all "
                        "Microsoft Visual Studio windows are closed.",  # VS2019
                        "These changes will take effect the next time Microsoft Visual Studio is "
                        "opened."  # VS2022
                        }
    try:
        changesLabel = waitForObject(names.changes_scheduled_Label, timeout)
        test.passes("Changes to the installation were scheduled.")
        test.verify(changesLabel.text in expectedMessages,
                    "Did the message about scheduled changes display an expected text?")
    except:
        test.fail("Message about scheduled changes to the installation was not found.")


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
