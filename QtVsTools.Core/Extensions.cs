/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;

namespace QtVsTools.Core
{
    public static class Extensions
    {
        public static string Replace(this string original, string oldValue, string newValue,
            StringComparison comparison)
        {
            newValue ??= "";
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(oldValue)
                || string.Equals(oldValue, newValue, comparison)) {
                return original;
            }

            int pos = 0, index = 0;
            var result = new System.Text.StringBuilder();
            while ((index = original.IndexOf(oldValue, pos, comparison)) >= 0) {
                result.Append(original, pos, index - pos).Append(newValue);
                pos = index + oldValue.Length;
            }
            return result.Append(original, pos, original.Length - pos).ToString();
        }
    }
}
