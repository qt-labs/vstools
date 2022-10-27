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
