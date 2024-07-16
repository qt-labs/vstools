/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace QtVsTools.TestAdapter
{
    internal class QtTestResult
    {
        internal class TestFunction
        {
            internal string Name { get; set; }
            internal double Duration { get; set; }
            internal string IncidentType { get; set; }
            internal string IncidentFile { get; set; }
            internal int IncidentLine { get; set; }
            internal string IncidentDescription { get; set; }
        }

        internal string Name { get; set; }
        internal string QtVersion { get; set; }
        internal string QtBuild { get; set; }
        internal string QTestVersion { get; set; }
        internal Dictionary<string, TestFunction> TestFunctions { get; set; } = new();
        internal double TotalDuration { get; set; }

        internal static TestOutcome MapType(string incidentType)
        {
            return incidentType switch
            {
                "fail" or "xfail" => TestOutcome.Failed,
                "pass" or "xpass" => TestOutcome.Passed,
                "skip" or "todo" or "warn" => TestOutcome.Skipped,
                _ => TestOutcome.None
            };
        }
    }
}
