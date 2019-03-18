/****************************************************************************
**
** Copyright (C) 2019 The Qt Company Ltd.
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
using System.Collections.Generic;
using System.Linq;

namespace QtVsTools.SyntaxAnalysis
{
    public abstract partial class RegExpr
    {
        const string MetaChars = @"[]\/#^$.|?*+(){}-";

        protected static string Escape(string s)
        {
            return new string(s.SelectMany(c =>
                MetaChars.Contains(c) ? Items('\\', c) :
                (c == ' ') ? "\\x20".Cast<char>() :
                (c == '\t') ? "\\t".Cast<char>() :
                (c == '\r') ? "\\r".Cast<char>() :
                (c == '\n') ? "\\n".Cast<char>() :
                Items(c)).ToArray());
        }

        public static bool NeedsGroup(string literal)
        {
            if (literal.Length == 1)
                return false;
            if (literal.Length == 2 && literal.StartsWith(@"\"))
                return false;
            if (literal.Length == 3 && literal.StartsWith(@"\c"))
                return false;
            if (literal.Length == 4 && literal.StartsWith(@"\x"))
                return false;
            if (literal.Length == 6 && literal.StartsWith(@"\u"))
                return false;
            return true;
        }

        internal T As<T>()
            where T : RegExpr
        {
            if (this is T)
                return this as T;
            else
                throw new InvalidCastException();
        }

        internal static IEnumerable<T> Empty<T>()
        {
            return new T[] { };
        }

        internal static IEnumerable<T> Items<T>(params T[] items)
        {
            return items;
        }

        public sealed class Void { }
    }

    public static class RegExprExtensions
    {
        public static void ReverseTop<T>(this Stack<T> stack, int count = 2)
        {
            if (count < 2 || stack.Count < count)
                return;
            var items = Enumerable.Repeat(stack, count).Select(s => s.Pop()).ToList();
            items.ForEach(item => stack.Push(item));
        }
    }
}
