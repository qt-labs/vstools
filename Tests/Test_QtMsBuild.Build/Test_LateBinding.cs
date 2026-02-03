// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Build.Construction;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_LateBinding
    {
        private const string PropsFileName = "props.txt";

        [TestMethod]
        public void Concept()
        {
            using var temp = new TempProject();
            temp.Create($@"
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
  <Target Name=""DumpProps"">
    <PropertyGroup>
      <ZValue>$(Z)</ZValue> <!-- Force evaluation of $(Z) into $(ZValue) inside the target -->
    </PropertyGroup>
    <ItemGroup>
      <PropLine Include=""X=$(X)"" />         <!-- Evaluate $(X) -->
      <PropLine Include=""Y=$(Y)"" />         <!-- Evaluate $(Y) -->
      <PropLine Include=""Z=$(ZValue)"" />    <!-- Evaluate $(ZValue) -->
    </ItemGroup>
    <WriteLinesToFile File=""$(ProjectDir){PropsFileName}"" Lines=""@(PropLine)"" Overwrite=""true"" />
  </Target>
</Project>".Trim());

            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:DumpProps"); // run DumpProps target to capture property values to a file
            Assert.IsTrue(buildOk);

            var props = ReadProps(Path.Combine(temp.ProjectDir, PropsFileName));
            Assert.AreEqual("The sleek gray wolf", props["X"]);
            Assert.AreEqual("The quick brown fox jumped over the lazy dog.", props["Y"]);
            Assert.AreEqual("The sleek gray wolf jumped over the lazy dog.", props["Z"]);
        }

        [TestMethod]
        public void QtDeployDir()
        {
            using TempProject temp = new();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");

            var xml = ProjectRootElement.Open(temp.ProjectPath);
            Assert.IsNotNull(xml);
            AddDumpPropsTarget(xml);
            xml.Save();

            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:DumpProps",
                "-p:Platform=x64",
                "-p:Configuration=Debug");
            Assert.IsTrue(buildOk);

            var propLines = ReadProps(Path.Combine(temp.ProjectDir, PropsFileName));
            Assert.AreEqual(propLines["QtDeployDir"], propLines["OutDir"], ignoreCase: true);

            using TempProject temp2 = new();
            temp2.Clone(temp.ProjectPath);

            xml = ProjectRootElement.Open(temp2.ProjectPath);
            Assert.IsNotNull(xml);
            var props = xml.AddPropertyGroup();
            props.AddProperty("OutDir", @$"{temp2.ProjectDir}\out\");
            xml.Save();

            Assert.IsTrue(MsBuild.Run(
                temp2.ProjectDir,
                temp2.ProjectPath,
                "-t:DumpProps", // run DumpProps target to capture property values to a file
                "-p:Platform=x64",
                "-p:Configuration=Debug"));

            propLines = ReadProps(Path.Combine(temp2.ProjectDir, PropsFileName));
            Assert.AreEqual(propLines["QtDeployDir"], propLines["OutDir"], ignoreCase: true);
        }

        private static void AddDumpPropsTarget(ProjectRootElement xml)
        {
            var target = xml.AddTarget("DumpProps");

            // Use CreateProperty to evaluate $(QtDeployDir) and $(OutDir) into
            // $(QtDeployDirValue)/$(OutDirValue) (avoid item‑list concatenation errors).

            var qtDeployTask = target.AddTask("CreateProperty");
            qtDeployTask.SetParameter("Value", "$(QtDeployDir)");
            qtDeployTask.AddOutputProperty("ValueSetByTask", "QtDeployDirValue");

            var outDirTask = target.AddTask("CreateProperty");
            outDirTask.SetParameter("Value", "$(OutDir)");
            outDirTask.AddOutputProperty("ValueSetByTask", "OutDirValue");

            var itemGroup = target.AddItemGroup();
            itemGroup.AddItem("PropLine", "QtDeployDir=$(QtDeployDirValue)");
            itemGroup.AddItem("PropLine", "OutDir=$(OutDirValue)");

            var task = target.AddTask("WriteLinesToFile");
            task.SetParameter("File", "$(ProjectDir)" + PropsFileName);
            task.SetParameter("Lines", "@(PropLine)");
            task.SetParameter("Overwrite", "true");
        }

        private static Dictionary<string, string> ReadProps(string filePath)
        {
            var result = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(filePath)) {
                var idx = line.IndexOf('=');
                if (idx <= 0)
                    continue;
                result[line.Substring(0, idx)] = line.Substring(idx + 1);
            }
            return result;
        }
    }
}
