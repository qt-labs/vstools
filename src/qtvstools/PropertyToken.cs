/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace QtVsTools
{
    public class PropertyToken : Token
    {
        //Classification used th token
        protected IClassificationType classificationType;
        //Tracked span of token
        protected ITrackingSpan trackingSpan;
        //Version of text when Tracking was created
        protected ITextVersion textVersion;

        public PropertyToken()
            : base(TokenType.Property)
        {
        }

        public PropertyToken(IClassificationType type, ITrackingSpan span, ITextVersion version)
            : base(TokenType.Property)
        {
            classificationType = type;
            trackingSpan = span;
            textVersion = version;
        }

        public IClassificationType Classification()
        {
            return classificationType;
        }
        public ITrackingSpan Tracking()
        {
            return trackingSpan;
        }
        public ITextVersion Version()
        {
            return textVersion;
        }
    }
}
