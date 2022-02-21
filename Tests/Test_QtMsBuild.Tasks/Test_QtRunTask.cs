/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

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
            else if (Directory.Exists(Path.Combine(vcTargetsPath, "v150")))
                vcTargetsPath = Path.Combine(vcTargetsPath, "v150");
            else
                Assert.Inconclusive("MSBuild VC targets directory not found");

            ITaskItem[] sourceItems = new TaskItem[]
            {
                new TaskItem("main.cpp", new Dictionary<string, string> {
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
            Assert.IsTrue(result != null && result.Length == 1);
            Assert.IsTrue(result[0].GetMetadata("CommandLine").Contains("/Zc:rvalueCast-"));
        }
    }
}
