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
    public class Test_Cpp20
    {
        private MsBuild.Build TestBuild { get; set; }

        [TestMethod]
        public void Cpp20()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var xml = ProjectRootElement.Open(temp.ProjectPath);
            xml.AddItemDefinitionGroup()
                .AddItemDefinition("ClCompile")
                .AddMetadata("LanguageStandard", "stdcpp20");
            xml.Save();

            var srcCpp = File.ReadAllText(Path.Combine(temp.ProjectDir, "QtProjectV304.cpp"));
            srcCpp = @"
#include <iostream>
#include <vector>
#include <ranges>
int foo() {
    std::vector<int> numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    auto result = numbers
        | std::ranges::views::filter([](int n) { return n % 2 == 0; })
        | std::ranges::views::transform([](int n) { return n * 2; });
    for (int n : result)
        std::cout << n << "" "";  // Output: 4 8 12 16 20
    return 0;
}
" + srcCpp;
            File.WriteAllText(Path.Combine(temp.ProjectDir, "QtProjectV304.cpp"), srcCpp);

            var project = MsBuild.Evaluate(temp.ProjectPath,
                ("Platform", "x64"), ("Configuration", "Debug"));

            TestBuild = MsBuild.Prepare(project);
            MsBuild.Log.EventAdded += OnBuildEvent;

            var buildOk = MsBuild.Run(TestBuild);

            MsBuild.Log.EventAdded -= OnBuildEvent;

            Assert.IsTrue(CppStdBefore.Values.All(x => !CppStdAfter.ContainsKey(x)
                || CppStdBefore[x] == CppStdAfter[x]));
            Assert.IsTrue(buildOk);
        }

        private Dictionary<string, string> CppStdBefore { get; set; } = null;
        private Dictionary<string, string> CppStdAfter { get; set; } = null;

        private void OnBuildEvent(object sender, EventArgs e)
        {
            if (e is TargetStartedEventArgs { TargetName: "QtUpdateCompilerOptions" }) {
                CppStdBefore = TestBuild.Project.GetItems("ClCompile")
                    .ToDictionary(x => x.EvaluatedInclude, x => x.GetMetadataValue("LanguageStandard"));
            } else if (e is TargetFinishedEventArgs { TargetName: "QtUpdateCompilerOptions" }) {
                CppStdAfter = TestBuild.Project.GetItems("ClCompile")
                    .ToDictionary(x => x.EvaluatedInclude, x => x.GetMetadataValue("LanguageStandard"));
            }
        }
    }
}
