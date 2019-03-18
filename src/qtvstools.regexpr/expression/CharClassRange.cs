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
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    ////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    /// CharClassRange ( -> CharClassSet.Element -> CharClass -> RegExpr )
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Represents an elementary character class defined by a range of characters
    /// </summary>
    ///
    public partial class CharClassRange : CharClassSet.Element
    {
        public CharClassLiteral LowerBound { get; set; }
        public CharClassLiteral UpperBound { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode);

            if (parent == null || !(parent is CharClass))
                pattern.Append("[");

            return Items(LowerBound, UpperBound);
        }

        protected override void OnRenderNext(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRenderNext(defaultTokenWs, parent, pattern, ref mode);
            pattern.Append("-");
        }

        protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode);

            if (parent == null || !(parent is CharClass))
                pattern.Append("]");
        }
    }

    public abstract partial class CharClass : RegExpr
    {
        public static CharClassRange CharRange(string lBound, string uBound)
        {
            return CharRange(CharLiteral(lBound), CharLiteral(uBound));
        }

        public static CharClassRange CharRange(CharClassLiteral lBound, CharClassLiteral uBound)
        {
            return new CharClassRange
            {
                LowerBound = lBound,
                UpperBound = uBound
            };
        }

        public partial class CharExprBuilder
        {
            public CharClassRange this[char lBound, char uBound]
            { get { return CharRange(lBound.ToString(), uBound.ToString()); } }
        }
    }
}
