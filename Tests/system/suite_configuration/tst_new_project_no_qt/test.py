############################################################################
#
# Copyright (C) 2023 The Qt Company Ltd.
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
