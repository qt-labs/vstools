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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QtProjectLib
{
    #region QMake Process

    class InfoDialog : Form
    {
        private Label label1 = null;
        private IContainer components = null;
        private ProgressBar progressBar1 = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        public InfoDialog(string name)
        {
            label1 = new Label();
            components = new Container();
            progressBar1 = new System.Windows.Forms.ProgressBar();
            SuspendLayout();
            //
            // label1
            //
            label1.Location = new System.Drawing.Point(12, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(370, 13);
            label1.TabIndex = 0;
            label1.Text = SR.GetString("QMakeProcess_OpenSolutionFromFile") + name;
            //
            // progressBar1
            //
            progressBar1.Location = new System.Drawing.Point(13, 28);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new System.Drawing.Size(369, 23);
            progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 1;
            //
            // Form1
            //
            ClientSize = new System.Drawing.Size(394, 67);
            MinimumSize = new System.Drawing.Size(402, 94);
            ControlBox = false;
            Controls.Add(progressBar1);
            Controls.Add(label1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            ShowInTaskbar = false;
            Text = SR.GetString("Resources_QtVsTools");
            StartPosition = FormStartPosition.CenterParent;

            ResumeLayout(false);
            PerformLayout();
        }

        public void CloseEventHandler()
        {
            Close();
        }
    }

    class QMake
    {
        public delegate void ProcessEventHandler();
        public event ProcessEventHandler CloseEvent;

        public delegate void ProcessEventHandlerArg(string data);
        public event ProcessEventHandlerArg PaneMessageDataEvent;

        private string file = null;
        protected int errorValue = 0;
        private EnvDTE.DTE dteObject;
        private bool recursive = false;
        protected Process qmakeProcess = null;
        protected VersionInformation qtVersionInformation;

        protected static int stdOutputLines = 0;
        protected static int errOutputLines = 0;
        protected static StringBuilder stdOutput = null;
        protected static StringBuilder errOutput = null;

        public int ErrorValue
        {
            get { return errorValue; }
        }

        public QMake(EnvDTE.DTE dte, string fileName, bool recursiveRun, VersionInformation vi)
        {
            dteObject = dte;
            file = fileName;
            recursive = recursiveRun;
            qtVersionInformation = vi;
        }

        public void RunQMake()
        {
            var fi = new FileInfo(file);
            string vcproj = HelperFunctions.RemoveFileNameExtension(fi) + ".vcxproj";

            string qmakeArgs = "-tp vc \"" + fi.Name + "\" ";

            if (recursive)
                qmakeArgs += "-recursive";
            else
                qmakeArgs += "-o \"" + vcproj + "\"";

            qmakeArgs += @" QMAKE_INCDIR_QT=$(QTDIR)\include ";
            qmakeArgs += @"QMAKE_LIBDIR=$(QTDIR)\lib "
                       + @"QMAKE_MOC=$(QTDIR)\bin\moc.exe "
                       + @"QMAKE_QMAKE=$(QTDIR)\bin\qmake.exe";

            qmakeProcess = CreateQmakeProcess(qmakeArgs, qtVersionInformation.qtDir + "\\bin\\qmake", fi.DirectoryName);

            // We must set the QTDIR environment variable, because we're clearing QMAKE_LIBDIR_QT above.
            // If we do not set this, the Qt libraries will be QtCored.lib instead of QtCore4d.lib even
            // for shared builds.
            qmakeProcess.StartInfo.EnvironmentVariables["QTDIR"] = qtVersionInformation.qtDir;

            // determine which vs version we are currently using and inform qmake about it
            string regPath = dteObject.Application.RegistryRoot + "\\Setup\\VC";
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath);
            if (key != null) {
                var keyValue = key.GetValue("ProductDir", (object) "").ToString();
                string envVar = qmakeProcess.StartInfo.EnvironmentVariables["path"];
                if (envVar != null) {
                    string value = envVar + ";" + keyValue;
                    qmakeProcess.StartInfo.EnvironmentVariables["path"] = value;
                } else
                    qmakeProcess.StartInfo.EnvironmentVariables.Add("path", keyValue);
            }

            try {
                InvokeExternalTarget(PaneMessageDataEvent, "--- (qmake) : Using: " + qmakeProcess.StartInfo.FileName);
                InvokeExternalTarget(PaneMessageDataEvent, "--- (qmake) : Working Directory: " + qmakeProcess.StartInfo.WorkingDirectory);
                InvokeExternalTarget(PaneMessageDataEvent, "--- (qmake) : Arguments: "
                    + qmakeProcess.StartInfo.Arguments
                    + Environment.NewLine);
                if (qmakeProcess.StartInfo.EnvironmentVariables.ContainsKey("QMAKESPEC")) {
                    var qmakeSpec = qmakeProcess.StartInfo.EnvironmentVariables["QMAKESPEC"];
                    if (qmakeSpec != qtVersionInformation.QMakeSpecDirectory) {
                        InvokeExternalTarget(PaneMessageDataEvent, "--- (qmake) : Environment "
                            + "variable QMAKESPEC overwriting Qt version QMAKESPEC.");
                        InvokeExternalTarget(PaneMessageDataEvent, "--- (qmake) : Qt version "
                            + "QMAKESPEC: " + qtVersionInformation.QMakeSpecDirectory);
                        InvokeExternalTarget(PaneMessageDataEvent,"--- (qmake) : Environment "
                            + "variable QMAKESPEC: " + qmakeSpec + Environment.NewLine);
                    }
                }

                if (qmakeProcess.Start()) {
                    errOutput = new StringBuilder();
                    errOutputLines = 0;
                    stdOutput = new StringBuilder();
                    stdOutputLines = 0;
                    var errorThread = new Thread(new ThreadStart(ReadStandardError));
                    var outputThread = new Thread(new ThreadStart(ReadStandardOutput));
                    errorThread.Start();
                    outputThread.Start();

                    qmakeProcess.WaitForExit();

                    errorThread.Join();
                    outputThread.Join();

                    errorValue = qmakeProcess.ExitCode;

                    if (stdOutputLines > 0) {
                        InvokeExternalTarget(PaneMessageDataEvent, stdOutput.ToString());
                        InvokeExternalTarget(PaneMessageDataEvent, "--- (Import): Success: " + stdOutputLines.ToString());
                    }

                    if (errOutputLines > 0) {
                        InvokeExternalTarget(PaneMessageDataEvent, errOutput.ToString());
                        InvokeExternalTarget(PaneMessageDataEvent, "--- (Import): Error(s): " + errOutputLines.ToString());
                    }
                }

                InvokeExternalTarget(PaneMessageDataEvent, "--- (qmake) : Exit Code: " + errorValue + Environment.NewLine);
                qmakeProcess.Close();
            } catch (Exception e) {
                qmakeProcess = null;
                InvokeExternalTarget(PaneMessageDataEvent, e.Message);
                errorValue = -1;
            } finally {
                InvokeExternalTarget(CloseEvent);
                Messages.ActivateMessagePane();
            }
        }

        protected Process CreateQmakeProcess(string qmakeArgs, string filename, string workingDir)
        {
            var qmakeProcess = new System.Diagnostics.Process();
            qmakeProcess.StartInfo.CreateNoWindow = true;
            qmakeProcess.StartInfo.UseShellExecute = false;
            qmakeProcess.StartInfo.RedirectStandardError = true;
            qmakeProcess.StartInfo.RedirectStandardOutput = true;
            qmakeProcess.StartInfo.Arguments = qmakeArgs;
            qmakeProcess.StartInfo.FileName = filename;
            qmakeProcess.StartInfo.WorkingDirectory = workingDir;
            return qmakeProcess;
        }

        protected static void InvokeExternalTarget(Delegate dlg, params object[] objList)
        {
            try {
                // make sure there are delegates assigned...
                var invocationList = dlg.GetInvocationList();
                if (invocationList == null)
                    return;

                // we can only call one delegate at the time...
                foreach (Delegate singleDelegate in invocationList) {
                    var synchronizeTarget = singleDelegate.Target as ISynchronizeInvoke;
                    if (synchronizeTarget == null)
                        singleDelegate.DynamicInvoke(objList);
                    else
                        synchronizeTarget.BeginInvoke(singleDelegate, objList);
                }
            } catch { }
        }

        protected void ReadStandardError()
        {
            if (qmakeProcess == null)
                return;

            string error;
            while ((error = qmakeProcess.StandardError.ReadLine()) != null) {
                errOutputLines++;
                errOutput.Append("[" + errOutputLines.ToString() + "] - " + error.Trim() + "\n");
            }
        }

        protected void ReadStandardOutput()
        {
            if (qmakeProcess == null)
                return;

            string output;
            while ((output = qmakeProcess.StandardOutput.ReadLine()) != null) {
                stdOutputLines++;
                stdOutput.Append("[" + stdOutputLines.ToString() + "] - " + output.Trim() + "\n");
            }
        }
    }

    class QMakeQuery : QMake
    {
        public delegate void EventHandler(string result);
        public event EventHandler ReadyEvent;
        private string queryResult;

        public QMakeQuery(VersionInformation vi)
            : base(null, "", false, vi)
        {
            qtVersionInformation = vi;
        }

        public string query(string property)
        {
            ReadyEvent += resultObtained;
            var qmakeThread = new System.Threading.Thread(new ParameterizedThreadStart(RunQMakeQuery));
            qmakeThread.Start(property);
            qmakeThread.Join();
            return queryResult;
        }

        private void resultObtained(string result)
        {
            queryResult = result;
        }

        private void RunQMakeQuery(object property)
        {
            if (property == null)
                return;

            var propertyString = property.ToString();
            string result = "";

            qmakeProcess = CreateQmakeProcess("-query " + propertyString.Trim(), qtVersionInformation.qtDir + "\\bin\\qmake", qtVersionInformation.qtDir);
            try {
                if (qmakeProcess.Start()) {
                    errOutput = new StringBuilder();
                    errOutputLines = 0;
                    stdOutput = new StringBuilder();
                    stdOutputLines = 0;
                    var errorThread = new Thread(new ThreadStart(ReadStandardError));
                    var outputThread = new Thread(new ThreadStart(ReadStandardOutput));
                    errorThread.Start();
                    outputThread.Start();

                    qmakeProcess.WaitForExit();

                    errorThread.Join();
                    outputThread.Join();

                    errorValue = qmakeProcess.ExitCode;

                    if (stdOutputLines > 0) {
                        result = stdOutput.ToString();
                        var dashIndex = result.IndexOf('-');
                        if (dashIndex == -1) {
                            errorValue = -1;
                            result = "";
                        } else {
                            result = result.Substring(dashIndex + 1).Trim();
                        }
                    }
                }
                qmakeProcess.Close();
            } catch (Exception) {
                qmakeProcess = null;
                errorValue = -1;
            } finally {
                InvokeExternalTarget(ReadyEvent, result);
            }
        }
    }

    #endregion
}
