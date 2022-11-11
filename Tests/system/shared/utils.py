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

import globalnames

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
        mouseClick(waitForObject(globalnames.continueWithoutCode_Label))
    return version


def openVsToolsMenu(version):
    if version == "2017":
        mouseClick(waitForObject(globalnames.qt_VS_Tools_MenuItem, 5000))
    else:
        mouseClick(waitForObject(globalnames.extensions_MenuItem))
        mouseClick(waitForObject(globalnames.pART_Popup_Qt_VS_Tools_MenuItem, 5000))


def closeMainWindow():
    mouseClick(waitForObject(globalnames.file_MenuItem))
    mouseClick(waitForObject(globalnames.pART_Popup_Exit_MenuItem))
