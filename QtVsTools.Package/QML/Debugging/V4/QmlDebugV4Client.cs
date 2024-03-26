/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QtVsTools.Qml.Debug.V4
{
    using Core.Options;

    enum DebugClientState { Unavailable, Disconnected, Connecting, Connected, Disconnecting }

    interface IConnectionEventSink
    {
        bool QueryRuntimeFrozen();

        void NotifyStateTransition(
            DebugClient client,
            DebugClientState oldState,
            DebugClientState newState);

        void NotifyMessageReceived(
            DebugClient client,
            string messageType,
            byte[] messageParams);
    }

    class DebugClient : Finalizable
    {
        IConnectionEventSink sink;
        IntPtr client;
        Task clientThread;
        readonly EventWaitHandle clientCreated = new(false, EventResetMode.ManualReset);
        EventWaitHandle clientConnected;

        public uint? ThreadId { get; private set; }

        DebugClientState state = DebugClientState.Unavailable;
        public DebugClientState State
        {
            get
            {
                if (clientThread is not {Status: TaskStatus.Running})
                    return DebugClientState.Unavailable;
                return state;
            }
            set
            {
                if (state != value) {
                    var oldState = state;
                    state = value;
                    _ = Task.Run(() => sink.NotifyStateTransition(this, oldState, value));
                }
            }
        }

        public static DebugClient Create(IConnectionEventSink sink)
        {
            var _this = new DebugClient();
            return _this.Initialize(sink) ? _this : null;
        }

        private DebugClient()
        { }

        private bool Initialize(IConnectionEventSink sink)
        {
            this.sink = sink;

            QtVsToolsPackage.Instance.JoinableTaskFactory.Run(async () =>
            {
                await Task.WhenAny(
                    // Try to start client thread
                    // Unblock if thread was abruptly terminated (e.g. DLL not found)
                    clientThread = Task.Run(ClientThread),

                    // Unblock if client was created (i.e. client thread is running)
                    Task.Run(() => clientCreated.WaitOne()));
            });

            if (State == DebugClientState.Unavailable) {
                // Client thread did not start
                clientCreated.Set();
                Dispose();
                return false;
            }

            return true;
        }

        protected override void DisposeManaged()
        {
            clientCreated.Dispose();

            EnterCriticalSection();
            if (clientConnected != null) {
                LeaveCriticalSection();
                clientConnected.Dispose();
            }
        }

        protected override void DisposeUnmanaged()
        {
            if (State != DebugClientState.Unavailable) {
                NativeMethods.DebugClientShutdown(client);

                QtVsToolsPackage.Instance.JoinableTaskFactory.Run(
                    async () => await Task.WhenAll(new[] { clientThread }));
            }
        }

        private void ClientThread()
        {
            ThreadId = NativeMethods.GetCurrentThreadId();

            var clientCreated =
                new NativeMethods.QmlDebugClientCreated(ClientCreated);
            var clientDestroyed =
                new NativeMethods.QmlDebugClientDestroyed(ClientDestroyed);
            var clientConnected =
                new NativeMethods.QmlDebugClientConnected(ClientConnected);
            var clientDisconnected =
                new NativeMethods.QmlDebugClientDisconnected(ClientDisconnected);
            var clientMessageReceived =
                new NativeMethods.QmlDebugClientMessageReceived(ClientMessageReceived);
            try {
                NativeMethods.DebugClientThread(
                    clientCreated, clientDestroyed,
                    clientConnected, clientDisconnected,
                    clientMessageReceived);
            } finally {
                State = DebugClientState.Unavailable;
                GC.KeepAlive(clientCreated);
                GC.KeepAlive(clientDestroyed);
                GC.KeepAlive(clientConnected);
                GC.KeepAlive(clientDisconnected);
                GC.KeepAlive(clientMessageReceived);
            }
        }

        public EventWaitHandle Connect(string hostName, ushort hostPort)
        {
            if (State != DebugClientState.Disconnected)
                return null;

            clientConnected = new EventWaitHandle(false, EventResetMode.ManualReset);
            State = DebugClientState.Connecting;
            if (string.IsNullOrEmpty(hostName))
                hostName = "localhost";
            var hostNameData = Encoding.UTF8.GetBytes(hostName);

            uint timeout = (uint)QtOptionsPage.QmlDebuggerTimeout;
            _ = Task.Run(() =>
            {
                var connectTimer = new System.Diagnostics.Stopwatch();
                connectTimer.Start();

                var probe = new Socket(
                    AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                while (!probe.Connected
                    && (timeout == 0 || connectTimer.ElapsedMilliseconds < timeout)) {
                    try {
                        probe.Connect(hostName, hostPort);
                    } catch {
                        Thread.Sleep(3000);
                    }
                }

                if (probe.Connected) {
                    probe.Disconnect(false);

                    NativeMethods.DebugClientConnect(client,
                        hostNameData, hostNameData.Length, hostPort);
                    connectTimer.Restart();

                    uint connectionTimeout = Math.Max(3000, timeout / 20);
                    while (!clientConnected.WaitOne(1000)) {

                        if (sink.QueryRuntimeFrozen()) {
                            connectTimer.Restart();

                        } else {
                            if (connectTimer.ElapsedMilliseconds > connectionTimeout) {
                                if (!Disposing)
                                    clientConnected.Set();

                                if (Atomic(() => State == DebugClientState.Connected,
                                           () => State = DebugClientState.Disconnecting)) {
                                    NativeMethods.DebugClientDisconnect(client);
                                }

                            } else {
                                NativeMethods.DebugClientConnect(client,
                                    hostNameData, hostNameData.Length, hostPort);
                            }
                        }
                    }
                }
            });

            return clientConnected;
        }

        public EventWaitHandle StartLocalServer(string fileName)
        {
            if (State != DebugClientState.Disconnected)
                return null;

            clientConnected = new EventWaitHandle(false, EventResetMode.ManualReset);
            State = DebugClientState.Connecting;
            var fileNameData = Encoding.UTF8.GetBytes(fileName);
            if (!NativeMethods.DebugClientStartLocalServer(client,
                fileNameData, fileNameData.Length)) {
                return null;
            }

            uint timeout = (uint)QtOptionsPage.QmlDebuggerTimeout;
            if (timeout != 0) {
                _ = Task.Run(() =>
                {
                    var connectTimer = new System.Diagnostics.Stopwatch();
                    connectTimer.Start();

                    while (!clientConnected.WaitOne(100)) {

                        if (sink.QueryRuntimeFrozen()) {
                            connectTimer.Restart();

                        } else {
                            if (connectTimer.ElapsedMilliseconds > timeout) {
                                if (!Disposing)
                                    clientConnected.Set();

                                if (Atomic(() => State == DebugClientState.Connected,
                                           () => State = DebugClientState.Disconnecting)) {
                                    NativeMethods.DebugClientDisconnect(client);
                                }
                            }
                        }
                    }
                });
            }

            return clientConnected;
        }

        public bool Disconnect()
        {
            if (State != DebugClientState.Connected)
                return false;
            State = DebugClientState.Disconnecting;
            return NativeMethods.DebugClientDisconnect(client);
        }

        public bool SendMessage(string messageType, byte[] messageParams)
        {
            if (State != DebugClientState.Connected)
                return false;
            var messageTypeData = Encoding.UTF8.GetBytes(messageType);
            messageParams ??= Array.Empty<byte>();

            System.Diagnostics.Debug.WriteLine(
                $">> {messageType} {Encoding.UTF8.GetString(messageParams)}");

            return NativeMethods.DebugClientSendMessage(client,
                messageTypeData, messageTypeData.Length,
                messageParams, messageParams.Length);
        }

        void ClientCreated(IntPtr qmlDebugClient)
        {
            if (client != IntPtr.Zero || Disposing)
                return;

            client = qmlDebugClient;
            State = DebugClientState.Disconnected;
            clientCreated.Set();
        }

        void ClientDestroyed(IntPtr qmlDebugClient)
        {
            if (qmlDebugClient != client)
                return;
            State = DebugClientState.Unavailable;
        }

        void ClientConnected(IntPtr qmlDebugClient)
        {
            if (qmlDebugClient != client || Disposing)
                return;
            State = DebugClientState.Connected;
            clientConnected.Set();
        }

        void ClientDisconnected(IntPtr qmlDebugClient)
        {
            if (qmlDebugClient != client)
                return;
            State = DebugClientState.Disconnected;
        }

        void ClientMessageReceived(
            IntPtr qmlDebugClient,
            byte[] messageTypeData,
            int messageTypeLength,
            byte[] messageParamsData,
            int messageParamsLength)
        {
            if (Disposed)
                return;
            if (qmlDebugClient != client)
                return;
            var messageType = Encoding.UTF8.GetString(messageTypeData);

            System.Diagnostics.Debug.WriteLine(
                $"<< {messageType} {Encoding.UTF8.GetString(messageParamsData)}");

            sink.NotifyMessageReceived(this, messageType, messageParamsData);
        }

        #region //////////////////// Native Methods ///////////////////////////////////////////////

        internal static class NativeMethods
        {
            public delegate void QmlDebugClientCreated(IntPtr qmlDebugClient);
            public delegate void QmlDebugClientDestroyed(IntPtr qmlDebugClient);
            public delegate void QmlDebugClientConnected(IntPtr qmlDebugClient);
            public delegate void QmlDebugClientDisconnected(IntPtr qmlDebugClient);
            public delegate void QmlDebugClientMessageReceived(
                IntPtr qmlDebugClient,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] messageTypeData,
                int messageTypeLength,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] messageParamsData,
                int messageParamsLength);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlDebugClientThread")]
            public static extern bool DebugClientThread(
                QmlDebugClientCreated clientCreated,
                QmlDebugClientDestroyed clientDestroyed,
                QmlDebugClientConnected clientConnected,
                QmlDebugClientDisconnected clientDisconnected,
                QmlDebugClientMessageReceived clientMessageReceived);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlDebugClientDisconnect")]
            public static extern bool DebugClientDisconnect(IntPtr qmlDebugClient);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlDebugClientConnect")]
            public static extern bool DebugClientConnect(
                IntPtr qmlDebugClient,
                [MarshalAs(UnmanagedType.LPArray)] byte[] hostNameData,
                int hostNameLength,
                ushort hostPort);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlDebugClientStartLocalServer")]
            public static extern bool DebugClientStartLocalServer(
                IntPtr qmlDebugClient,
                [MarshalAs(UnmanagedType.LPArray)] byte[] fileNameData,
                int fileNameLength);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlDebugClientSendMessage")]
            public static extern bool DebugClientSendMessage(
                IntPtr qmlDebugClient,
                [MarshalAs(UnmanagedType.LPArray)] byte[] messageTypeData,
                int messageTypeLength,
                [MarshalAs(UnmanagedType.LPArray)] byte[] messageParamsData,
                int messageParamsLength);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlDebugClientShutdown")]
            public static extern bool DebugClientShutdown(IntPtr qmlDebugClient);

            [DllImport("kernel32.dll")]
            public static extern uint GetCurrentThreadId();
        }

        #endregion //////////////////// Native Methods ////////////////////////////////////////////

    }
}
