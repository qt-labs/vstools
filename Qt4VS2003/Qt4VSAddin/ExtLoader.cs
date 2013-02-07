/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia. For licensing terms and
** conditions see http://qt.digia.com/licensing. For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights. These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using EnvDTE;
using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Collections;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using Digia.Qt5ProjectLib;

namespace Qt5VSAddin
// --------------------------------------------------------------------------------------
{
    public class ExtLoader
    {

        private struct DesignerData
        {
            public System.Diagnostics.Process process;
            public int port;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        
        private static Dictionary<string, DesignerData> designerDict
            = new Dictionary<string, DesignerData>();
        private static ManualResetEvent portFound = new ManualResetEvent(false);
        private static int designerPort = 0;

        // Functions ------------------------------------------------------
        public ExtLoader()
        {
        }

        public static void ImportProFile()
        {
            QtVersionManager vm = QtVersionManager.The();
            string qtVersion = vm.GetDefaultVersion();
            string qtDir = vm.GetInstallPath(qtVersion);
            if (qtDir == null)
            {
                Messages.DisplayErrorMessage(SR.GetString("CannotFindQMake"));
                return;
            }
#if (VS2010 || VS2012)
            VersionInformation vi = new VersionInformation(qtDir);
            if (vi.qtMajor == 4 && vi.qtMinor < 7)
            {
#if VS2010
                Messages.DisplayErrorMessage(SR.GetString("NoVS2010Support"));
#else
                Messages.DisplayErrorMessage(SR.GetString("NoVS2012Support"));
#endif
                return;
            }
#endif
            if (Connect._applicationObject != null)
            {
                ProjectImporter proFileImporter = new ProjectImporter(Connect._applicationObject);
                proFileImporter.ImportProFile(qtVersion);
            }
        }

        public static void ImportPriFile(EnvDTE.Project project)
        {
            VCProject vcproj;

            if (!HelperFunctions.IsQtProject(project))
                return;

            vcproj = project.Object as VCProject;
            if (vcproj == null)
                return;

            // make the user able to choose .pri file
            OpenFileDialog fd = new OpenFileDialog();
            fd.Multiselect = false;
            fd.CheckFileExists = true;
            fd.Title = SR.GetString("ExportProject_ImportPriFile");
            fd.Filter = "Project Include Files (*.pri)|*.pri";
            fd.FileName = vcproj.ProjectDirectory + vcproj.Name + ".pri";

            if (fd.ShowDialog() != DialogResult.OK)
                return;

            ImportPriFile(project, fd.FileName);
        }

        public static void ImportPriFile(EnvDTE.Project project, string fileName)
        {
            VCProject vcproj;

            if (!HelperFunctions.IsQtProject(project))
                return;

            vcproj = project.Object as VCProject;
            if (vcproj == null)
                return;

            QtVersionManager vm = QtVersionManager.The();
            string qtDir = vm.GetInstallPath(vm.GetDefaultVersion());
            if (qtDir == null)
            {
                Messages.DisplayErrorMessage(SR.GetString("CannotFindQMake"));
                return;
            }

            FileInfo priFileInfo = new FileInfo(fileName);

            QMakeWrapper qmake = new QMakeWrapper();
            qmake.setQtDir(qtDir);
            if (qmake.readFile(priFileInfo.FullName))
            {
                bool flat = qmake.isFlat();
                List<string> priFiles = ResolveFilesFromQMake(qmake.sourceFiles(), project, priFileInfo.DirectoryName);
                List<string> projFiles = HelperFunctions.GetProjectFiles(project, FilesToList.FL_CppFiles);
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
            }
            else
            {
                Messages.PaneMessage(project.DTE, "--- (Importing .pri file) file: "
                    + priFileInfo + " could not be read.");
            }
        }

        private static List<String> ResolveFilesFromQMake(System.Array files, EnvDTE.Project project, string path)
        {
            return ResolveFilesFromQMake(files as string[], project, path);
        }

        private static List<String> ResolveFilesFromQMake(string[] files, EnvDTE.Project project, string path)
        {
            List<string> lst = new List<string>();
            foreach (string file in files)
            {
                string s = ResolveEnvironmentVariables(file, project);
                if (s == null)
                {
                    Messages.PaneMessage(project.DTE, SR.GetString("ImportPriFileNotResolved", file));
                }
                else
                {
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
            Regex reg = new Regex(@"\$\(([^\s\(\)]+)\)");
            MatchCollection col = reg.Matches(str);
            for (int i = 0; i < col.Count; ++i)
            {
                env = col[i].Groups[1].ToString();
                if (env == "QTDIR")
                {
                    QtVersionManager vm = QtVersionManager.The();
                    val = vm.GetInstallPath(project);
                    if (val == null)
                        val = System.Environment.GetEnvironmentVariable(env);
                }
                else
                {
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
            if (Connect._applicationObject != null)
            {
                ProjectExporter proFileExporter = new ProjectExporter(Connect._applicationObject);
                proFileExporter.ExportToProFile();
            }
        }

        public static void ExportPriFile()
        {
            EnvDTE.DTE dte = Connect._applicationObject;
            if (dte != null)
            {
                ProjectExporter proFileExporter = new ProjectExporter(dte);
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

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            if (givenQtDir != null && givenQtDir.Length > 0)
            {
                process.StartInfo.FileName = givenQtDir + "\\bin\\" + applicationName;
                process.StartInfo.WorkingDirectory = workingDir;
            }
            if (!File.Exists(process.StartInfo.FileName)
                && HelperFunctions.GetSelectedQtProject(Connect._applicationObject) != null)
            {   // Try to find apllication in project's Qt dir first
                string path = null;
                QtVersionManager vm = QtVersionManager.The();
                Project prj = HelperFunctions.GetSelectedQtProject(Connect._applicationObject);
                if (prj != null)
                    path = vm.GetInstallPath(prj);
                if (path != null)
                {
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
                QtVersionManager vm = QtVersionManager.The();
                string qtDir = vm.GetInstallPath(vm.GetDefaultVersion());
                process.StartInfo.FileName = qtDir + "\\bin\\" + applicationName;
                process.StartInfo.WorkingDirectory = qtDir + "\\bin";
            }

            if (!File.Exists(process.StartInfo.FileName))
                return null;

            return process;
        }

        public void loadDesigner(string fileName)
        {
            Project prj = HelperFunctions.GetSelectedQtProject(Connect._applicationObject);
            string qtVersion = null;
            QtVersionManager vm = QtVersionManager.The();
            if (prj != null)
            {
                qtVersion = vm.GetProjectQtVersion(prj);
            }
            else
            {
                prj = HelperFunctions.GetSelectedProject(Connect._applicationObject);
                if (prj != null && HelperFunctions.IsQMakeProject(prj)) {
                    string qmakeQtDir = HelperFunctions.GetQtDirFromQMakeProject(prj);
                    qtVersion = vm.GetQtVersionFromInstallDir(qmakeQtDir);
                }
            }
            string qtDir = HelperFunctions.FindQtDirWithTools("designer", qtVersion);
            if (qtDir == null || qtDir.Length == 0)
            {
                MessageBox.Show(SR.GetString("NoDefaultQtVersionError"),
                                Resources.msgBoxCaption);
                return;
            }

            try
            {
                if (!designerDict.ContainsKey(qtDir) || designerDict[qtDir].process.HasExited)
                {
                    string workingDir, formFile;
                    if (fileName == null)
                    {
                        formFile = "";
                        workingDir = (prj == null) ? null : Path.GetDirectoryName(prj.FullName);
                    }
                    else
                    {
                        formFile = fileName;
                        workingDir = Path.GetDirectoryName(fileName);
                        if (!formFile.StartsWith("\""))
                        {
                            formFile = "\"" + formFile;
                        }
                        if (!formFile.EndsWith("\""))
                        {
                            formFile += "\"";
                        }
                    }

                    string launchCMD = "-server " + formFile;
                    System.Diagnostics.Process tmp = getQtApplicationProcess("designer", launchCMD, workingDir, qtDir);
                    tmp.StartInfo.UseShellExecute = false;
                    tmp.StartInfo.RedirectStandardOutput = true;
                    tmp.OutputDataReceived += new DataReceivedEventHandler(designerOutputHandler);
                    tmp.Start();
                    tmp.BeginOutputReadLine();
                    try
                    {
                        portFound.WaitOne(5000, false);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                    tmp.WaitForInputIdle();
                    DesignerData data;
                    data.process = tmp;
                    data.port = designerPort;
                    portFound.Reset();
                    designerDict[qtDir] = data;
                }
                else if (fileName != null)
                {
                    try
                    {
                        TcpClient c = new TcpClient("127.0.0.1", designerDict[qtDir].port);
                        System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
                        byte[] bArray = enc.GetBytes(fileName + "\n");
                        Stream stream = c.GetStream();
                        stream.Write(bArray, 0, bArray.Length);
                        c.Close();
                        stream.Close();
                    }
                    catch
                    {
                        Messages.DisplayErrorMessage(SR.GetString("DesignerAddError"));
                    }
                }
            }
            catch
            {
                MessageBox.Show(SR.GetString("QtAppNotFoundErrorMessage", "Qt Designer"),
                    SR.GetString("QtAppNotFoundErrorTitle", "Designer"));
                return;
            }
            try
            {
                if ((int)designerDict[qtDir].process.MainWindowHandle == 0)
                {
                    System.Diagnostics.Process prc = System.Diagnostics.Process.GetProcessById(designerDict[qtDir].process.Id);
                    if ((int)prc.MainWindowHandle != 0)
                    {
                        DesignerData data;
                        data.process = prc;
                        data.port = designerDict[qtDir].port;
                        designerDict[qtDir] = data;
                    }
                }
                SwitchToThisWindow(designerDict[qtDir].process.MainWindowHandle, true);
            }
            catch
            {
                // silent
            }
        }

        private void designerOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    designerPort = Convert.ToInt32(outLine.Data);
                    System.Diagnostics.Process tmp = sendingProcess as System.Diagnostics.Process;
                    tmp.CancelOutputRead();
                    portFound.Set();
                }
                catch { }
            }
        }

        public static void loadLinguist(string fileName)
        {
            Project prj = HelperFunctions.GetSelectedQtProject(Connect._applicationObject);
            string qtVersion = null;
            QtVersionManager vm = QtVersionManager.The();
            if (prj != null)
            {
                qtVersion = vm.GetProjectQtVersion(prj);
            }
            else
            {
                prj = HelperFunctions.GetSelectedProject(Connect._applicationObject);
                if (prj != null && HelperFunctions.IsQMakeProject(prj)) {
                    string qmakeQtDir = HelperFunctions.GetQtDirFromQMakeProject(prj);
                    qtVersion = vm.GetQtVersionFromInstallDir(qmakeQtDir);
                }
            }
            string qtDir = HelperFunctions.FindQtDirWithTools("linguist", qtVersion);
            if (qtDir == null || qtDir.Length == 0)
            {
                MessageBox.Show(SR.GetString("NoDefaultQtVersionError"),
                                Resources.msgBoxCaption);
                return;
            }

            try
            {
                string workingDir = null;
                string arguments = null;
                if (fileName != null)
                {
                    workingDir = Path.GetDirectoryName(fileName);
                    arguments = fileName;
                    if (!arguments.StartsWith("\""))
                    {
                        arguments = "\"" + arguments;
                    }
                    if (!arguments.EndsWith("\""))
                    {
                        arguments += "\"";
                    }
                }

                System.Diagnostics.Process tmp = getQtApplicationProcess("linguist", arguments, workingDir, qtDir);
                tmp.Start();
            }
            catch
            {
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
            string filename = Connect.Instance().InstallationDir;
            int idx = filename.Length - 1;
            if (filename.EndsWith("\\")) idx--;
            idx = filename.LastIndexOf('\\', idx);
            if (idx > -1)
                filename = filename.Substring(0, idx + 1);
            filename += "q5rceditor.exe";

            System.Diagnostics.Process tmp = null;
            try
            {
                if (!File.Exists(filename))
                    filename = Connect.Instance().InstallationDir + "q5rceditor.exe";

                tmp = new System.Diagnostics.Process();
                Project prj = HelperFunctions.GetSelectedProject(Connect._applicationObject);
                tmp.StartInfo.FileName = filename;
                tmp.StartInfo.Arguments = resourceFile;
                tmp.StartInfo.WorkingDirectory = Path.GetFullPath(prj.FullName);
                tmp.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                tmp.Start();
            }
            catch {
                tmp = null;
            }

            if (tmp == null)
            {
                MessageBox.Show(SR.GetString("QrcEditorNotFoundErrorMessage"),
                         SR.GetString("QtAppNotFoundErrorTitle", "QrcEditor"));
            }
        }
    }
}
