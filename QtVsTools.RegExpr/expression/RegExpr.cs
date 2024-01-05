/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    /// RegExpr
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Abstract representation of a regular expression.
    /// </summary>
    /// <remarks>
    /// RegExpr objects can be created and combined using C# expressions, and then rendered into a
    /// regular expression pattern and parser.
    /// </remarks>
    public abstract partial class RegExpr
    {
        /// <summary>
        /// Render the RegExpr into a corresponding pattern and parser.
        /// </summary>
        /// <param name="defaultTokenWs">Default token whitespace</param>
        /// <returns>
        /// <see cref="Parser"/> object that can process strings according to the pattern rendered.
        /// </returns>
        public Parser Render(RegExpr defaultTokenWs = null)
        {
            return new Parser(this, defaultTokenWs);
        }

        [Flags]
        protected enum RenderMode { Default = 0, Assert = 1 }

        /// <summary>
        /// Event triggered when starting the rendering process for this RegExpr.
        /// </summary>
        /// <param name="defaultTokenWs">Default token whitespace</param>
        /// <param name="parent">Parent expression</param>
        /// <param name="pattern">Rendered pattern</param>
        /// <param name="mode">Rendering mode</param>
        ///
        /// <returns>Sub-expressions to add to the rendering process.</returns>
        /// <remarks>
        /// RegExpr sub-classes will re-implement OnRenderBegin, OnRenderNext and OnRenderEnd to
        /// define their specific rendering process. Regular expression strings are appended to
        /// <paramref name="pattern"/>. When rendering a named capture group, a mapping to a
        /// production object can be defined and added to <paramref name="prodMap"/>. The production
        /// object will be responsible for translating the captured values into instances of
        /// external, application specific classes.
        /// </remarks>
        protected virtual IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        { return null; }

        /// <summary>
        /// Event triggered when rendering the next sub-expression.
        /// </summary>
        /// <param name="defaultTokenWs">Default token whitespace</param>
        /// <param name="parent">Parent expression</param>
        /// <param name="pattern">Rendered pattern</param>
        /// <param name="mode">Rendering mode</param>
        ///
        protected virtual void OnRenderNext(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        { }

        /// <summary>
        /// Event triggered after all sub-expressions have been rendered.
        /// </summary>
        /// <param name="defaultTokenWs">Default token whitespace</param>
        /// <param name="parent">Parent expression</param>
        /// <param name="pattern">Rendered pattern</param>
        /// <param name="mode">Rendering mode</param>
        ///
        protected virtual void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        { }

        public class RegExprException : Exception
        {
            public RegExprException(string message = null) : base(message) { }
        }
    }
}
