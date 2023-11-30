####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

source("../../shared/utils.py")
source("../shared/scripts/install_utils.py")

import names


def main():
    version = startAppGetVersion()
    if not version:
        return
    checkVSVersion(version)
    vsToolsLabelText = selectInstalledVsTools(version)
    if test.verify(vsToolsLabelText, "Are Qt VS Tools found in extension manager?"):
        test.verify(vsToolsLabelText.startswith("The Qt VS Tools for Visual Studio " + version),
                    "Are these 'Qt VS Tools for Visual Studio %s' as expected? Found:\n%s"
                    % (version, vsToolsLabelText))
        verifyVsToolsVersion()
    clickButton(waitForObject(names.manage_Extensions_Close_Button))
    checkMenuItems(version)
    closeMainWindow()


def checkVSVersion(version):
    mouseClick(waitForObject(names.help_MenuItem))
    mouseClick(waitForObject(names.help_About_Microsoft_Visual_Studio_MenuItem))
    vsVersionText = waitForObjectExists(names.about_Microsoft_Visual_Studio_Edit).text
    test.verify(version in vsVersionText,
                "Is this VS %s as expected? Found:\n%s" % (version, vsVersionText))
    clickButton(waitForObject(names.o_Microsoft_Visual_Studio_OK_Button))


def checkMenuItems(version):
    try:
        openVsToolsMenu(version)
        waitForObject(names.pART_Popup_qt_io_MenuItem, 5000)
        test.passes("Qt VS Tools show expected menu items.")
        mouseClick(waitForObject(globalnames.file_MenuItem))  # Close menu
    except:
        mouseClick(waitForObject(globalnames.file_MenuItem))  # Close Extensions menu
        test.fail("Missing menu items", "Qt VS Tools do not show expected menu items.")
