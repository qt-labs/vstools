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
using System.Text;

namespace QtVsTools.SyntaxAnalysis
{
    /// <summary>
    /// Sequential composition
    /// </summary>
    public partial class RegExprSequence : RegExpr
    {
        public IEnumerable<RegExpr> Exprs { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode, Stack<Token> tokenStack)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode, tokenStack);
            return Exprs;
        }
    }

    public abstract partial class RegExpr
    {
        public static RegExprSequence Concat(params RegExpr[] rxs)
        {
            return new RegExprSequence
            {
                Exprs = rxs.SelectMany(rx => rx is RegExprSequence
                    ? ((RegExprSequence)rx).Exprs
                    : Items(rx))
            };
        }

        public static RegExprSequence operator &(RegExpr rx1, RegExpr rx2)
        {
            return Concat(rx1, rx2);
        }
    }
}
