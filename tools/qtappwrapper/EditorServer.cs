/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace QtAppWrapper
{
    class EditorServer
    {
        #region WIN32 Definitions
        static uint TH32CS_SNAPPROCESS = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        };

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
        #endregion

        private TcpListener listener = null;
        private Thread listenThread = null;
        private List<TcpClient> clientList;
        private bool aboutToExit = false;
        private byte[] addinHelloMessage = new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F };

        private static Process GetParentProcess()
        {
            int iParentPid = 0;
            int iCurrentPid = Process.GetCurrentProcess().Id;

            IntPtr oHnd = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

            if (oHnd == IntPtr.Zero)
                return null;

            PROCESSENTRY32 oProcInfo = new PROCESSENTRY32();

            oProcInfo.dwSize =
                (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(PROCESSENTRY32));

            if (Process32First(oHnd, ref oProcInfo) == false)
                return null;

            do
            {
                if (iCurrentPid == oProcInfo.th32ProcessID)
                    iParentPid = (int)oProcInfo.th32ParentProcessID;
            }
            while (iParentPid == 0 && Process32Next(oHnd, ref oProcInfo));

            if (iParentPid > 0)
                return Process.GetProcessById(iParentPid);
            else
                return null;
        }

        public static void SendFileNameToServer(string fileName)
        {
            int ppid = -1;
            Process parentProcess = GetParentProcess();
            if (parentProcess != null)
                ppid = parentProcess.Id;
            SendFileNameToServer(fileName, ppid.ToString());
        }

        public static void SendFileNameToServer(string fileName, string processId)
        {
            TcpClient client = new TcpClient();
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 12015);
            bool clientConnected = false;
            try
            {
                client.Connect(serverEndPoint);
                clientConnected = client.Connected;
            }
            catch
            {}

            if (!clientConnected)
            {
                System.Windows.Forms.MessageBox.Show("Couldn't connect to QtAppWrapper server.\n" +
                                                     "Expected server address: " + serverEndPoint.ToString(),
                                                     "QtAppWrapper Error");
                return;
            }

            try
            {
                string data = processId + " " + fileName;
                data += "\n";
                NetworkStream stream = client.GetStream();

                UnicodeEncoding encoder = new UnicodeEncoding();
                byte[] buffer = encoder.GetBytes(data);

                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("SendFileNameToServer exception\n\n" + e.ToString(),
                                                     "Exception in QtAppWrapper");
            }

            if (client != null)
            {
                if (client.Connected)
                    client.GetStream().Close();
                client.Close();
            }
        }

        public EditorServer()
        {
            clientList = new List<TcpClient>();
            listener = new TcpListener(IPAddress.Loopback, 12015);
            listenThread = new Thread(new ThreadStart(ListenForClients));
            listenThread.Name = "listenThread";
            listenThread.Start();

            // The server will run for ever if no connection from an add-in occurs.
            // So we'll check after a certain time, if there's something in the client list.
            System.Timers.Timer watchDogTimer = new System.Timers.Timer();
            watchDogTimer.Interval = 60000;
            watchDogTimer.Elapsed += new System.Timers.ElapsedEventHandler(WatchDog);
            watchDogTimer.Start();
        }

        public void Shutdown()
        {
            aboutToExit = true;
            listener.Stop();
            lock (clientList)
            {
                foreach (TcpClient c in clientList)
                {
                    if (c != null)
                    {
                        if (c.Connected)
                            c.GetStream().Close();
                        c.Close();
                    }
                }
            }
            listenThread.Join(1000);
            Environment.Exit(0);
        }

        private void ListenForClients()
        {
            try
            {
                listener.Start();
            }
            catch
            { 
                return;
            }
            while (!aboutToExit)
            {
                try
                {
                    if (!listener.Pending())
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    //blocks until a client has connected to the server
                    TcpClient client = listener.AcceptTcpClient();
                    if (client == null || aboutToExit)
                        break;

                    byte[] message = new byte[4096];
                    NetworkStream stream = client.GetStream();
                    int bytesRead = stream.Read(message, 0, message.Length);
                    if (IsAddinHelloMessage(message, bytesRead))
                    {
                        Debug.WriteLine("Add-in connected to qtappwrapper");
                        lock (clientList)
                            clientList.Add(client);

                        // Create a thread to handle communication with connected client.
                        Thread clientThread = new Thread(new ParameterizedThreadStart(WatchAddinConnection));
                        clientThread.Name = "WatchAddinConnection";
                        clientThread.Start(client);
                    }
                    else
                    {
                        SendDataToAddins(message, bytesRead);
                        stream.Close();
                        client.Close();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
        }

        private void WatchAddinConnection(object clientObj)
        {
            TcpClient client = clientObj as TcpClient;

            try
            {
                NetworkStream stream = client.GetStream();

                try
                {
                    byte[] buffer = new byte[1024];
                    stream.Read(buffer, 0, buffer.Length);
                }
                catch (System.IO.IOException e)
                {
                    Debug.WriteLine(e.ToString());
                }

                lock (clientList)
                    clientList.Remove(client);

                client.Close();
                stream.Close();

                if (clientList.Count == 0)
                    Shutdown();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private bool IsAddinHelloMessage(byte[] message, int messageLength)
        {
            if (messageLength < addinHelloMessage.Length)
                return false;

            for (int i = 0; i < addinHelloMessage.Length; ++i)
                if (message[i] != addinHelloMessage[i])
                    return false;

            return true;
        }

        private void SendDataToAddins(byte[] data, int dataSize)
        {
            Debug.WriteLine("SendDataToAddins " + data.ToString());

            lock (clientList)
            {
                foreach (TcpClient c in clientList)
                {
                    try
                    {
                        NetworkStream clientStream = c.GetStream();
                        clientStream.Write(data, 0, dataSize);
                        clientStream.Flush();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
            }
            //System.Windows.Forms.MessageBox.Show("SendDataToAddins finished");
        }

        private void WatchDog(object sender, EventArgs e)
        {
            lock (clientList)
            {
                if (clientList.Count > 0)
                    return;
            }
            Shutdown();
        }
    }
}
