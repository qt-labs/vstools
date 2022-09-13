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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    public abstract partial class RegExpr
    {
        public enum SkipWhitespace { Disable, Enable }

        ////////////////////////////////////////////////////////////////////////////////////////////
        ///
        /// RegExpr.Token
        ///
        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// A token of the syntax under analysis.
        /// </summary>
        /// <remarks>
        /// Token objects contain an encapsulated <see cref="RegExpr"/> that defines the pattern
        /// of the token's syntax, rendered as a named capture group. The name of the capture group
        /// corresponds to the identifier of the token. A Token may also include production rules
        /// defining the actions to take when content is matched by the associated capture group.
        /// </remarks>
        public partial class Token : RegExpr, IEnumerable<IProductionRule>
        {
            public string Id { get; set; }

            private bool SkipLeadingWhitespace { get; }
            private RegExpr LeadingWhitespace { get; }

            private RegExpr Expr { get; }

            private HashSet<Token> Children { get; }
            public Dictionary<string, Token> Parents { get; }
            public IEnumerable<string> CaptureIds => Parents.Keys;

            public Token(string id, RegExpr skipWs, RegExpr expr)
            {
                Id = id;
                SkipLeadingWhitespace = true;
                LeadingWhitespace = skipWs;
                Expr = expr;
                Rules = new TokenRules();
                Children = new HashSet<Token>();
                Parents = new Dictionary<string, Token>();
            }

            public Token(string id, SkipWhitespace skipWs, RegExpr expr)
            {
                Id = id;
                SkipLeadingWhitespace = (skipWs == SkipWhitespace.Enable);
                Expr = expr;
                Rules = new TokenRules();
                Children = new HashSet<Token>();
                Parents = new Dictionary<string, Token>();
            }

            public Token(Enum id, RegExpr expr)
                : this(id.ToString(), SkipWhitespace.Enable, expr)
            { }

            public Token(string id, RegExpr expr)
                : this(id, SkipWhitespace.Enable, expr)
            { }

            public Token(Enum id, SkipWhitespace skipWs, RegExpr expr)
                : this(id.ToString(), skipWs, expr)
            { }

            public Token(Enum id, RegExpr skipWs, RegExpr expr)
                : this(id.ToString(), skipWs, expr)
            { }

            public Token(RegExpr skipWs, RegExpr expr)
                : this(string.Empty, skipWs, expr)
            { }

            public Token(RegExpr expr)
                : this(string.Empty, SkipWhitespace.Enable, expr)
            { }

            public Token()
                : this(string.Empty, SkipWhitespace.Enable, null)
            { }

            public static Token CreateRoot()
            {
                var rootToken = new Token();
                rootToken.Parents[ParseTree.KeyRoot] = rootToken;
                return rootToken;
            }

            protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
                StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
            {
                base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);

                var tokenWs = GetTokenWhitespace(defaultTokenWs);
                if (tokenWs != null)
                    pattern.Append("(?:");
                if (NeedsWhitespaceGroup(tokenWs, mode))
                    pattern.Append("(?:");
                return Items(tokenWs, Expr);
            }

            protected override void OnRenderNext(RegExpr defaultTokenWs, RegExpr parent,
                StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
            {
                base.OnRenderNext(defaultTokenWs, parent, pattern, ref mode, tokenStack);
                var tokenWs = GetTokenWhitespace(defaultTokenWs);
                if (NeedsWhitespaceGroup(tokenWs, mode))
                    pattern.Append(")");
                if (Expr != null) {
                    if (!mode.HasFlag(RenderMode.Assert) && !string.IsNullOrEmpty(Id)) {
                        string captureId = GenerateCaptureId(Id);
                        Parents.Add(captureId, tokenStack.Peek());
                        tokenStack.Peek().Children.Add(this);
                        pattern.AppendFormat("(?<{0}>", captureId);
                    } else {
                        pattern.Append("(?:");
                    }
                }
                tokenStack.Push(this);
            }

            protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
                StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
            {
                base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode, tokenStack);
                if (Expr != null)
                    pattern.Append(")");
                var tokenWs = GetTokenWhitespace(defaultTokenWs);
                if (tokenWs != null)
                    pattern.Append(")");
                tokenStack.Pop();
            }

            /// <summary>
            /// Set of rules that can be applied to the token.
            /// </summary>
            TokenRules Rules { get; }

            public void Add(IProductionRule rule)
            {
                Rules.Add(rule);
                rule.Token = this;
            }

            public IProductionRule SelectRule(ITokenCapture tokenCapture)
            {
                return Rules.Select(tokenCapture);
            }

            const string TokenUniqueIdTemplate = "TOKEN_{0}_{1}";

            protected static string GenerateCaptureId(string Id)
            {
                return string.Concat(string.Format(TokenUniqueIdTemplate,
                    Path.GetRandomFileName().Replace(".", ""), Id)
                    .Take(32));
            }

            RegExpr GetTokenWhitespace(RegExpr defaultTokenWs)
            {
                if (!SkipLeadingWhitespace)
                    return null;
                var tokenWs = LeadingWhitespace;
                if (tokenWs == null)
                    tokenWs = defaultTokenWs;
                return tokenWs;
            }

            bool NeedsWhitespaceGroup(RegExpr tokenWs, RenderMode mode)
            {
                return tokenWs != null && !mode.HasFlag(RenderMode.Assert)
                    && (tokenWs is RegExprLiteral || tokenWs is RegExprSequence);
            }

            public IEnumerator<IProductionRule> GetEnumerator()
            {
                return ((IEnumerable<IProductionRule>)Rules).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<IProductionRule>)Rules).GetEnumerator();
            }
        }

        public class TokenGroup : IEnumerable<string>
        {
            HashSet<string> TokenIds { get; }

            public TokenGroup(params string[] tokenIds)
            {
                TokenIds = new HashSet<string>(tokenIds);
            }

            public void Add(string tokenId)
            {
                TokenIds.Add(tokenId);
            }

            public void Add(IEnumerable<string> tokenIds)
            {
                TokenIds.UnionWith(tokenIds);
            }

            public void Add(Token token)
            {
                Add(token.Id);
            }

            public static implicit operator TokenGroup(string tokenId)
            {
                return new TokenGroup(tokenId);
            }

            public IEnumerator<string> GetEnumerator()
            {
                return TokenIds.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return TokenIds.GetEnumerator();
            }
        }

        public class TokenRules : IEnumerable<IProductionRule>
        {
            Dictionary<RuleCallback.Selector, IProductionRule> Rules { get; }
            IProductionRule DefaultRule { get; set; }

            public TokenRules()
            {
                Rules = new Dictionary<RuleCallback.Selector, IProductionRule>();
            }

            public void Add(IProductionRule item)
            {
                if (item.Selector == Default)
                    DefaultRule = item;
                else
                    Rules[item.Selector] = item;
            }

            bool TestSelector(
                KeyValuePair<RuleCallback.Selector, IProductionRule> pairSelectorRule,
                ITokenCapture tokenCapture)
            {
                var selector = pairSelectorRule.Key;
                var rule = pairSelectorRule.Value;
                if (rule == null)
                    return false;
                if (selector != null && !selector(tokenCapture))
                    return false;
                return true;
            }

            public IProductionRule Select(ITokenCapture tokenCapture)
            {
                var selectedRules = Rules
                    .Where(rule => TestSelector(rule, tokenCapture))
                    .Select(rule => rule.Value);
                return selectedRules.Any() ? selectedRules.First() : DefaultRule;
            }

            public IEnumerator<IProductionRule> GetEnumerator()
            {
                return Rules.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Rules.Values.GetEnumerator();
            }
        }

        public interface ITokenCapture
        {
            string TokenId { get; }
            string Value { get; }
            bool IsFirst { get; }
            bool IsLast { get; }
            IEnumerable<ITokenCapture> LookAhead(params TokenGroup[] tokenIds);
            IEnumerable<ITokenCapture> LookBehind(params TokenGroup[] tokenIds);
            bool Is(params TokenGroup[] tokenIds);
            bool IsNot(params TokenGroup[] tokenIds);
        }

        public interface IOperatorCapture : ITokenCapture
        {
            IOperandCapture Operand { get; }
            IOperandCapture LeftOperand { get; }
            IOperandCapture RightOperand { get; }
            bool HasOperand { get; }
            bool HasLeftOperand { get; }
            bool HasRightOperand { get; }
        }

        public interface IOperandCapture : ITokenCapture
        {
            object Production { get; }
        }

        public class TokenEndOfList : IOperatorCapture, IOperandCapture
        {
            public string TokenId { get; }
            public string Value { get; }
            public bool IsFirst { get; }
            public bool IsLast { get; }
            public IEnumerable<ITokenCapture> LookAhead(params TokenGroup[] tokenIds)
            {
                return Items(this);
            }

            public IEnumerable<ITokenCapture> LookBehind(params TokenGroup[] tokenIds)
            {
                return Items(this);
            }

            public bool Is(params TokenGroup[] tokenIds)
            {
                return false;
            }

            public bool IsNot(params TokenGroup[] tokenIds)
            {
                return true;
            }

            public IOperandCapture Operand => this;

            public IOperandCapture LeftOperand => this;

            public IOperandCapture RightOperand => this;

            public bool HasOperand { get; }
            public bool HasLeftOperand { get; }
            public bool HasRightOperand { get; }
            public object Production { get; }
        }

        static readonly TokenEndOfList EndOfList = new TokenEndOfList();
    }
}
