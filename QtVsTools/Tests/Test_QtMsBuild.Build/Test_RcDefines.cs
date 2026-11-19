// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_ResourceCompileDefines
    {
        public enum BuildDirSpacing
        {
            WithSpaces,
            WithoutSpaces
        }

        public enum BuildDirQuoteStyle
        {
            Quoted,
            Unquoted
        }

        [TestMethod]
        [DataRow(BuildDirSpacing.WithSpaces, BuildDirQuoteStyle.Quoted)]
        [DataRow(BuildDirSpacing.WithSpaces, BuildDirQuoteStyle.Unquoted)]
        [DataRow(BuildDirSpacing.WithoutSpaces, BuildDirQuoteStyle.Quoted)]
        [DataRow(BuildDirSpacing.WithoutSpaces, BuildDirQuoteStyle.Unquoted)]
        public void QtPrepareResourceCompilerDefines_Normalize_QT_TESTCASE_BUILDDIR(
            BuildDirSpacing spacing, BuildDirQuoteStyle quoting)
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var buildDir = CreateQtTestCaseBuildDir(temp.ProjectDir, spacing);

            var project = ProjectRootElement.Open(temp.ProjectPath);
            Assert.IsNotNull(project);

            var props = project.AddPropertyGroup();
            props.AddProperty("Qt_DEFINES_", BuildQtDefines(buildDir, quoting));

            var target = project.AddTarget("DumpQtResourceCompilerDefines");
            target.DependsOnTargets = "QtPrepareResourceCompilerDefines";
            var write = target.AddTask("WriteLinesToFile");
            write.SetParameter("File", "$(ProjectDir)qt_rc_defines.txt");
            write.SetParameter("Lines", "$(QtResourceCompilerDefines)");
            write.SetParameter("Overwrite", "true");

            project.Save();

            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:DumpQtResourceCompilerDefines",
                "-p:Platform=x64",
                "-p:Configuration=Debug");
            Assert.IsTrue(buildOk);

            var rcDefines = File.ReadAllText(Path.Combine(temp.ProjectDir, "qt_rc_defines.txt"))
                .Trim();
            var entries = rcDefines.Split(new[] { ';', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

            CollectionAssert.Contains(entries, "_CONSOLE");
            CollectionAssert.Contains(entries, "QT_TESTLIB_LIB");
            CollectionAssert.Contains(entries, "QT_CORE_LIB");
            CollectionAssert.Contains(entries, $"QT_TESTCASE_BUILDDIR={buildDir}");

            Assert.IsFalse(entries.Any(x => x.StartsWith("QT_TESTCASE_BUILDDIR=\"")));
        }

        [TestMethod]
        [DataRow(BuildDirSpacing.WithSpaces, BuildDirQuoteStyle.Quoted)]
        [DataRow(BuildDirSpacing.WithSpaces, BuildDirQuoteStyle.Unquoted)]
        [DataRow(BuildDirSpacing.WithoutSpaces, BuildDirQuoteStyle.Quoted)]
        [DataRow(BuildDirSpacing.WithoutSpaces, BuildDirQuoteStyle.Unquoted)]
        public void ResourceCompile_WithAndWithoutSpaces_in_QT_TESTCASE_BUILDDIR(
            BuildDirSpacing spacing, BuildDirQuoteStyle quoting)
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var buildDir = CreateQtTestCaseBuildDir(temp.ProjectDir, spacing);

            File.WriteAllText(Path.Combine(temp.ProjectDir, "repro.rc"), @"
1 VERSIONINFO
FILEVERSION 1,0,0,0
PRODUCTVERSION 1,0,0,0
BEGIN
  BLOCK ""StringFileInfo""
  BEGIN
    BLOCK ""040904B0""
    BEGIN
      VALUE ""FileDescription"", ""Qt Rc Repro\0""
    END
  END
  BLOCK ""VarFileInfo""
  BEGIN
    VALUE ""Translation"", 0x0409, 1200
  END
END".Trim());

            var project = ProjectRootElement.Open(temp.ProjectPath);
            Assert.IsNotNull(project);

            project.AddItem("ResourceCompile", "repro.rc");

            var props = project.AddPropertyGroup();
            props.AddProperty("Qt_DEFINES_", BuildQtDefines(buildDir, quoting));

            // Override QtAddCompilerSources with an empty target to isolate this test to rc.exe
            // behavior. The last target definition with the same name wins, so this becomes a
            // no-op and avoids qmake-dependent source generation unrelated to this regression.
            project.AddTarget("QtAddCompilerSources");
            project.Save();

            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:ResourceCompile",
                "-p:Platform=x64",
                "-p:Configuration=Debug");

            Assert.IsTrue(buildOk);
        }

        private static string CreateQtTestCaseBuildDir(string projectDir, BuildDirSpacing spacing)
        {
            var leaf = spacing == BuildDirSpacing.WithSpaces
                ? "Qt Test Rc Space Err"
                : "QtTestRcSpaceErr";
            var buildDir = Path.Combine(projectDir, leaf, "x64", "Debug", "qt", "qmake");
            Directory.CreateDirectory(buildDir);
            return buildDir.Replace('\\', '/');
        }

        private static string BuildQtDefines(string buildDir, BuildDirQuoteStyle quoting)
        {
            var qtTestCaseBuildDir = quoting == BuildDirQuoteStyle.Quoted
                ? $"QT_TESTCASE_BUILDDIR=\"{buildDir}\""
                : $"QT_TESTCASE_BUILDDIR={buildDir}";
            return $"_CONSOLE;QT_TESTLIB_LIB;QT_CORE_LIB;{qtTestCaseBuildDir}";
        }
    }
}
