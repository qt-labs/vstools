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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QtVsTools.Qml.Debug
{
    using Common;
    using SyntaxAnalysis;
    using V4;

    using RegExprParser = SyntaxAnalysis.RegExpr.Parser;

    using static SyntaxAnalysis.RegExpr;

    struct FrameInfo
    {
        public int Number;
        public string QrcPath;
        public int Line;
        public string Name;
        public List<int> Scopes;
    }

    interface IDebuggerEventSink
    {
        bool QueryRuntimeFrozen();
        void NotifyClientDisconnected();
        void NotifyStackContext(IList<FrameInfo> frames);
        void NotifyBreak();
        void NotifyError(string errorMessage);
    }

    interface IBreakpoint
    {
        string QrcPath { get; }
        uint Line { get; }
        void NotifySet();
        void NotifyClear();
        void NotifyBreak();
        void NotifyError(string errorMessage);
    }

    class QmlDebugger : Disposable, IMessageEventSink
    {
        static LazyFactory StaticLazy { get; } = new LazyFactory();

        IDebuggerEventSink sink;
        ProtocolDriver driver;
        string connectionHostName;
        ushort connectionHostPortFrom;
        ushort connectionHostPortTo;
        string connectionFileName;
        bool connectionBlock;

        List<Request> outbox;
        Dictionary<int, IBreakpoint> breakpoints;

        private bool Started { get; set; }

        private bool Running { get; set; }

        private string Version { get; set; }

        public uint? ThreadId { get { return driver.ThreadId; } }

        public static QmlDebugger Create(IDebuggerEventSink sink, string execPath, string args)
        {
            var _this = new QmlDebugger();
            return _this.Initialize(sink, execPath, args) ? _this : null;
        }

        private QmlDebugger()
        { }

        private bool Initialize(IDebuggerEventSink sink, string execPath, string args)
        {
            this.sink = sink;
            if (sink == null)
                return false;

            if (!ParseCommandLine(execPath, args,
                out connectionHostPortFrom, out connectionHostPortTo,
                out connectionHostName, out connectionFileName, out connectionBlock)) {
                return false;
            }

            driver = ProtocolDriver.Create(this);
            if (driver == null)
                return false;

            outbox = new List<Request>();
            breakpoints = new Dictionary<int, IBreakpoint>();
            return true;
        }

        protected override void DisposeManaged()
        {
            driver.Dispose();
        }

        void ConnectToDebugger()
        {
            if (!string.IsNullOrEmpty(connectionFileName))
                driver.StartLocalServer(connectionFileName).WaitOne();
            else
                driver.Connect(connectionHostName, connectionHostPortFrom).WaitOne();

            if (driver.ConnectionState != DebugClientState.Connected) {
                sink.NotifyClientDisconnected();
                return;
            }

            var reqVersion = Message.Create<VersionRequest>(driver);
            var resVersion = reqVersion.Send();
            if (resVersion != null)
                ThreadSafe(() => Version = resVersion.Body.Version);

            foreach (var request in ThreadSafe(() => outbox.ToList()))
                request.Send();

            ThreadSafe(() => outbox.Clear());

            Message.Send<ConnectMessage>(driver);
        }

        bool IMessageEventSink.QueryRuntimeFrozen()
        {
            return sink.QueryRuntimeFrozen();
        }

        public void Run()
        {
            EnterCriticalSection();

            if (!Started) {
                Running = Started = true;
                LeaveCriticalSection();
                _ = Task.Run(() => ConnectToDebugger());

            } else if (!Running) {
                Running = true;
                LeaveCriticalSection();
                Request.Send<ContinueRequest>(driver);

            } else {
                LeaveCriticalSection();
            }
        }

        public void StepOver()
        {
            var reqContinue = Message.Create<ContinueRequest>(driver);
            reqContinue.Arguments.StepAction = ContinueRequest.StepAction.Next;
            reqContinue.Send();
        }

        public void StepInto()
        {
            var reqContinue = Message.Create<ContinueRequest>(driver);
            reqContinue.Arguments.StepAction = ContinueRequest.StepAction.StepIn;
            reqContinue.Send();
        }

        public void StepOut()
        {
            var reqContinue = Message.Create<ContinueRequest>(driver);
            reqContinue.Arguments.StepAction = ContinueRequest.StepAction.StepOut;
            reqContinue.Send();
        }

        public void SetBreakpoint(IBreakpoint breakpoint)
        {
            var setBreakpoint = Message.Create<SetBreakpointRequest>(driver);
            setBreakpoint.Arguments.TargetType = SetBreakpointRequest.TargetType.ScriptRegExp;
            setBreakpoint.Arguments.Target = breakpoint.QrcPath;
            setBreakpoint.Arguments.Line = (int)breakpoint.Line;
            setBreakpoint.Tag = breakpoint;
            if (driver.ConnectionState == DebugClientState.Connected)
                setBreakpoint.SendRequest();
            else
                ThreadSafe(() => outbox.Add(setBreakpoint));
        }

        void SetBreakpointResponded(SetBreakpointRequest reqSetBreak)
        {
            System.Diagnostics.Debug.Assert(reqSetBreak.Response != null);

            var breakpoint = reqSetBreak.Tag as IBreakpoint;
            System.Diagnostics.Debug.Assert(breakpoint != null);

            if (reqSetBreak.Response.Success) {
                ThreadSafe(() => breakpoints[reqSetBreak.Response.Body.Breakpoint] = breakpoint);
                breakpoint.NotifySet();
            } else {
                breakpoint.NotifyError(reqSetBreak.Response.Message);
            }
        }

        public void ClearBreakpoint(IBreakpoint breakpoint)
        {
            var breakpointNum = ThreadSafe(() => breakpoints
                .ToDictionary(x => x.Value, x => x.Key));

            if (!breakpointNum.ContainsKey(breakpoint))
                return;

            var reqClearBreak = Message.Create<ClearBreakpointRequest>(driver);
            reqClearBreak.Arguments.Breakpoint = breakpointNum[breakpoint];
            reqClearBreak.SendRequest();
        }

        void RefreshFrames()
        {
            var frames = new List<FrameInfo>();
            currentScope = null;

            var reqBacktrace = Message.Create<BacktraceRequest>(driver);
            var resBacktrace = reqBacktrace.Send();
            if (resBacktrace != null && resBacktrace.Success) {

                foreach (var frameRef in resBacktrace.Body.Frames) {
                    var reqFrame = Message.Create<FrameRequest>(driver);
                    reqFrame.Arguments.FrameNumber = frameRef.Index;

                    var resFrame = reqFrame.Send();
                    if (resFrame == null)
                        continue;

                    var frame = new FrameInfo
                    {
                        Number = resFrame.Frame.Index,
                        Name = resFrame.Frame.Function,
                        QrcPath = resFrame.Frame.Script,
                        Line = resFrame.Frame.Line,
                        Scopes = new List<int>()
                    };

                    foreach (var scope in resFrame.Frame.Scopes
                        .Where(x => x.Type != Scope.ScopeType.Global)) {
                        frame.Scopes.Add(scope.Index);
                    }

                    frames.Add(frame);
                }
            } else if (resBacktrace != null) {
                sink.NotifyError(resBacktrace.Message);
            } else {
                sink.NotifyError("Error sending 'backtrace' message to QML runtime.");
            }
            sink.NotifyStackContext(frames);
        }

        void BreakNotified(BreakEvent evtBreak)
        {
            Running = false;

            RefreshFrames();

            if (evtBreak.Body.Breakpoints == null || evtBreak.Body.Breakpoints.Count == 0) {
                sink.NotifyBreak();

            } else {
                foreach (int breakpointId in evtBreak.Body.Breakpoints) {
                    if (!breakpoints.TryGetValue(breakpointId, out IBreakpoint breakpoint))
                        continue;
                    breakpoint.NotifyBreak();
                }
            }
        }

        Scope currentScope = null;

        Scope MoveToScope(int frameNumber, int scopeNumber)
        {
            lock (CriticalSection) {
                if (currentScope != null
                    && currentScope.FrameIndex == frameNumber
                    && currentScope.Index == scopeNumber) {
                    return currentScope;
                }

                var reqScope = Message.Create<ScopeRequest>(driver);
                reqScope.Arguments.FrameNumber = frameNumber;
                reqScope.Arguments.ScopeNumber = scopeNumber;

                var resScope = reqScope.Send();
                if (resScope == null)
                    return null;

                return currentScope = resScope.Scope;
            }
        }

        public IEnumerable<JsValue> RefreshScope(
            int frameNumber,
            int scopeNumber,
            bool forceScope = false)
        {
            if (forceScope)
                currentScope = null;

            var vars = new SortedList<string, JsValue>();
            lock (CriticalSection) {

                var scope = MoveToScope(frameNumber, scopeNumber);
                if (scope == null)
                    return null;

                var scopeObj = ((JsValue)scope.Object) as JsObject;
                if (scopeObj == null)
                    return null;

                scopeObj.Properties
                    .Where(x => x.HasData && !string.IsNullOrEmpty(((JsValue)x).Name))
                    .Select(x => new { name = ((JsValue)x).Name, value = (JsValue)x })
                    .ToList().ForEach(x => vars.Add(x.name, x.value));

                if (scope.Type == Scope.ScopeType.Local) {
                    var reqEval = Message.Create<EvaluateRequest>(driver);
                    reqEval.Arguments.Expression = "this";
                    reqEval.Arguments.Frame = frameNumber;

                    var resEval = reqEval.Send();
                    if (resEval != null && resEval.Result.HasData) {
                        JsValue resValue = resEval.Result;
                        resValue.Name = "this";
                        vars.Add(resValue.Name, resValue);
                    }
                }
            }
            return vars.Values;
        }

        public JsObject Lookup(int frameNumber, int scopeNumber, JsObjectRef objRef)
        {
            if (MoveToScope(frameNumber, scopeNumber) == null)
                return null;

            var reqLookup = Message.Create<LookupRequest>(driver);
            reqLookup.Arguments.Handles = new List<int> { objRef.Ref };

            var resLookup = reqLookup.Send();
            if (resLookup == null)
                return null;

            var defObj = resLookup.Objects.Values.FirstOrDefault();
            if (!defObj.HasData)
                return null;

            JsValue obj = defObj;
            if (obj is JsObject jsObject) {
                jsObject.Name = objRef.Name;
                return jsObject;
            }
            return null;
        }

        public JsValue Evaluate(int frameNumber, string expression)
        {
            var reqEval = Message.Create<EvaluateRequest>(driver);
            reqEval.Arguments.Expression = expression;
            reqEval.Arguments.Frame = frameNumber;

            var resEval = reqEval.Send();
            if (resEval == null)
                return new JsError { Message = "ERROR: Expression evaluation failed" };
            if (!resEval.Success)
                return new JsError { Message = resEval.Message };

            if (!resEval.Result.HasData)
                return new JsError { Message = "ERROR: Cannot read data" };

            return resEval.Result;
        }

        void IMessageEventSink.NotifyStateTransition(
            DebugClient client,
            DebugClientState oldState,
            DebugClientState newState)
        {
            if (oldState != DebugClientState.Unavailable
                && newState == DebugClientState.Disconnected) {
                _ = Task.Run(() => sink.NotifyClientDisconnected());
            }
        }

        void IMessageEventSink.NotifyRequestResponded(Request msgRequest)
        {
            if (msgRequest is SetBreakpointRequest)
                _ = Task.Run(() => SetBreakpointResponded(msgRequest as SetBreakpointRequest));
        }

        void IMessageEventSink.NotifyEvent(Event msgEvent)
        {
            if (msgEvent is BreakEvent)
                _ = Task.Run(() => BreakNotified(msgEvent as BreakEvent));
        }

        void IMessageEventSink.NotifyMessage(Message msg)
        {
            System.Diagnostics.Debug
                .Assert(msg is ConnectMessage, "Unexpected message");
        }

        public static bool CheckCommandLine(string execPath, string args)
        {
            return ParseCommandLine(execPath, args, out _, out _, out _, out _, out _);
        }

        /// <summary>
        /// Connection parameters for QML debug session
        /// </summary>
        class ConnectParams
        {
            public ushort Port { get; set; }
            public ushort? MaxPort { get; set; }
            public string Host { get; set; }
            public string File { get; set; }
            public bool Block { get; set; }
        }

        enum TokenId { ConnectParams, Port, MaxPort, Host, File, Block }

        /// <summary>
        /// Regex-based parser for QML debug connection parameters
        /// </summary>
        static RegExprParser ConnectParamsParser => StaticLazy.Get(() =>
            ConnectParamsParser, () => new Token(TokenId.ConnectParams, RxConnectParams)
            {
                new Rule<ConnectParams>
                {
                    Update(TokenId.Port, (ConnectParams conn, ushort n) => conn.Port = n),
                    Update(TokenId.MaxPort, (ConnectParams conn, ushort n) => conn.MaxPort = n),
                    Update(TokenId.Host, (ConnectParams conn, string s) => conn.Host = s),
                    Update(TokenId.File, (ConnectParams conn, string s) => conn.File = s),
                    Update(TokenId.Block, (ConnectParams conn, bool b) => conn.Block = b)
                }
            }
            .Render());

        /// <summary>
        /// Regular expression for parsing connection parameters string in the form:
        ///
        ///   -qmljsdebugger=port:<port_num>[,port_max][,host:<address>][,file:<name>][,block]
        ///
        /// </summary>
        static RegExpr RxConnectParams =>
            "-qmljsdebugger="
            & ((RxPort | RxHost | RxFile) & RxDelim).Repeat(atLeast: 1) & RxBlock.Optional();

        static RegExpr RxPort =>
            "port:" & new Token(TokenId.Port, CharDigit.Repeat(atLeast: 1))
            {
                new Rule<ushort> { Capture(token => ushort.Parse(token)) }
            }
            & (
                "," & new Token(TokenId.MaxPort, CharDigit.Repeat(atLeast: 1))
                {
                    new Rule<ushort> { Capture(token => ushort.Parse(token)) }
                }
            ).Optional();

        static RegExpr RxHost =>
            "host:" & new Token(TokenId.Host, (~CharSet[CharSpace, Chars[","]]).Repeat(atLeast: 1));

        static RegExpr RxFile =>
            "file:" & new Token(TokenId.File, (~CharSet[CharSpace, Chars[","]]).Repeat(atLeast: 1));

        static RegExpr RxBlock =>
            new Token(TokenId.Block, "block")
            {
                new Rule<bool> { Capture(token => true) }
            };

        static RegExpr RxDelim =>
            ("," & !LookAhead[CharSpace | EndOfLine]) | LookAhead[CharSpace | EndOfLine];

        /// <summary>
        /// Extract QML debug connection parameters from command line args
        /// </summary>
        public static bool ParseCommandLine(
            string execPath,
            string args,
            out ushort portFrom,
            out ushort portTo,
            out string hostName,
            out string fileName,
            out bool block)
        {
            portFrom = portTo = 0;
            hostName = fileName = "";
            block = false;

            ConnectParams connParams = ConnectParamsParser
                .Parse(args)
                .GetValues<ConnectParams>(TokenId.ConnectParams)
                .FirstOrDefault();

            if (connParams == null)
                return false;

            portFrom = connParams.Port;
            if (connParams.MaxPort.HasValue)
                portTo = connParams.MaxPort.Value;
            hostName = connParams.Host;
            fileName = connParams.File;
            block = connParams.Block;

            return true;
        }
    }
}
