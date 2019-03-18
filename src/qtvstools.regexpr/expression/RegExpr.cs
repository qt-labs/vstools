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
            StringBuilder pattern, ref RenderMode mode)
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
            StringBuilder pattern, ref RenderMode mode)
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
            StringBuilder pattern, ref RenderMode mode)
        { }

        public class Exception : System.Exception
        {
            public Exception(string message = null) : base(message) { }
        }
    }
}
