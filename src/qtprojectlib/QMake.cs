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

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace QtProjectLib
{
    class QMake
    {
        public delegate void ProcessEventHandler();
        public event ProcessEventHandler CloseEvent;

        public delegate void ProcessEventHandlerArg(string data);
        public event ProcessEventHandlerArg PaneMessageDataEvent;

        private string file;
        protected int errorValue;
        private EnvDTE.DTE dteObject;
        private bool recursive;
        protected Process qmakeProcess;
        protected VersionInformation qtVersionInformation;

        protected static int stdOutputLines;
        protected static int errOutputLines;
        protected static StringBuilder stdOutput;
        protected static StringBuilder errOutput;

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
            var vcproj = HelperFunctions.RemoveFileNameExtension(fi) + ".vcxproj";

            var qmakeArgs = "-tp vc \"" + fi.Name + "\" ";

            if (recursive)
                qmakeArgs += "-recursive";
            else
                qmakeArgs += "-o \"" + vcproj + "\"";

            qmakeArgs += @" QMAKE_INCDIR_QT=$(QTDIR)\include ";
            qmakeArgs += @"QMAKE_LIBDIR=$(QTDIR)\lib "
                       + @"QMAKE_MOC=$(QTDIR)\bin\moc.exe "
                       + @"QMAKE_QMAKE=$(QTDIR)\bin\qmake.exe";

            qmakeProcess = CreateQmakeProcess(qmakeArgs, qtVersionInformation.qtDir + "\\bin\\qmake", fi.DirectoryName);

            if (!SetVCVars(qmakeProcess.StartInfo))
                InvokeExternalTarget(PaneMessageDataEvent, "--- (Import): Error setting VC vars");

            // We must set the QTDIR environment variable, because we're clearing QMAKE_LIBDIR_QT above.
            // If we do not set this, the Qt libraries will be QtCored.lib instead of QtCore4d.lib even
            // for shared builds.
            qmakeProcess.StartInfo.EnvironmentVariables["QTDIR"] = qtVersionInformation.qtDir;

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
                        InvokeExternalTarget(PaneMessageDataEvent, "--- (qmake) : Environment "
                            + "variable QMAKESPEC: " + qmakeSpec + Environment.NewLine);
                    }
                }

                if (qmakeProcess.Start()) {
                    errOutput = new StringBuilder();
                    errOutputLines = 0;
                    stdOutput = new StringBuilder();
                    stdOutputLines = 0;
                    var errorThread = new Thread(ReadStandardError);
                    var outputThread = new Thread(ReadStandardOutput);
                    errorThread.Start();
                    outputThread.Start();

                    qmakeProcess.WaitForExit();

                    errorThread.Join();
                    outputThread.Join();

                    errorValue = qmakeProcess.ExitCode;

                    if (stdOutputLines > 0) {
                        InvokeExternalTarget(PaneMessageDataEvent, stdOutput.ToString());
                        InvokeExternalTarget(PaneMessageDataEvent, "--- (Import): Success: " + stdOutputLines);
                    }

                    if (errOutputLines > 0) {
                        InvokeExternalTarget(PaneMessageDataEvent, errOutput.ToString());
                        InvokeExternalTarget(PaneMessageDataEvent, "--- (Import): Error(s): " + errOutputLines);
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
            var qmakeProcess = new Process();
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
                // we can only call one delegate at the time...
                foreach (var singleDelegate in invocationList) {
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
                errOutput.Append("[" + errOutputLines + "] - " + error.Trim() + "\n");
            }
        }

        protected void ReadStandardOutput()
        {
            if (qmakeProcess == null)
                return;

            string output;
            while ((output = qmakeProcess.StandardOutput.ReadLine()) != null) {
                stdOutputLines++;
                stdOutput.Append("[" + stdOutputLines + "] - " + output.Trim() + "\n");
            }
        }

        private string GetRegistrySoftwareString(string subKeyName, string valueName)
        {
            var keyName = new StringBuilder();
            keyName.Append(@"SOFTWARE\");
            if (System.Environment.Is64BitOperatingSystem && IntPtr.Size == 4)
                keyName.Append(@"WOW6432Node\");
            keyName.Append(subKeyName);

            try {
                using (var key = Registry.LocalMachine.OpenSubKey(keyName.ToString(), false)) {
                    if (key == null)
                        return ""; //key not found

                    RegistryValueKind valueKind = key.GetValueKind(valueName);
                    if (valueKind != RegistryValueKind.String
                        && valueKind != RegistryValueKind.ExpandString) {
                        return ""; //wrong value kind
                    }

                    Object objValue = key.GetValue(valueName);
                    if (objValue == null)
                        return ""; //error getting value

                    return objValue.ToString();
                }
            } catch {
                return "";
            }
        }

        private string GetVCPath()
        {
#if VS2017
            string vsPath = GetRegistrySoftwareString(@"Microsoft\VisualStudio\SxS\VS7", "15.0");
            if (string.IsNullOrEmpty(vsPath))
                return "";
            string vcPath = Path.Combine(vsPath, "VC");
#elif VS2015
            string vcPath = GetRegistrySoftwareString(@"Microsoft\VisualStudio\SxS\VC7", "14.0");
            if (string.IsNullOrEmpty(vcPath))
                return ""; //could not get registry key
#elif VS2013
            string vcPath = GetRegistrySoftwareString(@"Microsoft\VisualStudio\SxS\VC7", "12.0");
            if (string.IsNullOrEmpty(vcPath))
                return ""; //could not get registry key
#endif
            return vcPath;
        }

        private bool SetVCVars(ProcessStartInfo startInfo)
        {
            bool isOS64Bit = System.Environment.Is64BitOperatingSystem;
            bool isQt64Bit = QtVersionManager.The().GetVersionInfo(
                QtVersionManager.The().GetDefaultVersion()).is64Bit();

            string vcPath = GetVCPath();
            if (vcPath == "")
                return false;

            string comspecPath = Environment.GetEnvironmentVariable("COMSPEC");
#if VS2017
            string vcVarsCmd = "";
            string vcVarsArg = "";
            if (isOS64Bit && isQt64Bit)
                vcVarsCmd = Path.Combine(vcPath, @"Auxiliary\Build\vcvars64.bat");
            else if (!isOS64Bit && !isQt64Bit)
                vcVarsCmd = Path.Combine(vcPath, @"Auxiliary\Build\vcvars32.bat");
            else if (isOS64Bit && !isQt64Bit)
                vcVarsCmd = Path.Combine(vcPath, @"Auxiliary\Build\vcvarsamd64_x86.bat");
            else if (!isOS64Bit && isQt64Bit)
                vcVarsCmd = Path.Combine(vcPath, @"Auxiliary\Build\vcvarsx86_amd64.bat");
#elif VS2015 || VS2013
            string vcVarsCmd = Path.Combine(vcPath, "vcvarsall.bat");
            string vcVarsArg = "";
            if (isOS64Bit && isQt64Bit)
                vcVarsArg = "amd64";
            else if (!isOS64Bit && !isQt64Bit)
                vcVarsArg = "x86";
            else if (isOS64Bit && !isQt64Bit)
                vcVarsArg = "amd64_x86";
            else if (!isOS64Bit && isQt64Bit)
                vcVarsArg = "x86_amd64";
#endif
            const string markSTX = ":@:@:@";
            const string markEOL = ":#:#:#";
            string command = string.Format("/c \"{0}\" {1} && echo {2} && set", vcVarsCmd, vcVarsArg, markSTX);
            var vcVarsStartInfo = new ProcessStartInfo(comspecPath, command);
            vcVarsStartInfo.CreateNoWindow = true;
            vcVarsStartInfo.UseShellExecute = false;
            vcVarsStartInfo.RedirectStandardError = true;
            vcVarsStartInfo.RedirectStandardOutput = true;

            var process = Process.Start(vcVarsStartInfo);
            StringBuilder stdOut = new StringBuilder();

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                stdOut.AppendFormat("{0}\n{1}\n", e.Data, markEOL);
            process.BeginOutputReadLine();

            process.WaitForExit();
            bool ok = (process.ExitCode == 0);
            process.Close();
            if (!ok)
                return false;

            SortedDictionary<string, List<string>> vcVars = new SortedDictionary<string, List<string>>();
            string[] split = stdOut.ToString().Split(new string[] { "\n", "=", ";" }, StringSplitOptions.None);
            int i = 0;
            for (; i < split.Length && split[i].Trim() != markSTX; i++) {
                //Skip to start of data
            }
            i++; //Advance to next item
            for (; i < split.Length && split[i].Trim() != markEOL; i++) {
                //Skip to end of line
            }
            i++; //Advance to next item
            for (; i < split.Length; i++) {
                //Process first item (variable name)
                string key = split[i].ToUpper().Trim();
                i++; //Advance to next item
                List<string> vcVarValue = vcVars[key] = new List<string>();
                for (; i < split.Length && split[i].Trim() != markEOL; i++) {
                    //Process items up to end of line (variable value(s))
                    vcVarValue.Add(split[i].Trim());
                }
            }

            foreach (var vcVar in vcVars) {
                if (vcVar.Value.Count == 1) {
                    startInfo.EnvironmentVariables[vcVar.Key] = vcVar.Value[0];
                } else {
                    if (!startInfo.EnvironmentVariables.ContainsKey(vcVar.Key)) {
                        foreach (var vcVarValue in vcVar.Value) {
                            if (!string.IsNullOrWhiteSpace(vcVarValue)) {
                                startInfo.EnvironmentVariables[vcVar.Key] += vcVarValue + ";";
                            }
                        }
                    } else {
                        string[] startInfoVariableValues = startInfo.EnvironmentVariables[vcVar.Key]
                            .Split(new string[] { ";" }, StringSplitOptions.None);
                        foreach (var vcVarValue in vcVar.Value) {
                            if (!string.IsNullOrWhiteSpace(vcVarValue)
                                && !startInfoVariableValues.Any(s => s.Trim().Equals(vcVarValue, StringComparison.OrdinalIgnoreCase))) {
                                if (!startInfo.EnvironmentVariables[vcVar.Key].EndsWith(";"))
                                    startInfo.EnvironmentVariables[vcVar.Key] += ";";
                                startInfo.EnvironmentVariables[vcVar.Key] += vcVarValue + ";";
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
