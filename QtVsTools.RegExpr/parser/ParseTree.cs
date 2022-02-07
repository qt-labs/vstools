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

using System.Collections.Generic;
using System.Linq;

namespace QtVsTools.SyntaxAnalysis
{
    public abstract partial class RegExpr
    {
        ////////////////////////////////////////////////////////////////////////////////////////////
        ///
        /// RegExpr.ParseTree
        ///
        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Result of processing input text with a pattern rendered from a RegExpr.
        /// </summary>
        /// <remarks>
        /// Nodes in a ParseTree correspond to captured tokens. The parent-child relationship
        /// between nodes reflects token embedding.
        /// </remarks>
        public class ParseTree
        {
            public Node Root { get; set; }
            public const string KeyRoot = "0";

            public class Node : IOperatorCapture, IOperandCapture
            {
                public string CaptureId { get; set; }
                public string Value { get; set; }
                public int Begin { get; set; }
                public int End { get; set; }
                public int GroupIdx { get; set; }
                public int CaptureIdx { get; set; }

                class NodeComparer : IComparer<Node>
                {
                    public int Compare(Node x, Node y)
                    {
                        return Comparer<int>.Default.Compare(x.Begin, y.Begin);
                    }
                }

                static readonly NodeComparer _Comparer = new NodeComparer();
                public static IComparer<Node> Comparer { get { return _Comparer; } }

                public Token Token { get; set; }
                public string TokenId { get { return Token.Id; } }

                public object Production { get; set; }

                public Node Parent { get; set; }

                readonly SortedList<int, Node> _ChildNodes = new SortedList<int, Node>();
                public SortedList<int, Node> ChildNodes { get { return _ChildNodes; } }

                readonly ProductionObjects _ChildProductions = new ProductionObjects();
                public ProductionObjects ChildProductions { get { return _ChildProductions; } }

                public Queue<Node> TokenStream { get; set; }
                public Stack<Node> OperatorStack { get; set; }
                public Stack<Node> OperandStack { get; set; }

                IProductionRule _Rule = null;
                public IProductionRule Rule
                {
                    get
                    {
                        if (_Rule == null)
                            _Rule = Token.SelectRule(this);
                        return _Rule;
                    }
                }

                public string Key
                {
                    get
                    {
                        if (CaptureId == KeyRoot)
                            return KeyRoot;
                        return string.Format("{0}:{1}:{2}", CaptureId, Begin, End);
                    }
                }

                public override string ToString()
                {
                    return string.Format("{0}[{1}]", TokenId, Value);
                }

                public static implicit operator ParseTree(Node node)
                {
                    return new ParseTree { Root = node };
                }

                int SiblingIdx
                {
                    get
                    {
                        if (Parent == null)
                            return 0;
                        return Parent.ChildNodes.IndexOfKey(Begin);
                    }
                }

                int SiblingCount
                {
                    get
                    {
                        if (Parent == null)
                            return 1;
                        return Parent.ChildNodes.Count;
                    }
                }

                public bool IsFirst { get { return SiblingIdx == 0; } }

                public bool IsLast { get { return SiblingIdx == SiblingCount - 1; } }

                public IEnumerable<ITokenCapture> LookAhead(params TokenGroup[] ids)
                {
                    if (Parent == null)
                        return Empty<ITokenCapture>();
                    var lookAhead = Parent.ChildNodes.Values
                        .Skip(SiblingIdx + 1);
                    if (ids.Any())
                        lookAhead = lookAhead.Where(x => ids.Any(g => g.Contains(x.TokenId)));
                    return lookAhead.Cast<ITokenCapture>().Concat(Items(EndOfList));
                }

                public IEnumerable<ITokenCapture> LookBehind(params TokenGroup[] ids)
                {
                    if (Parent == null)
                        return Empty<ITokenCapture>();
                    var lookBehind = Parent.ChildNodes.Values
                        .Take(SiblingIdx)
                        .Reverse();
                    if (ids.Any())
                        lookBehind = lookBehind.Where(x => ids.Any(g => g.Contains(x.TokenId)));
                    return lookBehind.Cast<ITokenCapture>().Concat(Items(EndOfList));
                }

                public bool Is(params TokenGroup[] tokenIds)
                {
                    return tokenIds.Any(g => g.Contains(TokenId));
                }

                public bool IsNot(params TokenGroup[] tokenIds)
                {
                    return !tokenIds.Any(g => g.Contains(TokenId));
                }

                public IOperandCapture Operand
                {
                    get
                    {
                        if (Parent == null)
                            return EndOfList;
                        if (Parent.OperandStack == null)
                            return EndOfList;
                        if (!Parent.OperandStack.Any())
                            return EndOfList;
                        return Parent.OperandStack.Peek();
                    }
                }

                public IOperandCapture LeftOperand
                {
                    get
                    {
                        if (Parent == null)
                            return EndOfList;
                        if (Parent.OperandStack == null)
                            return EndOfList;
                        if (Parent.OperandStack.Count() < 2)
                            return EndOfList;
                        return Parent.OperandStack.Skip(1).First();
                    }
                }

                public IOperandCapture RightOperand
                {
                    get
                    {
                        if (Parent == null)
                            return EndOfList;
                        if (Parent.OperandStack == null)
                            return EndOfList;
                        if (Parent.OperandStack.Count() < 2)
                            return EndOfList;
                        return Parent.OperandStack.Peek();
                    }
                }

                public bool HasOperand
                {
                    get { return Operand != EndOfList; }
                }

                public bool HasLeftOperand
                {
                    get { return LeftOperand != EndOfList; }
                }

                public bool HasRightOperand
                {
                    get { return RightOperand != EndOfList; }
                }
            }
        }
    }
}
