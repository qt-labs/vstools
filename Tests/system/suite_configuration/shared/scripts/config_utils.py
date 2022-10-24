####################################################################################################
# Copyright (C) 2023 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
####################################################################################################

# -*- coding: utf-8 -*-

import os


def readQtDirs():
    dirString = os.getenv("SQUISH_VSTOOLS_QTDIRS")
    if not dirString:
        return []
    uniquePaths = set()
    qtDirs = []
    for current in dirString.split(";"):
        loweredPath = current.lower()
        if loweredPath in uniquePaths:
            continue
        uniquePaths.add(loweredPath)
        qtDirs.append({"path": current,
                       "name": current.rsplit(":")[-1].strip("\\").replace("\\", "_")})
    return qtDirs
