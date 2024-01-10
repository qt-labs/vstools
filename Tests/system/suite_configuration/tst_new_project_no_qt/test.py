####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

source("../shared/scripts/config_utils.py")

import names


def testForMissingQt(_, __):
    test.compare(waitForObjectExists(names.qt_ConfigTable).rowCount, 0)
    test.compare(waitForObjectExists(names.no_Qt_version_Label).text,
                 'Register at least one Qt version using "Qt VS Tools" -> "Qt Options".')
    test.verify(not waitForObjectExists(names.qt_Wizard_Next_Button).enabled,
                '"Next" button should be disabled when there are no Qt versions')
    test.verify(not waitForObjectExists(names.qt_Wizard_Finish_Button).enabled,
                '"Finish" button should be disabled when there are no Qt versions')


def main():
    testAllQtWizards(funcPage2=testForMissingQt, setupQtVersions=False)
