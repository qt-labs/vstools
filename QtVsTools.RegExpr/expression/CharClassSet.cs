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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    using static CharClassSet;
    using static CharClass.CharSetExprBuilder;

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    /// CharClassSet ( -> CharClass -> RegExpr )
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Represents a complex class defined by a combination of elementary character classes.
    /// </summary>
    ///
    public partial class CharClassSet : CharClass, IEnumerable<IEnumerable<Element>>
    {
        private IEnumerable<Element> Positives { get; set; }
        private IEnumerable<Element> Negatives { get; set; }

        bool IsSubSet { get; set; }
        bool HasPositive => Positives?.Any() == true;
        bool HasNegative => Negatives?.Any() == true;

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);

            if (!HasPositive && !HasNegative)
                return null;

            if (!IsSubSet) {
                if (HasPositive)
                    pattern.Append("[");
                else
                    pattern.Append("[^");
            }

            IEnumerable<RegExpr> children = null;
            if (HasPositive && HasNegative) {
                children = Items(
                    new CharClassSet(positives: Positives) { IsSubSet = true },
                    new CharClassSet(negatives: Negatives) { IsSubSet = true });
            } else {
                if (HasPositive)
                    children = Positives;
                else if (HasNegative)
                    children = Negatives;
            }

            return children;
        }

        protected override void OnRenderNext(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRenderNext(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            if (!IsSubSet && HasPositive && HasNegative)
                pattern.Append("-[");
        }

        protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            if (!IsSubSet) {
                if (HasPositive && HasNegative)
                    pattern.Append("]]");
                else
                    pattern.Append("]");
            }
        }

        public const bool Invert = true;
        public const bool Positive = true;

        public CharClassSet(
            IEnumerable<Element> positives = null,
            IEnumerable<Element> negatives = null)
        {
            Positives = positives != null ? positives : Empty<Element>();
            Negatives = negatives != null ? negatives : Empty<Element>();
        }

        public CharClassSet(Element element, bool negative = false) : this()
        {
            if (negative)
                Negatives = Items(element);
            else
                Positives = Items(element);
        }

        public CharClassSet(CharClassSet set, bool invert = false) : this()
        {
            Add(set, invert);
        }

        public void Add(Element element, bool negative = false)
        {
            if (negative)
                Negatives = Negatives.Concat(Items(element));
            else
                Positives = Positives.Concat(Items(element));
        }

        public void Add(CharClassSet set, bool invert = false)
        {
            Positives = Positives.Concat(!invert ? set.Positives : set.Negatives);
            Negatives = Negatives.Concat(!invert ? set.Negatives : set.Positives);
        }

        public IEnumerator<IEnumerable<Element>> GetEnumerator()
        {
            return (new[] { Positives, Negatives }).AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract class Element : CharClass
        {
            public static CharClassSet operator ~(Element x)
            { return new CharClassSet(x, negative: true); }

            public static PositiveSet operator +(Element x, Element y)
            { return new PositiveSet(Op.Plus, x, y); }

            public static PositiveSet operator +(Element x, PositiveSet y)
            { return new PositiveSet(Op.Plus, x, y); }

            public static PositiveSet operator +(PositiveSet x, Element y)
            { return new PositiveSet(Op.Plus, x, y); }

            public static Expr operator -(Element x, Element y)
            { return new Expr(Op.Minus, x, y); }

            public static Expr operator -(Element x, PositiveSet y)
            { return new Expr(Op.Minus, x, y); }

            public static Expr operator -(PositiveSet x, Element y)
            { return new Expr(Op.Minus, x, y); }
        }

        public static CharClassSet operator ~(CharClassSet x)
        { return new CharClassSet(x, invert: true); }

        public static Expr operator +(Element x, CharClassSet y)
        { return new Expr(Op.Plus, x, y); }

        public static Expr operator +(CharClassSet x, Element y)
        { return new Expr(Op.Plus, x, y); }

        public static Expr operator +(PositiveSet x, CharClassSet y)
        { return new Expr(Op.Plus, x, y); }

        public static Expr operator +(CharClassSet x, PositiveSet y)
        { return new Expr(Op.Plus, x, y); }

        public static Expr operator +(CharClassSet x, CharClassSet y)
        { return new Expr(Op.Plus, x, y); }

        public static Expr operator -(CharClassSet x, Element y)
        { return new Expr(Op.Minus, x, y); }

        public static Expr operator -(CharClassSet x, PositiveSet y)
        { return new Expr(Op.Minus, x, y); }
    }

    public abstract partial class CharClass : RegExpr
    {
        public partial class CharSetExprBuilder
        {
            public enum Op { Term, Tilde, Plus, Minus }

            public class Expr
            {
                public Op Operator { get; }

                public List<Expr> Factors { get; }
                public Expr(Op op, List<Expr> factors) { Operator = op; Factors = factors; }
                public Expr(Op op, params Expr[] factors) : this(op, factors.ToList()) { }

                public CharClass Term { get; }
                public Expr(CharClass c) { Operator = Op.Term; Term = c; }
                public static implicit operator Expr(CharClass c) { return new Expr(c); }
                public static implicit operator Expr(string s) { return Char[s]; }
                public static implicit operator Expr(char c) { return Char[c]; }
                public static Expr operator ~(Expr x)
                { return new Expr(Op.Tilde, x); }
            }

            public class PositiveSet : Expr
            {
                public PositiveSet(Op op, params Expr[] factors) : base(op, factors) { }

                public static PositiveSet operator +(PositiveSet x, PositiveSet y)
                { return new PositiveSet(Op.Plus, x, y); }

                public static Expr operator -(PositiveSet x, PositiveSet y)
                { return new Expr(Op.Minus, x, y); }
            }

            public CharClassSet this[params Expr[] exprs] => this[new PositiveSet(Op.Plus, exprs)];

            public CharClassSet this[Expr expr]
            {
                get
                {
                    var stack = new Stack<StackFrame>();
                    stack.Push(expr);
                    CharClassSet classSet = null;

                    while (stack.Any()) {
                        var context = stack.Pop();
                        expr = context.Expr;
                        if (context == null || expr == null) {
                            continue;
                        } else if (context.Children == null) {
                            context.Children = new Queue<Expr>();
                            if (expr.Factors != null && expr.Factors.Any())
                                expr.Factors.ForEach(x => context.Children.Enqueue(x));
                            stack.Push(context);
                            continue;
                        } else if (context.Children.Any()) {
                            expr = context.Children.Dequeue();
                            stack.Push(context);
                            stack.Push(expr);
                            continue;
                        }

                        classSet = null;
                        if (expr.Operator == Op.Term) {
                            if (expr.Term is CharClassSet charClassSet)
                                classSet = charClassSet;
                            else
                                classSet = new CharClassSet(expr.Term as Element);
                        } else if (context.SubSets != null && context.SubSets.Any()) {
                            switch (expr.Operator) {
                            case Op.Tilde:
                                classSet = new CharClassSet
                                    {
                                        { context.SubSets.First(), Invert }
                                    };
                                break;
                            case Op.Plus:
                                classSet = new CharClassSet
                                    {
                                        { context.SubSets.First() },
                                        { context.SubSets.Last() }
                                    };
                                break;
                            case Op.Minus:
                                classSet = new CharClassSet
                                    {
                                        { context.SubSets.First() },
                                        { context.SubSets.Last(), Invert }
                                    };
                                break;
                            }
                        }

                        var parentContext = stack.Any() ? stack.Peek() : null;
                        if (classSet != null && parentContext != null)
                            parentContext.SubSets.Add(classSet);
                    }

                    if (classSet == null)
                        throw new CharClassEvalException();

                    return classSet;
                }
            }

            public CharClassSet this[IEnumerable<char> chars] =>
                this[chars.Select(c => (Expr)c).ToArray()];

            class StackFrame
            {
                public Expr Expr { get; set; }
                public Queue<Expr> Children { get; set; }
                public List<CharClassSet> SubSets { get; }
                public StackFrame()
                {
                    Expr = null;
                    Children = null;
                    SubSets = new List<CharClassSet>();
                }
                public static implicit operator StackFrame(Expr e)
                {
                    return new StackFrame { Expr = e };
                }
            }
        }

        public partial class CharSetRawExprBuilder
        {
            public CharClassLiteral this[string s] => CharRawLiteral(s);
        }

        public class CharClassEvalException : RegExprException
        {
            public CharClassEvalException(string message = null) : base(message) { }
        }
    }
}
