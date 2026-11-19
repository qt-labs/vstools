// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

/* https://bugreports.qt.io/browse/QTVSADDINBUG-1340 */

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.QtMsBuild.Build
{
    [TestClass]
    public class Test_QTVSADDINBUG_1340
    {
        [TestMethod]
        public async Task QtVsAddinBug1340()
        {
            using var temp = new TempProject();
            temp.Clone($@"{Properties.SolutionDir}Tests\ProjectFormats\304\QtProjectV304.vcxproj");

            var commonArgs = new[]
            {
                "-p:Platform=x64",
                "-p:Configuration=Debug"
            };

            string qtToolsPath = null;
            try {
                qtToolsPath = MsBuild.GetProperty(temp.ProjectDir, temp.ProjectPath, "QtToolsPath",
                    commonArgs);
            } catch (Exception ex) {
                Assert.Inconclusive($"Could not evaluate QtToolsPath: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(qtToolsPath))
                Assert.Inconclusive("QtToolsPath is empty. A configured Qt is required.");

            var hasQmake = File.Exists(Path.Combine(qtToolsPath, "qmake.exe"));
            if (!hasQmake)
                Assert.Inconclusive($"qmake was not found under QtToolsPath '{qtToolsPath}'.");

            var qtVarsFilePath = MsBuild.GetProperty(temp.ProjectDir, temp.ProjectPath,
                "QtVarsFilePath", commonArgs);
            var qtVarsFileFull = Path.IsPathRooted(qtVarsFilePath)
                ? qtVarsFilePath
                : Path.GetFullPath(Path.Combine(temp.ProjectDir, qtVarsFilePath));

            var firstBuild = MsBuild.Run(
                temp.ProjectDir,
                temp.ProjectPath,
                "-t:QtVarsDesignTime",
                commonArgs[0],
                commonArgs[1]);
            if (!firstBuild)
                Assert.Inconclusive("Warm-up QtVarsDesignTime build failed in this environment.");

            const int rounds = 6;
            const int parallelBuilds = 4;
            var projectDir = temp.ProjectDir;
            var projectPath = temp.ProjectPath;

            for (var round = 0; round < rounds; ++round) {
                if (File.Exists(qtVarsFileFull))
                    File.Delete(qtVarsFileFull);

                var buildTasks = Enumerable.Range(0, parallelBuilds)
                    .Select(_ => Task.Run(() =>
                    {
                        try {
                            return MsBuild.Run(
                                projectDir,
                                projectPath,
                                "-t:QtVarsDesignTime",
                                commonArgs[0],
                                commonArgs[1],
                                // Give each parallel invocation a unique QtVars input path so
                                // QtVarsDesignTime tests concurrent execution deterministically.
                                string.Format("-p:QtQmlStaticImportFile={0}",
                                    Path.Combine(projectDir, $"force_input_{Guid.NewGuid():N}.qml")));
                        } catch {
                            return false;
                        }
                    }))
                    .ToArray();

                await Task.WhenAll(buildTasks).ConfigureAwait(false);

                var failedBuilds = buildTasks.Count(t => !t.Result);
                Assert.AreEqual(0, failedBuilds,
                    $"QtVarsDesignTime failed in round {round + 1}/{rounds}.");

                Assert.IsTrue(File.Exists(qtVarsFileFull),
                    $"qtvars.xml was not generated in round {round + 1}/{rounds}.");

                var qtVarsData = File.ReadAllText(qtVarsFileFull);
                try {
                    _ = XDocument.Parse(qtVarsData, LoadOptions.PreserveWhitespace);
                } catch (Exception ex) {
                    Assert.Fail($"qtvars.xml is not well-formed after round {round + 1}/{rounds}"
                        + $":{Environment.NewLine}{ex.Message}{Environment.NewLine}{qtVarsData}");
                }
            }
        }
    }
}
