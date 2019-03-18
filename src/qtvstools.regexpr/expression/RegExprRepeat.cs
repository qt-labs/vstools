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

        bool ExprNeedsGroup
        {
            get
            {
                return (Expr is RegExprSequence)
                    || (Expr is RegExprLiteral
                    && NeedsGroup(Expr.As<RegExprLiteral>().LiteralExpr));
            }
        }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode);

            if (ExprNeedsGroup)
                pattern.Append("(?:");

            return Items(Expr);
        }

        protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode);

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

        public class NestedRepeatException : RegExpr.Exception
        {
            public NestedRepeatException(string message = null) : base(message) { }
        }
    }
}
