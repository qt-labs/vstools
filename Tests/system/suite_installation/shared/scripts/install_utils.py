####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

from xml.dom import minidom

import globalnames


def openExtensionManager(version):
    if version == "2017":
        mouseClick(waitForObject(names.tools_MenuItem))
        mouseClick(waitForObject(names.pART_Popup_Extensions_and_Updates_MenuItem))
    else:
        mouseClick(waitForObject(globalnames.extensions_MenuItem))
        mouseClick(waitForObject(names.pART_Popup_Manage_Extensions_MenuItem))


def selectInstalledVsTools(version):
    openExtensionManager(version)
    if version == "2017":
        try:
            vsToolsLabel = waitForObject(names.extensionManager_UI_InstalledExtItem_Qt_2017_Label,
                                         5000)
        except:
            return None
    else:
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


def readExpectedVsToolsVersion():
    expectedVersion = os.getenv("SQUISH_VSTOOLS_VERSION")
    if expectedVersion:
        return expectedVersion
    test.warning("No expected Qt VS Tools version set.",
                 "The environment variable SQUISH_VSTOOLS_VERSION is not set. Falling back to "
                 "reading the expected version from version.targets")
    try:
        versionXml = minidom.parse("../../../../version.targets")
        return versionXml.getElementsByTagName("QtVSToolsVersion")[0].firstChild.data
    except:
        test.fatal("Can't read expected VS Tools version from sources.")
        return ""


def verifyVsToolsVersion():
    displayedVersion = waitForObjectExists(names.manage_Extensions_Version_Label).text
    expectedVersion = readExpectedVsToolsVersion()
    test.compare(displayedVersion, expectedVersion,
                 "Expected version of VS Tools is displayed?")
