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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace QtVsTools
{
    internal static class NativeMethods
    {
        [ResourceExposure(ResourceScope.None)]
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern void SwitchToThisWindow(IntPtr hWnd,
            [MarshalAs(UnmanagedType.Bool)] bool fAltTab);
    }

    public class ExtLoader
    {
        private struct DesignerData
        {
            public System.Diagnostics.Process process;
            public int port;
        }

        private static Dictionary<string, DesignerData> designerDict
            = new Dictionary<string, DesignerData>();
        private static ManualResetEvent portFound = new ManualResetEvent(false);
        private static int designerPort = 0;

        public static void ImportProFile()
        {
            var vm = QtVersionManager.The();
            var qtVersion = vm.GetDefaultVersion();
            var qtDir = vm.GetInstallPath(qtVersion);
            if (qtDir == null) {
                Messages.DisplayErrorMessage(SR.GetString("CannotFindQMake"));
                return;
            }
            var vi = new VersionInformation(qtDir);
            if (vi.qtMajor < 5) {
                Messages.DisplayErrorMessage(SR.GetString("NoVSSupport"));
                return;
            }
            if (Vsix.Instance.Dte != null) {
                var proFileImporter = new ProjectImporter(Vsix.Instance.Dte);
                proFileImporter.ImportProFile(qtVersion);
            }
        }

        public static void ImportPriFile(EnvDTE.Project project)
        {
            if (project == null)
                return;

            VCProject vcproj;
            if (!HelperFunctions.IsQtProject(project))
                return;

            vcproj = project.Object as VCProject;
            if (vcproj == null)
                return;

            // make the user able to choose .pri file
            using (var fd = new OpenFileDialog()) {
                fd.Multiselect = false;
                fd.CheckFileExists = true;
                fd.Title = SR.GetString("ExportProject_ImportPriFile");
                fd.Filter = "Project Include Files (*.pri)|*.pri";
                fd.FileName = vcproj.ProjectDirectory + vcproj.Name + ".pri";

                if (fd.ShowDialog() != DialogResult.OK)
                    return;

                ImportPriFile(project, fd.FileName);
            }
        }

        public static void ImportPriFile(EnvDTE.Project project, string fileName)
        {
            if (project == null)
                return;

            VCProject vcproj;
            if (!HelperFunctions.IsQtProject(project))
                return;

            vcproj = project.Object as VCProject;
            if (vcproj == null)
                return;

            var vm = QtVersionManager.The();
            var qtDir = vm.GetInstallPath(vm.GetDefaultVersion());
            if (qtDir == null) {
                Messages.DisplayErrorMessage(SR.GetString("CannotFindQMake"));
                return;
            }

            var priFileInfo = new FileInfo(fileName);

            var qmake = new QMakeWrapper();
            qmake.setQtDir(qtDir);
            if (qmake.readFile(priFileInfo.FullName)) {
                var flat = qmake.isFlat();
                var priFiles = ResolveFilesFromQMake(qmake.sourceFiles(), project, priFileInfo.DirectoryName);
                var projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_CppFiles);
                projFiles = ProjectExporter.ConvertFilesToFullPath(projFiles, vcproj.ProjectDirectory);
                ProjectExporter.SyncIncludeFiles(vcproj, priFiles, projFiles, project.DTE, flat, Filters.SourceFiles());

                priFiles = ResolveFilesFromQMake(qmake.headerFiles(), project, priFileInfo.DirectoryName);
                projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_HFiles);
                projFiles = ProjectExporter.ConvertFilesToFullPath(projFiles, vcproj.ProjectDirectory);
                ProjectExporter.SyncIncludeFiles(vcproj, priFiles, projFiles, project.DTE, flat, Filters.HeaderFiles());

                priFiles = ResolveFilesFromQMake(qmake.formFiles(), project, priFileInfo.DirectoryName);
                projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_UiFiles);
                projFiles = ProjectExporter.ConvertFilesToFullPath(projFiles, vcproj.ProjectDirectory);
                ProjectExporter.SyncIncludeFiles(vcproj, priFiles, projFiles, project.DTE, flat, Filters.FormFiles());

                priFiles = ResolveFilesFromQMake(qmake.resourceFiles(), project, priFileInfo.DirectoryName);
                projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_Resources);
                projFiles = ProjectExporter.ConvertFilesToFullPath(projFiles, vcproj.ProjectDirectory);
                ProjectExporter.SyncIncludeFiles(vcproj, priFiles, projFiles, project.DTE, flat, Filters.ResourceFiles());
            } else {
                Messages.PaneMessage(project.DTE, "--- (Importing .pri file) file: "
                    + priFileInfo + " could not be read.");
            }
        }

        private static List<string> ResolveFilesFromQMake(string[] files, EnvDTE.Project project, string path)
        {
            var lst = new List<string>();
            foreach (string file in files) {
                var s = ResolveEnvironmentVariables(file, project);
                if (s == null) {
                    Messages.PaneMessage(project.DTE, SR.GetString("ImportPriFileNotResolved", file));
                } else {
                    if (!HelperFunctions.IsAbsoluteFilePath(s))
                        s = path + "\\" + s;
                    lst.Add(s);
                }
            }
            return lst;
        }

        private static string ResolveEnvironmentVariables(string str, EnvDTE.Project project)
        {
            string env = null;
            string val = null;
            var reg = new Regex(@"\$\(([^\s\(\)]+)\)");
            var col = reg.Matches(str);
            for (int i = 0; i < col.Count; ++i) {
                env = col[i].Groups[1].ToString();
                if (env == "QTDIR") {
                    var vm = QtVersionManager.The();
                    val = vm.GetInstallPath(project);
                    if (val == null)
                        val = System.Environment.GetEnvironmentVariable(env);
                } else {
                    val = System.Environment.GetEnvironmentVariable(env);
                }
                if (val == null)
                    return null;
                str = str.Replace("$(" + env + ")", val);
            }
            return str;
        }

        public static void ExportProFile()
        {
            if (Vsix.Instance.Dte != null) {
                var proFileExporter = new ProjectExporter(Vsix.Instance.Dte);
                proFileExporter.ExportToProFile();
            }
        }

        public static void ExportPriFile()
        {
            EnvDTE.DTE dte = Vsix.Instance.Dte;
            if (dte != null) {
                var proFileExporter = new ProjectExporter(dte);
                proFileExporter.ExportToPriFile(HelperFunctions.GetSelectedQtProject
                    (dte));
            }
        }

        private static System.Diagnostics.Process getQtApplicationProcess(string applicationName,
                                                              string arguments,
                                                              string workingDir,
                                                              string givenQtDir)
        {
            if (!applicationName.ToLower().EndsWith(".exe"))
                applicationName += ".exe";

            var process = new System.Diagnostics.Process();
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            if (givenQtDir != null && givenQtDir.Length > 0) {
                process.StartInfo.FileName = givenQtDir + "\\bin\\" + applicationName;
                process.StartInfo.WorkingDirectory = workingDir;
            }
            if (!File.Exists(process.StartInfo.FileName)
                && HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte) != null) {   // Try to find apllication in project's Qt dir first
                string path = null;
                var vm = QtVersionManager.The();
                var prj = HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte);
                if (prj != null)
                    path = vm.GetInstallPath(prj);
                if (path != null) {
                    process.StartInfo.FileName = path + "\\bin\\" + applicationName;
                    process.StartInfo.WorkingDirectory = workingDir;
                }
            }

            if (!File.Exists(process.StartInfo.FileName)) // Try with Path
            {
                process.StartInfo.FileName = HelperFunctions.FindFileInPATH(applicationName);
                if (workingDir != null)
                    process.StartInfo.WorkingDirectory = workingDir;
            }

            if (!File.Exists(process.StartInfo.FileName)) // try to start application of the default Qt version
            {
                var vm = QtVersionManager.The();
                var qtDir = vm.GetInstallPath(vm.GetDefaultVersion());
                process.StartInfo.FileName = qtDir + "\\bin\\" + applicationName;
                process.StartInfo.WorkingDirectory = qtDir + "\\bin";
            }

            if (!File.Exists(process.StartInfo.FileName))
                return null;

            return process;
        }

        public void loadDesigner(string fileName)
        {
            var prj = HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte);
            string qtVersion = null;
            var vm = QtVersionManager.The();
            if (prj != null) {
                qtVersion = vm.GetProjectQtVersion(prj);
            } else {
                prj = HelperFunctions.GetSelectedProject(Vsix.Instance.Dte);
                if (prj != null && HelperFunctions.IsQMakeProject(prj)) {
                    var qmakeQtDir = HelperFunctions.GetQtDirFromQMakeProject(prj);
                    qtVersion = vm.GetQtVersionFromInstallDir(qmakeQtDir);
                }
            }
            var qtDir = HelperFunctions.FindQtDirWithTools("designer", qtVersion);
            if (qtDir == null || qtDir.Length == 0) {
                MessageBox.Show(SR.GetString("NoDefaultQtVersionError"),
                                Resources.msgBoxCaption);
                return;
            }

            try {
                if (!designerDict.ContainsKey(qtDir) || designerDict[qtDir].process.HasExited) {
                    string workingDir, formFile;
                    if (fileName == null) {
                        formFile = "";
                        workingDir = (prj == null) ? null : Path.GetDirectoryName(prj.FullName);
                    } else {
                        formFile = fileName;
                        workingDir = Path.GetDirectoryName(fileName);
                        if (!formFile.StartsWith("\"")) {
                            formFile = "\"" + formFile;
                        }
                        if (!formFile.EndsWith("\"")) {
                            formFile += "\"";
                        }
                    }

                    string launchCMD = "-server " + formFile;
                    var tmp = getQtApplicationProcess("designer", launchCMD, workingDir, qtDir);
                    tmp.StartInfo.UseShellExecute = false;
                    tmp.StartInfo.RedirectStandardOutput = true;
                    tmp.OutputDataReceived += designerOutputHandler;
                    tmp.Start();
                    tmp.BeginOutputReadLine();
                    try {
                        portFound.WaitOne(5000, false);
                    } catch (Exception e) {
                        MessageBox.Show(e.Message);
                    }
                    tmp.WaitForInputIdle();
                    DesignerData data;
                    data.process = tmp;
                    data.port = designerPort;
                    portFound.Reset();
                    designerDict[qtDir] = data;
                } else if (fileName != null) {
                    try {
                        using (var c = new TcpClient("127.0.0.1", designerDict[qtDir].port)) {
                            var enc = new System.Text.UTF8Encoding();
                            var bArray = enc.GetBytes(fileName + "\n");
                            Stream stream = c.GetStream();
                            stream.Write(bArray, 0, bArray.Length);
                            stream.Close();
                        }
                    } catch {
                        Messages.DisplayErrorMessage(SR.GetString("DesignerAddError"));
                    }
                }
            } catch {
                MessageBox.Show(SR.GetString("QtAppNotFoundErrorMessage", "Qt Designer"),
                    SR.GetString("QtAppNotFoundErrorTitle", "Designer"));
                return;
            }
            try {
                if ((int) designerDict[qtDir].process.MainWindowHandle == 0) {
                    var prc = System.Diagnostics.Process.GetProcessById(designerDict[qtDir].process.Id);
                    if ((int) prc.MainWindowHandle != 0) {
                        DesignerData data;
                        data.process = prc;
                        data.port = designerDict[qtDir].port;
                        designerDict[qtDir] = data;
                    }
                }
                NativeMethods.SwitchToThisWindow(designerDict[qtDir].process.MainWindowHandle, true);
            } catch {
                // silent
            }
        }

        private void designerOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data)) {
                try {
                    designerPort = Convert.ToInt32(outLine.Data);
                    var tmp = sendingProcess as System.Diagnostics.Process;
                    tmp.CancelOutputRead();
                    portFound.Set();
                } catch { }
            }
        }

        public static void loadLinguist(string fileName)
        {
            var prj = HelperFunctions.GetSelectedQtProject(Vsix.Instance.Dte);
            string qtVersion = null;
            var vm = QtVersionManager.The();
            if (prj != null) {
                qtVersion = vm.GetProjectQtVersion(prj);
            } else {
                prj = HelperFunctions.GetSelectedProject(Vsix.Instance.Dte);
                if (prj != null && HelperFunctions.IsQMakeProject(prj)) {
                    var qmakeQtDir = HelperFunctions.GetQtDirFromQMakeProject(prj);
                    qtVersion = vm.GetQtVersionFromInstallDir(qmakeQtDir);
                }
            }
            var qtDir = HelperFunctions.FindQtDirWithTools("linguist", qtVersion);
            if (qtDir == null || qtDir.Length == 0) {
                MessageBox.Show(SR.GetString("NoDefaultQtVersionError"),
                                Resources.msgBoxCaption);
                return;
            }

            try {
                string workingDir = null;
                string arguments = null;
                if (fileName != null) {
                    workingDir = Path.GetDirectoryName(fileName);
                    arguments = fileName;
                    if (!arguments.StartsWith("\"")) {
                        arguments = "\"" + arguments;
                    }
                    if (!arguments.EndsWith("\"")) {
                        arguments += "\"";
                    }
                }

                var tmp = getQtApplicationProcess("linguist", arguments, workingDir, qtDir);
                tmp.Start();
            } catch {
                MessageBox.Show(SR.GetString("QtAppNotFoundErrorMessage", "Qt Linguist"),
                    SR.GetString("QtAppNotFoundErrorTitle", "Linguist"));
            }
        }

        public static void loadQrcEditor(string file)
        {
            string resourceFile = null;
            if (file != null && file.Length != 0 && file.ToLower().EndsWith(".qrc"))
                resourceFile = "\"" + file + "\"";

            // locate qrceditor.exe in the parent directory of the installation directory
            string filename = Vsix.Instance.PkgInstallPath;
            int idx = filename.Length - 1;
            if (filename.EndsWith("\\")) idx--;
            idx = filename.LastIndexOf('\\', idx);
            if (idx > -1)
                filename = filename.Substring(0, idx + 1);
            filename += "QrcEditor.exe";

            System.Diagnostics.Process tmp = null;
            try {
                if (!File.Exists(filename))
                    filename = Vsix.Instance.PkgInstallPath + "QrcEditor.exe";

                tmp = new System.Diagnostics.Process();
                var prj = HelperFunctions.GetSelectedProject(Vsix.Instance.Dte);
                tmp.StartInfo.FileName = filename;
                tmp.StartInfo.Arguments = resourceFile;
                tmp.StartInfo.WorkingDirectory = Path.GetFullPath(prj.FullName);
                tmp.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                tmp.Start();
            } catch {
                tmp = null;
            }

            if (tmp == null) {
                MessageBox.Show(SR.GetString("QrcEditorNotFoundErrorMessage"),
                         SR.GetString("QtAppNotFoundErrorTitle", "QrcEditor"));
            }
        }
    }
}
