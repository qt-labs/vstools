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
    /// Alternating composition
    /// </summary>
    public partial class RegExprChoice : RegExpr
    {
        public IEnumerable<RegExpr> Exprs { get; set; }

        protected override IEnumerable<RegExpr> OnRender(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRender(defaultTokenWs, parent, pattern, ref mode);
            if (!(parent is Token))
                pattern.Append("(?:");
            pattern.Append("(?:");
            return Exprs;
        }

        protected override void OnRenderNext(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRenderNext(defaultTokenWs, parent, pattern, ref mode);
            pattern.Append(")|(?:");
        }

        protected override void OnRenderEnd(RegExpr defaultTokenWs, RegExpr parent,
            StringBuilder pattern, ref RenderMode mode)
        {
            base.OnRenderEnd(defaultTokenWs, parent, pattern, ref mode);
            pattern.Append(")");
            if (!(parent is Token))
                pattern.Append(")");
        }
    }

    public abstract partial class RegExpr
    {
        public static RegExpr Choice(params RegExpr[] rxs)
        {
            return new RegExprChoice
            {
                Exprs = rxs.SelectMany(rx => rx is RegExprChoice
                    ? ((RegExprChoice)rx).Exprs
                    : Items(rx))
            };

        }

        public static RegExpr operator |(RegExpr rx1, RegExpr rx2)
        {
            return Choice(rx1, rx2);
        }
    }
}
