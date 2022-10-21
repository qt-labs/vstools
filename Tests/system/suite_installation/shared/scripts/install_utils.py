############################################################################
#
# Copyright (C) 2022 The Qt Company Ltd.
# Contact: https://www.qt.io/licensing/
#
# This file is part of the Qt VS Tools.
#
# $QT_BEGIN_LICENSE:GPL-EXCEPT$
# Commercial License Usage
# Licensees holding valid commercial Qt licenses may use this file in
# accordance with the commercial license agreement provided with the
# Software or, alternatively, in accordance with the terms contained in
# a written agreement between you and The Qt Company. For licensing terms
# and conditions see https://www.qt.io/terms-conditions. For further
# information use the contact form at https://www.qt.io/contact-us.
#
# GNU General Public License Usage
# Alternatively, this file may be used under the terms of the GNU
# General Public License version 3 as published by the Free Software
# Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
# included in the packaging of this file. Please review the following
# information to ensure the GNU General Public License requirements will
# be met: https://www.gnu.org/licenses/gpl-3.0.html.
#
# $QT_END_LICENSE$
#
############################################################################

# -*- coding: utf-8 -*-


def openExtensionManager(version):
    if version == "2017":
        mouseClick(waitForObject(names.tools_MenuItem))
        mouseClick(waitForObject(names.pART_Popup_Extensions_and_Updates_MenuItem))
    else:
        mouseClick(waitForObject(names.extensions_MenuItem))
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
