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
                 "It is designed to run on VS without Qt VS Tools installed "
                 "and requires manual steps.")
    version = startAppGetVersion()
    if not version:
        return
    if downloadQtVsTools(version):
        test.warning("If the test succeeded so far, it now requires manual steps.",
                     "Please finish the steps of the VSIX Installer wizard which should have "
                     "appeared. After this, you can run tst_install_verify to verify the result.")
    closeMainWindow()


def downloadQtVsTools(version):
    openExtensionManager(version)
    mouseClick(waitForObjectItem(names.o_Extensions_ProvidersTree_Tree, "Online"))
    mouseClick(waitForObjectItem(names.providersTree_Online_TreeItem, "Visual Studio Marketplace"))
    mouseClick(waitForObject(names.o_Extensions_Edit))
    type(waitForObject(names.o_Extensions_Edit), "qt")
    type(waitForObject(names.o_Extensions_Edit), "<Return>")
    mouseClick(waitForObject(names.extensionManager_UI_InstalledExtItem_Qt_Label))
    verifyVsToolsVersion()
    try:
        downloadButton = waitForObject(names.OnlineExtensionItem_Download_Button)
    except:
        test.fatal("Could not find the download button.",
                   "If the Qt VS Tools are already installed, "
                   "please remove them before running this test.")
        clickButton(waitForObject(names.manage_Extensions_Close_Button))
        return False
    clickButton(downloadButton)
    test.verify(waitFor(changesScheduledLabelExists, 60000),
                "Were changes to the installation scheduled?")
    clickButton(waitForObject(names.manage_Extensions_Close_Button))
    return True
