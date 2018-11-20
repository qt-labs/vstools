/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using QtProjectLib;
using QtProjectLib.QtMsBuild;

namespace QtVsTools.Qml.Debug
{
    using AD7;

    class Launcher : Disposable, IDebugEventCallback2
    {
        public static Launcher Instance { get; private set; }
        IVsDebugger debugger;
        IVsDebugger4 debugger4;

        public static void Initialize()
        {
            Instance = new Launcher();
            Instance.debugger = Package.GetGlobalService(typeof(IVsDebugger)) as IVsDebugger;
            Instance.debugger4 = Package.GetGlobalService(typeof(IVsDebugger)) as IVsDebugger4;
            if (Instance.debugger != null && Instance.debugger4 != null)
                Instance.debugger.AdviseDebugEventCallback(Instance);
        }

        private Launcher()
        { }

        protected override void DisposeManaged()
        {
            if (debugger != null)
                debugger.UnadviseDebugEventCallback(this);
        }

        int IDebugEventCallback2.Event(
            IDebugEngine2 pEngine,
            IDebugProcess2 pProcess,
            IDebugProgram2 pProgram,
            IDebugThread2 pThread,
            IDebugEvent2 pEvent,
            ref Guid riidEvent,
            uint dwAttrib)
        {
            var evLoadComplete = pEvent as IDebugLoadCompleteEvent2;
            if (evLoadComplete == null)
                return VSConstants.S_OK;

            if (pProgram == null)
                return VSConstants.S_OK;

            if (pProcess == null && pProgram.GetProcess(out pProcess) != VSConstants.S_OK)
                return VSConstants.S_OK;

            if (!IsNativeProgram(pProgram))
                return VSConstants.S_OK;

            string execPath;
            uint procId;
            if (!GetProcessInfo(pProcess, out execPath, out procId))
                return VSConstants.S_OK;

            string execCmd;
            IEnumerable<string> rccItems;
            if (!GetProjectInfo(execPath, out execCmd, out rccItems))
                return VSConstants.S_OK;

            LaunchDebug(execPath, execCmd, procId, rccItems);
            return VSConstants.S_OK;
        }

        bool IsNativeProgram(IDebugProgram2 pProgram)
        {
            string engineName;
            Guid engineGuid;
            if (pProgram.GetEngineInfo(out engineName, out engineGuid) != VSConstants.S_OK)
                return false;

            return (engineGuid == NativeEngine.Id);
        }

        bool GetProcessInfo(IDebugProcess2 pProcess, out string execPath, out uint procId)
        {
            execPath = "";
            procId = 0;

            string fileName;
            if (pProcess.GetName(enum_GETNAME_TYPE.GN_FILENAME, out fileName) != VSConstants.S_OK)
                return false;

            var pProcessId = new AD_PROCESS_ID[1];
            if (pProcess.GetPhysicalProcessId(pProcessId) != VSConstants.S_OK)
                return false;

            execPath = Path.GetFullPath(fileName);
            procId = pProcessId[0].dwProcessId;
            return true;
        }

        bool GetProjectInfo(string execPath, out string execCmd, out IEnumerable<string> rccItems)
        {
            execCmd = "";
            rccItems = null;

            IEnumHierarchies vsProjects;
            debugger4.EnumCurrentlyDebuggingProjects(out vsProjects);
            if (vsProjects == null)
                return false;

            var vsProject = new IVsHierarchy[1];
            uint fetched = 0;
            while (vsProjects.Next(1, vsProject, out fetched) == VSConstants.S_OK) {

                object objProj;
                vsProject[0].GetProperty(VSConstants.VSITEMID_ROOT,
                    (int)__VSHPROPID.VSHPROPID_ExtObject, out objProj);

                var project = objProj as EnvDTE.Project;
                if (project == null)
                    continue;

                var vcProject = project.Object as VCProject;
                if (vcProject == null)
                    continue;

                var vcConfig = vcProject.ActiveConfiguration;

                var props = vcProject as IVCBuildPropertyStorage;

                var debugCommand = props.GetPropertyValue("LocalDebuggerCommand",
                    vcConfig.Name, "UserFile");

                bool sameFile = string.Equals(execPath, Path.GetFullPath(debugCommand),
                    StringComparison.InvariantCultureIgnoreCase);

                if (!sameFile)
                    continue;

                var qtProject = QtProject.Create(vcProject);
                if (qtProject == null || !qtProject.IsQtMsBuildEnabled())
                    continue;

                var execArgs = props.GetPropertyValue("LocalDebuggerCommandArguments",
                    vcConfig.Name, "UserFile");
                if (string.IsNullOrEmpty(execArgs))
                    continue;

                var cmd = execPath + " " + execArgs;

                if (!QmlDebugger.CheckCommandLine(execPath, cmd))
                    continue;

                execCmd = cmd;
                rccItems = ((IVCCollection)vcProject.Files).Cast<VCFile>()
                    .Where(x => x.ItemType == QtRcc.ItemTypeName)
                    .Select(x => x.FullPath);

                return true;
            }

            return false;
        }

        void LaunchDebug(
            string execPath,
            string execCmd,
            uint procId,
            IEnumerable<string> rccItems)
        {
            var targets = new[] { new VsDebugTargetInfo4
            {
                dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_CreateProcess,
                bstrExe = new Uri(execPath).LocalPath,
                bstrArg = execCmd,
                bstrOptions = procId.ToString(),
                bstrEnv = "QTRCC=" + string.Join(";", rccItems),
                guidLaunchDebugEngine = QmlEngine.Id,
                LaunchFlags = (uint)__VSDBGLAUNCHFLAGS5.DBGLAUNCH_BreakOneProcess,
            }};

            var processInfo = new VsDebugTargetProcessInfo[targets.Length];
            try {
                debugger4.LaunchDebugTargets4((uint)targets.Length, targets, processInfo);

            } catch (Exception e) {
                Messages.PaneMessageSafe(Vsix.Instance.Dte,
                    e.Message + "\r\n\r\nStacktrace:\r\n" + e.StackTrace, 5000);
            }
        }
    }
}
