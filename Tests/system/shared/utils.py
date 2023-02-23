####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

import os
import subprocess

import globalnames

def startAppGetVersion():
    appContext = startApplication("devenv /LCID 1033 /RootSuffix SquishTestInstance")
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
    mouseClick(waitForObject(globalnames.continueWithoutCode_Label))
    return version


def openVsToolsMenu(version):
    mouseClick(waitForObject(globalnames.extensions_MenuItem))
    mouseClick(waitForObject(globalnames.pART_Popup_Qt_VS_Tools_MenuItem, 5000))


def closeMainWindow():
    mouseClick(waitForObject(globalnames.file_MenuItem))
    mouseClick(waitForObject(globalnames.pART_Popup_Exit_MenuItem))
