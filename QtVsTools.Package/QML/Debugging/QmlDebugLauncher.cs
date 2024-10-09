/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Internal;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;

using static Microsoft.VisualStudio.VSConstants;
using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Qml.Debug
{
    using AD7;
    using Common;
    using Core;
    using Core.MsBuild;
    using Core.Options;
    using SyntaxAnalysis;
    using VisualStudio;
    using static Core.Common.Utils;
    using static Core.Instances;
    using static Instances;
    using static SyntaxAnalysis.RegExpr;

    class Launcher : Disposable, IDebugEventCallback2
    {
        LazyFactory Lazy { get; } = new();

        private static Launcher Instance { get; set; }
        IVsDebugger debugger;
        IVsDebugger4 debugger4;

        HashSet<Guid> ExcludedProcesses => Lazy.Get(() =>
            ExcludedProcesses, () => new HashSet<Guid>());

        HashSet<uint> ExcludedProcIds => Lazy.Get(() =>
            ExcludedProcIds, () => new HashSet<uint>());

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

            if (!QtOptionsPage.QmlDebuggerEnabled)
                return S_OK;

            if (riidEvent != typeof(IDebugThreadCreateEvent2).GUID
                && riidEvent != typeof(IDebugProgramDestroyEvent2).GUID) {
                return S_OK;
            }

            if (pProcess == null && pProgram.GetProcess(out pProcess) != S_OK)
                return S_OK;

            if (pProcess.GetProcessId(out Guid procGuid) != S_OK)
                return S_OK;

            bool native;
            Guid engineId = GetEngineId(pProgram);
            if (engineId == NativeEngine.Id || engineId == COMPlusNativeEngine.Id)
                native = true;
            else if (engineId == GdbEngine.Id)
                native = false;
            else
                return S_OK;

            // Run only once per process
            if (riidEvent == typeof(IDebugProgramDestroyEvent2).GUID) {
                ExcludedProcesses.Remove(procGuid);
                if (GetProcessInfo(pProcess, native, out var terminatedProcId))
                    ExcludedProcIds.Remove(terminatedProcId);
                return S_OK;
            }
            if (ExcludedProcesses.Contains(procGuid))
                return S_OK;
            ExcludedProcesses.Add(procGuid);

            if (pEvent is not (IDebugLoadCompleteEvent2 or IDebugThreadCreateEvent2))
                return S_OK;

            if (pProgram == null)
                return S_OK;

            if (!GetProcessInfo(pProcess, native, out var procId, out var execPath, out var cmd))
                return S_OK;

            if (!QmlDebugger.CheckCommandLine(execPath, cmd))
                return S_OK;

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(
                async () => await LaunchDebugAsync(execPath, cmd, procId));
            return S_OK;
        }

        public static bool TryAttachToProcess(uint procId)
        {
            if (!Instance.GetProcessInfo(procId, out var execPath, out var cmd))
                return false;

            if (!QmlDebugger.CheckCommandLine(execPath, cmd))
                return false;

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(
                async () => await Instance.LaunchDebugAsync(execPath, cmd, procId));

            return true;
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


        bool GetProcessInfo(IDebugProcess2 pProcess, bool native, out uint procId)
        {
            procId = 0;
            GetProcessInfo(pProcess, native, out procId, out _, out _);
            return procId != 0;
        }

        bool GetProcessInfo(IDebugProcess2 pProcess, bool native,
            out uint procId, out string procExecPath, out string procCmdLine)
        {
            procId = 0;
            procExecPath = "";
            procCmdLine = "";

            try {
                if (pProcess.GetName(enum_GETNAME_TYPE.GN_FILENAME, out var fileName) != S_OK)
                    return false;

                var pProcessId = new AD_PROCESS_ID[1];
                if (pProcess.GetPhysicalProcessId(pProcessId) != S_OK)
                    return false;

                if (native) {
                    procExecPath = Path.GetFullPath(fileName);
                } else {
                    var wslPath = wslPathParser.Parse(fileName)
                        .GetValues<WslPath>("WSLPATH").FirstOrDefault();
                    procExecPath = wslPath != null ? Path.GetFullPath(wslPath) : fileName;
                }

                procId = pProcessId[0].dwProcessId;

            } catch (Exception e) {
                e.Log();
                return false;
            }

            return !native || GetProcessInfo(procId, out procExecPath, out procCmdLine);
        }

        bool GetProcessInfo(uint procId, out string procExecPath, out string procCmdLine)
        {
            procExecPath = "";
            procCmdLine = "";

            try {
                using var query = new ManagementObjectSearcher(@$"
                    SELECT ExecutablePath, CommandLine
                    FROM Win32_Process
                    WHERE ProcessId = {procId}");
                if (query?.Get()?.Cast<ManagementObject>()?.FirstOrDefault() is not { } queryResult)
                    return false;
                if (queryResult["ExecutablePath"] is not string executablePath)
                    return false;
                if (queryResult["CommandLine"] is not string commandLine)
                    return false;

                procExecPath = executablePath;
                procCmdLine = commandLine;

            } catch (Exception e) {
                e.Log();
                return false;
            }
            return true;
        }

        private async Task LaunchDebugAsync(string execPath, string cmd, uint procId)
        {
            // Attach only once per process
            if (ExcludedProcIds.Contains(procId))
                return;
            ExcludedProcIds.Add(procId);

            if (!QtVsToolsPackage.IsInitialized)
                Notifications.NotifyMessage.Show("QML Debugger: Waiting for package initialization...");

            await TaskScheduler.Default;
            await QtVsToolsPackage.WaitUntilInitializedAsync();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Notifications.NotifyMessage.Close();
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
                if (HelperFunctions.ProjectsInSolution(Package.Dte) is not {} projects)
                    return new();
                foreach (var project in projects) {
                    solutionDirs.Add(project.ProjectDirectory);
                    var projectRccs = ((IVCCollection)project.Files).Cast<VCFile>()
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
                    e.Log();
                }
            }
            return solutionRccs;
        }
    }

    internal class ProvideLaunchHookAttribute : RegistrationAttribute
    {
        public Type LaunchHookType { get; set; }
        private string ClassId => $"{LaunchHookType?.GUID:B}";
        private string ClassKey => @$"CLSID\{ClassId}";
        private const string HooksKey = @"Debugger\LaunchHooks110";

        public ProvideLaunchHookAttribute()
        {
        }

        public ProvideLaunchHookAttribute(Type launchHookType)
        {
            LaunchHookType = launchHookType;
        }

        public override void Register(RegistrationContext context)
        {
            using var hook = context.CreateKey(ClassKey);
            hook.SetValue(string.Empty, LaunchHookType.Name);
            hook.SetValue("CodeBase", context.CodeBase);
            hook.SetValue("Class", LaunchHookType.FullName);
            hook.SetValue("InprocServer32", context.InprocServerPath);
            hook.SetValue("ThreadingModel", "Both");
            using var hooks = context.CreateKey(HooksKey);
            hooks.SetValue(LaunchHookType.Name, ClassId);
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(ClassKey);
            using var hooks = context.CreateKey(HooksKey);
            hooks.SetValue(LaunchHookType.Name, null);
        }
    }

    public class QmlDebugLaunchHook : IVsDebugLaunchHook110
    {
        private IVsDebugLaunchHook110 NextHook { get; set; }

        public int SetNextHook(IVsDebugLaunchHook110 nextHook)
        {
            NextHook = nextHook;
            return S_OK;
        }

        public int IsProcessRecycleRequired(VsDebugTargetProcessInfo[] proc)
        {
            return NextHook?.IsProcessRecycleRequired(proc) ?? S_FALSE;
        }

        public int OnLaunchDebugTargets(uint targetCount,
            VsDebugTargetInfo4[] targets, VsDebugTargetProcessInfo[] results)
        {
            var isNative = targets[0].guidLaunchDebugEngine == NativeEngine.Id ||
                targets[0].guidLaunchDebugEngine == COMPlusNativeEngine.Id;
            var hasEnv = targets[0].bstrEnv is { Length: > 0 };
            var noDebug = (targets[0].LaunchFlags & (uint)__VSDBGLAUNCHFLAGS.DBGLAUNCH_NoDebug) > 0;
            if (!isNative || !hasEnv || noDebug)
                return NextHook?.OnLaunchDebugTargets(targetCount, targets, results) ?? S_OK;

            var envString = string.Join("\r\n", targets[0].bstrEnv.Split('\0'));
            if (ParseEnvironment(envString) is not { Count: > 0} env)
                return NextHook?.OnLaunchDebugTargets(targetCount, targets, results) ?? S_OK;

            if (env.ContainsKey("PATH") && env.ContainsKey("QTDIR")) {
                env["PATH"] += $";{env["QTDIR"]}/bin";
                targets[0].bstrEnv = string.Join("\0", env.Select(x => $"{x.Key}={x.Value}"));
            }

            if (QtOptionsPage.QmlDebuggerEnabled && env.TryGetValue("QML_DEBUG_ARGS",
                out var qmlDebugArgs)) {
                targets[0].bstrArg = $"{targets[0].bstrArg?.Trim()} {qmlDebugArgs.Trim()}".Trim();
            }

            return NextHook?.OnLaunchDebugTargets(targetCount, targets, results) ?? S_OK;
        }
    }
}
