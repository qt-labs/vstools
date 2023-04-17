/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Tasks
{
    using QtVsTools.QtMsBuild.Tasks;

    [TestClass]
    public class Test_QtRunTask
    {
        [TestMethod]
        public void CLCommandLine()
        {
            var argv = Environment.GetCommandLineArgs();
            var path = argv[0];
            if (string.IsNullOrEmpty(Path.GetPathRoot(path)))
                Assert.Inconclusive("Executable path is not rooted.");

            do
                path = Path.GetFullPath(Path.GetDirectoryName(path));
            while (path != Path.GetPathRoot(path)
                && !File.Exists(Path.Combine(path, "devenv.exe")));

            if (!File.Exists(Path.Combine(path, "devenv.exe")))
                Assert.Inconclusive("devenv.exe not found");

            string vsPath = Path.GetDirectoryName(Path.GetDirectoryName(path));
            string vcTargetsPath = Path.Combine(vsPath, "MSBuild", "Microsoft", "VC");
            if (Directory.Exists(Path.Combine(vcTargetsPath, "v170")))
                vcTargetsPath = Path.Combine(vcTargetsPath, "v170");
            else if (Directory.Exists(Path.Combine(vcTargetsPath, "v160")))
                vcTargetsPath = Path.Combine(vcTargetsPath, "v160");
            else
                Assert.Inconclusive("MSBuild VC targets directory not found");

            ITaskItem[] sourceItems = new TaskItem[]
            {
                new("main.cpp", new Dictionary<string, string> {
                    { "EnforceTypeConversionRules", "false" },
                })
            };

            Assert.IsTrue(
                QtRunTask.Execute(
                    sourceItems,
                    $@"{vcTargetsPath}\Microsoft.Build.CPPTasks.Common.dll",
                    "Microsoft.Build.CPPTasks.CLCommandLine",
                    "Sources",
                    out ITaskItem[] result,
                    "CommandLines",
                    "CommandLine"));
            Assert.IsTrue(result is {Length: 1});
            Assert.IsTrue(result[0].GetMetadata("CommandLine").Contains("/Zc:rvalueCast-"));
        }
    }
}
