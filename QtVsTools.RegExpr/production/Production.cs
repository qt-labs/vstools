/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QtVsTools.SyntaxAnalysis
{
    public abstract partial class RegExpr
    {
        public partial class Parser
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///
            /// RegExpr.Parser.GetProductionObjects()
            ///
            ////////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// Extract productions from a parse tree.
            /// </summary>
            /// <param name="root">
            /// Root node of parse tree, obtained from parsing an input text with a RegExpr
            /// </param>
            /// <returns>Productions by token id</returns>
            ProductionObjects GetProductionObjects(ParseTree parseTree)
            {
                var outputProductions = new ProductionObjects();

                var stack = new Stack<ParseTree.Node>();
                stack.Push(parseTree.Root);
                while (stack.Any()) {
                    var node = stack.Pop();

                    // Depth-first traversal
                    if (node.TokenStream == null) {
                        node.TokenStream = new Queue<ParseTree.Node>(node.ChildNodes.Values);
                        node.OperandStack = new Stack<ParseTree.Node>();
                        node.OperatorStack = new Stack<ParseTree.Node>();
                        stack.Push(node);
                        continue;
                    }
                    if (node.TokenStream.Any()) {
                        var nextNode = node.TokenStream.Dequeue();
                        stack.Push(node);
                        stack.Push(nextNode);
                        continue;
                    }

                    if (node.Parent == null)
                        continue;

                    var operatorStack = node.Parent.OperatorStack;
                    var operandStack = node.Parent.OperandStack;
                    var rule = node.Rule;
                    if (rule == null) {
                        // Default token (without rule definitions)
                        // just use captured value as production
                        node.Production = node.Value;
                        operandStack.Push(node);
                    } else if (rule.Delimiters != Delimiter.None) {
                        // Delimiter token
                        if (rule.Delimiters == Delimiter.Left) {
                            // if left delim, push to operator stack
                            operatorStack.Push(node);
                        } else {
                            // if right delim, unwind operator stack until left delim
                            UnwindOperatorStack(HaltUnwind.AtLeftDelimiter,
                                operatorStack, operandStack);
                            // set left delim as left operand, delimited expr as right operand
                            operandStack.ReverseTop();
                            // execute delimiter rule
                            node.Production = rule.Execute(node);
                            operandStack.Push(node);
                        }
                    } else if (rule.Operands != Operand.None) {
                        // Operator token
                        // unwind operator stack until lower priority operator or empty
                        UnwindOperatorStack(HaltUnwind.AtLowerPriority,
                            operatorStack, operandStack, rule.Priority);

                        // if operator needs left operand but none is available, error out
                        if (rule.Operands.HasFlag(Operand.Left) && !operandStack.Any())
                            throw new ParseErrorException();

                        if (rule.Operands.HasFlag(Operand.Right)) {
                            // if needs right operand, push to operator stack
                            operatorStack.Push(node);
                        } else {
                            // if left operand only, execute rule immediately
                            node.Production = rule.Execute(node);
                            operandStack.Push(node);
                        }
                    } else {
                        // Captured value or embedded captures ("nullary operator")
                        // execute rule immediately
                        node.Production = rule.Execute(node);
                        operandStack.Push(node);
                    }

                    if (node.IsLast) {
                        // Last token
                        // unwind operator stack until empty
                        UnwindOperatorStack(HaltUnwind.WhenEmpty,
                            operatorStack, operandStack);

                        // get output from operand stack
                        foreach (var operand in operandStack.Reverse()) {

                            // check if it's a dangling left delimiter
                            if (operand.Rule is {Delimiters: Delimiter.Left})
                                throw new ParseErrorException();

                            // add production to parent context
                            node.Parent.ChildProductions.Add(operand.TokenId, operand.Production);

                            // add production to output list
                            outputProductions.Add(operand.TokenId, operand.Production);
                        }
                        operandStack.Clear();
                    }
                }

                return outputProductions;
            }

            enum HaltUnwind
            {
                WhenEmpty,
                AtLeftDelimiter,
                AtLowerPriority
            }

            void UnwindOperatorStack(
                HaltUnwind haltingCondition,
                Stack<ParseTree.Node> operatorStack,
                Stack<ParseTree.Node> operandStack,
                int priority = int.MinValue)
            {
                while (operatorStack.Any()) {
                    var node = operatorStack.Pop();
                    Debug.Assert(node != null);

                    var rule = node.Rule;
                    Debug.Assert(rule != null);

                    if (haltingCondition == HaltUnwind.AtLeftDelimiter
                        && rule.Delimiters == Delimiter.Left
                    ) {
                        // Halting stack unwind: left delimiter found
                        // check if an operand (i.e. delimited expression) is available
                        if (!operandStack.Any())
                            throw new ParseErrorException();

                        // execute left delimiter rule
                        node.Production = rule.Execute(node);

                        // add to operands (will be picked up by right delimiter rule)
                        operandStack.Push(node);
                        return;
                    }

                    if (haltingCondition == HaltUnwind.AtLowerPriority
                        && rule.Priority < priority
                    ) {
                        // Halting stack unwind: lower priority operator found
                        // push operator back into stack
                        operatorStack.Push(node);
                        return;
                    }

                    // still haven't found what we're looking for; continue stack unwind
                    node.Production = rule.Execute(node);
                    operandStack.Push(node);
                }

                // error-out if didn't find left delimiter
                if (haltingCondition == HaltUnwind.AtLeftDelimiter)
                    throw new ParseErrorException();
            }
        }

        /// <summary>
        /// Collection of production objects, grouped by token ID
        /// </summary>
        public partial class ProductionObjects : IEnumerable<KeyValuePair<string, object>>
        {
            List<KeyValuePair<string, object>> Productions { get; }
            Dictionary<string, List<object>> ProductionsByTokenId { get; }

            public ProductionObjects()
            {
                Productions = new List<KeyValuePair<string, object>>();
                ProductionsByTokenId = new Dictionary<string, List<object>>();
            }

            public void Add(string tokenId, object prodObj)
            {
                Productions.Add(new KeyValuePair<string, object>(tokenId, prodObj));
                if (!ProductionsByTokenId.TryGetValue(tokenId, out List<object> prodObjs))
                    ProductionsByTokenId.Add(tokenId, prodObjs = new List<object>());
                prodObjs.Add(prodObj);
            }

            public IEnumerable<T> GetValues<T>(string tokenId)
            {
                if (string.IsNullOrEmpty(tokenId))
                    return Empty<T>();

                if (!ProductionsByTokenId.TryGetValue(tokenId, out List<object> tokenProds))
                    return Empty<T>();

                return tokenProds
                    .Where(x => x is T)
                    .Select(x => (T)x);
            }

            public IEnumerable<T> GetValues<T>(Enum tokenId)
            {
                return GetValues<T>(tokenId.ToString());
            }

            public IEnumerable<T> GetValues<T>(Token token)
            {
                return GetValues<T>(token.Id);
            }

            public IEnumerable<T> GetValues<T>(ProductionRule<T> production)
            {
                return GetValues<T>(production.Token.Id);
            }

            public IEnumerable<object> GetValues(string tokenId)
            {
                return GetValues<object>(tokenId);
            }

            public IEnumerable<object> GetValues(Enum tokenId)
            {
                return GetValues<object>(tokenId.ToString());
            }

            public IEnumerable<object> GetValues(Token token)
            {
                return GetValues<object>(token.Id);
            }

            public IEnumerable<object> GetValues(IProductionRule production)
            {
                return GetValues<object>(production.Token.Id);
            }

            public object this[string tokenId, int index = 0] =>
                GetValues(tokenId).ElementAtOrDefault(index);

            public object this[Enum tokenId, int index = 0] => this[tokenId.ToString(), index];

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<string, object>>)Productions).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<string, object>>)Productions).GetEnumerator();
            }
        }
    }
}
