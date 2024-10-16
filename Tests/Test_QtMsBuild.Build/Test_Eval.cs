/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    using Core;

    [TestClass]
    public class Test_Eval
    {
        [TestMethod]
        public void Eval()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var project = MsBuild.Evaluate(temp.ProjectPath, ("Platform", "x64"));
            Assert.AreEqual(project.ExpandString("$(QtVSToolsVersion)"), Version.PRODUCT_VERSION);
        }
    }
}
