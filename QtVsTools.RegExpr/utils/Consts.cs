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

namespace QtVsTools.SyntaxAnalysis
{
    using static CharClass;

    public abstract partial class RegExpr
    {
        /// <summary><![CDATA[
        /// Equivalent to: [\w]
        /// ]]></summary>
        public static CharClassLiteral CharWord => new CharClassLiteral { LiteralChars = @"\w" };

        /// <summary><![CDATA[
        /// Equivalent to: [\w]*
        /// ]]></summary>
        public static RegExpr Word => CharWord.Repeat();

        /// <summary><![CDATA[
        /// Equivalent to: [\d]
        /// ]]></summary>
        public static CharClassLiteral CharDigit => new CharClassLiteral { LiteralChars = @"\d" };

        /// <summary><![CDATA[
        /// Equivalent to: [\d]*
        /// ]]></summary>
        public static RegExpr Number => CharDigit.Repeat();

        /// <summary><![CDATA[
        /// Equivalent to: [\r]
        /// ]]></summary>
        public static CharClassLiteral CharCr => new CharClassLiteral { LiteralChars = @"\r" };

        /// <summary><![CDATA[
        /// Equivalent to: [\n]
        /// ]]></summary>
        public static CharClassLiteral CharLf => new CharClassLiteral { LiteralChars = @"\n" };

        /// <summary><![CDATA[
        /// Equivalent to: [\s]
        /// ]]></summary>
        public static CharClassLiteral CharSpace => new CharClassLiteral { LiteralChars = @"\s" };

        /// <summary><![CDATA[
        /// Equivalent to: [\S]
        /// ]]></summary>
        private static CharClassLiteral CharNonSpace => new CharClassLiteral { LiteralChars = @"\S" };

        /// <summary><![CDATA[
        /// Equivalent to: [\r\n]
        /// ]]></summary>
        public static CharClassSet CharVertSpace => CharSet[CharCr + CharLf];

        /// <summary><![CDATA[
        /// Equivalent to: [^\S\r\n]
        /// ]]></summary>
        public static CharClassSet CharHorizSpace => CharSet[~(CharNonSpace + CharVertSpace)];

        /// <summary><![CDATA[
        /// Equivalent to: .
        /// ]]></summary>
        public static RegExprLiteral AnyChar => new RegExprLiteral { LiteralExpr = "." };

        /// <summary><![CDATA[
        /// Equivalent to: ^
        /// ]]></summary>
        public static RegExprLiteral StartOfLine => new RegExprLiteral { LiteralExpr = "^" };

        /// <summary><![CDATA[
        /// Equivalent to: $
        /// ]]></summary>
        public static RegExprLiteral EndOfLine => new RegExprLiteral { LiteralExpr = "$" };

        /// <summary><![CDATA[
        /// Equivalent to: \A
        /// ]]></summary>
        public static RegExprLiteral StartOfFile => new RegExprLiteral { LiteralExpr = @"\A" };

        /// <summary><![CDATA[
        /// Equivalent to: \z
        /// ]]></summary>
        public static RegExprLiteral EndOfFile => new RegExprLiteral { LiteralExpr = @"\z" };

        /// <summary><![CDATA[
        /// Equivalent to: \r?\n
        /// ]]></summary>
        public static RegExprSequence LineBreak => CharCr.Optional() & CharLf;

        /// <summary><![CDATA[
        /// Equivalent to: [\s]*
        /// ]]></summary>
        public static RegExpr Space => CharSpace.Repeat();

        /// <summary><![CDATA[
        /// Equivalent to: [\S]*
        /// ]]></summary>
        public static RegExpr NonSpace => CharNonSpace.Repeat();

        /// <summary><![CDATA[
        /// Equivalent to: [\r\n]*
        /// ]]></summary>
        public static RegExpr VertSpace => CharVertSpace.Repeat();

        /// <summary><![CDATA[
        /// Equivalent to: [^\S\r\n]*
        /// ]]></summary>
        public static RegExpr HorizSpace => CharHorizSpace.Repeat();

        /// <summary><![CDATA[
        /// Equivalent to: [^\r\n]*
        /// ]]></summary>
        public static RegExpr Line => CharSet[~CharVertSpace].Repeat();

        /// <summary><![CDATA[
        /// Equivalent to: (?i)
        /// ]]></summary>
        public static RegExprLiteral IgnoreCase => new RegExprLiteral { LiteralExpr = @"(?i)" };

        /// <summary><![CDATA[
        /// Equivalent to: (?-i)
        /// ]]></summary>
        public static RegExprLiteral SenseCase => new RegExprLiteral { LiteralExpr = @"(?-i)" };

        /// <summary>
        /// Applies the same whitespace skipping rules as tokens, but does not any capture text.
        /// </summary>
        public static RegExpr SkipWs => new Token();

        public static CharExprBuilder Char { get; } = new CharExprBuilder();

        public static CharExprBuilder Chars => Char;

        public static CharSetExprBuilder CharSet { get; } = new CharSetExprBuilder();

        public static CharSetRawExprBuilder CharSetRaw { get; } = new CharSetRawExprBuilder();

        public static AssertExprBuilder LookAhead { get; } = new AssertExprBuilder(AssertLookAhead);

        public static AssertExprBuilder LookBehind { get; } = new AssertExprBuilder(AssertLookBehind);

        public const SkipWhitespace SkipWs_Disable = SkipWhitespace.Disable;
    }
}
