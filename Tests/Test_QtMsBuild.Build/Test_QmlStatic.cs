/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
