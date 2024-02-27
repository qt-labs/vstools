####################################################################################################
# Copyright (C) 2024 The Qt Company Ltd.
# SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
####################################################################################################

# -*- coding: utf-8 -*-

import test

class TestSection:

    def __init__(self, description):
        self.description = description

    def __enter__(self):
        test.startSection(self.description)

    def __exit__(self, _, __, ___):
        test.endSection()
