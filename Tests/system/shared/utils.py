####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

import builtins
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


msvsProductLine = None
msvsVersion = None


def getMsvsProductLine():
    global msvsProductLine
    if not msvsProductLine:
        msvsProductLine = getAppProperty("catalog_productLineVersion")
    return msvsProductLine


def getMsvsVersionAsList():
    global msvsVersion
    if not msvsVersion:
        versionString = getAppProperty("catalog_productDisplayVersion")
        msvsVersion = list(map(builtins.int, versionString.split(".")))
    return msvsVersion


def fixAppContext(wantedName="devenv"):
    waitFor(lambda: len(applicationContextList()) > 1, 30000)
    appContexts = applicationContextList()
    if len(appContexts) == 1:  # Might have changed after waitFor()
        if appContexts[0].name != wantedName:
            test.fatal("The only application context is " + appContexts[0].name)
    else:
        for ctxt in appContexts:
            if ctxt.name == wantedName:
                setApplicationContext(ctxt)
                return
        test.fatal("There's no %s application context, only: %s" % (wantedName, appContexts))


def startApp(waitForInitialDialogs=False, clearSettings=True):
    command = "devenv /LCID 1033 /RootSuffix %s"
    if clearSettings:
        command += " /Command QtVSTools.ClearSettings"
    startApplication(command % rootSuffix)
    version = getMsvsProductLine()
    if waitForInitialDialogs:
        try:
            if version == "2022":
                clickButton(waitForObject(globalnames.msvs_Skip_this_for_now_Button, 10000))
            else:
                mouseClick(waitForObject(globalnames.msvs_Not_now_maybe_later_Label, 20000))
            clickButton(waitForObject(globalnames.msvs_Start_Visual_Studio_Button))
            if version == "2019":
                fixAppContext()
        except:
            pass
    if clearSettings:
        try:
            label = waitForObjectExists(names.Command_not_valid_Label, 3000)
            if label.text == 'Command "QtVSTools.ClearSettings" is not valid.':
                test.warning('Command "QtVSTools.ClearSettings" could not be handled.',
                             'Qt VS Tools might be outdated or inactive.')
            else:
                test.warning('An unexpected error message appeared.', label.text)
            clickButton(waitForObject(globalnames.microsoft_Visual_Studio_OK_Button))
        except:
            # "QtVSTools.ClearSettings" was handled successfully
            pass
    else:
        mouseClick(waitForObject(globalnames.continueWithoutCode_Label))
    try:
        if version == "2022":
            # If it appears, close the "Sign in" nagscreen.
            clickButton(waitForObject(globalnames.msvs_Account_Close_Button, 10000))
    except:
        pass


def openVsToolsMenu():
    while True:
        mouseClick(waitForObject(globalnames.extensions_MenuItem))
        mouseClick(waitForObject(globalnames.extensions_Qt_VS_Tools_MenuItem, 5000))
        if not waitFor(lambda: object.exists(globalnames.Initializing_MenuItem), 500):
            break
        mouseClick(waitForObject(globalnames.extensions_MenuItem))  # close menu
        snooze(4)


def closeMainWindow():
    mouseClick(waitForObject(globalnames.file_MenuItem))
    mouseClick(waitForObject(globalnames.file_Exit_MenuItem))
