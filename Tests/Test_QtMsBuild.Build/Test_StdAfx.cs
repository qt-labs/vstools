/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_StdAfx
    {
        private MsBuild.Build TestBuild { get; set; }

        [TestMethod]
        public void StdAfx()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var xml = ProjectRootElement.Open(temp.ProjectPath);
            var cppItemDef = xml.AddItemDefinitionGroup()
                .AddItemDefinition("ClCompile");
            cppItemDef.AddMetadata("PrecompiledHeader", "Use");
            cppItemDef.AddMetadata("PrecompiledHeaderFile", "stdafx.h");
            xml.AddItemDefinitionGroup()
                .AddItemDefinition("QtMoc")
                .AddMetadata("PrependInclude", "stdafx.h");
            xml.AddItemGroup()
                .AddItem("ClCompile", "stdafx.cpp")
                .AddMetadata("PrecompiledHeader", "Create");
            xml.AddItemGroup()
                .AddItem("ClInclude", "stdafx.h");
            xml.Save();

            File.WriteAllText(Path.Combine(temp.ProjectDir, "stdafx.h"), @"
#include <QtWidgets>
");
            File.WriteAllText(Path.Combine(temp.ProjectDir, "stdafx.cpp"), @"
#include ""stdafx.h""
");
            File.WriteAllText(Path.Combine(temp.ProjectDir, "main.cpp"), @"
#include ""stdafx.h""
" + File.ReadAllText(Path.Combine(temp.ProjectDir, "main.cpp")));
            File.WriteAllText(Path.Combine(temp.ProjectDir, "QtProjectV304.cpp"), @"
#include ""stdafx.h""
" + File.ReadAllText(Path.Combine(temp.ProjectDir, "QtProjectV304.cpp")));

            var project = MsBuild.Evaluate(temp.ProjectPath,
                ("Platform", "x64"), ("Configuration", "Debug"));

            Assert.IsTrue(project.Build());
        }
    }
}
