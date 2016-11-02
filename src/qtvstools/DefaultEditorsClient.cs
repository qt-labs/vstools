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

using QtProjectLib;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace QtVsTools
{
    class DefaultEditorsClient
    {
        public static DefaultEditorsClient Instance
        {
            get; private set;
        }

        public static void Initialize(DteEventsHandler handler)
        {
            Instance = new DefaultEditorsClient(handler);
        }

        public void Listen()
        {
            listenForBroadcastThread.Start();
            handleBroadcastMessageThread.Start();
        }

        public void Shutdown()
        {
            aboutToExit = true;
            autoResetEvent.Set();

            if (listenForBroadcastThread.IsAlive) {
                TerminateClient();
                if (!listenForBroadcastThread.Join(1000))
                    listenForBroadcastThread.Abort();
            }

            if (handleBroadcastMessageThread.IsAlive && !handleBroadcastMessageThread.Join(1000))
                handleBroadcastMessageThread.Abort();
        }

        private TcpClient client;
        private DteEventsHandler handler;
        private volatile bool aboutToExit;
        private Thread listenForBroadcastThread;
        private Thread handleBroadcastMessageThread;

        private const int qtAppWrapperPort = 12015;
        private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        private static readonly byte[] helloMessage = { 0x48, 0x45, 0x4C, 0x4C, 0x4F };
        private readonly ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

        private DefaultEditorsClient(DteEventsHandler handler)
        {
            this.handler = handler;
            listenForBroadcastThread = new Thread(ListenForBroadcastMessage)
            {
                Name = "ListenForBroadcastMessage"
            };
            handleBroadcastMessageThread = new Thread(HandleBroadcastMessage)
            {
                Name = "HandleBroadcastMessage"
            };
        }

        private void HandleBroadcastMessage()
        {
            while (!aboutToExit) {
                autoResetEvent.WaitOne();
                if (aboutToExit)
                    break;

                string message;
                while (!aboutToExit && messageQueue.TryDequeue(out message)) {
                    if (message.EndsWith(".qrc", StringComparison.OrdinalIgnoreCase))
                        handler.OnQRCFileSaved(message);
                    else if (message.StartsWith("Autotests:set", StringComparison.Ordinal)) {
#if DEBUG
                        // Messageformat from Autotests is Autotests:set<dir>:<value>
                        // where dir is MocDir, RccDir or UicDir

                        //remove Autotests:set
                        message = message.Substring(13);

                        var dir = message.Remove(6);
                        var value = message.Substring(7);

                        handler.setDirectory(dir, value);
#endif
                    } else {
                        DefaultEditorsHandler.Instance.StartEditor(message);
                    }
                }
            }
        }

        private void ListenForBroadcastMessage()
        {
            if (Vsix.Instance.AppWrapperPath == null) {
                Messages.DisplayCriticalErrorMessage("QtAppWrapper can't be found in the "
                    + "installation directory."); aboutToExit = true; return;
            }

            Process qtAppWrapperProcess = null;
            try {
                qtAppWrapperProcess = new Process();
                qtAppWrapperProcess.StartInfo.FileName = Vsix.Instance.AppWrapperPath;

                bool firstIteration = true;
                while (!aboutToExit) {
                    try {
                        if (!firstIteration && qtAppWrapperProcess.HasExited)
                            qtAppWrapperProcess.Close();
                    } catch { } finally {
                        firstIteration = false;
                        qtAppWrapperProcess.Start();
                    }

                    var connectionAttempts = 0;
                    if (!aboutToExit) {
                        client = new TcpClient();
                        while (!aboutToExit && !client.Connected && connectionAttempts < 10) {
                            try {
                                client.Connect(IPAddress.Loopback, qtAppWrapperPort);
                                if (!client.Connected)
                                    Thread.Sleep(1000);
                            } catch {
                                Thread.Sleep(1000);
                            } finally {
                                ++connectionAttempts;
                            }
                        }
                    }

                    if (connectionAttempts >= 10) {
                        Messages.DisplayErrorMessage(SR.GetString("CouldNotConnectToAppwrapper",
                            qtAppWrapperPort));
                        aboutToExit = true;
                    }

                    if (!aboutToExit) {
                        var stream = client.GetStream();
                        stream.Write(helloMessage, 0, helloMessage.Length);
                        stream.Flush(); // say hello to qt application wrapper
                    }

                    var data = new byte[4096];
                    while (!aboutToExit) {
                        try {
                            var bytesRead = 0;
                            try {
                                // blocks until the default editors server sends a message
                                var stream = client.GetStream();
                                bytesRead = stream.Read(data, 0, 4096);
                            } catch {
                                // A socket error has occurred, probably because the QtAppWrapper
                                // has been terminated. Break and then try to restart the QtAppWrapper.
                                break;
                            }

                            if (bytesRead == 0)
                                break; // The server has disconnected from us.

                            // data has successfully been received
                            var decodedData = new UnicodeEncoding().GetString(data, 0, bytesRead);
                            var messages = decodedData.Split(new[] { '\n' },
                                StringSplitOptions.RemoveEmptyEntries);

                            foreach (var message in messages) {
                                var index = message.IndexOf(' ');
                                var requestedPid = Convert.ToInt32(message.Substring(0, index));
                                if (requestedPid == Process.GetCurrentProcess().Id)
                                    messageQueue.Enqueue(message.Substring(index + 1));
                            }
                            autoResetEvent.Set(); // Actual file opening is done in a different thread.
                        } catch (ThreadAbortException) {
                            break;
                        } catch { }
                    }
                    TerminateClient();
                }
            } finally {
                if (qtAppWrapperProcess != null)
                    qtAppWrapperProcess.Dispose();
                TerminateClient();
            }
        }

        private void TerminateClient()
        {
            try {
                if (client != null) {
                    var tmp = client;
                    client = null;

                    if (tmp.Connected) {
                        var stream = tmp.GetStream();
                        if (stream != null)
                            stream.Close();
                        tmp.Close();
                    }
                }
            } catch { /* ignore */ }
        }
    }
}
