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

source("../../shared/utils.py")

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

    def changesScheduledLabelExists():
        return object.exists(names.changes_scheduled_Label)

    openExtensionManager(version)
    mouseClick(waitForObjectItem(names.o_Extensions_ProvidersTree_Tree, "Online"))
    mouseClick(waitForObjectItem(names.providersTree_Online_TreeItem, "Visual Studio Marketplace"))
    mouseClick(waitForObject(names.o_Extensions_Edit))
    type(waitForObject(names.o_Extensions_Edit), "qt")
    type(waitForObject(names.o_Extensions_Edit), "<Return>")
    mouseClick(waitForObject(names.extensionManager_UI_InstalledExtensionItem_The_Qt_VS_Tools_for_Visual_Studio_2019_Label))
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
