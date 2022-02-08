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
        public static CharClassLiteral CharWord
        { get { return new CharClassLiteral { LiteralChars = @"\w" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: [\w]*
        /// ]]></summary>
        public static RegExpr Word
        { get { return CharWord.Repeat(); } }

        /// <summary><![CDATA[
        /// Equivalent to: [\d]
        /// ]]></summary>
        public static CharClassLiteral CharDigit
        { get { return new CharClassLiteral { LiteralChars = @"\d" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: [\d]*
        /// ]]></summary>
        public static RegExpr Number
        { get { return CharDigit.Repeat(); } }

        /// <summary><![CDATA[
        /// Equivalent to: [\r]
        /// ]]></summary>
        public static CharClassLiteral CharCr
        { get { return new CharClassLiteral { LiteralChars = @"\r" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: [\n]
        /// ]]></summary>
        public static CharClassLiteral CharLf
        { get { return new CharClassLiteral { LiteralChars = @"\n" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: [\s]
        /// ]]></summary>
        public static CharClassLiteral CharSpace
        { get { return new CharClassLiteral { LiteralChars = @"\s" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: [\S]
        /// ]]></summary>
        private static CharClassLiteral CharNonSpace
        { get { return new CharClassLiteral { LiteralChars = @"\S" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: [\r\n]
        /// ]]></summary>
        public static CharClassSet CharVertSpace
        { get { return CharSet[CharCr + CharLf]; } }

        /// <summary><![CDATA[
        /// Equivalent to: [^\S\r\n]
        /// ]]></summary>
        public static CharClassSet CharHorizSpace
        { get { return CharSet[~(CharNonSpace + CharVertSpace)]; } }

        /// <summary><![CDATA[
        /// Equivalent to: .
        /// ]]></summary>
        public static RegExprLiteral AnyChar
        { get { return new RegExprLiteral { LiteralExpr = "." }; } }

        /// <summary><![CDATA[
        /// Equivalent to: ^
        /// ]]></summary>
        public static RegExprLiteral StartOfLine
        { get { return new RegExprLiteral { LiteralExpr = "^" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: $
        /// ]]></summary>
        public static RegExprLiteral EndOfLine
        { get { return new RegExprLiteral { LiteralExpr = "$" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: \A
        /// ]]></summary>
        public static RegExprLiteral StartOfFile
        { get { return new RegExprLiteral { LiteralExpr = @"\A" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: \z
        /// ]]></summary>
        public static RegExprLiteral EndOfFile
        { get { return new RegExprLiteral { LiteralExpr = @"\z" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: \r?\n
        /// ]]></summary>
        public static RegExprSequence LineBreak
        { get { return CharCr.Optional() & CharLf; } }

        /// <summary><![CDATA[
        /// Equivalent to: [\s]*
        /// ]]></summary>
        public static RegExpr Space
        { get { return CharSpace.Repeat(); } }

        /// <summary><![CDATA[
        /// Equivalent to: [\S]*
        /// ]]></summary>
        public static RegExpr NonSpace
        { get { return CharNonSpace.Repeat(); } }

        /// <summary><![CDATA[
        /// Equivalent to: [\r\n]*
        /// ]]></summary>
        public static RegExpr VertSpace
        { get { return CharVertSpace.Repeat(); } }

        /// <summary><![CDATA[
        /// Equivalent to: [^\S\r\n]*
        /// ]]></summary>
        public static RegExpr HorizSpace
        { get { return CharHorizSpace.Repeat(); } }

        /// <summary><![CDATA[
        /// Equivalent to: [^\r\n]*
        /// ]]></summary>
        public static RegExpr Line
        { get { return CharSet[~CharVertSpace].Repeat(); } }

        /// <summary><![CDATA[
        /// Equivalent to: (?i)
        /// ]]></summary>
        public static RegExprLiteral IgnoreCase
        { get { return new RegExprLiteral { LiteralExpr = @"(?i)" }; } }

        /// <summary><![CDATA[
        /// Equivalent to: (?-i)
        /// ]]></summary>
        public static RegExprLiteral SenseCase
        { get { return new RegExprLiteral { LiteralExpr = @"(?-i)" }; } }

        /// <summary>
        /// Applies the same whitespace skipping rules as tokens, but does not any capture text.
        /// </summary>
        public static RegExpr SkipWs
        { get { return new Token(); } }

        static readonly CharExprBuilder _Char = new CharExprBuilder();
        public static CharExprBuilder Char { get { return _Char; } }
        public static CharExprBuilder Chars { get { return _Char; } }

        public static CharSetExprBuilder CharSet { get; } = new CharSetExprBuilder();

        public static CharSetRawExprBuilder CharSetRaw { get; } = new CharSetRawExprBuilder();

        public static AssertExprBuilder LookAhead { get; } = new AssertExprBuilder(AssertLookAhead);

        public static AssertExprBuilder LookBehind { get; } = new AssertExprBuilder(AssertLookBehind);

        public const SkipWhitespace SkipWs_Disable = SkipWhitespace.Disable;
    }
}
