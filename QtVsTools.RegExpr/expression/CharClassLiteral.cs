/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    /// CharClassLiteral ( -> CharClassSet.Element -> CharClass -> RegExpr )
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Elementary character class defined by one or more allowed characters
    /// </summary>
    ///
    public class CharClassLiteral : CharClassSet.Element
    {
        public string LiteralChars { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            if (parent is not CharClass && NeedsGroup(LiteralChars))
                pattern.AppendFormat("[{0}]", LiteralChars);
            else
                pattern.Append(LiteralChars);
            return null;
        }
    }

    public abstract partial class CharClass : RegExpr
    {
        public static CharClassLiteral CharLiteral(string s)
        {
            return new CharClassLiteral
            {
                LiteralChars = Escape(s)
            };
        }

        public static CharClassLiteral CharRawLiteral(string s)
        {
            return new CharClassLiteral
            {
                LiteralChars = s
            };
        }

        public static CharClassLiteral CharLiteral(char c)
        {
            return CharLiteral(c.ToString());
        }

        public partial class CharExprBuilder
        {
            public CharClassLiteral this[string s] => CharLiteral(s);
            public CharClassLiteral this[char c] => CharLiteral(c);
        }
    }
}
