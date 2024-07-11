/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_BigSolution
    {
        [TestMethod]
        public void BigSolution_Build()
        {
            if (Properties.Configuration == "Release")
                Assert.Inconclusive("Disabled in the 'Release' configuration.");

            using var temp = new TempProject();
            temp.GenerateBigSolution(
                $@"{Properties.SolutionDir}Tests\BigSolution\template", 100);
            Assert.IsTrue(MsBuild.Run(temp.ProjectDir,
                $"-p:QtMsBuild={Path.Combine(Environment.CurrentDirectory, "QtMsBuild")}",
                "-p:Platform=x64", "-p:Configuration=Release",
                "-m", "-t:Build", temp.ProjectFileName));
        }
    }
}
