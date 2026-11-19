// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_QmlStatic
    {
        [TestMethod]
        public void QtQmlStaticGatherQmlPaths()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");

            File.WriteAllText($@"{temp.ProjectDir}\QtProjectV304.qrc", @"
<RCC>
    <qresource prefix=""QtProjectV304"">
        <file>foo.qml</file>
        <file>bar.nqml</file>
    </qresource>
</RCC>");

            var xml = ProjectRootElement.Open(temp.ProjectPath);
            Assert.IsNotNull(xml);

            // Add a target to write the resulting @(QtQmlPaths)item list to a file
            var target = xml.AddTarget("DumpQmlPaths");
            target.DependsOnTargets = "QtQmlStaticGatherQmlPaths";
            var task = target.AddTask("WriteLinesToFile");
            task.SetParameter("File", "$(ProjectDir)qtqmlpaths.txt");
            task.SetParameter("Lines", "@(QtQmlPaths)");
            task.SetParameter("Overwrite", "true");

            xml.Save();

            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:DumpQmlPaths", // force QtQmlStaticGatherQmlPaths target to run
                "-p:Platform=x64",
                "-p:Configuration=Debug",
                "-p:QtStaticPlugins=true");
            Assert.IsTrue(buildOk);

            var qmlPaths = File.ReadAllLines(Path.Combine(temp.ProjectDir, "qtqmlpaths.txt"))
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            Assert.HasCount(1, qmlPaths);
            Assert.AreEqual("foo.qml", qmlPaths[0]);
        }

        [TestMethod]
        public void QtQmlStaticGenerateImportFile()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");

            File.WriteAllText($@"{temp.ProjectDir}\QtProjectV304.qrc", @"
<RCC>
    <qresource prefix=""QtProjectV304"">
        <file>foo.qml</file>
        <file>bar.nqml</file>
    </qresource>
</RCC>");

            File.WriteAllText($@"{temp.ProjectDir}\foo.qml", @"
import Foo;
import Bar;
nimport Baz");

            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:QtQmlStaticGenerateImportFile",
                "-p:Platform=x64",
                "-p:Configuration=Debug",
                "-p:QtStaticPlugins=true");
            Assert.IsTrue(buildOk);

            var importFile = MsBuild.GetProperty(
                temp.ProjectDir,
                temp.ProjectPath,
                "QtQmlStaticImportFile",
                "-p:Platform=x64",
                "-p:Configuration=Debug",
                "-p:QtStaticPlugins=true");
            var importFilePath = Path.IsPathRooted(importFile)
                ? importFile
                : Path.GetFullPath(Path.Combine(temp.ProjectDir, importFile));
            var resultFile = NormalizeNewlines(File.ReadAllText(importFilePath));
            const string expectedFile = @"import Foo;
import Bar;
QmlObject { }
";
            Assert.AreEqual(NormalizeNewlines(expectedFile), resultFile);
        }

        [TestMethod]
        public void QtQmlStaticPlugin()
        {
            using var qtVersions = Registry.CurrentUser
                .OpenSubKey(@"SOFTWARE\QtProject\QtVsTools\Versions");
            if (qtVersions == null || !qtVersions.GetSubKeyNames().Contains("dev_static"))
                Assert.Inconclusive("Requires static build registered as 'dev_static'.");

            var originalTemp = System.Environment.GetEnvironmentVariable("TEMP");
            var originalTmp = System.Environment.GetEnvironmentVariable("TMP");
            var testTempRoot = Path.Combine(Properties.SolutionDir, "Tests", "Temp");
            Directory.CreateDirectory(testTempRoot);
            System.Environment.SetEnvironmentVariable("TEMP", testTempRoot);
            System.Environment.SetEnvironmentVariable("TMP", testTempRoot);

            using var temp = new TempProject();
            try {
                temp.Clone(Path.Combine(Properties.SolutionDir,
                    @"Tests\ProjectTemplates\QtQuickApplication", "QtQuickApplication.vcxproj"));

                var xml = ProjectRootElement.Open(temp.ProjectPath);
                Assert.IsNotNull(xml);
                foreach (var qtSettings in xml.PropertyGroups.Where(x => x.Label == "QtSettings")) {
                    qtSettings.SetProperty("QtInstall", "dev_static");
                    qtSettings.SetProperty("QtQMLDebugEnable", "false");
                }
                xml.Save();

                File.WriteAllText($@"{temp.ProjectDir}\main.qml", @"
import QtQuick 2.9
import QtQuick.Window 2.2
import QtQuick.Particles 2.0
Item { Component.onCompleted: Qt.exit(42) }
");
                var buildOk = MsBuild.Run(
                    temp.ProjectDir,
                    temp.ProjectPath,
                    "-t:Build",
                    "-p:Platform=x64",
                    "-p:Configuration=Debug");
                Assert.IsTrue(buildOk);

                var targetPath = MsBuild.GetProperty(
                    temp.ProjectDir,
                    temp.ProjectPath,
                    "TargetPath",
                    "-p:Platform=x64",
                    "-p:Configuration=Debug");
                Assert.IsTrue(File.Exists(targetPath));

                var qtVarsOutputDir = MsBuild.GetProperty(
                    temp.ProjectDir,
                    temp.ProjectPath,
                    "QtVarsOutputDir",
                    "-p:Platform=x64",
                    "-p:Configuration=Debug");
                var qtVarsFull = Path.IsPathRooted(qtVarsOutputDir)
                    ? qtVarsOutputDir
                    : Path.GetFullPath(Path.Combine(temp.ProjectDir, qtVarsOutputDir));
                Assert.IsTrue(File.Exists(Path.Combine(
                    qtVarsFull, "qtvars_qml_plugin_import.cpp")));

                var workingDir = Path.GetDirectoryName(targetPath);
                Assert.IsNotNull(workingDir);

                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = targetPath,
                    WorkingDirectory = workingDir,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                Assert.IsNotNull(proc);
                if (!proc.WaitForExit(3000)) {
                    proc.Kill();
                    Assert.Fail();
                }
                Assert.AreEqual(42, proc.ExitCode);
            } finally {
                System.Environment.SetEnvironmentVariable("TEMP", originalTemp);
                System.Environment.SetEnvironmentVariable("TMP", originalTmp);
            }
        }

        private static string NormalizeNewlines(string text)
        {
            return text?.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
