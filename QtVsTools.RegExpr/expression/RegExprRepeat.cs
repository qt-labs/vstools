/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    /// <summary>
    /// Regular expression quantifier.
    /// </summary>
    public class RegExprRepeat : RegExpr
    {
        public int AtLeast { get; set; }
        public int AtMost { get; set; }
        public RegExpr Expr { get; set; }

        bool ExprNeedsGroup => Expr is RegExprSequence
            || (Expr is RegExprLiteral && NeedsGroup(Expr.As<RegExprLiteral>().LiteralExpr));

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);

            if (ExprNeedsGroup)
                pattern.Append("(?:");

            return Items(Expr);
        }

        protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode, tokenStack);

            if (ExprNeedsGroup)
                pattern.Append(")");

            if (AtLeast == 0 && AtMost == 1)
                pattern.Append("?");
            else if (AtLeast == 0 && AtMost == int.MaxValue)
                pattern.Append("*");
            else if (AtLeast == 1 && AtMost == int.MaxValue)
                pattern.Append("+");
            else if (AtLeast == AtMost)
                pattern.AppendFormat("{{{0}}}", AtLeast);
            else if (AtMost == int.MaxValue)
                pattern.AppendFormat("{{{0},}}", AtLeast);
            else if (AtLeast == 0)
                pattern.AppendFormat("{{,{0}}}", AtMost);
            else
                pattern.AppendFormat("{{{0},{1}}}", AtLeast, AtMost);
        }
    }

    public abstract partial class RegExpr
    {
        public RegExpr Optional()
        {
            return Repeat(0, 1);
        }

        public RegExpr Repeat(int count)
        {
            return Repeat(count, count);
        }

        public RegExpr Repeat(int atLeast = 0, int atMost = int.MaxValue)
        {
            if (this is RegExprRepeat)
                throw new NestedRepeatException();

            return new RegExprRepeat
            {
                AtLeast = atLeast,
                AtMost = atMost,
                Expr = this
            };
        }

        public class NestedRepeatException : RegExprException
        {
            public NestedRepeatException(string message = null) : base(message) { }
        }
    }
}
