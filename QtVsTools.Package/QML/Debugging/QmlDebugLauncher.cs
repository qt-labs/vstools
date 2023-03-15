/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace QtVsTools.Qml.Debug
{
    using AD7;
    using Common;
    using Core;
    using Core.QtMsBuild;
    using SyntaxAnalysis;
    using VisualStudio;
    using static Utils;
    using static SyntaxAnalysis.RegExpr;

    class Launcher : Disposable, IDebugEventCallback2
    {
        LazyFactory Lazy { get; } = new LazyFactory();

        private static Launcher Instance { get; set; }
        IVsDebugger debugger;
        IVsDebugger4 debugger4;

        HashSet<Guid> ExcludedProcesses => Lazy.Get(() =>
            ExcludedProcesses, () => new HashSet<Guid>());

        public static void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Instance = new Launcher();
            Instance.debugger = VsServiceProvider.GetService<IVsDebugger>();
            Instance.debugger4 = VsServiceProvider.GetService<IVsDebugger, IVsDebugger4>();

            if (Instance is { debugger: {}, debugger4: {} })
                Instance.debugger.AdviseDebugEventCallback(Instance);
        }

        protected override void DisposeManaged()
        {
            if (debugger != null) {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    debugger.UnadviseDebugEventCallback(this);
                });
            }
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
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!QtVsToolsPackage.Instance.Options.QmlDebuggerEnabled)
                return VSConstants.S_OK;

            if (riidEvent != typeof(IDebugThreadCreateEvent2).GUID
                && riidEvent != typeof(IDebugProgramDestroyEvent2).GUID) {
                return VSConstants.S_OK;
            }

            if (pProcess == null && pProgram.GetProcess(out pProcess) != VSConstants.S_OK)
                return VSConstants.S_OK;

            if (pProcess.GetProcessId(out Guid procGuid) != VSConstants.S_OK)
                return VSConstants.S_OK;

            // Run only once per process
            if (riidEvent == typeof(IDebugProgramDestroyEvent2).GUID) {
                ExcludedProcesses.Remove(procGuid);
                return VSConstants.S_OK;
            }
            if (ExcludedProcesses.Contains(procGuid))
                return VSConstants.S_OK;
            ExcludedProcesses.Add(procGuid);

            if (pEvent is not (IDebugLoadCompleteEvent2 or IDebugThreadCreateEvent2))
                return VSConstants.S_OK;

            if (pProgram == null)
                return VSConstants.S_OK;

            bool native;
            Guid engineId = GetEngineId(pProgram);
            if (engineId == NativeEngine.Id)
                native = true;
            else if (engineId == GdbEngine.Id)
                native = false;
            else
                return VSConstants.S_OK;

            if (!GetProcessInfo(pProcess, native, out string execPath, out uint procId))
                return VSConstants.S_OK;

            if (!GetProjectInfo(execPath, native, out string execCmd, out IEnumerable<string> rccItems))
                return VSConstants.S_OK;

            LaunchDebug(execPath, execCmd, procId, rccItems);
            return VSConstants.S_OK;
        }

        Guid GetEngineId(IDebugProgram2 pProgram)
        {
            if (pProgram.GetEngineInfo(out _, out Guid engineGuid) != VSConstants.S_OK)
                return Guid.Empty;
            return engineGuid;
        }

        class WslPath
        {
            public string Drive;
            public string Path;
            public static implicit operator string(WslPath wslPath)
            {
                return $@"{wslPath.Drive}:\{wslPath.Path}";
            }
        }

        static readonly RegExpr wslPathRegex = new Token("WSLPATH", SkipWs_Disable, StartOfFile
            & "/mnt/" & new Token("DRIVE", CharWord) & "/" & new Token("PATH", AnyChar.Repeat()))
        {
            new Rule<WslPath>
            {
                Update("DRIVE", (WslPath wslPath, string drive) => wslPath.Drive = drive),
                Update("PATH", (WslPath wslPath, string path) => wslPath.Path = path),
            }
        };
        static readonly RegExpr.Parser wslPathParser = wslPathRegex.Render();

        bool GetProcessInfo(IDebugProcess2 pProcess, bool native, out string execPath, out uint procId)
        {
            execPath = "";
            procId = 0;

            if (pProcess.GetName(enum_GETNAME_TYPE.GN_FILENAME, out string fileName) != VSConstants.S_OK)
                return false;

            var pProcessId = new AD_PROCESS_ID[1];
            if (pProcess.GetPhysicalProcessId(pProcessId) != VSConstants.S_OK)
                return false;

            if (native) {
                execPath = Path.GetFullPath(fileName);
            } else {
                var wslPath = wslPathParser.Parse(fileName)
                    .GetValues<WslPath>("WSLPATH").FirstOrDefault();
                execPath = wslPath != null ? Path.GetFullPath(wslPath) : fileName;
            }

            procId = pProcessId[0].dwProcessId;
            return true;
        }

        bool GetProjectInfo(string execPath, bool native, out string execCmd, out IEnumerable<string> rccItems)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            execCmd = "";
            rccItems = null;

            foreach (var project in HelperFunctions.ProjectsInSolution(QtVsToolsPackage.Instance.Dte)) {
                if (project.Object is not VCProject {Configurations: IVCCollection configs} vcProject)
                    continue;

                if (project.ConfigurationManager.ActiveConfiguration is not {} activeConfig)
                    continue;

                var activeConfigId = $"{activeConfig.ConfigurationName}|{activeConfig.PlatformName}";
                if (configs.Item(activeConfigId) is not VCConfiguration vcConfig)
                    continue;

                if (vcProject is not IVCBuildPropertyStorage props)
                    continue;

                var localDebugCommand = props.GetPropertyValue("LocalDebuggerCommand",
                    vcConfig.Name, "UserFile");

                var remoteDebugCommand = props.GetPropertyValue("RemoteDebuggerCommand",
                    vcConfig.Name, "UserFile");

                string debugCommand = (native || string.IsNullOrEmpty(remoteDebugCommand))
                    ? localDebugCommand : remoteDebugCommand;

                bool sameFile = string.Equals(execPath, Path.GetFullPath(debugCommand), IgnoreCase);

                if (!sameFile)
                    continue;

                OutputWriteLine($"Debugging project '{vcProject.Name}'...");

                var qtProject = QtProject.Create(vcProject);
                if (qtProject == null) {
                    OutputWriteLine("DISABLED: Non-Qt project");
                    return false;
                }

                if (!qtProject.IsQtMsBuildEnabled()) {
                    OutputWriteLine("DISABLED: Non-Qt/MSBuild project");
                    return false;
                }

                if (!qtProject.QmlDebug) {
                    OutputWriteLine("DISABLED: QML debugging disabled in Qt project settings");
                    return false;
                }

                var execArgs = props.GetPropertyValue(
                    native ? "LocalDebuggerCommandArguments" : "RemoteDebuggerCommandArguments",
                    vcConfig.Name, "UserFile");
                if (string.IsNullOrEmpty(execArgs)) {
                    OutputWriteLine("DISABLED: Error reading command line arguments");
                    return false;
                }

                var cmd = "\"" + execPath + "\" " + execArgs;

                if (!QmlDebugger.CheckCommandLine(execPath, cmd)) {
                    OutputWriteLine("DISABLED: Error parsing command line arguments");
                    return false;
                }

                OutputWriteLine("Starting QML debug session...");

                execCmd = cmd;
                rccItems = ((IVCCollection)vcProject.Files).Cast<VCFile>()
                    .Where(x => x.ItemType == QtRcc.ItemTypeName)
                    .Select(x => x.FullPath);

                return true;
            }

            OutputWriteLine("DISABLED: Could not identify project being debugged");

            return false;
        }

        void OutputWriteLine(string msg)
        {
            Messages.Print(msg);
        }

        void LaunchDebug(
            string execPath,
            string execCmd,
            uint procId,
            IEnumerable<string> rccItems)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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

            } catch (Exception exception) {
                exception.Log();
            }
        }
    }
}
