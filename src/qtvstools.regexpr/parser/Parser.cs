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
using System.Diagnostics;
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
            Renderer Renderer { get; set; }
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

                // Flat list of captures (parse-tree nodes)
                var captures = match.Groups.Cast<Group>()
                    .SelectMany((g, gIdx) => g.Captures.Cast<Capture>()
                        .Where(c => !string.IsNullOrEmpty(c.Value))
                        .Select((c, cIdx) => new ParseTree.Node
                        {
                            CaptureId = Regex.GroupNameFromNumber(gIdx),
                            Token = Pattern.Tokens[ Regex.GroupNameFromNumber(gIdx)],
                            Value = c.Value,
                            Begin = c.Index,
                            End = c.Index + c.Length,
                            GroupIdx = gIdx,
                            CaptureIdx = cIdx,
                        }))
                    .OrderByDescending(c => c.Begin)
                    .ThenBy(c => c.End)
                    .ThenByDescending(c => c.GroupIdx)
                    .ThenByDescending(c => c.CaptureIdx);

                // Capture index
                var capture = captures
                    .GroupBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.First());

                // Parent(x) ::= smallest capture y, such that x is contained in y
                var subCaptures = captures
                    .Select((x, xIdx) => new
                    {
                        IdxSelf = xIdx,
                        KeySelf = x.Key,
                        KeyParent = captures.Skip(xIdx + 1)
                            .SkipWhile(y => y.End < x.End)
                            .Select(y => y.Key)
                            .FirstOrDefault()
                    })
                    .Where(x => !string.IsNullOrEmpty(x.KeyParent));

                // Link x <--> Parent(x)
                foreach (var subCapture in subCaptures) {
                    var self = capture[subCapture.KeySelf];
                    var parent = capture[subCapture.KeyParent];
                    self.OrderKey = -subCapture.IdxSelf;
                    (self.Parent = parent).ChildNodes.Add(self.OrderKey, self);
                }

                // Return parse tree root
                return capture[ParseTree.KeyRoot];
            }
        }

        public class ParseErrorException : RegExpr.Exception
        {
            public ParseErrorException(string message = null) : base(message) { }
        }
    }
}
