// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

/* https://bugreports.qt.io/browse/QTVSADDINBUG-1283 */

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_QTVSADDINBUG_1283
    {
        [TestMethod]
        public void QtVsAddinBug1283()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");

            var srcHeader = File.ReadAllText(Path.Combine(temp.ProjectDir, "QtProjectV304.h"));

            srcHeader = srcHeader.Replace("Q_OBJECT", @"Q_OBJECT
public Q_SLOTS:
    void func1();
#ifdef Q_OS_WIN
    void func2();
#endif");

            File.WriteAllText(Path.Combine(temp.ProjectDir, "QtProjectV304.h"), srcHeader);

            var project = MsBuild.Evaluate(temp.ProjectPath,
                ("Platform", "x64"), ("Configuration", "Debug"));
            var build = MsBuild.Prepare(project, "QtAddCompilerSources");
            Assert.IsTrue(MsBuild.Run(build));

            var genSrcMoc = File.ReadAllText(Path.Combine(temp.ProjectDir,
                build.Project.ExpandString("$(IntDir)") ?? "", @"qt\moc\moc_QtProjectV304.cpp"));
            Assert.Contains("func1()", genSrcMoc);
            Assert.Contains("func2()", genSrcMoc);
        }
    }
}
