/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_IntDir
    {
        [TestMethod]
        public void IntDir()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var xml = ProjectRootElement.Open(temp.ProjectPath);
            var props = xml.AddPropertyGroup();
            props.AddProperty("OldIntDir", "$(IntDir)");
            props.AddProperty("IntDir", @"$(ProjectDir)build\$(Configuration)\");
            xml.Save();

            var project = MsBuild.Evaluate(temp.ProjectPath,
                ("Platform", "x64"), ("Configuration", "Debug"));
            Assert.IsTrue(project.Build("Rebuild"));
            Assert.IsTrue(
                File.Exists(project.ExpandString($@"$(IntDir)qt\qmake\props.txt")));
            Assert.IsFalse(
                File.Exists(project.ExpandString($@"$(OldIntDir)qt\qmake\props.txt")));
        }
    }
}
