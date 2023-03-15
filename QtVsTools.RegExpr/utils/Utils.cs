/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
            return literal.Length switch
            {
                1 => false,
                2 when literal.StartsWith(@"\") => false,
                3 when literal.StartsWith(@"\c") => false,
                4 when literal.StartsWith(@"\x") => false,
                6 when literal.StartsWith(@"\u") => false,
                _ => true
            };
        }

        internal T As<T>()
            where T : RegExpr
        {
            if (this is T expr)
                return expr;
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
