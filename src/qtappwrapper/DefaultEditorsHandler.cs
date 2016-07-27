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
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace QtAppWrapper
{
    static class DefaultEditorsHandler
    {
        private static string GetParentProcessID(string[] args)
        {
            Debug.WriteLine("Entering function GetParentPID().");

            var ppid = "-1";
            try {
                if (args.Length == 1) { // the first argument is the file name
                    var hwnd = NativeMethods.CreateToolhelp32Snapshot(NativeMethods
                        .TH32CS_SNAPPROCESS, 0);
                    if (hwnd == NativeMethods.INVALID_HANDLE_VALUE)
                        return ppid;

                    var entry = new NativeMethods.PROCESSENTRY32();
                    entry.dwSize = Marshal.SizeOf(typeof(NativeMethods.PROCESSENTRY32));

                    if (NativeMethods.Process32First(hwnd, ref entry)) {
                        var parentPid = 0;
                        do {
                            var currentPid = Process.GetCurrentProcess().Id;
                            if (currentPid == entry.th32ProcessID)
                                parentPid = entry.th32ParentProcessID;
                        } while (parentPid == 0 && NativeMethods.Process32Next(hwnd, ref entry));

                        if (parentPid > 0) {
                            ppid = Process.GetProcessById(parentPid).Id.ToString(CultureInfo
                                .InvariantCulture);
                        }
                    }
                    NativeMethods.CloseHandle(hwnd);
                } else if (args.Length >= 1 && args[1].StartsWith("-pid ", StringComparison.Ordinal)) {
                    ppid = args[1].Substring("-pid ".Length);
                }
                return ppid;
            } finally {
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture,
                    "Leaving function GetParentPID() with PID: '{0}'.", ppid));
            }
        }

        public static void SendFileNameToDefaultEditorsServer(string[] args)
        {
            Debug.WriteLine("Entering function SendFileNameToDefaultEditorsServer().");
            Debug.WriteLine("Argument:");
            Debug.Indent();
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "string[]: '{0}'",
                string.Join(Environment.NewLine, args)));
            Debug.Unindent();

            var fileName = args[0];
            var processId = GetParentProcessID(args);

            try {
                using (var client = new TcpClient()) {
                    var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 12015);
                    try {
                        client.Connect(serverEndPoint);
                    } catch (Exception e) {
                        Debug.WriteLine("Exception thrown:");
                        Debug.Indent();
                        Debug.WriteLine(e.Message);
                        Debug.Unindent();
                    }

                    if (!client.Connected) {
                        Debug.WriteLine(string.Format(CultureInfo.CurrentCulture,
                            "Could not connect to server. Expected server address: '{0}'.",
                            serverEndPoint));
                        return;
                    }

                    try {
                        var encoder = new UnicodeEncoding();
                        var buffer = encoder.GetBytes(processId + " " + fileName + "\n");

                        Debug.WriteLine(string.Format(CultureInfo.CurrentCulture,
                            "Trying to send file name: '{0}' and process ID: '{1}'.", fileName,
                            processId));

                        var stream = client.GetStream();
                        stream.Write(buffer, 0, buffer.Length);
                        stream.Flush();
                    } catch (Exception e) {
                        Debug.WriteLine("Exception thrown:");
                        Debug.Indent();
                        Debug.WriteLine(e.Message);
                        Debug.Unindent();
                    }

                    if (client.Connected)
                        client.GetStream().Close();
                }
            } finally {
                Debug.WriteLine("Leaving function SendFileNameToDefaultEditorsServer().");
            }
        }
    }
}
