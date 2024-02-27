/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Build.Construction;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_LateBinding
    {
        [TestMethod]
        public void Concept()
        {
            using var temp = new TempProject();
            temp.Create(@"
<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <Eval Include=""LateBinding"">
      <X>$(X)</X>
    </Eval>
  </ItemGroup>
  <PropertyGroup>
    <X>The quick brown fox</X>
    <Y>$(X) jumped over the lazy dog.</Y>
    <Z>@(Eval->'%(X)') jumped over the lazy dog.</Z>
    <X>The sleek gray wolf</X>
  </PropertyGroup>
</Project>".Trim());

            var project = MsBuild.Evaluate(temp.ProjectPath);
            Assert.AreEqual(
                project.ExpandString("$(X)"), "The sleek gray wolf");
            Assert.AreEqual(
                project.ExpandString("$(Y)"), "The quick brown fox jumped over the lazy dog.");
            Assert.AreEqual(
                project.ExpandString("$(Z)"), "The sleek gray wolf jumped over the lazy dog.");
        }

        [TestMethod]
        public void QtDeployDir()
        {
            using TempProject temp = new();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");

            var project = MsBuild.Evaluate(temp.ProjectPath, ("Platform", "x64"),
                ("QtMsBuild", Path.Combine(Environment.CurrentDirectory, "QtMsBuild")));
            Assert.AreEqual(
                project.ExpandString("$(QtDeployDir)"), project.ExpandString("$(OutDir)"),
                ignoreCase: true);

            var xml = ProjectRootElement.Open(temp.ProjectPath);
            var props = xml.AddPropertyGroup();
            props.AddProperty("OutDir", @$"{temp.ProjectDir}\out\");
            xml.Save();

            project = MsBuild.Evaluate(temp.ProjectPath, ("Platform", "x64"),
                ("QtMsBuild", Path.Combine(Environment.CurrentDirectory, "QtMsBuild")));
            Assert.AreEqual(
                project.ExpandString("$(QtDeployDir)"), project.ExpandString("$(OutDir)"),
                ignoreCase: true);
        }
    }
}
