/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
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
            var project = MsBuild.Evaluate(temp.ProjectPath,("Platform", "x64"),
                ("QtMsBuild", Path.Combine(Environment.CurrentDirectory, "QtMsBuild")));
            Assert.IsTrue(project.Build());
        }
    }
}
