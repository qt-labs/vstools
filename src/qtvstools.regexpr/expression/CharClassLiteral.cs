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
    /// CharClassLiteral ( -> CharClassSet.Element -> CharClass -> RegExpr )
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Elementary character class defined by one or more allowed characters
    /// </summary>
    ///
    public class CharClassLiteral : CharClassSet.Element
    {
        public string LiteralChars { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode);
            if ((parent == null || !(parent is CharClass)) && NeedsGroup(LiteralChars))
                pattern.AppendFormat("[{0}]", LiteralChars);
            else
                pattern.Append(LiteralChars);
            return null;
        }
    }

    public abstract partial class CharClass : RegExpr
    {
        public static CharClassLiteral CharLiteral(string s)
        {
            return new CharClassLiteral
            {
                LiteralChars = Escape(s)
            };
        }

        public static CharClassLiteral CharRawLiteral(string s)
        {
            return new CharClassLiteral
            {
                LiteralChars = s
            };
        }

        public static CharClassLiteral CharLiteral(char c)
        {
            return CharLiteral(c.ToString());
        }

        public partial class CharExprBuilder
        {
            public CharClassLiteral this[string s] { get { return CharLiteral(s); } }
            public CharClassLiteral this[char c] { get { return CharLiteral(c); } }
        }
    }
}
