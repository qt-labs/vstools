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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Legacy
{
    using Core;

    using static Core.HelperFunctions;
    using BuildAction = QtVsTools.Translation.BuildAction;
    using Legacy = Core.Legacy;

    internal static class Translation
    {
        internal static void Run(BuildAction buildAction, QtProject qtProject,
            IEnumerable<string> tsFiles)
        {
            var qtInstallPath = QtVersionManager.The().GetInstallPath(qtProject.GetQtVersion());
            if (string.IsNullOrEmpty(qtInstallPath)) {
                Messages.Print("translation: Error accessing Qt installation");
                return;
            }

            if (tsFiles == null) {
                tsFiles = (qtProject.VCProject
                    .GetFilesEndingWith(".ts") as IVCCollection)
                    .Cast<VCFile>()
                    .Select(vcFile => vcFile.RelativePath);
            }

            if (tsFiles != null) {
                var project = qtProject.Project;
                var tempFile = Path.GetTempFileName();

                File.WriteAllLines(tempFile, GetProjectFiles(project, FilesToList.FL_HFiles)
                    .Union(GetProjectFiles(project, FilesToList.FL_CppFiles))
                    .Union(GetProjectFiles(project, FilesToList.FL_UiFiles))
                    .Union(GetProjectFiles(project, FilesToList.FL_QmlFiles)));

                var procInfo = new ProcessStartInfo
                {
                    WorkingDirectory = qtProject.ProjectDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    Arguments = ""
                };
                procInfo.FileName = Path.Combine(qtInstallPath, "bin",
                    buildAction == BuildAction.Update ? "lupdate.exe" : "lrelease.exe");

                foreach (var file in tsFiles.Where(file => file != null))
                    Run(buildAction, file, tempFile, procInfo);
            } else {
                Messages.Print("translation: No translation files found");
            }
        }

        private static void Run(BuildAction buildAction, string tsFile, string tempFile,
            ProcessStartInfo procInfo)
        {
            switch (buildAction) {
            case BuildAction.Update:
                Messages.Print("\r\n--- (lupdate) file: " + tsFile);
                var options = Legacy.QtVSIPSettings.GetLUpdateOptions();
                if (!string.IsNullOrEmpty(options))
                    procInfo.Arguments += options + " ";
                procInfo.Arguments += string.Format("\"@{0}\" -ts \"{1}\"", tempFile, tsFile);
                break;
            case BuildAction.Release:
                Messages.Print("\r\n--- (lrelease) file: " + tsFile);
                options = Legacy.QtVSIPSettings.GetLReleaseOptions();
                if (!string.IsNullOrEmpty(options))
                    procInfo.Arguments += options + " ";
                procInfo.Arguments += string.Format("\"{0}\"", tsFile);
                break;
            }

            using (var proc = Process.Start(procInfo)) {
                proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Messages.Print(e.Data);
                };
                proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Messages.Print(e.Data);
                };
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
                switch (proc.ExitCode) {
                case 0:
                    Messages.Print("translation: ok");
                    break;
                default:
                    Messages.Print(string.Format("translation: ERROR {0}", proc.ExitCode));
                    break;
                }
            }
        }
    }
}
