/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Workspace.Build;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_BigSolution
    {
        private bool Build(int count, bool fixedTimeout = false, int delay = -1, int timeout = -1)
        {
            using var temp = new TempProject();
            temp.GenerateBigSolution(
                $@"{Properties.SolutionDir}Tests\BigSolution\template", count);
            return MsBuild.Run(temp.ProjectDir,
                $"-p:QtMsBuild={Path.Combine(Environment.CurrentDirectory, "QtMsBuild")}",
                timeout >= 0 ? $"-p:QtCriticalSectionTimeout={timeout}" : null,
                fixedTimeout ? "-p:QtCriticalSectionFixedTimeout=true" : null,
                delay >= 0 ? $"-p:QtCriticalSectionDelay={delay}" : null,
                "-p:Platform=x64", "-p:Configuration=Release",
                "-m", "-t:Build", temp.ProjectFileName);
        }

        [TestMethod]
        public void BigSolution_FailByTimeout()
        {
            if (Properties.Configuration == "Release")
                Assert.Inconclusive("Disabled in the 'Release' configuration.");

            Assert.IsTrue(Build(2, true, 1000));

            if (Build(100, true, 1000))
                Assert.Inconclusive();
        }

        [TestMethod]
        public void BigSolution_TimeoutAdjustment()
        {
            if (Properties.Configuration == "Release")
                Assert.Inconclusive("Disabled in the 'Release' configuration.");

            Assert.IsTrue(Build(100));
        }
    }
}
