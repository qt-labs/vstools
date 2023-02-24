/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
                public static IComparer<Node> Comparer => _Comparer;

                public Token Token { get; set; }
                public string TokenId => Token.Id;

                public object Production { get; set; }

                public Node Parent { get; set; }

                public SortedList<int, Node> ChildNodes { get; } = new SortedList<int, Node>();

                public ProductionObjects ChildProductions { get; } = new ProductionObjects();

                public Queue<Node> TokenStream { get; set; }
                public Stack<Node> OperatorStack { get; set; }
                public Stack<Node> OperandStack { get; set; }

                IProductionRule _Rule = null;
                public IProductionRule Rule
                {
                    get { return _Rule ??= Token.SelectRule(this); }
                }

                public string Key
                {
                    get => CaptureId == KeyRoot ? KeyRoot : $"{CaptureId}:{Begin}:{End}";
                }

                public override string ToString()
                {
                    return $"{TokenId}[{Value}]";
                }

                public static implicit operator ParseTree(Node node)
                {
                    return new ParseTree { Root = node };
                }

                int SiblingIdx
                {
                    get => Parent?.ChildNodes.IndexOfKey(Begin) ?? 0;
                }

                int SiblingCount
                {
                    get => Parent?.ChildNodes.Count ?? 1;
                }

                public bool IsFirst => SiblingIdx == 0;

                public bool IsLast => SiblingIdx == SiblingCount - 1;

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
                        if (Parent is not { OperandStack: {} })
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
                        if (Parent is not { OperandStack: {} })
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
                        if (Parent is not { OperandStack: {} })
                            return EndOfList;
                        if (Parent.OperandStack.Count() < 2)
                            return EndOfList;
                        return Parent.OperandStack.Peek();
                    }
                }

                public bool HasOperand => Operand != EndOfList;

                public bool HasLeftOperand => LeftOperand != EndOfList;

                public bool HasRightOperand => RightOperand != EndOfList;
            }
        }
    }
}
