// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

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

            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:Rebuild",
                "-p:Platform=x64",
                "-p:Configuration=Debug");
            Assert.IsTrue(buildOk);

            var intDir = MsBuild.GetProperty(
                temp.ProjectDir,
                temp.ProjectPath,
                "IntDir",
                "-p:Platform=x64",
                "-p:Configuration=Debug");
            var oldIntDir = MsBuild.GetProperty(
                temp.ProjectDir,
                temp.ProjectPath,
                "OldIntDir",
                "-p:Platform=x64",
                "-p:Configuration=Debug");

            var intQmakeDir = Path.Combine(ResolveDir(temp.ProjectDir, intDir), "qt", "qmake");
            var oldIntQmakeDir = Path.Combine(ResolveDir(temp.ProjectDir, oldIntDir), "qt", "qmake");

            Assert.HasCount(1, GetFiles(intQmakeDir, "props.txt"),
                "Expected qmake props.txt under current IntDir.");
            Assert.HasCount(0, GetFiles(oldIntQmakeDir, "props.txt"),
                "Did not expect qmake props.txt under previous IntDir.");
        }

        private static string ResolveDir(string projectDir, string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
                return dir;
            return Path.IsPathRooted(dir) ? dir : Path.GetFullPath(Path.Combine(projectDir, dir));
        }

        private static string[] GetFiles(string dir, string pattern)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return Array.Empty<string>();
            return Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
        }
    }
}
