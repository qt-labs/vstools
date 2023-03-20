####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

import os
import subprocess

import globalnames

rootSuffix = "SquishTestInstance"


def getAppProperty(property):
    vsDirectory = currentApplicationContext().commandLine.strip('"').partition("\\Common7")[0]
    programFilesDir = os.getenv("ProgramFiles(x86)")
    plv = subprocess.check_output('"%s/Microsoft Visual Studio/Installer/vswhere.exe" '
                                  '-path "%s" -property %s'
                                  % (programFilesDir, vsDirectory, property))
    return plv.decode().strip()


def startAppGetVersion(waitForInitialDialogs=False):
    startApplication("devenv /LCID 1033 /RootSuffix %s" % rootSuffix)
    version = getAppProperty("catalog_productLineVersion")
    if waitForInitialDialogs:
        try:
            if version == "2022":
                clickButton(waitForObject(globalnames.msvs_Skip_this_for_now_Button, 10000))
            else:
                mouseClick(waitForObject(globalnames.msvs_Not_now_maybe_later_Label, 10000))
            clickButton(waitForObject(globalnames.msvs_Start_Visual_Studio_Button))
        except:
            pass
    mouseClick(waitForObject(globalnames.continueWithoutCode_Label))
    return version


def openVsToolsMenu(version):
    while True:
        mouseClick(waitForObject(globalnames.extensions_MenuItem))
        mouseClick(waitForObject(globalnames.pART_Popup_Qt_VS_Tools_MenuItem, 5000))
        if not object.exists(globalnames.Initializing_MenuItem):
            break
        mouseClick(waitForObject(globalnames.extensions_MenuItem))  # close menu
        snooze(4)


def closeMainWindow():
    mouseClick(waitForObject(globalnames.file_MenuItem))
    mouseClick(waitForObject(globalnames.pART_Popup_Exit_MenuItem))
