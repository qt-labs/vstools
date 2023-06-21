/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Threading;
using EnvDTE;
using Task = System.Threading.Tasks.Task;
using static Microsoft.VisualStudio.VSConstants;

namespace QtVsTools.Qml.Debug
{
    using AD7;
    using Common;
    using Core;
    using Core.MsBuild;
    using SyntaxAnalysis;
    using VisualStudio;
    using static Utils;
    using static Instances;
    using static Notifications;
    using static Core.Instances;
    using static SyntaxAnalysis.RegExpr;

    class Launcher : Disposable, IDebugEventCallback2
    {
        LazyFactory Lazy { get; } = new();

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
                return S_OK;

            if (riidEvent != typeof(IDebugThreadCreateEvent2).GUID
                && riidEvent != typeof(IDebugProgramDestroyEvent2).GUID) {
                return S_OK;
            }

            if (pProcess == null && pProgram.GetProcess(out pProcess) != S_OK)
                return S_OK;

            if (pProcess.GetProcessId(out Guid procGuid) != S_OK)
                return S_OK;

            // Run only once per process
            if (riidEvent == typeof(IDebugProgramDestroyEvent2).GUID) {
                ExcludedProcesses.Remove(procGuid);
                return S_OK;
            }
            if (ExcludedProcesses.Contains(procGuid))
                return S_OK;
            ExcludedProcesses.Add(procGuid);

            if (pEvent is not (IDebugLoadCompleteEvent2 or IDebugThreadCreateEvent2))
                return S_OK;

            if (pProgram == null)
                return S_OK;

            bool native;
            Guid engineId = GetEngineId(pProgram);
            if (engineId == NativeEngine.Id)
                native = true;
            else if (engineId == GdbEngine.Id)
                native = false;
            else
                return S_OK;

            if (!GetProcessInfo(pProcess, native, out var execPath, out var procId, out var cmd))
                return S_OK;

            if (!QmlDebugger.CheckCommandLine(execPath, cmd))
                return S_OK;

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(
                async () => await LaunchDebugAsync(execPath, cmd, procId));
            return S_OK;
        }

        Guid GetEngineId(IDebugProgram2 pProgram)
        {
            if (pProgram.GetEngineInfo(out _, out Guid engineGuid) != S_OK)
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
                Update("PATH", (WslPath wslPath, string path) => wslPath.Path = path)
            }
        };
        static readonly Parser wslPathParser = wslPathRegex.Render();

        bool GetProcessInfo(IDebugProcess2 pProcess, bool native,
            out string execPath, out uint procId, out string procCmd)
        {
            execPath = "";
            procId = 0;
            procCmd = "";

            try {

                if (pProcess.GetName(enum_GETNAME_TYPE.GN_FILENAME, out var fileName) != S_OK)
                    return false;

                var pProcessId = new AD_PROCESS_ID[1];
                if (pProcess.GetPhysicalProcessId(pProcessId) != S_OK)
                    return false;

                if (native) {
                    execPath = Path.GetFullPath(fileName);
                } else {
                    var wslPath = wslPathParser.Parse(fileName)
                        .GetValues<WslPath>("WSLPATH").FirstOrDefault();
                    execPath = wslPath != null ? Path.GetFullPath(wslPath) : fileName;
                }

                procId = pProcessId[0].dwProcessId;

                using var cmdLineQuery = new ManagementObjectSearcher(
                    @$"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {procId}");

                var cmdLineQueryResults = cmdLineQuery?.Get()?.Cast<ManagementObject>();
                if (cmdLineQueryResults?.FirstOrDefault() is not ManagementObject cmdLineResult)
                    return false;
                if (cmdLineResult["CommandLine"] is not string cmdLine)
                    return false;
                procCmd = cmdLine;

            } catch (Exception e) {
                Messages.Log(e);
                return false;
            }

            return true;
        }

        void OutputWriteLine(string msg)
        {
            Messages.Print(msg);
        }

        private async Task LaunchDebugAsync(string execPath, string cmd, uint procId)
        {
            if (!Package.IsInitialized)
                NotifyMessage.Show("QML Debugger: Waiting for package initialization...");
            await TaskScheduler.Default;
            Package.WaitUntilInitialized();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            NotifyMessage.Close();
            LaunchDebug(execPath, cmd, procId);
        }

        private void LaunchDebug(string execPath, string cmd, uint procId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var targets = new[] { new VsDebugTargetInfo4
            {
                dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_CreateProcess,
                bstrExe = new Uri(execPath).LocalPath,
                bstrArg = cmd,
                bstrOptions = procId.ToString(),
                bstrEnv = "QTRCC=" + string.Join(";", FindAllRcc()),
                guidLaunchDebugEngine = QmlEngine.Id,
                LaunchFlags = (uint)__VSDBGLAUNCHFLAGS5.DBGLAUNCH_BreakOneProcess
            }};

            var processInfo = new VsDebugTargetProcessInfo[targets.Length];
            try {
                debugger4.LaunchDebugTargets4((uint)targets.Length, targets, processInfo);

            } catch (Exception exception) {
                exception.Log();
            }
        }

        private HashSet<string> FindAllRcc()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionDirs = new HashSet<string>(CaseIgnorer);
            var solutionRccs = new HashSet<string>(CaseIgnorer);
            if (CMake.ActiveProject != null) {
                if (string.IsNullOrEmpty(CMake.RootPath))
                    return new();
                solutionDirs.Add(CMake.RootPath);
            } else {
                if (HelperFunctions.ProjectsInSolution(Package.Dte) is not List<Project> projects)
                    return new();
                foreach (var project in HelperFunctions.ProjectsInSolution(Package.Dte)) {
                    if (project.Object is not VCProject vcProject)
                        continue;
                    solutionDirs.Add(vcProject.ProjectDirectory);
                    var projectRccs = ((IVCCollection)vcProject.Files).Cast<VCFile>()
                        .Where(file => file.ItemType == QtRcc.ItemTypeName)
                        .Select(rcc => rcc.FullPath)
                        .ToList();
                    projectRccs.ForEach(rcc => solutionRccs.Add(rcc));
                }
            }
            foreach (var solutionDir in solutionDirs) {
                try {
                    Directory.GetFiles(solutionDir, "*.qrc", SearchOption.AllDirectories)
                        .ToList()
                        .ForEach(rcc => solutionRccs.Add(rcc));
                } catch (Exception e) {
                    Messages.Log(e);
                }
            }
            return solutionRccs;
        }
    }
}
