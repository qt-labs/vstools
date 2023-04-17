/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QtVsTools.Qml.Debug.V4
{
    interface IMessageEventSink
    {
        bool QueryRuntimeFrozen();

        void NotifyStateTransition(
            DebugClient client,
            DebugClientState oldState,
            DebugClientState newState);

        void NotifyRequestResponded(Request req);
        void NotifyEvent(Event evt);
        void NotifyMessage(Message msg);
    }

    class ProtocolDriver : Finalizable, IConnectionEventSink
    {
        IMessageEventSink sink;
        DebugClient client;
        int nextRequestSeq;
        readonly Dictionary<int, PendingRequest> pendingRequests = new();
        Task eventHandlingThread;
        readonly EventWaitHandle eventReceived = new(false, EventResetMode.AutoReset);
        readonly ConcurrentQueue<Event> eventQueue = new();

        public uint? ThreadId => client.ThreadId;

        public static ProtocolDriver Create(IMessageEventSink sink)
        {
            var _this = new ProtocolDriver();
            return _this.Initialize(sink) ? _this : null;
        }

        private ProtocolDriver()
        { }

        private bool Initialize(IMessageEventSink sink)
        {
            this.sink = sink;
            eventHandlingThread = Task.Run(EventHandlingThread);
            client = DebugClient.Create(this);
            return client != null;
        }

        protected override void DisposeManaged()
        {
            foreach (var req in ThreadSafe(() => pendingRequests.Values.ToList()))
                req.Dispose();

            ThreadSafe(() => pendingRequests.Clear());
            client.Dispose();
        }

        protected override void DisposeFinally()
        {
            eventReceived.Set();
            QtVsToolsPackage.Instance.JoinableTaskFactory.Run(
                async () => await Task.WhenAll(new[] { eventHandlingThread }));
            eventReceived.Dispose();
        }

        private void EventHandlingThread()
        {
            while (!Disposing) {
                eventReceived.WaitOne();
                while (!Disposing && eventQueue.TryDequeue(out Event evt))
                    sink.NotifyEvent(evt);
            }
        }

        public DebugClientState ConnectionState => client.State;

        bool IConnectionEventSink.QueryRuntimeFrozen()
        {
            return sink.QueryRuntimeFrozen();
        }

        public EventWaitHandle Connect(string hostName, ushort hostPort)
        {
            return client.Connect(hostName, hostPort);
        }

        public EventWaitHandle StartLocalServer(string fileName)
        {
            return client.StartLocalServer(fileName);
        }

        public bool Disconnect()
        {
            return client.Disconnect();
        }

        public bool SendMessage(Message message)
        {
            if (client.State != DebugClientState.Connected)
                return false;

            var messageParams = message.Serialize();

            System.Diagnostics.Debug
                .Assert(messageParams != null, "Empty message data");

            return client.SendMessage(message.Type, messageParams);
        }

        public PendingRequest SendRequest(Request request)
        {
            ThreadSafe(() => request.SequenceNum = nextRequestSeq++);

            var pendingRequest = new PendingRequest(request);
            ThreadSafe(() => pendingRequests[request.SequenceNum] = pendingRequest);

            if (!SendMessage(request as Message)) {
                ThreadSafe(() => pendingRequests.Remove(request.SequenceNum));
                pendingRequest.Dispose();
                return new PendingRequest();
            }

            return pendingRequest;
        }

        void IConnectionEventSink.NotifyStateTransition(
            DebugClient client,
            DebugClientState oldState,
            DebugClientState newState)
        {
            sink.NotifyStateTransition(client, oldState, newState);
        }

        void IConnectionEventSink.NotifyMessageReceived(
            DebugClient client,
            string messageType,
            byte[] messageParams)
        {
            if (client != this.client || Disposing)
                return;

            var msg = Message.Deserialize(messageType, messageParams);
            if (msg == null)
                return;

            if (msg is Response msgResponse) {
                EnterCriticalSection();
                if (pendingRequests.TryGetValue(msgResponse.RequestSeq, out var pendingRequest)) {
                    pendingRequests.Remove(msgResponse.RequestSeq);
                    LeaveCriticalSection();
                    pendingRequest.SetResponse(msgResponse);
                    sink.NotifyRequestResponded(pendingRequest.Request);
                    pendingRequest.Dispose();
                } else {
                    LeaveCriticalSection();
                    sink.NotifyMessage(msgResponse);
                }

            } else if (msg is Event msgEvent) {
                eventQueue.Enqueue(msgEvent);
                eventReceived.Set();

            } else {
                sink.NotifyMessage(msg);
            }
        }

        #region //////////////////// PendingRequest ///////////////////////////////////////////////

        public class PendingRequest : Finalizable
        {
            public Request Request { get; }
            readonly EventWaitHandle responded;

            public PendingRequest()
            {
                Request = null;
                responded = null;
            }

            public PendingRequest(Request req)
            {
                responded = new EventWaitHandle(false, EventResetMode.ManualReset);
                Request = req;
            }

            public bool RequestSent => Request != null;

            public bool ResponseReceived => Request?.Response != null;

            public Response WaitForResponse()
            {
                if (Request == null)
                    return null;
                responded?.WaitOne();
                return Request.Response;
            }

            protected override void DisposeManaged()
            {
                if (Request == null)
                    return;
                responded?.Dispose();
            }

            public void SetResponse(Response res)
            {
                if (Request == null || Disposing)
                    return;

                Request.Response = res;
                responded?.Set();
            }
        }

        #endregion //////////////////// PendingRequest ////////////////////////////////////////////

    }
}
