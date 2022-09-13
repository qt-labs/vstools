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
        int nextRequestSeq = 0;
        readonly Dictionary<int, PendingRequest> pendingRequests = new Dictionary<int, PendingRequest>();
        Task eventHandlingThread;
        readonly EventWaitHandle eventReceived = new EventWaitHandle(false, EventResetMode.AutoReset);
        readonly ConcurrentQueue<Event> eventQueue = new ConcurrentQueue<Event>();

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
            eventHandlingThread = Task.Run(() => EventHandlingThread());
            client = DebugClient.Create(this);
            if (client == null)
                return false;

            return true;
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
                PendingRequest pendingRequest = null;
                if (pendingRequests.TryGetValue(msgResponse.RequestSeq, out pendingRequest)) {
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
                if (responded != null)
                    responded.WaitOne();
                return Request.Response;
            }

            protected override void DisposeManaged()
            {
                if (Request == null)
                    return;

                if (responded != null)
                    responded.Dispose();
            }

            public void SetResponse(Response res)
            {
                if (Request == null || Disposing)
                    return;

                Request.Response = res;
                if (responded != null)
                    responded.Set();
            }
        }

        #endregion //////////////////// PendingRequest ////////////////////////////////////////////

    }
}
