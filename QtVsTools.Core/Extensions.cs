/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;

namespace QtVsTools.Core
{
    public static class Extensions
    {
        public static string Quoute(this string input)
        {
            if (!input.StartsWith("\"", StringComparison.Ordinal))
                input = "\"" + input;
            if (!input.EndsWith("\"", StringComparison.Ordinal))
                input += "\"";
            return input;
        }

        public static string Replace(this string original, string oldValue, string newValue,
            StringComparison comparison)
        {
            newValue = newValue ?? string.Empty;
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
