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
    /// CharClassRange ( -> CharClassSet.Element -> CharClass -> RegExpr )
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Represents an elementary character class defined by a range of characters
    /// </summary>
    ///
    public partial class CharClassRange : CharClassSet.Element
    {
        public CharClassLiteral LowerBound { get; set; }
        public CharClassLiteral UpperBound { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);

            if (parent is not CharClass)
                pattern.Append("[");

            return Items(LowerBound, UpperBound);
        }

        protected override void OnRenderNext(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRenderNext(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            pattern.Append("-");
        }

        protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode, tokenStack);

            if (parent is not CharClass)
                pattern.Append("]");
        }
    }

    public abstract partial class CharClass : RegExpr
    {
        public static CharClassRange CharRange(string lBound, string uBound)
        {
            return CharRange(CharLiteral(lBound), CharLiteral(uBound));
        }

        public static CharClassRange CharRange(CharClassLiteral lBound, CharClassLiteral uBound)
        {
            return new CharClassRange
            {
                LowerBound = lBound,
                UpperBound = uBound
            };
        }

        public partial class CharExprBuilder
        {
            public CharClassRange this[char lBound, char uBound] =>
                CharRange(lBound.ToString(), uBound.ToString());
        }
    }
}
