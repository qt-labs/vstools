/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    using static RegExprAssert;

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    /// RegExprAssert ( -> RegExpr)
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Asserts a pattern on the input string without consuming chars.
    /// </summary>
    ///
    public partial class RegExprAssert : RegExpr
    {
        public enum AssertLook { Ahead, Behind }

        public AssertLook Context { get; set; }
        public bool Negative { get; set; }
        public RegExpr Expr { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);

            if (mode.HasFlag(RenderMode.Assert))
                throw new NestedAssertException();

            switch (Context) {
            case AssertLook.Ahead:
                pattern.Append(Negative ? "(?!" : "(?=");
                break;
            case AssertLook.Behind:
                pattern.Append(Negative ? "(?<!" : "(?<=");
                break;
            }

            mode |= RenderMode.Assert;
            return Items(Expr);
        }

        protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            pattern.Append(")");
            mode &= ~RenderMode.Assert;
        }
    }

    public abstract partial class RegExpr
    {
        RegExprAssert AsAssert()
        {
            if (this is RegExprAssert)
                return this as RegExprAssert;

            return new RegExprAssert
            {
                Context = AssertLook.Ahead,
                Negative = false,
                Expr = this
            };
        }

        public static RegExprAssert AssertLookAhead(RegExpr expr)
        {
            var assert = expr.AsAssert();
            return new RegExprAssert
            {
                Context = AssertLook.Ahead,
                Negative = assert.Negative,
                Expr = assert.Expr
            };
        }

        public static RegExprAssert AssertLookBehind(RegExpr expr)
        {
            var assert = expr.AsAssert();
            return new RegExprAssert
            {
                Context = AssertLook.Behind,
                Negative = assert.Negative,
                Expr = assert.Expr
            };
        }

        public static RegExprAssert AssertNegated(RegExpr expr)
        {
            var assert = expr.AsAssert();
            return new RegExprAssert
            {
                Context = assert.Context,
                Negative = !assert.Negative,
                Expr = assert.Expr
            };
        }

        public delegate RegExprAssert AssertTemplate(RegExpr expr);

        public class AssertExprBuilder
        {
            AssertTemplate Template { get; }

            public AssertExprBuilder(AssertTemplate template)
            {
                Template = template;
            }

            public class Expr
            {
                public RegExprAssert Assert { get; set; }
                public Expr(RegExprAssert assert) { Assert = assert; }

                public static implicit operator RegExpr(Expr e)
                {
                    return e.Assert;
                }

                public static RegExpr operator &(RegExpr rx1, Expr rx2)
                {
                    return Concat(rx1, rx2);
                }

                public static RegExpr operator |(RegExpr rx1, Expr rx2)
                {
                    return Choice(rx1, rx2);
                }
            }

            public class NegateableExpr : Expr
            {
                public NegateableExpr(RegExprAssert assert) : base(assert) { }

                public static Expr operator !(NegateableExpr x)
                {
                    return new Expr(AssertNegated(x.Assert));
                }
            }

            public NegateableExpr this[RegExpr expr] => new NegateableExpr(Template(expr));
        }

        public class NestedAssertException : RegExprException
        {
            public NestedAssertException(string message = null) : base(message) { }
        }
    }
}
