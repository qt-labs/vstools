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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace QtVsTools.Qml.Debug.AD7
{
    sealed partial class PendingBreakpoint : Disposable,

        IDebugPendingBreakpoint2 // "This interface represents a breakpoint that is ready to bind
                                 //  to a code location."
    {
        static readonly string[] ValidExtensions = new string[] { ".qml", ".js" };

        public QmlEngine Engine { get; private set; }
        public IDebugBreakpointRequest2 Request { get; private set; }
        public enum_BP_LOCATION_TYPE LocationType { get; private set; }
        public BP_REQUEST_INFO RequestInfo { get; private set; }
        public string FileName { get; private set; }
        public TEXT_POSITION BeginPosition { get; private set; }
        public TEXT_POSITION EndPosition { get; private set; }
        public bool Enabled { get; private set; }

        HashSet<Breakpoint> breakpoints;

        public static PendingBreakpoint Create(QmlEngine engine, IDebugBreakpointRequest2 request)
        {
            var _this = new PendingBreakpoint();
            return _this.Initialize(engine, request) ? _this : null;
        }

        private PendingBreakpoint()
        { }

        private bool Initialize(QmlEngine engine, IDebugBreakpointRequest2 request)
        {
            var locationType = new enum_BP_LOCATION_TYPE[1];
            if (request.GetLocationType(locationType) != VSConstants.S_OK)
                return false;

            var requestInfo = new BP_REQUEST_INFO[1];
            if (request.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_ALLFIELDS, requestInfo)
                != VSConstants.S_OK) {
                return false;
            }

            if (requestInfo[0].bpLocation.bpLocationType
                != (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE) {
                return false;
            }

            var docPosition = Marshal.GetObjectForIUnknown(requestInfo[0].bpLocation.unionmember2)
                as IDebugDocumentPosition2;
            if (docPosition == null)
                return false;

            string fileName;
            if (docPosition.GetFileName(out fileName) != VSConstants.S_OK)
                return false;

            if (!ValidExtensions.Where(x => string.Equals(x, Path.GetExtension(fileName))).Any())
                return false;

            TEXT_POSITION[] beginPosition = new TEXT_POSITION[1];
            TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
            if (docPosition.GetRange(beginPosition, endPosition) != VSConstants.S_OK)
                return false;

            Engine = engine;
            Request = request;
            LocationType = locationType[0];
            RequestInfo = requestInfo[0];
            FileName = fileName;
            BeginPosition = beginPosition[0];
            EndPosition = endPosition[0];

            breakpoints = new HashSet<Breakpoint>();

            return true;
        }

        protected override void DisposeManaged()
        {
            foreach (var breakpoint in ThreadSafe(() => breakpoints.ToList()))
                breakpoint.Dispose();

            ThreadSafe(() => breakpoints.Clear());
        }

        public void DisposeBreakpoint(Breakpoint breakpoint)
        {
            ThreadSafe(() => breakpoints.Remove(breakpoint));
            breakpoint.Dispose();
        }

        int IDebugPendingBreakpoint2.Bind()
        {
            foreach (var program in Engine.Programs) {
                var breakpoint = Breakpoint.Create(this, program);
                ThreadSafe(() => breakpoints.Add(breakpoint));
                program.SetBreakpoint(breakpoint);
            }
            return VSConstants.S_OK;
        }

        int IDebugPendingBreakpoint2.Enable(int fEnable)
        {
            bool enable = (fEnable != 0);
            if (Atomic(() => Enabled != enable, () => Enabled = enable)) {
                foreach (var breakpoint in ThreadSafe(() => breakpoints.ToList()))
                    (breakpoint as IDebugBoundBreakpoint2).Enable(fEnable);
            }
            return VSConstants.S_OK;
        }

        int IDebugPendingBreakpoint2.Delete()
        {
            Engine.DisposePendingBreakpoint(this);
            return VSConstants.S_OK;
        }

        int IDebugPendingBreakpoint2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            ppEnum = BoundBreakpointsEnum.Create(ThreadSafe(() => breakpoints.ToList()));
            return VSConstants.S_OK;
        }

        int IDebugPendingBreakpoint2.GetState(PENDING_BP_STATE_INFO[] pState)
        {
            if (Disposed) {
                pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_DELETED;

            } else if (Enabled) {
                pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_ENABLED;

            } else {
                pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_DISABLED;
            }

            return VSConstants.S_OK;
        }

        int IDebugPendingBreakpoint2.GetBreakpointRequest(out IDebugBreakpointRequest2 ppBPRequest)
        {
            ppBPRequest = Request;
            return VSConstants.S_OK;
        }
    }

    sealed partial class Breakpoint : Disposable, IBreakpoint,

        IDebugBoundBreakpoint2,     // "This interface represents a breakpoint that is bound to a
                                    //  code location."

        IDebugBreakpointResolution2 // "This interface represents the information that describes a
                                    //  bound breakpoint."
    {
        public QmlDebugger Debugger { get; private set; }
        public QmlEngine Engine { get; private set; }
        public Program Program { get; private set; }
        public PendingBreakpoint Parent { get; private set; }
        public CodeContext CodeContext { get; private set; }
        public bool Enabled { get; set; }

        bool supressNotify;

        string IBreakpoint.QrcPath
        {
            get
            {
                var qrcPath = Engine.FileSystem[Parent.FileName].QrcPath;
                if (qrcPath.StartsWith("qrc:///", StringComparison.InvariantCultureIgnoreCase))
                    qrcPath = qrcPath.Substring("qrc:///".Length);
                return qrcPath;
            }
        }

        uint IBreakpoint.Line
        {
            get { return Parent.BeginPosition.dwLine; }
        }

        public static Breakpoint Create(PendingBreakpoint parent, Program program)
        {
            return new Breakpoint
            {
                Engine = parent.Engine,
                Parent = parent,
                Program = program,
                Debugger = program.Debugger,
                Enabled = parent.Enabled,
                CodeContext = CodeContext.Create(
                    parent.Engine,
                    program,
                    parent.FileName,
                    parent.BeginPosition.dwLine),
            };
        }

        private Breakpoint()
        { }

        protected override void DisposeManaged()
        {
            Program.ClearBreakpoint(this);
        }

        int IDebugBoundBreakpoint2.Enable(int fEnable)
        {
            bool enable = (fEnable != 0);
            if (Atomic(() => Enabled != enable,
                       () => { Enabled = enable; supressNotify = true; })) {

                if (enable)
                    Debugger.SetBreakpoint(this);
                else
                    Debugger.ClearBreakpoint(this);
            }

            return VSConstants.S_OK;
        }

        int IDebugBoundBreakpoint2.Delete()
        {
            Parent.DisposeBreakpoint(this);
            return VSConstants.S_OK;
        }

        void IBreakpoint.NotifySet()
        {
            if (!Atomic(() => supressNotify, () => supressNotify = false))
                Program.NotifyBreakpointSet(this);
        }

        void IBreakpoint.NotifyClear()
        {
            if (!Atomic(() => supressNotify, () => supressNotify = false))
                Program.NotifyBreakpointCleared(this);
        }

        void IBreakpoint.NotifyBreak()
        {
            Program.NotifyBreakpointHit(this);
        }

        void IBreakpoint.NotifyError(string errorMessage)
        {
            QtProjectLib.Messages.PaneMessage(Vsix.Instance.Dte,
                string.Format("QML Debug: {0}", errorMessage));
        }

        int IDebugBoundBreakpoint2.GetPendingBreakpoint(
            out IDebugPendingBreakpoint2 ppPendingBreakpoint)
        {
            ppPendingBreakpoint = Parent;
            return VSConstants.S_OK;
        }

        int IDebugBoundBreakpoint2.GetState(enum_BP_STATE[] pState)
        {
            pState[0] = 0;
            if (Disposed) {
                pState[0] = enum_BP_STATE.BPS_DELETED;

            } else if (Enabled) {
                pState[0] = enum_BP_STATE.BPS_ENABLED;

            } else {
                pState[0] = enum_BP_STATE.BPS_DISABLED;
            }

            return VSConstants.S_OK;
        }


        #region //////////////////// IDebugBreakpointResolution2 //////////////////////////////////

        int IDebugBoundBreakpoint2.GetBreakpointResolution(
            out IDebugBreakpointResolution2 ppBPResolution)
        {
            ppBPResolution = this;
            return VSConstants.S_OK;
        }

        int IDebugBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
        {
            pBPType[0] = enum_BP_TYPE.BPT_CODE;
            return VSConstants.S_OK;
        }

        int IDebugBreakpointResolution2.GetResolutionInfo(
            enum_BPRESI_FIELDS dwFields,
            BP_RESOLUTION_INFO[] pBPResolutionInfo)
        {
            if ((dwFields & enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION) != 0) {
                BP_RESOLUTION_LOCATION location = new BP_RESOLUTION_LOCATION();
                location.bpType = (uint)enum_BP_TYPE.BPT_CODE;
                location.unionmember1
                    = Marshal.GetComInterfaceForObject(CodeContext, typeof(IDebugCodeContext2));
                pBPResolutionInfo[0].bpResLocation = location;
                pBPResolutionInfo[0].dwFields |= enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION;
            }
            if ((dwFields & enum_BPRESI_FIELDS.BPRESI_PROGRAM) != 0) {
                pBPResolutionInfo[0].pProgram = Program as IDebugProgram2;
                pBPResolutionInfo[0].dwFields |= enum_BPRESI_FIELDS.BPRESI_PROGRAM;
            }

            return VSConstants.S_OK;
        }

        #endregion //////////////////// IDebugBreakpointResolution2 ///////////////////////////////

    }
}
