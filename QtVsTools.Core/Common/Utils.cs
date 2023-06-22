/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;

namespace QtVsTools
{
    public static class Utils
    {
        public static class ProjectTypes
        {
            public const string C_PLUS_PLUS = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        }

        public static StringComparison IgnoreCase => StringComparison.OrdinalIgnoreCase;
        public static StringComparer CaseIgnorer => StringComparer.OrdinalIgnoreCase;
        public static string EmDash => "\u2014";
    }
}
