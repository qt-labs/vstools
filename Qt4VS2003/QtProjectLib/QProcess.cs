/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Add-in.
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
using System.Diagnostics;
using System.Collections;

namespace Digia.Qt5ProjectLib
{
    public class QProcess : Process
    {
        private Hashtable errorCodes = null;

        public QProcess()
        {
        }

        public Hashtable ErrorCodes
        {
            get { return errorCodes; }
            internal set { errorCodes = value; }
        }

        public string errorString(int errorCode)
        {
            if (errorCodes != null && errorCodes.Contains(errorCode)) 
            {
                string[] msgs = (string[])errorCodes[errorCode];
                return msgs[0];
            }
            return SR.GetString("QProcess_UnspecifiedError");
        }
        
        public string solutionString(int errorCode)
        {
            if (errorCodes != null && errorCodes.Contains(errorCode)) 
            {
                string[] msgs = (string[])errorCodes[errorCode];
                return msgs[1];
            }
            return null;
        }
    }
}
