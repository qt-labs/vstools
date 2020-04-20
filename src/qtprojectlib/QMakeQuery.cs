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

using System;
using System.IO;
using System.Text;
using System.Threading;

namespace QtProjectLib
{
    class QMakeQuery : QMake
    {
        public QMakeQuery(VersionInformation vi) : base(vi)
        { }

        StringBuilder stdOutput;
        protected override void OutMsg(string msg)
        {
            if (stdOutput != null && !string.IsNullOrEmpty(msg))
                stdOutput.Append(msg);
        }

        public string QueryValue(string property)
        {
            string result = string.Empty;
            stdOutput = new StringBuilder();
            Query = property;
            if (Run() == 0 && stdOutput.Length > 0)
                return stdOutput.ToString();
            else
                return string.Empty;
        }
    }
}
