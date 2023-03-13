####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

source("../../shared/utils.py")

import builtins
import subprocess

# This script does not actually test anything. It only resets the experimental environment the
# tests are running in so you can start from scratch. After running the script, the environment
# will not contain any user settings. Only nagsreens from first start will already be handled.


def main():
    # Start MSVS to determine its version and instanceID, then close it immediately.
    version = startAppGetVersion(True)
    vsVersionNr = {"2019":"16.0", "2022":"17.0"}[version]
    vsInstance = "_".join([vsVersionNr, getAppProperty("instanceID")])
    installationPath = getAppProperty("installationPath")
    closeMainWindow()
    # Wait for MSVS to shut down
    waitFor("not currentApplicationContext().isRunning")
    snooze(2)
    # Reset the experimental environment
    subprocess.check_output('"%s/VSSDK/VisualStudioIntegration/Tools/Bin/'
                            'CreateExpInstance.exe" /Reset /VSInstance=%s /RootSuffix=%s'
                            % (installationPath, vsInstance, rootSuffix))
    try:
        # Start MSVS again to click away the nagscreens shown on first start
        startAppGetVersion(True)
        closeMainWindow()
    except (LookupError, TypeError) as e:
        if version != "2019":
            raise
        # After clicking away the nagscreens, MSVS2019 seems to restart itself. After the restart,
        # Squish can't find objects anymore. Just ignore the thrown exception. The instance was
        # reset already at that point.
        test.log("Caught %s from MSVS2019" % builtins.type(e), str(e))
