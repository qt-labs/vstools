/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    /// <summary>
    /// Literal character sequence
    /// </summary>
    public class RegExprLiteral : RegExpr
    {
        public string LiteralExpr { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            pattern.Append(LiteralExpr);
            return null;
        }
    }

    public abstract partial class RegExpr
    {
        public static RegExprLiteral RegX(string s)
        {
            return new RegExprLiteral() { LiteralExpr = Escape(s) };
        }

        public static RegExprLiteral RegXRaw(string s)
        {
            return new RegExprLiteral() { LiteralExpr = s };
        }

        public static implicit operator RegExpr(string s)
        {
            return RegX(s);
        }

        public static implicit operator RegExpr(char c)
        {
            return RegX(c.ToString());
        }
    }
}
