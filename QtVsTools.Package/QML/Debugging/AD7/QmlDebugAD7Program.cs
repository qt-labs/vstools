/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QtVsTools.Qml.Debug.AD7
{
    using VisualStudio;

    sealed partial class Program : Disposable, IDebuggerEventSink,

        IDebugProgramNode2,  // "This interface represents a program that can be debugged."

        IDebugProgram3,      // "This interface represents a program that is running in a process."

        IDebugProcess2,      // "This interface represents a process running on a port. If the
                             //  port is the local port, then IDebugProcess2 usually represents
                             //  a physical process on the local machine."

        IDebugThread2,       // "This interface represents a thread running in a program."
        IDebugThread100,

        IDebugModule3,       // "This interface represents a module -- that is, an executable unit
                             //  of a program -- such as a DLL."

        IDebugEventCallback2 // "This interface is used by the debug engine (DE) to send debug
                             //  events to the session debug manager (SDM)."
    {
        public QmlDebugger Debugger { get; private set; }

        public QmlEngine Engine { get; private set; }

        private List<StackFrame> CurrentFrames { get; set; }

        private const string Name = "QML Debugger";
        public Guid ProcessId { get; set; }
        public Guid ProgramId { get; set; }
        private IDebugProcess2 NativeProc { get; set; }
        private uint NativeProcId { get; set; }
        private string ExecPath { get; set; }
        private string ExecArgs { get; set; }
        private IVsDebugger VsDebugger { get; set; }
        private Dispatcher vsDebuggerThreadDispatcher;

        private static readonly object criticalSectionGlobal = new();
        private static bool originalBreakAllProcesses = BreakAllProcesses;
        private static int runningPrograms;

        public static Program Create(
            QmlEngine engine,
            IDebugProcess2 nativeProc,
            string execPath,
            string execArgs)
        {
            var _this = new Program();
            return _this.Initialize(engine, nativeProc, execPath, execArgs) ? _this : null;
        }

        private Program()
        { }

        private bool Initialize(
            QmlEngine engine,
            IDebugProcess2 nativeProc,
            string execPath,
            string execArgs)
        {
            Engine = engine;
            NativeProc = nativeProc;

            var nativeProcId = new AD_PROCESS_ID[1];
            nativeProc.GetPhysicalProcessId(nativeProcId);
            NativeProcId = nativeProcId[0].dwProcessId;

            ExecPath = execPath;
            ExecArgs = execArgs;

            Debugger = QmlDebugger.Create(this, execPath, execArgs);
            if (Debugger == null)
                return false;

            VsDebugger = VsServiceProvider.GetService<IVsDebugger>();
            if (VsDebugger != null) {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    VsDebugger.AdviseDebugEventCallback(this);
                });
            }
            vsDebuggerThreadDispatcher = Dispatcher.CurrentDispatcher;

            ProcessId = Guid.NewGuid();
            CurrentFrames = new List<StackFrame>();

            lock (criticalSectionGlobal) {
                if (runningPrograms == 0)
                    originalBreakAllProcesses = BreakAllProcesses;
                runningPrograms++;
            }

            return true;
        }

        public override bool CanDispose => !Engine.ProgramIsRunning(this);

        protected override void DisposeManaged()
        {
            Debugger.Dispose();
            if (VsDebugger != null) {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    VsDebugger.UnadviseDebugEventCallback(this as IDebugEventCallback2);
                });
            }

            lock (criticalSectionGlobal) {
                runningPrograms--;
                if (runningPrograms == 0)
                    BreakAllProcesses = originalBreakAllProcesses;
            }
        }

        public void OutputWriteLine(string msg)
        {
            var execFileName = Path.GetFileName(ExecPath);
            Engine.OutputWriteLine($"'{execFileName}' (QML): {msg}");
        }

        bool IDebuggerEventSink.QueryRuntimeFrozen()
        {
            var debugMode = new DBGMODE[1];
            int res = VSConstants.S_FALSE;

            QtVsToolsPackage.Instance.JoinableTaskFactory.Run(async () =>
            {
                await QtVsToolsPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
                res = VsDebugger.GetMode(debugMode);
            });

            if (res != VSConstants.S_OK)
                return false;
            return debugMode[0] != DBGMODE.DBGMODE_Run;
        }

        void IDebuggerEventSink.NotifyError(string errorMessage)
        {
            OutputWriteLine(errorMessage);
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
            if (pEngine == Engine)
                return VSConstants.S_OK;

            if (pProcess == null && pProgram == null)
                return VSConstants.S_OK;

            if (pProcess == null) {
                if (pProgram.GetProcess(out pProcess) != VSConstants.S_OK || pProcess == null)
                    return VSConstants.S_OK;
            }

            var pProcessId = new AD_PROCESS_ID[1];
            if (pProcess.GetPhysicalProcessId(pProcessId) != VSConstants.S_OK)
                return VSConstants.S_OK;

            if (pProcessId[0].dwProcessId != NativeProcId)
                return VSConstants.S_OK;

            if (riidEvent == typeof(IDebugProgramDestroyEvent2).GUID)
                TerminateProcess();

            return VSConstants.S_OK;
        }

        void IDebuggerEventSink.NotifyClientDisconnected()
        {
            TerminateProcess();
        }

        bool terminated;
        void TerminateProcess()
        {
            if (!terminated) {
                terminated = true;
                var engineLaunch = Engine as IDebugEngineLaunch2;
                engineLaunch.TerminateProcess(this as IDebugProcess2);
            }
        }


        #region //////////////////// Execution Control ////////////////////////////////////////////

        public int /*IDebugProgram3*/ Continue(IDebugThread2 pThread)
        {
            Debugger.Run();
            return VSConstants.S_OK;
        }

        public int /*IDebugProgram3*/ ExecuteOnThread(IDebugThread2 pThread)
        {
            Debugger.Run();
            return VSConstants.S_OK;
        }

        public int /*IDebugProgram3*/ Step(
            IDebugThread2 pThread,
            enum_STEPKIND sk,
            enum_STEPUNIT Step)
        {
            if (sk == enum_STEPKIND.STEP_OVER)
                Debugger.StepOver();
            else if (sk == enum_STEPKIND.STEP_INTO)
                Debugger.StepInto();
            else if (sk == enum_STEPKIND.STEP_OUT)
                Debugger.StepOut();
            else
                return VSConstants.E_FAIL;
            return VSConstants.S_OK;
        }

        void IDebuggerEventSink.NotifyBreak()
        {
            BreakAllProcesses = false;
            DebugEvent.Send(new StepCompleteEvent(this));
        }

        #endregion //////////////////// Execution Control /////////////////////////////////////////


        #region //////////////////// Breakpoints //////////////////////////////////////////////////

        public void SetBreakpoint(Breakpoint breakpoint)
        {
            Debugger.SetBreakpoint(breakpoint);
        }

        public void NotifyBreakpointSet(Breakpoint breakpoint)
        {
            DebugEvent.Send(new BreakpointBoundEvent(breakpoint));
        }

        public void ClearBreakpoint(Breakpoint breakpoint)
        {
            Debugger.ClearBreakpoint(breakpoint);
        }

        public void NotifyBreakpointCleared(Breakpoint breakpoint)
        {
            breakpoint.Parent.DisposeBreakpoint(breakpoint);
        }

        public void NotifyBreakpointHit(Breakpoint breakpoint)
        {
            BreakAllProcesses = false;
            DebugEvent.Send(new BreakpointEvent(this, BoundBreakpointsEnum.Create(breakpoint)));
        }

        static bool BreakAllProcesses
        {
            get => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return (bool)QtVsToolsPackage.Instance.Dte
                    .Properties["Debugging", "General"]
                    .Item("BreakAllProcesses")
                    .Value;
            });
            set => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                QtVsToolsPackage.Instance.Dte
                    .Properties["Debugging", "General"]
                    .Item("BreakAllProcesses")
                    .let_Value(value ? "True" : "False");
            });
        }

        #endregion //////////////////// Breakpoints ///////////////////////////////////////////////


        #region //////////////////// Call Stack ///////////////////////////////////////////////////

        void IDebuggerEventSink.NotifyStackContext(IList<FrameInfo> frames)
        {
            CurrentFrames.Clear();
            foreach (var frame in frames) {
                CurrentFrames.Add(StackFrame.Create(frame.Name, frame.Number, frame.Scopes,
                    CodeContext.Create(Engine, this,
                    Engine.FileSystem[frame.QrcPath].FilePath, (uint)frame.Line)));
            }
        }

        public void Refresh()
        {
            CurrentFrames.ForEach(x => x.Refresh());
        }

        int IDebugThread2.EnumFrameInfo(
            enum_FRAMEINFO_FLAGS dwFieldSpec,
            uint nRadix,
            out IEnumDebugFrameInfo2 ppEnum)
        {
            ppEnum = null;

            if (CurrentFrames is not { Count: not 0 }) {
                ppEnum = FrameInfoEnum.Create();
                return VSConstants.S_OK;
            }

            var frameInfos = new List<FRAMEINFO>();
            foreach (var frame in CurrentFrames) {
                var frameInfo = new FRAMEINFO[1];
                (frame as IDebugStackFrame2).GetInfo(dwFieldSpec, nRadix, frameInfo);
                frameInfos.Add(frameInfo[0]);
            }

            ppEnum = FrameInfoEnum.Create(frameInfos);
            return VSConstants.S_OK;
        }

        #endregion //////////////////// Call Stack ////////////////////////////////////////////////


        #region //////////////////// Info /////////////////////////////////////////////////////////

        class ProgramInfo : InfoHelper<ProgramInfo>
        {
            public uint? ThreadId { get; set; }
            public uint? SuspendCount { get; set; }
            public uint? ThreadState { get; set; }
            public string Priority { get; set; }
            public string Name { get; set; }
            public string Location { get; set; }
            public string DisplayName { get; set; }
            public uint? DisplayNamePriority { get; set; }
            public uint? ThreadCategory { get; set; }
            public uint? AffinityMask { get; set; }
            public int? PriorityId { get; set; }
            public string ModuleName { get; set; }
            public string ModuleUrl { get; set; }
        }

        ProgramInfo Info => new()
        {
            ThreadId = Debugger.ThreadId,
            SuspendCount = 0,
            ThreadCategory = 0,
            AffinityMask = 0,
            PriorityId = 0,
            ThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING,
            Priority = "Normal",
            Location = "",
            Name = Name,
            DisplayName = Name,
            DisplayNamePriority = 10, // Give this display name a higher priority
                                      // than the default (0) so that it will
                                      // actually be displayed
            ModuleName = Path.GetFileName(ExecPath),
            ModuleUrl = ExecPath

        };

        static readonly ProgramInfo.Mapping MappingToTHREADPROPERTIES =

        #region //////////////////// THREADPROPERTIES <-- ProgramInfo /////////////////////////////
            // r: Ref<THREADPROPERTIES>
            // f: enum_THREADPROPERTY_FIELDS
            // i: ProgramInfo
            // v: value of i.<<property>>

            new ProgramInfo.Mapping<THREADPROPERTIES, enum_THREADPROPERTY_FIELDS>
                ((r, f) => r.s.dwFields |= f)
            {
                { enum_THREADPROPERTY_FIELDS.TPF_ID,
                    (r, v) => r.s.dwThreadId = v, i => i.ThreadId },

                { enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT,
                    (r, v) => r.s.dwSuspendCount = v, i => i.SuspendCount },

                { enum_THREADPROPERTY_FIELDS.TPF_STATE,
                    (r, v) => r.s.dwThreadState = v, i => i.ThreadState },

                { enum_THREADPROPERTY_FIELDS.TPF_PRIORITY,
                    (r, v) => r.s.bstrPriority = v, i => i.Priority },

                { enum_THREADPROPERTY_FIELDS.TPF_NAME,
                    (r, v) => r.s.bstrName = v, i => i.Name },

                { enum_THREADPROPERTY_FIELDS.TPF_LOCATION,
                    (r, v) => r.s.bstrLocation = v, i => i.Location }
            };

        #endregion //////////////////// THREADPROPERTIES <-- ProgramInfo //////////////////////////


        static readonly ProgramInfo.Mapping MappingToTHREADPROPERTIES100 =

        #region //////////////////// THREADPROPERTIES100 <-- ProgramInfo //////////////////
            // r: Ref<THREADPROPERTIES100>
            // f: enum_THREADPROPERTY_FIELDS100
            // i: ProgramInfo
            // v: value of i.<<property>>

            new ProgramInfo.Mapping<THREADPROPERTIES100, enum_THREADPROPERTY_FIELDS100>
                ((r, f) => r.s.dwFields |= (uint)f)
            {
                { enum_THREADPROPERTY_FIELDS100.TPF100_ID,
                    (r, v) => r.s.dwThreadId = v, i => i.ThreadId },

                { enum_THREADPROPERTY_FIELDS100.TPF100_SUSPENDCOUNT,
                    (r, v) => r.s.dwSuspendCount = v, i => i.SuspendCount },

                { enum_THREADPROPERTY_FIELDS100.TPF100_STATE,
                    (r, v) => r.s.dwThreadState = v, i => i.ThreadState },

                { enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY,
                    (r, v) => r.s.bstrPriority = v, i => i.Priority },

                { enum_THREADPROPERTY_FIELDS100.TPF100_NAME,
                    (r, v) => r.s.bstrName = v, i => i.Name },

                { enum_THREADPROPERTY_FIELDS100.TPF100_LOCATION,
                    (r, v) => r.s.bstrLocation = v, i => i.Location },

                { enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME,
                    (r, v) => r.s.bstrDisplayName = v, i => i.DisplayName },

                { enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME,
                  enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME_PRIORITY,
                    (r, v) => r.s.DisplayNamePriority = v, i => i.DisplayNamePriority },

                { enum_THREADPROPERTY_FIELDS100.TPF100_CATEGORY,
                    (r, v) => r.s.dwThreadCategory = v, i => i.ThreadCategory },

                { enum_THREADPROPERTY_FIELDS100.TPF100_AFFINITY,
                    (r, v) => r.s.AffinityMask = v, i => i.AffinityMask },

                { enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY_ID,
                    (r, v) => r.s.priorityId = v, i => i.PriorityId }
            };

        #endregion //////////////////// THREADPROPERTIES100 <-- ProgramInfo ///////////////////////


        static readonly ProgramInfo.Mapping MappingToMODULE_INFO =

        #region //////////////////// MODULE_INFO <-- ProgramInfo //////////////////////////////////
            // r: Ref<MODULE_INFO>
            // f: enum_MODULE_INFO_FIELDS
            // i: ProgramInfo
            // v: value of i.<<property>>

            new ProgramInfo.Mapping<MODULE_INFO, enum_MODULE_INFO_FIELDS>
                ((r, bit) => r.s.dwValidFields |= bit)
            {
                { enum_MODULE_INFO_FIELDS.MIF_NAME,
                    (r, v) => r.s.m_bstrName = v, i => i.ModuleName },

                { enum_MODULE_INFO_FIELDS.MIF_URL,
                    (r, v) => r.s.m_bstrUrl = v, i => i.ModuleUrl }
            };

        #endregion //////////////////// MODULE_INFO <-- ProgramInfo ///////////////////////////////


        public int /*IDebugProgram3*/ GetName(out string pbstrName)
        {
            pbstrName = Name;
            return VSConstants.S_OK;
        }

        int IDebugProgramNode2.GetProgramName(out string pbstrProgramName)
        {
            return GetName(out pbstrProgramName);
        }

        int IDebugProgramNode2.GetEngineInfo(out string pbstrEngine, out Guid pguidEngine)
        {
            pbstrEngine = "QML";
            pguidEngine = QmlEngine.Id;
            return VSConstants.S_OK;
        }

        int IDebugProgramNode2.GetHostPid(AD_PROCESS_ID[] pHostProcessId)
        {
            pHostProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pHostProcessId[0].guidProcessId = ProcessId;
            return VSConstants.S_OK;
        }

        int IDebugProcess2.GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            pProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pProcessId[0].guidProcessId = ProcessId;
            return VSConstants.S_OK;
        }

        int IDebugProcess2.GetProcessId(out Guid pguidProcessId)
        {
            pguidProcessId = ProcessId;
            return VSConstants.S_OK;
        }

        int IDebugProcess2.GetPort(out IDebugPort2 ppPort)
        {
            return NativeProc.GetPort(out ppPort);
        }

        public int /*IDebugProgram3*/ GetProgramId(out Guid pguidProgramId)
        {
            pguidProgramId = ProgramId;
            return VSConstants.S_OK;
        }

        int IDebugThread2.GetThreadProperties(
            enum_THREADPROPERTY_FIELDS dwFields,
            THREADPROPERTIES[] ptp)
        {
            Info.Map(MappingToTHREADPROPERTIES, dwFields, out ptp[0]);
            return VSConstants.S_OK;
        }

        int IDebugThread100.GetThreadProperties100(uint dwFields, THREADPROPERTIES100[] ptp)
        {
            Info.Map(MappingToTHREADPROPERTIES100, dwFields, out ptp[0]);
            return VSConstants.S_OK;
        }

        int IDebugThread2.GetName(out string pbstrName)
        {
            pbstrName = Name;
            return VSConstants.S_OK;
        }

        int IDebugThread2.GetThreadId(out uint pdwThreadId)
        {
            pdwThreadId = (uint)Debugger.ThreadId;
            return VSConstants.S_OK;
        }

        public int /*IDebugModule3*/ GetInfo(enum_MODULE_INFO_FIELDS dwFields, MODULE_INFO[] pinfo)
        {
            Info.Map(MappingToMODULE_INFO, dwFields, out pinfo[0]);
            return VSConstants.S_OK;
        }

        public int /*IDebugProgram3*/ EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            ppEnum = ThreadEnum.Create(this);
            return VSConstants.S_OK;
        }

        public int /*IDebugProgram3*/ EnumModules(out IEnumDebugModules2 ppEnum)
        {
            ppEnum = ModuleEnum.Create(this);
            return VSConstants.S_OK;
        }

        #endregion //////////////////// Info //////////////////////////////////////////////////////

    }
}
