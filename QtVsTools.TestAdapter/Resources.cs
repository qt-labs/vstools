/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
**************************************************************************************************/

using System;
using System.Collections.Generic;

namespace QtVsTools.TestAdapter
{
    using static QtVsTools.Core.Resources;

    public static class Resources
    {
        internal const string FileExtension = ".exe";
        internal const string ExecutorUriString = "executor://QtTestExecutor/v1";
        internal static readonly Uri ExecutorUri = new(ExecutorUriString);

        internal const string SettingsName = "QtTest";
        public const string GlobalSettingsName = "QtTestGlobal";
        internal const string TestSettingsPath = CurrentRootPath + @"\TestAdapter";

        internal static readonly HashSet<string> SupportedOutputFormats = new()
        {
            "txt",
            "csv",
            "junitxml",
            "xml",
            "lightxml",
            "teamcity",
            "tap"
        };
    }
}
