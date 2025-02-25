// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
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

            var targetName = "QtQmlStaticGatherQmlPaths";
            var project = MsBuild.Evaluate(temp.ProjectPath,
                ("Platform", "x64"),
                ("Configuration", "Debug"),
                ("QtStaticPlugins", "true"));
            var build = MsBuild.Prepare(project, targetName);
            Assert.IsTrue(MsBuild.Run(build));

            var items = build.Result.ResultsByTarget[targetName].Items;
            Assert.IsTrue(items.Length == 1);
            Assert.IsTrue(Path.GetFileName(items[0].ItemSpec) == "foo.qml");
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

            var targetName = "QtQmlStaticGenerateImportFile";
            var project = MsBuild.Evaluate(temp.ProjectPath,
                ("Platform", "x64"),
                ("Configuration", "Debug"),
                ("QtStaticPlugins", "true"));
            var build = MsBuild.Prepare(project, targetName);
            Assert.IsTrue(MsBuild.Run(build));

            var resultFile = File.ReadAllText(project.ExpandString("$(QtQmlStaticImportFile)"));
            var expectedFile = $@"import Foo;
import Bar;
QmlObject {{ }}
";
            Assert.IsTrue(resultFile == expectedFile);
        }

        [TestMethod]
        public void QtQmlStaticPlugin()
        {
            using var qtVersions = Registry.CurrentUser
                .OpenSubKey(@"SOFTWARE\QtProject\QtVsTools\Versions");
            if (qtVersions == null || !qtVersions.GetSubKeyNames().Contains("dev_static"))
                Assert.Inconclusive("Requires static build registered as 'dev_static'.");

            using var temp = new TempProject();
            temp.Clone(Path.Combine(Properties.SolutionDir,
                @"Tests\ProjectTemplates\QtQuickApplication", "QtQuickApplication.vcxproj"));

            var xml = ProjectRootElement.Open(temp.ProjectPath);
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
            var project = MsBuild.Evaluate(temp.ProjectPath,
                ("Platform", "x64"), ("Configuration", "Debug"));
            var build = MsBuild.Prepare(project);
            Assert.IsTrue(MsBuild.Run(build));

            Assert.IsTrue(File.Exists(project.ExpandString("$(TargetPath)")));
            Assert.IsTrue(File.Exists(Path.Combine(
                project.ExpandString("$(QtVarsOutputDir)"), "qtvars_qml_plugin_import.cpp")));
            Assert.IsTrue(build.Project
                .GetItems("ClCompile")
                .Select(x => x.GetMetadataValue("Filename"))
                .Contains("qtvars_qml_plugin_import"));

            var proc = Process.Start(new ProcessStartInfo
            {
                FileName = project.ExpandString("$(TargetPath)"),
                WorkingDirectory = project.ExpandString("$(OutDir)"),
                CreateNoWindow = true,
                UseShellExecute = false
            });
            if (!proc.WaitForExit(3000)) {
                proc.Kill();
                Assert.Fail();
            }
            Assert.IsTrue(proc.ExitCode == 42);
        }
    }
}
