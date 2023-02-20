/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    /// <summary>
    /// Alternating composition
    /// </summary>
    public partial class RegExprChoice : RegExpr
    {
        public IEnumerable<RegExpr> Exprs { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            if (!(parent is Token))
                pattern.Append("(?:");
            pattern.Append("(?:");
            return Exprs;
        }

        protected override void OnRenderNext(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRenderNext(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            pattern.Append(")|(?:");
        }

        protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            pattern.Append(")");
            if (!(parent is Token))
                pattern.Append(")");
        }
    }

    public abstract partial class RegExpr
    {
        public static RegExpr Choice(params RegExpr[] rxs)
        {
            return new RegExprChoice
            {
                Exprs = rxs.SelectMany(rx => rx is RegExprChoice choice
                    ? choice.Exprs
                    : Items(rx))
            };

        }

        public static RegExpr operator |(RegExpr rx1, RegExpr rx2)
        {
            return Choice(rx1, rx2);
        }
    }
}
