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
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace QtVsTools.Qml.Debug.AD7
{
    [ComVisible(true)]
    [Guid(CLSID_ENGINE)]
    sealed partial class QmlEngine :

        IDebugEngine2,       // "This interface represents a debug engine (DE). It is used to manage
                             //  various aspects of a debugging session, from creating breakpoints
                             //  to setting and clearing exceptions."

        IDebugEngineLaunch2, // "Used by a debug engine (DE) to launch and terminate programs."

        IDebugEventCallback2 // "This interface is used by the debug engine (DE) to send debug
                             //  events to the session debug manager (SDM)."
    {
        const string CLSID_ENGINE = "fa2993e3-8b2a-40a6-8853-ac2db2daed5a";
        public static readonly Guid ClassId = new Guid(CLSID_ENGINE);

        const string ID_ENGINE = "86102a1b-4378-4964-a7ed-21852a8afb7f";
        public static readonly Guid Id = new Guid(ID_ENGINE);

        public IDebugEventCallback2 Callback { get; private set; }

        public FileSystem FileSystem { get; private set; }

        int IDebugEventCallback2.Event(
            IDebugEngine2 pEngine,
            IDebugProcess2 pProcess,
            IDebugProgram2 pProgram,
            IDebugThread2 pThread,
            IDebugEvent2 pEvent,
            ref Guid riidEvent,
            uint dwAttrib)
        {
            if (Callback == null)
                return VSConstants.S_OK;
            return Callback.Event(pEngine, pProcess,
                pProgram, pThread, pEvent, ref riidEvent, dwAttrib);
        }

        public QmlEngine()
        {
            FileSystem = FileSystem.Create();
        }

        Dictionary<Guid, Program> programs = new Dictionary<Guid, Program>();
        public IEnumerable<Program> Programs
        {
            get { return ThreadSafe(() => programs.Values.ToList()); }
        }

        HashSet<PendingBreakpoint> pendingBreakpoints = new HashSet<PendingBreakpoint>();
        public IEnumerable<PendingBreakpoint> PendingBreakpoints
        {
            get { return ThreadSafe(() => pendingBreakpoints.ToList()); }
        }

        int IDebugEngine2.GetEngineId(out Guid pguidEngine)
        {
            pguidEngine = Id;
            return VSConstants.S_OK;
        }

        int IDebugEngineLaunch2.LaunchSuspended(string pszServer, IDebugPort2 pPort,
            string pszExe, string pszArgs, string pszDir, string bstrEnv, string pszOptions,
            enum_LAUNCH_FLAGS dwLaunchFlags, uint hStdInput, uint hStdOutput, uint hStdError,
            IDebugEventCallback2 pCallback, out IDebugProcess2 ppProcess)
        {
            ppProcess = null;

            if (string.IsNullOrEmpty(pszOptions))
                return VSConstants.E_FAIL;

            uint procId;
            if (!uint.TryParse(pszOptions, out procId))
                return VSConstants.E_FAIL;

            var env = bstrEnv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new[] { '=' }))
                .Where(x => x.Length >= 2)
                .ToDictionary(x => x[0], x => x[1].Split(new[] { ';' }));

            if (env.ContainsKey("QTRCC")) {
                foreach (var rccFile in env["QTRCC"])
                    FileSystem.RegisterRccFile(rccFile);
            }

            IDebugProcess2 nativeProc;
            var nativeProcId = new AD_PROCESS_ID
            {
                ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM,
                dwProcessId = procId
            };
            if (pPort.GetProcess(nativeProcId, out nativeProc) != VSConstants.S_OK)
                return VSConstants.E_FAIL;

            var program = Program.Create(this, nativeProc, pszExe, pszArgs);
            if (program == null)
                return VSConstants.E_FAIL;

            programs.Add(program.ProcessId, program);
            ppProcess = program;
            return VSConstants.S_OK;
        }

        int IDebugEngineLaunch2.ResumeProcess(IDebugProcess2 process)
        {
            var program = process as Program;
            if (program == null)
                return VSConstants.E_FAIL;

            IDebugPort2 port;
            if (process.GetPort(out port) != VSConstants.S_OK)
                return VSConstants.E_FAIL;

            string portName;
            port.GetPortName(out portName);

            Guid guidPort;
            port.GetPortId(out guidPort);

            IDebugDefaultPort2 defaultPort = (IDebugDefaultPort2)port;

            IDebugPortNotify2 portNotify;
            if (defaultPort.GetPortNotify(out portNotify) != VSConstants.S_OK)
                return VSConstants.E_FAIL;

            if (portNotify.AddProgramNode(program) != VSConstants.S_OK)
                return VSConstants.E_FAIL;

            return VSConstants.S_OK;
        }

        int IDebugEngine2.Attach(
            IDebugProgram2[] rgpPrograms,
            IDebugProgramNode2[] rgpProgramNodes,
            uint celtPrograms,
            IDebugEventCallback2 pCallback,
            enum_ATTACH_REASON dwReason)
        {
            var program = rgpProgramNodes[0] as Program;
            if (program == null)
                return VSConstants.E_FAIL;

            Callback = pCallback;

            DebugEvent.Send(new EngineCreateEvent(this));

            Guid pguidProgramId;
            if (rgpPrograms[0].GetProgramId(out pguidProgramId) != VSConstants.S_OK)
                return VSConstants.E_FAIL;

            program.ProgramId = pguidProgramId;

            DebugEvent.Send(new ProgramCreateEvent(program));
            DebugEvent.Send(new ThreadCreateEvent(program));
            DebugEvent.Send(new LoadCompleteEvent(program));
            DebugEvent.Send(new EntryPointEvent(program));

            program.OutputWriteLine("Connecting to the QML runtime...");

            return VSConstants.S_OK;
        }

        int IDebugEngineLaunch2.CanTerminateProcess(IDebugProcess2 pProcess)
        {
            Guid procId;
            if (pProcess.GetProcessId(out procId) != VSConstants.S_OK)
                return VSConstants.E_FAIL;

            Program program;
            if (!programs.TryGetValue(procId, out program))
                return VSConstants.S_FALSE;

            return VSConstants.S_OK;
        }

        public bool ProgramIsRunning(Program program)
        {
            return programs.ContainsKey(program.ProcessId);
        }

        int IDebugEngineLaunch2.TerminateProcess(IDebugProcess2 pProcess)
        {
            Guid procId;
            if (pProcess.GetProcessId(out procId) != VSConstants.S_OK)
                return VSConstants.E_FAIL;

            Program program;
            if (!programs.TryGetValue(procId, out program))
                return VSConstants.S_FALSE;

            programs.Remove(procId);

            DebugEvent.Send(new ThreadDestroyEvent(program, 0));
            DebugEvent.Send(new ProgramDestroyEvent(program, 0));

            return VSConstants.S_OK;
        }

        int IDebugEngine2.ContinueFromSynchronousEvent(IDebugEvent2 pEvent)
        {
            var evtProgramDestroy = pEvent as ProgramDestroyEvent;
            if (evtProgramDestroy != null)
                evtProgramDestroy.Program.Dispose();

            return VSConstants.S_OK;
        }

        int IDebugEngine2.CreatePendingBreakpoint(
            IDebugBreakpointRequest2 pBPRequest,
            out IDebugPendingBreakpoint2 ppPendingBP)
        {
            ppPendingBP = null;

            var pendingBreakpoint = PendingBreakpoint.Create(this, pBPRequest);
            if (pendingBreakpoint == null)
                return VSConstants.E_FAIL;

            ppPendingBP = pendingBreakpoint;
            pendingBreakpoints.Add(pendingBreakpoint);

            return VSConstants.S_OK;
        }

        public void DisposePendingBreakpoint(PendingBreakpoint pendingBreakpoint)
        {
            pendingBreakpoints.Remove(pendingBreakpoint);
            pendingBreakpoint.Dispose();
        }

        public void OutputWriteLine(string msg)
        {
            DebugEvent.Send(new OutputStringEvent(this, msg + "\r\n"));
        }

        #region //////////////////// Concurrent ///////////////////////////////////////////////////

        LocalConcurrent concurrent = new LocalConcurrent();
        class LocalConcurrent : Concurrent
        {
            public void LocalThreadSafe(Action action)
            { ThreadSafe(action); }

            public T LocalThreadSafe<T>(Func<T> func)
            { return ThreadSafe(func); }

            public new bool Atomic(Func<bool> test, Action action, Action actionElse = null)
            { return base.Atomic(test, action, actionElse); }
        }

        void ThreadSafe(Action action)
        {
            concurrent.LocalThreadSafe(action);
        }

        T ThreadSafe<T>(Func<T> func)
        {
            return concurrent.LocalThreadSafe(func);
        }

        bool Atomic(Func<bool> test, Action action, Action actionElse = null)
        {
            return concurrent.Atomic(test, action, actionElse);
        }

        #endregion //////////////////// Concurrent ////////////////////////////////////////////////
    }

    [ComVisible(true)]
    [Guid(CLSID_PROGRAMPROVIDER)]
    sealed partial class ProgramProvider :

        IDebugProgramProvider2 // "This registered interface allows the session debug manager (SDM)
                               //  to obtain information about programs that have been "published"
                               //  through the IDebugProgramPublisher2 interface."
    {
        public const string CLSID_PROGRAMPROVIDER = "f2ff34e2-7fa5-461b-9e59-b5997ee0a637";
        public static readonly Guid ClassId = new Guid(CLSID_PROGRAMPROVIDER);

        public ProgramProvider()
        { }
    }

    public static class NativeEngine
    {
        const string ID_NATIVEENGINE = "3b476d35-a401-11d2-aad4-00c04f990171";
        public static readonly Guid Id = new Guid(ID_NATIVEENGINE);

        const string ID_LANGUAGE_CPP = "3a12d0b7-c26c-11d0-b442-00a0244a1dd2";
        public static Guid IdLanguageCpp = new Guid(ID_LANGUAGE_CPP);
    }
}
