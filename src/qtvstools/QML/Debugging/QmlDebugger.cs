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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QtVsTools.Qml.Debug
{
    using V4;
    using CommandLineParser = QtProjectLib.CommandLine.Parser;
    using CommandLineOption = QtProjectLib.CommandLine.Option;

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
        IDebuggerEventSink sink;
        ProtocolDriver driver;
        string connectionHostName;
        ushort connectionHostPortFrom;
        ushort connectionHostPortTo;
        string connectionFileName;
        bool connectionBlock;

        List<Request> outbox;
        Dictionary<int, IBreakpoint> breakpoints;

        public bool Started { get; private set; }

        public bool Running { get; private set; }

        public string Version { get; private set; }

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
                Task.Run(() => ConnectToDebugger());

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
                setBreakpoint.SendAsync();
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
            reqClearBreak.SendAsync();
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
                    IBreakpoint breakpoint;
                    if (!breakpoints.TryGetValue(breakpointId, out breakpoint))
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
            if (!(obj is JsObject))
                return null;

            obj.Name = objRef.Name;
            return obj as JsObject;
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
                Task.Run(() => sink.NotifyClientDisconnected());
            }
        }

        void IMessageEventSink.NotifyRequestResponded(Request msgRequest)
        {
            if (msgRequest is SetBreakpointRequest)
                Task.Run(() => SetBreakpointResponded(msgRequest as SetBreakpointRequest));
        }

        void IMessageEventSink.NotifyEvent(Event msgEvent)
        {
            if (msgEvent is BreakEvent)
                Task.Run(() => BreakNotified(msgEvent as BreakEvent));
        }

        void IMessageEventSink.NotifyMessage(Message msg)
        {
            System.Diagnostics.Debug
                .Assert(msg is ConnectMessage, "Unexpected message");
        }

        public static bool CheckCommandLine(string execPath,string args)
        {
            ushort portFrom;
            ushort portTo;
            string hostName;
            string fileName;
            bool block;
            return ParseCommandLine(
                execPath, args, out portFrom, out portTo, out hostName, out fileName, out block);
        }

        static Regex regexDebuggerParams = new Regex(
            @"^(?:(?:port\:(\d+)(?:\,(\d+))?)(?:,(?!$)|$)"
            + @"|(?:host\:([^\,\r\n]+))(?:,(?!$)|$)"
            + @"|(?:file\:([^\,\r\n]+))(?:,(?!$)|$)"
            + @"|(block(?:,(?!$)|$)))+$");

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

            var parser = new CommandLineParser();
            parser.SetSingleDashWordOptionMode(
                CommandLineParser.SingleDashWordOptionMode.ParseAsLongOptions);
            var qmlJsDebugger = new CommandLineOption("qmljsdebugger")
            {
                ValueName = "port:<port_from>[,port_to][,host:<ip address>][,block]"
            };
            parser.AddOption(qmlJsDebugger);
            try {
                if (!parser.Parse(args, null, Path.GetFileName(execPath)))
                    return false;
            } catch {
                return false;
            }
            var debuggerParams = parser.Value(qmlJsDebugger);
            var match = regexDebuggerParams.Match(debuggerParams);
            if (!match.Success)
                return false;

            if (match.Groups.Count > 1 && match.Groups[1].Success) {
                if (!ushort.TryParse(match.Groups[1].Value, out portFrom))
                    return false;
            }

            if (match.Groups.Count > 2 && match.Groups[2].Success) {
                if (!ushort.TryParse(match.Groups[2].Value, out portTo))
                    return false;
            }

            if (match.Groups.Count > 3 && match.Groups[3].Success)
                hostName = match.Groups[3].Value;

            if (match.Groups.Count > 4 && match.Groups[4].Success)
                fileName = match.Groups[4].Value;

            if (match.Groups.Count > 5 && match.Groups[5].Success)
                block = true;

            return true;
        }
    }
}
