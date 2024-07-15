####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

source("../../shared/utils.py")
source("../shared/scripts/install_utils.py")

import names


def main():
    test.warning("This is a semi-manual test.",
                 "It is designed to run on VS with Qt VS Tools installed "
                 "and requires manual steps.")
    startApp()
    if uninstallQtVsTools():
        test.warning("If the test succeeded so far, it now requires manual steps.",
                     "Please finish the steps of the VSIX Installer wizard which should have "
                     "appeared. After this, you can run tst_uninstall_verify to check the result.")
    closeMainWindow()


def uninstallQtVsTools():
    selectInstalledVsTools()
    mouseClick(waitForObject(names.msvs_ExtensionManager_UI_InstalledExtItem_Uninstall_Label))
    clickButton(waitForObject(names.microsoft_Visual_Studio_Yes_Button))
    testChangesScheduledLabel()
    closeExtensionManager()
    return True
