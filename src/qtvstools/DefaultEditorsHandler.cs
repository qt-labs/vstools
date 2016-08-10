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

using QtProjectLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;

namespace QtVsTools
{
    static class DefaultEditor
    {
        public enum Kind
        {
            Ts,
            Ui
        }
    }

    class DefaultEditorsHandler
    {
        public static DefaultEditorsHandler Instance
        {
            get; private set;
        }

        public static void Initialize(EnvDTE.DTE dte)
        {
            Instance = new DefaultEditorsHandler(dte);
        }

        public void StartEditor(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            bool abortOperation;
            CheckoutFileIfNeeded(fileName, out abortOperation);
            if (abortOperation)
                return;

            switch (Path.GetExtension(fileName).ToUpperInvariant()) {
            case ".TS":
                StartLinguist(fileName);
                break;
            case ".UI":
                StartDesigner(fileName);
                Thread.Sleep(1000); // Designer can't cope with many files in a short time.
                break;
            }
        }

        public void StartEditor(DefaultEditor.Kind type)
        {
            switch (type) {
            case DefaultEditor.Kind.Ts:
                StartLinguist(string.Empty);
                break;
            case DefaultEditor.Kind.Ui:
                StartDesigner(string.Empty);
                break;
            }
        }

        private struct Server
        {
            public int Port { get; set; }
            public Process Process { get; set; }
        }

        private static class NativeMethods
        {
            [ResourceExposure(ResourceScope.None)]
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern void SwitchToThisWindow(IntPtr hWnd,
                [MarshalAs(UnmanagedType.Bool)] bool fAltTab);
        }

        private EnvDTE.DTE dte;
        private static int port;

        private static ManualResetEvent portFound = new ManualResetEvent(false);
        private static Dictionary<string, Server> servers = new Dictionary<string, Server>();

        private DefaultEditorsHandler(EnvDTE.DTE dte)
        {
            this.dte = dte;
        }

        private void CheckoutFileIfNeeded(string fileName, out bool abortOperation)
        {
            abortOperation = false;

            if (QtVSIPSettings.GetDisableCheckoutFiles())
                return;

            var sourceControl = dte.SourceControl;
            if (sourceControl == null)
                return;

            if (!sourceControl.IsItemUnderSCC(fileName))
                return;

            if (sourceControl.IsItemCheckedOut(fileName))
                return;

            if (QtVSIPSettings.GetAskBeforeCheckoutFile()) {
                var shortFileName = Path.GetFileName(fileName);
                var dr = MessageBox.Show(SR.GetString("QuestionSCCCheckoutOnOpen", shortFileName),
                    Resources.msgBoxCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (dr == DialogResult.Cancel)
                    abortOperation = true;
                if (dr != DialogResult.Yes)
                    return;
            }

            sourceControl.CheckOutItem(fileName);
        }

        private void StartLinguist(string fileName)
        {
            var qtDir = string.Empty;
            if (!GetQtProjectAndDirectory("linguist.exe", out qtDir))
                return;

            try {
                var workingDir = string.Empty;
                if (!string.IsNullOrEmpty(fileName)) {
                    fileName = fileName.Quoute();
                    workingDir = Path.GetDirectoryName(fileName);
                }
                GetEditorProcess("linguist.exe", fileName, workingDir, qtDir).Start();
            } catch {
                System.Windows.MessageBox.Show(SR.GetString("QtAppNotFoundErrorMessage",
                    "Qt Linguist"), SR.GetString("QtAppNotFoundErrorTitle", "Linguist"));
            }
        }

        private void StartDesigner(string fileName)
        {
            var qtDir = string.Empty;
            if (!GetQtProjectAndDirectory("designer.exe", out qtDir))
                return;

            try {
                if (!servers.ContainsKey(qtDir) || servers[qtDir].Process.HasExited) {
                    var arguments = "-server";
                    var workingDir = string.Empty;
                    if (!string.IsNullOrEmpty(fileName)) {
                        arguments += " " + fileName.Quoute();
                        workingDir = Path.GetDirectoryName(fileName);
                    }
                    var process = GetEditorProcess("designer.exe", arguments, workingDir, qtDir);
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.OutputDataReceived += DesignerOutputHandler;

                    process.Start();
                    process.BeginOutputReadLine();

                    try {
                        portFound.WaitOne(5000, false);
                    } catch (Exception e) {
                        MessageBox.Show(e.Message);
                    }

                    process.WaitForInputIdle();
                    servers[qtDir] = new Server {
                        Port = port,
                        Process = process
                    };
                    portFound.Reset();
                } else {
                    try {
                        using (var client = new TcpClient("127.0.0.1", servers[qtDir].Port)) {
                            var encoder = new System.Text.UTF8Encoding();
                            var buffer = encoder.GetBytes(fileName + "\n");

                            var stream = client.GetStream();
                            stream.Write(buffer, 0, buffer.Length);
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
                if (servers[qtDir].Process.MainWindowHandle == IntPtr.Zero) {
                    var process = Process.GetProcessById(servers[qtDir].Process.Id);
                    if (process.MainWindowHandle != IntPtr.Zero) {
                        servers[qtDir] = new Server {
                            Process = process,
                            Port = servers[qtDir].Port
                        };
                    }
                }
                NativeMethods.SwitchToThisWindow(servers[qtDir].Process.MainWindowHandle, true);
            } catch {
                // silent
            }
        }

        private void DesignerOutputHandler(object sender, DataReceivedEventArgs args)
        {
            var process = sender as Process;
            process.CancelOutputRead();
            process.OutputDataReceived -= DesignerOutputHandler;

            try {
                port = Convert.ToInt32(args.Data);
                portFound.Set(); // might throw object exposed exception
            } catch { }
        }

        private bool GetQtProjectAndDirectory(string tool, out string qtDir)
        {
            var qtVersion = "$(DefaultQtVersion)";
            var project = HelperFunctions.GetSelectedQtProject(dte);
            if (project == null) {
                project = HelperFunctions.GetSelectedProject(dte);
                if (project != null && HelperFunctions.IsQMakeProject(project)) {
                    var qmakeQtDir = HelperFunctions.GetQtDirFromQMakeProject(project);
                    qtVersion = QtVersionManager.The().GetQtVersionFromInstallDir(qmakeQtDir);
                }
            } else {
                qtVersion = QtVersionManager.The().GetProjectQtVersion(project);
            }

            qtDir = HelperFunctions.FindQtDirWithTools(tool, qtVersion);
            if (string.IsNullOrEmpty(qtDir))
                MessageBox.Show(SR.GetString("NoDefaultQtVersionError"), Resources.msgBoxCaption);
            return !string.IsNullOrEmpty(qtDir);
        }

        private Process GetEditorProcess(string editor, string args, string workingDir, string qtDir)
        {
            var fileName = string.Empty;
            if (!string.IsNullOrEmpty(qtDir))
                fileName = Path.Combine(qtDir, "bin", editor);

            // Try to find application in project's Qt directory first
            if (!File.Exists(fileName)) {
                var project = HelperFunctions.GetSelectedQtProject(dte);
                if (project != null) {
                    var path = QtVersionManager.The().GetInstallPath(project);
                    if (string.IsNullOrEmpty(path))
                        fileName = Path.Combine(path, "bin", editor);
                }
            }

            // Try with Path
            if (!File.Exists(fileName))
                fileName = HelperFunctions.FindFileInPATH(editor);

            // try default Qt version
            if (!File.Exists(fileName)) {
                var vm = QtVersionManager.The();
                qtDir = vm.GetInstallPath(vm.GetDefaultVersion());
                workingDir = Path.Combine(qtDir, "bin");
                fileName = Path.Combine(workingDir, editor);
            }

            if (!File.Exists(fileName))
                return null;

            return new Process {
                StartInfo = new ProcessStartInfo {
                    Arguments = args,
                    FileName = fileName,
                    WorkingDirectory = workingDir,
                    WindowStyle = ProcessWindowStyle.Normal
                }
            };
        }
    }
}
