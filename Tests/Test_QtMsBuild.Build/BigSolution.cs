/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QtVsTools.Core.MsBuild;

namespace QtVsTools.Test.QtMsBuild.Build
{
    public static class BigSolution
    {
        static void GenerateProject(
            StringBuilder genSolutionProjectRef,
            StringBuilder genSolutionProjectConfigs,
            string pathToTemplateDir,
            string pathToGeneratedDir,
            int projectCount,
            string projectGuid,
            string projectName,
            string projectRef,
            IEnumerable<string> projectConfigsList)
        {
            var projectFiles = Directory.GetFiles(
                Path.Combine(pathToTemplateDir, projectName),
                "*", SearchOption.TopDirectoryOnly);

            bool singleProject = false;
            for (int i = 1; !singleProject && i <= projectCount; i++) {
                var idxStr = string.Format("{0:D3}", i);
                var genProjectName = projectName.Replace("NNN", idxStr);
                if (genProjectName == projectName)
                    singleProject = true;
                var genProjectDirPath = Path.Combine(pathToGeneratedDir, genProjectName);
                var genProjectGuid = projectGuid.Replace("000", idxStr);

                Directory.CreateDirectory(genProjectDirPath);

                foreach (var projectFile in projectFiles) {
                    var genProjectFileName = Regex.Replace(
                        Path.GetFileName(projectFile), "NNN", idxStr, RegexOptions.IgnoreCase);
                    var genProjectFilePath = Path.Combine(genProjectDirPath, genProjectFileName);

                    var genProjectFileText = File.ReadAllText(projectFile);
                    genProjectFileText = Regex.Replace(genProjectFileText,
                        "NNN", idxStr, RegexOptions.IgnoreCase);
                    genProjectFileText = Regex.Replace(genProjectFileText,
                        projectGuid, genProjectGuid, RegexOptions.IgnoreCase);
                    File.WriteAllText(genProjectFilePath, genProjectFileText);
                }
                var genProjectRef = Regex.Replace(
                    Regex.Replace(projectRef, "NNN", idxStr, RegexOptions.IgnoreCase),
                    projectGuid, genProjectGuid, RegexOptions.IgnoreCase);
                genSolutionProjectRef.Append(genProjectRef);
                foreach (var projectConfig in projectConfigsList) {
                    var genProjectConfig = Regex.Replace(projectConfig,
                        projectGuid, genProjectGuid, RegexOptions.IgnoreCase);
                    genSolutionProjectConfigs.Append(genProjectConfig);
                }
            }
        }

        public static void Generate(string outputPath, string templatePath, int projectCount)
        {
            var templateFiles = Directory.GetFiles(
                templatePath, "*", SearchOption.AllDirectories);
            var solutionFilePath = templateFiles
                .Where(x => Path.GetExtension(x) == ".sln")
                .First();
            var solutionName = Path.GetFileName(solutionFilePath);
            var solutionText = File.ReadAllText(solutionFilePath);
            var projectFilePaths = templateFiles
                .Where(x => Path.GetExtension(x) == ".vcxproj");
            var genSolutionText = solutionText;
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            foreach (var projectFilePath in projectFilePaths) {
                var genSolutionProjectRef = new StringBuilder();
                var genSolutionProjectConfigs = new StringBuilder();
                var projectText = File.ReadAllText(projectFilePath);
                var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
                var projectGuidMatch = Regex.Match(projectText, @"<ProjectGuid>({[^}]+})<");
                var projectGuid = projectGuidMatch.Groups[1].Value;
                var projectRef = Regex
                    .Match(solutionText,
                        @"^Project.*" + projectGuid + @"""\r\nEndProject\r\n",
                        RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    .Value;
                var projectConfigsList = Regex
                    .Matches(solutionText,
                        @"^\s+" + projectGuid + @".*\r\n",
                        RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Select(x => x.Value);
                var projectConfigs = string.Join("", projectConfigsList);
                GenerateProject(
                    genSolutionProjectRef,
                    genSolutionProjectConfigs,
                    templatePath,
                    outputPath,
                    projectCount,
                    projectGuid,
                    projectName,
                    projectRef,
                    projectConfigsList);
                genSolutionText = genSolutionText
                    .Replace(projectRef, genSolutionProjectRef.ToString())
                    .Replace(projectConfigs, genSolutionProjectConfigs.ToString());
            }
            var genSolutionFilePath = Path.Combine(outputPath, solutionName);
            File.WriteAllText(genSolutionFilePath, genSolutionText);
        }
    }
}
