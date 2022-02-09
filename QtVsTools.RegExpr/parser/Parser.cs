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
                var match = Regex.Match(text);
                if (!match.Success || match.Length == 0)
                    throw new ParseErrorException();

                // Flat list of parse-tree nodes, from Regex captures
                var nodes = match.Groups.Cast<Group>()
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

                // Return parse tree root
                return nodesByToken[Pattern.Root].FirstOrDefault();
            }
        }

        public class ParseErrorException : RegExprException
        {
            public ParseErrorException(string message = null) : base(message) { }
        }
    }
}
