/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace QtAppWrapper
{
    enum MessageType
    {
        Data,
        Hello
    }

    class DefaultEditorsServer
    {
        public static void StartListen()
        {
            Instance = new DefaultEditorsServer();
        }

        public static DefaultEditorsServer Instance
        {
            get; private set;
        }

        private bool aboutToExit;
        private Thread listenThread;
        private TcpListener tcpListener;
        private readonly List<TcpClient> clientList = new List<TcpClient>();
        private static readonly byte[] helloMessage = { 0x48, 0x45, 0x4C, 0x4C, 0x4F };

        private DefaultEditorsServer()
        {
            Debug.WriteLine("Entering function DefaultEditorsServer().");

            tcpListener = new TcpListener(IPAddress.Loopback, 12015);
            listenThread = new Thread(ListenForClients);
            listenThread.Start();

            // The server will run for ever if no connection from an VSIX extension occurs.
            // So we'll check after a certain time, if there's something in the client list.
            var timer = new System.Timers.Timer(60000);
            var handle = GCHandle.Alloc(timer);
            timer.Elapsed += (sender, args) =>
            {
                Debug.WriteLine("Entering function Elapsed().");
                Debug.WriteLine("Arguments:");
                Debug.Indent();
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture,
                    "Object: '{0}', EventArgs: '{1}'", sender, args));
                Debug.Unindent();

                timer.Stop();
                handle.Free();
                timer.Dispose();
                try {
                    lock (clientList) {
                        if (clientList.Count > 0)
                            return;
                    }
                    Shutdown();
                } finally {
                    Debug.WriteLine("Leaving function Elapsed().");
                }
            };
            timer.Start();

            Debug.WriteLine("Leaving function DefaultEditorsServer().");
        }

        private void Shutdown()
        {
            Debug.WriteLine("Entering function Shutdown().");

            aboutToExit = true;
            tcpListener.Stop();
            lock (clientList) {
                foreach (var client in clientList) {
                    if (client != null) {
                        if (client.Connected)
                            client.GetStream().Close();
                        client.Close();
                    }
                }
            }
            listenThread.Join(1000);
            Environment.Exit(0);

            Debug.WriteLine("Leaving function Shutdown().");
        }

        private void ListenForClients()
        {
            Debug.WriteLine("Entering function ListenForClients().");

            try {
                try {
                    tcpListener.Start();
                } catch (Exception e) {
                    Debug.WriteLine("Exception thrown:");
                    Debug.Indent();
                    Debug.WriteLine(e.Message);
                    Debug.Unindent();
                    return;
                }

                while (!aboutToExit) {
                    try {
                        if (!tcpListener.Pending()) {
                            Thread.Sleep(250);
                            continue;
                        }

                        //blocks until a client has connected to the server
                        var client = tcpListener.AcceptTcpClient();
                        if (client == null || aboutToExit)
                            break;

                        byte[] message = new byte[4096];
                        var stream = client.GetStream();
                        var bytesRead = stream.Read(message, 0, message.Length);
                        if (MessageReceived(message, bytesRead) == MessageType.Hello) {
                            lock (clientList)
                                clientList.Add(client);

                            // Create a thread to handle communication with connected client.
                            var clientThread = new Thread(WatchConnection);
                            clientThread.Start(client);
                        } else {
                            BroadcastMessageToVsixClients(message, bytesRead);
                            stream.Close();
                            client.Close();
                        }
                    } catch (Exception e) {
                        Debug.WriteLine("Exception thrown:");
                        Debug.Indent();
                        Debug.WriteLine(e.Message);
                        Debug.Unindent();
                    }
                }
            } finally {
                Debug.WriteLine("Leaving function ListenForClients().");
            }
        }

        private void WatchConnection(object client)
        {
            Debug.WriteLine("Entering function WatchConnection().");
            Debug.WriteLine("Argument:");
            Debug.Indent();
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "object: '{0}'", client));
            Debug.Unindent();

            try {
                var tcpClient = client as TcpClient;
                var stream = tcpClient.GetStream();

                try {
                    byte[] buffer = new byte[1024];
                    stream.Read(buffer, 0, buffer.Length);
                } catch (System.IO.IOException e) {
                    Debug.WriteLine("Exception thrown:");
                    Debug.Indent();
                    Debug.WriteLine(e.Message);
                    Debug.Unindent();
                }

                lock (clientList)
                    clientList.Remove(tcpClient);

                tcpClient.Close();
                stream.Close();

                if (clientList.Count == 0)
                    Shutdown();
            } catch (Exception e) {
                Debug.WriteLine("Exception thrown:");
                Debug.Indent();
                Debug.WriteLine(e.Message);
                Debug.Unindent();
            } finally {
                Debug.WriteLine("Leaving function WatchConnection().");
            }
        }

        private static MessageType MessageReceived(byte[] message, int length)
        {
            Debug.WriteLine("Entering function MessageReceived().");
            Debug.WriteLine("Arguments:");
            Debug.Indent();
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "byte: '{0}', int: '{1}'",
                new UnicodeEncoding().GetString(message, 0, length).Replace("\n", ""), length));
            Debug.Unindent();

            try {
                if (length < helloMessage.Length)
                    return MessageType.Data;

                for (int i = 0; i < helloMessage.Length; ++i) {
                    if (message[i] != helloMessage[i])
                        return MessageType.Data;
                }
            } finally {
                Debug.WriteLine("Leaving function MessageReceived().");
            }
            return MessageType.Hello;
        }

        private void BroadcastMessageToVsixClients(byte[] data, int dataSize)
        {
            Debug.WriteLine("Entering function BroadcastMessageToVsixClients().");
            Debug.WriteLine("Arguments:");
            Debug.Indent();
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "byte: '{0}', int: '{1}'",
                new UnicodeEncoding().GetString(data, 0, dataSize).Replace("\n", ""), dataSize));
            Debug.Unindent();

            try {
                lock (clientList) {
                    foreach (var client in clientList) {
                        try {
                            var clientStream = client.GetStream();
                            clientStream.Write(data, 0, dataSize);
                            clientStream.Flush();
                        } catch (Exception e) {
                            Debug.WriteLine("Exception thrown:");
                            Debug.Indent();
                            Debug.WriteLine(e.Message);
                            Debug.Unindent();
                        }
                    }
                }
            } finally {
                Debug.WriteLine("Leaving function BroadcastMessageToVsixClients().");
            }
        }
    }
}
