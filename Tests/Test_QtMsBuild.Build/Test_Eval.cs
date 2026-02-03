// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

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
            var qtVsToolsVersion = MsBuild.GetProperty(
                temp.ProjectDir,
                temp.ProjectPath,
                "QtVSToolsVersion",
                "-p:Platform=x64",
                "-p:Configuration=Debug");
            Assert.AreEqual(Version.PRODUCT_VERSION, qtVsToolsVersion);
        }
    }
}
