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
    /// Sequential composition
    /// </summary>
    public partial class RegExprSequence : RegExpr
    {
        public IEnumerable<RegExpr> Exprs { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            return Exprs;
        }
    }

    public abstract partial class RegExpr
    {
        public static RegExprSequence Concat(params RegExpr[] rxs)
        {
            return new RegExprSequence
            {
                Exprs = rxs.SelectMany(rx => rx is RegExprSequence sequence
                    ? sequence.Exprs
                    : Items(rx))
            };
        }

        public static RegExprSequence operator &(RegExpr rx1, RegExpr rx2)
        {
            return Concat(rx1, rx2);
        }
    }
}
