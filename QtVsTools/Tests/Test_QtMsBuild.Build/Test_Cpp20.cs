// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    using QtVsTools.Core.Common;

    [TestClass]
    public class Test_Cpp20
    {
        [TestMethod]
        public void Cpp20()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");
            var xml = ProjectRootElement.Open(temp.ProjectPath);
            Assert.IsNotNull(xml);
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

            var xmlDump = ProjectRootElement.Open(temp.ProjectPath);
            Assert.IsNotNull(xmlDump);

            AddDumpCppStdTargets(xmlDump);
            xmlDump.Save();

            var buildOk = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:DumpCppStd", // force QtUpdateCompilerOptions target to run
                "-p:Platform=x64",
                "-p:Configuration=Debug");
            Assert.IsTrue(buildOk);

            var cppStdBefore = ReadCppStdFile(Path.Combine(temp.ProjectDir, "cppstd.before.txt"));
            var cppStdAfter = ReadCppStdFile(Path.Combine(temp.ProjectDir, "cppstd.after.txt"));
            Assert.IsTrue(cppStdBefore.Keys.All(x => !cppStdAfter.ContainsKey(x)
                || cppStdBefore[x] == cppStdAfter[x]));
        }

        private static void AddDumpCppStdTargets(ProjectRootElement xml)
        {
            var dump = xml.AddTarget("DumpCppStd");
            dump.DependsOnTargets = "QtUpdateCompilerOptions";

            var before = xml.AddTarget("DumpCppStdBefore");
            before.BeforeTargets = "QtUpdateCompilerOptions";
            AddDumpCppStdTask(before, "cppstd.before.txt");

            var after = xml.AddTarget("DumpCppStdAfter");
            after.AfterTargets = "QtUpdateCompilerOptions";
            AddDumpCppStdTask(after, "cppstd.after.txt");
        }

        private static void AddDumpCppStdTask(ProjectTargetElement target, string fileName)
        {
            var itemGroup = target.AddItemGroup();
            itemGroup.AddItem("CppStdLine", "@(ClCompile->'%(Identity)|%(LanguageStandard)')");
            var task = target.AddTask("WriteLinesToFile");
            task.SetParameter("File", $"$(ProjectDir){fileName}");
            task.SetParameter("Lines", "@(CppStdLine)");
            task.SetParameter("Overwrite", "true");
        }

        private static Dictionary<string, string> ReadCppStdFile(string filePath)
        {
            var entries = File.ReadAllLines(filePath)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(line => line.Split('|'))
                .Where(parts => parts.Length == 2)
                .Select(parts => (Key: parts[0], Value: parts[1]))
                .ToList();

            var result = new Dictionary<string, string>(Utils.CaseIgnorer);
            foreach (var group in entries.GroupBy(x => x.Key, Utils.CaseIgnorer)) {
                var values = group.Select(x => x.Value).Distinct().ToList();

                Assert.HasCount(1, values, $"Multiple LanguageStandard values for '{group.Key}': "
                    + $"{string.Join(", ", values)}");

                result[group.Key] = values[0];
            }
            return result;
        }
    }
}
