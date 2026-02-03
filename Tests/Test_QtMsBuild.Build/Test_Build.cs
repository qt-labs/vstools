// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_Build
    {
        [TestMethod]
        public void Build()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var binlog = Path.Combine(temp.ProjectDir, "msbuild.binlog");
            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:Build",
                "-p:Platform=x64",
                "-p:Configuration=Debug",
                $"-bl:{binlog}");

            Assert.IsTrue(buildOk);

            var intDir = MsBuild.GetProperty(
                temp.ProjectDir,
                temp.ProjectPath,
                "IntDir",
                "-p:Platform=x64",
                "-p:Configuration=Debug");
            Assert.IsFalse(string.IsNullOrWhiteSpace(intDir));
            var intDirFull = Path.IsPathRooted(intDir)
                ? intDir
                : Path.GetFullPath(Path.Combine(temp.ProjectDir, intDir));

            Assert.IsTrue(File.Exists(Path.Combine(intDirFull, "qt", "moc", "moc_QtProjectV304.cpp")));
            Assert.IsTrue(File.Exists(Path.Combine(intDirFull, "qt", "uic", "ui_QtProjectV304.h")));
            Assert.IsTrue(File.Exists(Path.Combine(intDirFull, "qt", "rcc", "qrc_QtProjectV304.cpp")));
        }
    }
}
