####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

source("../../shared/utils.py")
source("../shared/scripts/install_utils.py")


def main():
    startApp(clearSettings=False)
    vsToolsLabelText = selectInstalledVsTools()
    test.compare(vsToolsLabelText, None,
                "Are 'Qt VS Tools for Visual Studio' installed?")
    closeExtensionManager()
    checkMenuItems()
    closeMainWindow()


def checkMenuItems():
    try:
        openVsToolsMenu()
        test.fail("Surplus menu items", "Qt VS Tools show unexpected menu items.")
        mouseClick(waitForObject(globalnames.file_MenuItem))  # Close menu
    except:
        mouseClick(waitForObject(globalnames.file_MenuItem))  # Close Extensions menu
        test.passes("Qt VS Tools do not show unexpected menu items.")
