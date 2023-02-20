/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    public abstract partial class RegExpr
    {
        public class Pattern
        {
            public RegExpr Expr { get; set; }
            public string ExprRender { get; set; }
            public Dictionary<string, Token> Tokens { get; set; }
            public Token Root { get; set; }
        }

        class Renderer
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///
            /// RegExpr.Renderer.RenderPattern()
            ///
            ////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// Transform the RegExpr representation of a regular expression into a pattern string
            /// and a mapping of capture group id's into corresponding token definitions.
            /// </summary>
            /// <param name="rootExpr">RegExpr to render</param>
            /// <param name="wsExpr">Default token white-space</param>
            /// <returns>Pattern object containing pattern string and token map</returns>
            public Pattern RenderPattern(RegExpr rootExpr, RegExpr wsExpr)
            {
                var pattern = new StringBuilder();
                var rootToken = Token.CreateRoot();
                var tokenStack = new Stack<Token>();
                tokenStack.Push(rootToken);
                var tokens = new HashSet<Token>();

                var stack = new Stack<StackFrame>();
                var mode = RenderMode.Default;

                stack.Push(rootExpr);
                while (stack.Any()) {
                    var context = stack.Pop();

                    if (context.Expr == null)
                        continue;

                    var expr = context.Expr;
                    IEnumerable<RegExpr> children = context.Children;
                    RegExpr parent = stack.Any() ? stack.Peek() : null;

                    if (expr is Token token)
                        tokens.Add(token);

                    if (children == null) {
                        children = expr.OnRender(wsExpr, parent, pattern, ref mode, tokenStack);
                        if (children != null && children.Any()) {
                            stack.Push(new StackFrame { Expr = expr, Children = children.Skip(1) });
                            stack.Push(children.First());
                        }
                    } else if (children.Any()) {
                        expr.OnRenderNext(wsExpr, parent, pattern, ref mode, tokenStack);
                        stack.Push(new StackFrame { Expr = expr, Children = children.Skip(1) });
                        stack.Push(children.First());
                    } else {
                        expr.OnRenderEnd(wsExpr, parent, pattern, ref mode, tokenStack);
                    }
                }

                var tokensByCaptureId = tokens
                    .SelectMany(token => token.CaptureIds
                        .Select(captureId => new { Id = captureId, Token = token }))
                    .ToDictionary(idToken => idToken.Id, idToken => idToken.Token);
                tokensByCaptureId.Add(ParseTree.KeyRoot, rootToken);

                return new Pattern
                {
                    Expr = rootExpr,
                    ExprRender = pattern.ToString(),
                    Tokens = tokensByCaptureId,
                    Root = rootToken
                };
            }

            class StackFrame
            {
                public RegExpr Expr { get; set; }
                public IEnumerable<RegExpr> Children { get; set; }

                public static implicit operator StackFrame(RegExpr expr)
                {
                    return new StackFrame { Expr = expr };
                }

                public static implicit operator RegExpr(StackFrame frame)
                {
                    return (frame != null) ? frame.Expr : null;
                }
            }
        }
    }
}
