/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Diagnostics;
using System.Text;

namespace QtVsTools.Core
{
    public class QMakeQuery : QtBuildToolQuery
    {
        private class QMakeProcess : QMake, IQueryProcess
        {
            public QMakeProcess(string qtDir)
                : base(qtDir)
            {
                Query = " ";
            }

            protected override void OutMsg(Process process, string msg)
            {
                StdOutput.AppendLine(msg);
            }

            protected override void InfoStart(Process process)
            {
                InfoMsg(process, "Querying persistent properties");
                base.InfoStart(process);
            }

            private StringBuilder stdOutput;
            public StringBuilder StdOutput => stdOutput ??= new StringBuilder();
        }

        protected override IQueryProcess CreateQueryProcess(string qtDir) => new QMakeProcess(qtDir);
    }
}
