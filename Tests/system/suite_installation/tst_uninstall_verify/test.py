####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
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
    vsToolsLabelText = selectInstalledVsTools(version)
    test.compare(vsToolsLabelText, None,
                "Are 'Qt VS Tools for Visual Studio %s' installed?" % version)
    clickButton(waitForObject(names.manage_Extensions_Close_Button))
    checkMenuItems(version)
    closeMainWindow()


def checkMenuItems(version):
    try:
        openVsToolsMenu(version)
        test.fail("Surplus menu items", "Qt VS Tools show unexpected menu items.")
        mouseClick(waitForObject(globalnames.file_MenuItem))  # Close menu
    except:
        if version != "2017":
            mouseClick(waitForObject(globalnames.file_MenuItem))  # Close Extensions menu
        test.passes("Qt VS Tools do not show unexpected menu items.")
