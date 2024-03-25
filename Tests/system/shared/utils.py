####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
    try:
        if version == "2022":
            # If it appears, close the "Sign in" nagscreen.
            clickButton(waitForObject(globalnames.msvs_Account_Close_Button, 10000))
    except:
        pass
    return version


def openVsToolsMenu(version):
    while True:
        mouseClick(waitForObject(globalnames.extensions_MenuItem))
        mouseClick(waitForObject(globalnames.extensions_Qt_VS_Tools_MenuItem, 5000))
        if not waitFor("object.exists(globalnames.Initializing_MenuItem)", 500):
            break
        mouseClick(waitForObject(globalnames.extensions_MenuItem))  # close menu
        snooze(4)


def closeMainWindow():
    mouseClick(waitForObject(globalnames.file_MenuItem))
    mouseClick(waitForObject(globalnames.file_Exit_MenuItem))
