// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

/* https://bugreports.qt.io/browse/QTVSADDINBUG-1289 */

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_QTVSADDINBUG_1289
    {
        [TestMethod]
        public void QtVsAddinBug1289()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");

            string qtInstallDir = MsBuild.GetProperty(
                temp.ProjectDir,
                temp.ProjectPath,
                "QtInstallDir",
                "-p:Platform=x64",
                "-p:Configuration=Debug");

            // Qt sets QTDIR in LocalDebuggerEnvironment; the debug launch hook
            // converts it to a PATH entry at runtime. This avoids conflicts with
            // other components (e.g. ASAN) that also set PATH=.
            string lde = MsBuild.GetProperty(
                temp.ProjectDir,
                temp.ProjectPath,
                "LocalDebuggerEnvironment",
                "-p:Platform=x64",
                "-p:Configuration=Debug");

            Assert.Contains("QTDIR=", lde,
                $"Expected QTDIR in LocalDebuggerEnvironment.\n{lde}");
            Assert.Contains(qtInstallDir, lde,
                $"Expected QtInstallDir in LocalDebuggerEnvironment.\n{lde}");
            int pathEntryCount = Regex.Matches(lde, "(?mi)^PATH=").Count;
            Assert.AreEqual(0, pathEntryCount,
                $"Expected no PATH= entry (launch hook handles it).\n{lde}");

            // With ASAN enabled, ASAN adds its own PATH= and ASAN_SYMBOLIZER_PATH.
            // Qt's QTDIR must not conflict with these.
            string ldeAsan = MsBuild.GetProperty(
                temp.ProjectDir,
                temp.ProjectPath,
                "LocalDebuggerEnvironment",
                "-p:Platform=x64",
                "-p:Configuration=Debug",
                "-p:EnableASAN=true");

            Assert.Contains("QTDIR=", ldeAsan,
                $"Expected QTDIR in LocalDebuggerEnvironment with ASAN.\n{ldeAsan}");
            Assert.Contains(qtInstallDir, ldeAsan,
                $"Expected QtInstallDir in LocalDebuggerEnvironment with ASAN.\n{ldeAsan}");
            int pathEntryCountAsan = Regex.Matches(ldeAsan, "(?mi)^PATH=").Count;
            Assert.AreEqual(1, pathEntryCountAsan,
                $"Expected exactly one PATH= entry from ASAN.\n{ldeAsan}");
            Assert.Contains("ASAN_SYMBOLIZER_PATH=", ldeAsan,
                $"Expected ASAN_SYMBOLIZER_PATH in LocalDebuggerEnvironment.\n{ldeAsan}");
        }
    }
}
