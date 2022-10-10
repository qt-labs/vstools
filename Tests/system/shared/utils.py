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

import os
import subprocess
from xml.dom import minidom


def startAppGetVersion():
    appContext = startApplication("devenv /LCID 1033")
    try:
        vsDirectory = appContext.commandLine.strip('"').partition("\\Common7")[0]
        programFilesDir = os.getenv("ProgramFiles(x86)")
        plv = subprocess.check_output('"%s/Microsoft Visual Studio/Installer/vswhere.exe" '
                                      '-path "%s" -property catalog_productLineVersion'
                                      % (programFilesDir, vsDirectory))
        version = str(plv).strip("b'\\rn\r\n")
    except:
        test.fatal("Cannot determine used VS version")
        version = ""
    if version != "2017":
        mouseClick(waitForObject(names.continueWithoutCode_Label))
    return version


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
        vsToolsLabel = waitForObject(names.extensionManager_UI_InstalledExtensionItem_The_Qt_VS_Tools_for_Visual_Studio_2017_Label)
    else:
        mouseClick(waitForObject({"type": "TreeItem", "id": "Installed"}))
        vsToolsLabel = waitForObject(names.extensionManager_UI_InstalledExtensionItem_The_Qt_VS_Tools_for_Visual_Studio_2019_Label)
    mouseClick(vsToolsLabel)
    return vsToolsLabel.text


def changesScheduledLabelExists():
    return object.exists(names.changes_scheduled_Label)


def readExpectedVsToolsVersion():
    try:
        versionXml = minidom.parse("../../../../version.targets")
        return versionXml.getElementsByTagName("QtVSToolsVersion")[0].firstChild.data
    except:
        test.fatal("Can't read expected VS Tools version from sources.")
        return ""


def verifyVsToolsVersion():
    displayedVersion = waitForObjectExists(names.manage_Extensions_Version_Label).text
    expectedVersion = readExpectedVsToolsVersion()
    test.verify(expectedVersion and displayedVersion.startswith(expectedVersion),
                "Expected version of VS Tools is displayed? Displayed: %s, Expected: %s"
                % (displayedVersion, expectedVersion))


def closeMainWindow():
    mouseClick(waitForObject(names.file_MenuItem))
    mouseClick(waitForObject(names.pART_Popup_Exit_MenuItem))
