/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

using Microsoft.VisualStudio.VCProjectEngine;
using QtProjectLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace QtVsTools
{
    /// <summary>
    /// Summary description for Translation.
    /// </summary>
    public static class Translation
    {
        public static bool RunlRelease(VCFile vcFile)
        {
            if (vcFile == null)
                return false;

            var success = true;
            try {
                var vcProject = vcFile.project as VCProject;
                var cmdLine = string.Empty;
                if (HelperFunctions.IsQtProject(vcProject)) {
                    var options = QtVSIPSettings.GetLReleaseOptions();
                    if (!string.IsNullOrEmpty(options))
                        cmdLine += options + " ";
                }
                var project = vcProject.Object as EnvDTE.Project;
                Messages.PaneMessage(project.DTE,
                    "--- (lrelease) file: " + vcFile.FullPath);

                cmdLine += vcFile.RelativePath.Quoute();
                StartProcess(Resources.lreleaseCommand, cmdLine, vcProject.ProjectDirectory,
                    HelperFunctions.GetSelectedQtProject(project.DTE));
            } catch (QtVSException e) {
                success = false;
                Messages.DisplayErrorMessage(e.Message);
            }

            return success;
        }

        public static void RunlRelease(VCFile[] vcFiles)
        {
            if (vcFiles == null)
                return;

            foreach (var vcFile in vcFiles) {
                if (HelperFunctions.IsTranslationFile(vcFile)) {
                    if (!RunlRelease(vcFile))
                        return;
                }
            }
        }

        public static void RunlRelease(EnvDTE.Project project)
        {
            var qtPro = QtProject.Create(project);
            if (qtPro == null)
                return;

            var ts = Filters.TranslationFiles();
            var tsFilter = qtPro.FindFilterFromGuid(ts.UniqueIdentifier);
            if (tsFilter == null)
                return;

            var files = tsFilter.Files as IVCCollection;
            foreach (VCFile vcFile in files) {
                if (HelperFunctions.IsTranslationFile(vcFile)) {
                    if (!RunlRelease(vcFile))
                        return;
                }
            }
        }

        public static void RunlRelease(EnvDTE.Solution solution)
        {
            if (solution == null)
                return;

            foreach (var project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunlRelease(project);
        }

        public static bool RunlUpdate(VCFile vcFile, EnvDTE.Project pro)
        {
            if (vcFile == null || pro == null)
                return false;

            if (!HelperFunctions.IsQtProject(pro))
                return false;

            var cmdLine = string.Empty;
            var options = QtVSIPSettings.GetLUpdateOptions(pro);
            if (!string.IsNullOrEmpty(options))
                cmdLine += options + " ";
            var headers = HelperFunctions.GetProjectFiles(pro, FilesToList.FL_HFiles);
            var sources = HelperFunctions.GetProjectFiles(pro, FilesToList.FL_CppFiles);
            var uifiles = HelperFunctions.GetProjectFiles(pro, FilesToList.FL_UiFiles);

            foreach (var file in headers)
                cmdLine += file + " ";

            foreach (var file in sources)
                cmdLine += file + " ";

            foreach (var file in uifiles)
                cmdLine += file + " ";

            cmdLine += "-ts " + vcFile.RelativePath.Quoute();

            var cmdLineLength = cmdLine.Length + Resources.lupdateCommand.Length + 1;
            string temporaryProFile = null;
            if (cmdLineLength > HelperFunctions.GetMaximumCommandLineLength()) {
                var codec = string.Empty;
                if (!string.IsNullOrEmpty(options)) {
                    var cc4tr_location = options.IndexOf("-codecfortr", System.StringComparison.CurrentCultureIgnoreCase);
                    if (cc4tr_location != -1) {
                        codec = options.Substring(cc4tr_location).Split(' ')[1];
                        var remove_this = options.Substring(cc4tr_location, "-codecfortr".Length + 1 + codec.Length);
                        options = options.Replace(remove_this, "");
                    }
                }
                var vcPro = (VCProject) pro.Object;
                temporaryProFile = Path.GetTempFileName();
                temporaryProFile = Path.GetDirectoryName(temporaryProFile) + "\\"
                    + Path.GetFileNameWithoutExtension(temporaryProFile) + ".pro";
                if (File.Exists(temporaryProFile))
                    File.Delete(temporaryProFile);

                using (var sw = new StreamWriter(temporaryProFile)) {
                    writeFilesToPro(sw, "HEADERS",
                        ProjectExporter.ConvertFilesToFullPath(headers, vcPro.ProjectDirectory));
                    writeFilesToPro(sw, "SOURCES",
                        ProjectExporter.ConvertFilesToFullPath(sources, vcPro.ProjectDirectory));
                    writeFilesToPro(sw, "FORMS",
                        ProjectExporter.ConvertFilesToFullPath(uifiles, vcPro.ProjectDirectory));

                    var tsFiles = new List<string>(1);
                    tsFiles.Add(vcFile.FullPath);
                    writeFilesToPro(sw, "TRANSLATIONS", tsFiles);

                    if (!string.IsNullOrEmpty(codec))
                        sw.WriteLine("CODECFORTR = " + codec);
                }

                cmdLine = string.Empty;
                if (!string.IsNullOrEmpty(options))
                    cmdLine += options + " ";
                cmdLine += "\"" + temporaryProFile + "\"";
            }

            var success = true;
            try {
                Messages.PaneMessage(pro.DTE, "--- (lupdate) file: " + vcFile.FullPath);
                StartProcess(Resources.lupdateCommand, cmdLine, (vcFile.project as VCProject)
                    .ProjectDirectory, pro);
            } catch (QtVSException e) {
                success = false;
                Messages.DisplayErrorMessage(e.Message);
            }

            if (temporaryProFile != null && File.Exists(temporaryProFile)) {
                File.Delete(temporaryProFile);
                temporaryProFile = temporaryProFile.Substring(0, temporaryProFile.Length - 3);
                temporaryProFile += "TMP";
                if (File.Exists(temporaryProFile))
                    File.Delete(temporaryProFile);
            }

            return success;
        }

        private static void writeFilesToPro(StreamWriter pro, string section, List<string> files)
        {
            if (files.Count > 0) {
                pro.Write(section + " = ");
                foreach (var file in files) {
                    pro.WriteLine("\\");
                    pro.Write("\"" + file + "\"");
                }
                pro.WriteLine();
            }
        }

        public static void RunlUpdate(VCFile[] vcFiles, EnvDTE.Project pro)
        {
            if (vcFiles == null)
                return;

            foreach (var vcFile in vcFiles) {
                if (HelperFunctions.IsTranslationFile(vcFile)) {
                    if (!RunlUpdate(vcFile, pro))
                        return;
                }
            }
        }

        public static void RunlUpdate(EnvDTE.Project project)
        {
            var qtPro = QtProject.Create(project);
            if (qtPro == null)
                return;

            var ts = Filters.TranslationFiles();
            var tsFilter = qtPro.FindFilterFromGuid(ts.UniqueIdentifier);
            if (tsFilter == null)
                return;

            var files = tsFilter.Files as IVCCollection;
            foreach (VCFile vcFile in files) {
                if (HelperFunctions.IsTranslationFile(vcFile)) {
                    if (!RunlUpdate(vcFile, project))
                        return;
                }
            }
        }

        public static void RunlUpdate(EnvDTE.Solution solution)
        {
            if (solution == null)
                return;

            foreach (var project in HelperFunctions.ProjectsInSolution(solution.DTE))
                RunlUpdate(project);
        }

        public static void CreateNewTranslationFile(EnvDTE.Project project)
        {
            if (project == null)
                return;

            using (var transDlg = new AddTranslationDialog(project)) {
                if (transDlg.ShowDialog() == DialogResult.OK) {
                    try {
                        var qtPro = QtProject.Create(project);
                        var file = qtPro.AddFileInFilter(Filters.TranslationFiles(),
                            transDlg.TranslationFile, true);
                        RunlUpdate(file, project);
                    } catch (QtVSException e) {
                        Messages.DisplayErrorMessage(e.Message);
                    } catch (System.Exception ex) {
                        Messages.DisplayErrorMessage(ex.Message);
                    }
                }
            }
        }

        internal sealed class ThreadParameter
        {
            public EnvDTE.DTE Dte { get; set; }
            public Process Process { get; set; }
        }

        public static void StartProcess(string fileName, string arguments, string workingDirectory,
            EnvDTE.Project project)
        {
            Process process = null;
            try {
                Messages.ActivateMessagePane();

                process = new Process();
                process.EnableRaisingEvents = true;
                process.StartInfo = new ProcessStartInfo
                {
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    FileName = HelperFunctions.FindQtDirWithTools(project) + fileName,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                process.Start();
                var thread = new Thread(ReadQtStandardError);
                thread.Start(new ThreadParameter
                {
                    Dte = project.DTE,
                    Process = process
                });
                process.WaitForExit();
                thread.Join();

                if (process.ExitCode == 0) {
                    var index = arguments.IndexOf("-ts", System.StringComparison.OrdinalIgnoreCase);
                    var file = "file: " + arguments + " ";
                    if (index > 0)
                        file = "file: " + arguments.Substring(index + 3) + " ";

                    Messages.PaneMessage(project.DTE,
                        "--- (" + Path.GetFileNameWithoutExtension(fileName) + ") " + file
                        + ": Exit Code: " + process.ExitCode);
                } else {
                    DisplayErrorMessage(process);
                }
            } catch {
                throw new QtVSException(SR.GetString("Helpers_CannotStart", process.StartInfo.FileName));
            } finally {
                if (process != null)
                    process.Dispose();
            }
        }

        private static void ReadQtStandardError(object obj)
        {
            var tp = obj as ThreadParameter;
            if (tp == null)
                return;

            var error = string.Empty;
            while ((error = tp.Process.StandardError.ReadLine()) != null) {
                error = error.Trim();
                if (error.Length != 0)
                    Messages.PaneMessage(tp.Dte, "--- " + error);
            }
        }

        private static void DisplayErrorMessage(Process proccess)
        {
            if (proccess.ExitCode != 0) {
                Messages.DisplayErrorMessage(SR.GetString("Helpers_ExitError", proccess.ExitCode
                    .ToString()) + "\r\n" + SR.GetString("QProcess_UnspecifiedError"));
            }
        }
    }
}
