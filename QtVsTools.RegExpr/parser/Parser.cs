/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace QtVsTools.SyntaxAnalysis
{
    public abstract partial class RegExpr
    {
        ////////////////////////////////////////////////////////////////////////////////////////////
        ///
        /// RegExpr.Parser
        ///
        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Rendering of <see cref="RegExpr"/>
        /// </summary>
        public partial class Parser
        {
            Renderer Renderer { get; }
            Pattern Pattern { get; set; }
            public Regex Regex { get; private set; }

            internal Parser(RegExpr expr, RegExpr defaultTokenWs = null)
            {
                Renderer = new Renderer();
                Refresh(expr, defaultTokenWs);
            }

            /// <summary>
            /// Parse input text and return productions.
            /// </summary>
            /// <remarks>
            /// The parsing procedure will first calculate the parse tree corresponding to the input
            /// text, given the token data captured. The parse tree is then used to generate all
            /// productions, according to the production rules defined for each token.
            /// (see also <see cref="GetProductions(ParseTreeNode)"/>)
            /// </remarks>
            /// <param name="text">Text to be parsed.</param>
            /// <returns>Productions by token id</returns>
            public ProductionObjects Parse(string text)
            {
                var parseTree = GetParseTree(text);
                return GetProductionObjects(parseTree);
            }

            public void Refresh(RegExpr expr, RegExpr defaultTokenWs = null)
            {
                // Render Regex string
                Pattern = Renderer.RenderPattern(expr, defaultTokenWs);

                // Compile Regex
                Regex = new Regex(Pattern.ExprRender, RegexOptions.Multiline);
            }

            /// <summary>
            /// Parse input text using Regex and generate corresponding parse tree.
            /// </summary>
            /// <param name="text">Text to be parsed</param>
            /// <returns>Parse tree</returns>
            ParseTree GetParseTree(string text)
            {
                // Match regex pattern
                var nodes = new List<ParseTree.Node>();
                var matches = Regex.Matches(text);
                if (matches.Count == 0)
                    throw new ParseErrorException();
                foreach (Match match in matches) {
                    if (!match.Success || match.Length == 0) {
                        if (nodes.Any())
                            continue;
                        throw new ParseErrorException();
                    }

                    // Flat list of parse-tree nodes, from Regex captures
                    var matchNodes = match.Groups.Cast<Group>()
                        .SelectMany((group, groupIdx) => group.Captures.Cast<Capture>()
                            .Where(capture => !string.IsNullOrEmpty(capture.Value))
                            .Select((capture, captureIdx) => new ParseTree.Node
                            {
                                CaptureId = Regex.GroupNameFromNumber(groupIdx),
                                Token = Pattern.Tokens[Regex.GroupNameFromNumber(groupIdx)],
                                Value = capture.Value,
                                Begin = capture.Index,
                                End = capture.Index + capture.Length,
                                GroupIdx = groupIdx,
                                CaptureIdx = captureIdx,
                            }))
                        .OrderBy(c => c.Begin)
                        .ToList();
                    nodes.AddRange(matchNodes);
                }

                // Node list partitioned by token
                var nodesByToken = nodes
                    .GroupBy(node => node.Token)
                    .ToDictionary(g => g.Key, g => g.ToArray());

                foreach (var node in nodes.Where(n => n.Token != Pattern.Root)) {
                    // Get nodes captured by parent token
                    if (!node.Token.Parents.TryGetValue(node.CaptureId, out Token parentToken))
                        throw new ParseErrorException("Unknown capture ID");
                    if (!nodesByToken.TryGetValue(parentToken, out ParseTree.Node[] parentNodes))
                        throw new ParseErrorException("Missing parent nodes");
                    // Find parent node
                    int idx = Array.BinarySearch(parentNodes, node, ParseTree.Node.Comparer);
                    if (idx < 0) {
                        idx = (~idx) - 1;
                        if (idx < 0)
                            throw new ParseErrorException("Parent node not found");
                    }
                    // Attach to parent node
                    (node.Parent = parentNodes[idx]).ChildNodes.Add(node.Begin, node);
                }

                var topNodes = nodesByToken[Pattern.Root];
                if (topNodes.Length == 1)
                    return topNodes[0];

                var root = new ParseTree.Node()
                {
                    CaptureId = string.Empty,
                    Token = null,
                    Value = text,
                    Begin = 0,
                    End = text.Length,
                    GroupIdx = -1,
                    CaptureIdx = -1,
                };
                foreach (var node in nodesByToken[Pattern.Root])
                    (node.Parent = root).ChildNodes.Add(node.Begin, node);
                return root;
            }
        }

        public class ParseErrorException : RegExprException
        {
            public ParseErrorException(string message = null) : base(message) { }
        }
    }
}
