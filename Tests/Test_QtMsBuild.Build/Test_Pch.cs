// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_Pch
    {
        private static bool UsePrecompiledHeader { get; set; } = true;

        [TestMethod]
        public void PreCompiledHeaderWithout()
        {
            UsePrecompiledHeader = false;
            PreCompiledHeader();
            UsePrecompiledHeader = true;
        }

        [TestMethod]
        public void PreCompiledHeader()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");

            Directory.CreateDirectory($@"{temp.ProjectDir}\foo");
            File.WriteAllText($@"{temp.ProjectDir}\foo\foo.h", @"
#pragma once
#define FOO ""FOO""
");
            File.WriteAllText($@"{temp.ProjectDir}\QtClass.h", @"
#pragma once
#include <foo.h>
#include <QObject>
class QtClass  : public QObject
{
    Q_OBJECT
public:
    QtClass(QObject *parent);
    ~QtClass();
};
");
            File.WriteAllText($@"{temp.ProjectDir}\QtClass.cpp", @"
#include ""QtProjectV304.h""
#include ""QtClass.h""
QtClass::QtClass(QObject *parent) : QObject(parent) {}
QtClass::~QtClass() {}
");
            var xml = ProjectRootElement.Open(temp.ProjectPath);
            var clCompile = xml.AddItemDefinitionGroup()
                .AddItemDefinition("ClCompile");
            clCompile.AddMetadata("AdditionalIncludeDirectories", "foo");
            if (UsePrecompiledHeader)
                clCompile.AddMetadata("PrecompiledHeader", "Use");
            clCompile.AddMetadata("PrecompiledHeaderFile", "QtProjectV304.h");
            xml.AddItem("QtMoc", "QtClass.h");
            xml.AddItem("ClCompile", "QtClass.cpp");
            foreach (var pchCreate in xml.Items.Where(x => x.Include == "QtProjectV304.cpp"))
                pchCreate.AddMetadata("PrecompiledHeader", "Create");
            xml.Save();

            var project = MsBuild.Evaluate(temp.ProjectPath,
                ("Platform", "x64"), ("Configuration", "Debug"));
            var build = MsBuild.Prepare(project);
            Assert.IsTrue(MsBuild.Run(build));
        }
    }
}
