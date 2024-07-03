/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Diagnostics;

namespace QtVsTools.Core
{
    public class QtPathsQuery : QtBuildToolQuery
    {
        private class QPathsProcess : QtPaths
        {
            public QPathsProcess(string qtDir)
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
        }

        protected override IQueryProcess CreateQueryProcess(string qtDir) => new QPathsProcess(qtDir);
    }
}
