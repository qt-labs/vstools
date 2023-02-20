/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;

namespace QtVsTools
{
    public static class Utils
    {
        public static StringComparison IgnoreCase => StringComparison.OrdinalIgnoreCase;
        public static StringComparer CaseIgnorer => StringComparer.OrdinalIgnoreCase;
    }
}
