﻿/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace generator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var projectCount = args.Length > 0 ? Convert.ToInt32(args[0]) : 400;
            var pathToTemplateDir = Path.GetFullPath(@"..\..\..\template");
            var pathToGeneratedDir = Path.GetFullPath(@"..\..\..\generated");
            var templateFiles = Directory.GetFiles(
                pathToTemplateDir, "*", SearchOption.AllDirectories);
            var solutionFilePath = templateFiles
                .Where(x => Path.GetExtension(x) == ".sln")
                .First();
            var solutionName = Path.GetFileName(solutionFilePath);
            var solutionText = File.ReadAllText(solutionFilePath);
            var projectFilePath = templateFiles
                .Where(x => Path.GetExtension(x) == ".vcxproj")
                .First();
            var projectText = File.ReadAllText(projectFilePath);
            var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            var projectGuidMatch = Regex.Match(projectText, @"<ProjectGuid>({[^}]+})<");
            var projectGuid = projectGuidMatch.Groups[1].Value;

            var projectRef = Regex
                .Match(solutionText,
                    @"^Project.*" + projectGuid + @"""\r\nEndProject\r\n", RegexOptions.Multiline)
                .Value;

            var projectConfigsList = Regex
                .Matches(solutionText,
                    @"^\s+" + projectGuid + @".*\r\n", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(x => x.Value);
            var projectConfigs = string.Join("", projectConfigsList);

            var projectFiles = Directory.GetFiles(
                Path.Combine(pathToTemplateDir, projectName),
                "*", SearchOption.TopDirectoryOnly);

            if (Directory.Exists(pathToGeneratedDir))
                Directory.Delete(pathToGeneratedDir, true);

            var genSolutionProjectRef = new StringBuilder();
            var genSolutionProjectConfigs = new StringBuilder();

            for (int i = 1; i <= projectCount; i++) {
                var idxStr = string.Format("{0:D3}", i);
                var genProjectName = projectName.Replace("NNN", idxStr);
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
            var genSolutionText = solutionText
                .Replace(projectRef, genSolutionProjectRef.ToString())
                .Replace(projectConfigs, genSolutionProjectConfigs.ToString());
            var genSolutionFilePath = Path.Combine(pathToGeneratedDir, solutionName);
            File.WriteAllText(genSolutionFilePath, genSolutionText);
        }
    }
}