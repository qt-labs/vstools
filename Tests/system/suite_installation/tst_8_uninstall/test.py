####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

source("../../shared/utils.py")
source("../shared/scripts/install_utils.py")

import names


def main():
    test.warning("This is a semi-manual test.",
                 "It is designed to run on VS with Qt VS Tools installed "
                 "and requires manual steps.")
    version = startAppGetVersion()
    if not version:
        return
    if uninstallQtVsTools(version):
        test.warning("If the test succeeded so far, it now requires manual steps.",
                     "Please finish the steps of the VSIX Installer wizard which should have "
                     "appeared. After this, you can run tst_uninstall_verify to check the result.")
    closeMainWindow()


def uninstallQtVsTools(version):
    selectInstalledVsTools(version)
    mouseClick(waitForObject(names.msvs_ExtensionManager_UI_InstalledExtItem_Uninstall_Label))
    clickButton(waitForObject(names.microsoft_Visual_Studio_Yes_Button))
    test.verify(waitFor(changesScheduledLabelExists, 5000),
                "Were changes to the installation scheduled?")
    clickButton(waitForObject(names.manage_Extensions_Close_Button))
    return True
