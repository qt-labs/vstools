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
        public delegate void EventHandler(string result);
        public event EventHandler ReadyEvent;
        private string queryResult;

        public QMakeQuery(VersionInformation vi)
            : base(null, string.Empty, false, vi)
        {
            qtVersionInformation = vi;
        }

        public string query(string property)
        {
            ReadyEvent += resultObtained;
            var qmakeThread = new Thread(RunQMakeQuery);
            qmakeThread.Start(property);
            qmakeThread.Join();
            return queryResult;
        }

        private void resultObtained(string result)
        {
            queryResult = result;
        }

        private void RunQMakeQuery(object property)
        {
            if (property == null)
                return;

            var propertyString = property.ToString();
            var result = string.Empty;

            var qmakePath = Path.Combine(qtVersionInformation.qtDir, "bin", "qmake.exe");
            if (!File.Exists(qmakePath))
                qmakePath = Path.Combine(qtVersionInformation.qtDir, "qmake.exe");
            if (!File.Exists(qmakePath)) {
                qmakeProcess = null;
                errorValue = -1;
                InvokeExternalTarget(ReadyEvent, result);
                return;
            }

            qmakeProcess = CreateQmakeProcess("-query " + propertyString.Trim(), qmakePath, qtVersionInformation.qtDir);
            try {
                if (qmakeProcess.Start()) {
                    errOutput = new StringBuilder();
                    errOutputLines = 0;
                    stdOutput = new StringBuilder();
                    stdOutputLines = 0;
                    var errorThread = new Thread(ReadStandardError);
                    var outputThread = new Thread(ReadStandardOutput);
                    errorThread.Start();
                    outputThread.Start();

                    qmakeProcess.WaitForExit();

                    errorThread.Join();
                    outputThread.Join();

                    errorValue = qmakeProcess.ExitCode;

                    if (stdOutputLines > 0) {
                        result = stdOutput.ToString();
                        var dashIndex = result.IndexOf('-');
                        if (dashIndex == -1) {
                            errorValue = -1;
                            result = string.Empty;
                        } else {
                            result = result.Substring(dashIndex + 1).Trim();
                        }
                    }
                }
                qmakeProcess.Close();
            } catch (Exception) {
                qmakeProcess = null;
                errorValue = -1;
            } finally {
                InvokeExternalTarget(ReadyEvent, result);
            }
        }
    }
}
