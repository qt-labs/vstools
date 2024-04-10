/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_Build
    {
        private MsBuild.Build TestBuild { get; set; }

        [TestMethod]
        public void Build()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var project = MsBuild.Evaluate(temp.ProjectPath, ("Platform", "x64"), ("Configuration", "Debug"));

            TestBuild = MsBuild.Prepare(project);
            MsBuild.Log.EventAdded += OnBuildEvent;

            var buildOk = MsBuild.Run(TestBuild);

            MsBuild.Log.EventAdded -= OnBuildEvent;
            Debug.WriteLine(MsBuild.Log.Report());

            Assert.IsTrue(buildOk);
        }

        private void OnBuildEvent(object sender, EventArgs e)
        {
            if (e is TargetFinishedEventArgs targetEvent && targetEvent.TargetName == "QtWork") {
                var intDir = Path.Combine(
                    TestBuild.Project.ExpandString("$(ProjectDir)"),
                    TestBuild.Project.ExpandString("$(IntDir)"));
                Assert.IsTrue(File.Exists($@"{intDir}moc\moc_QtProjectV304.cpp"));
                Assert.IsTrue(File.Exists($@"{intDir}uic\ui_QtProjectV304.h"));
                Assert.IsTrue(File.Exists($@"{intDir}rcc\qrc_QtProjectV304.cpp"));
            }
        }
    }
}
