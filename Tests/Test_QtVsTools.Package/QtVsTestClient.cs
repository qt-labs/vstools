/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace QtVsTools.Test
{
    public class QtVsTestClient : IDisposable
    {
        public NamedPipeClientStream Stream { get; }

        public QtVsTestClient(int vsProcId)
        {
            Stream = new NamedPipeClientStream(".", $"QtVSTest_{vsProcId}", PipeDirection.InOut);
        }

        public static QtVsTestClient Attach(int? vsProcId = null)
        {
            if (vsProcId == null) {
                var procs = Process.GetProcessesByName("devenv");
                var vsProc = procs
                    .Where(p => p.Id != Process.GetCurrentProcess().Id
                        && !p.MainWindowTitle.StartsWith("vstools"))
                    .FirstOrDefault();
                if (vsProc == null)
                    throw new InvalidOperationException("VS process not found");
                vsProcId = vsProc.Id;
            }
            var client = new QtVsTestClient(vsProcId.Value);
            client.Connect();
            return client;
        }

        public void Connect() => Stream.Connect();

        public void Dispose() => Stream?.Dispose();

        public string RunMacro(string macroCode)
        {
            if (!Stream.IsConnected)
                Connect();

            var macroData = Encoding.UTF8.GetBytes(macroCode);
            int macroDataSize = macroData.Length;
            byte[] sizeData = BitConverter.GetBytes(macroDataSize);

            Stream.Write(sizeData, 0, sizeof(int));
            Stream.Write(macroData, 0, macroData.Length);
            Stream.Flush();
            if (!Stream.IsConnected)
                return Error("Disconnected");

            Stream.WaitForPipeDrain();
            if (!Stream.IsConnected)
                return Error("Disconnected");

            for (int i = 0; i < sizeof(int); i++) {
                int c = Stream.ReadByte();
                if (c == -1)
                    return Error("Disconnected");
                if (c < Byte.MinValue || c > Byte.MaxValue)
                    return Error("Pipe error");
                sizeData[i] = (byte)c;
            }

            int replyDataSize = BitConverter.ToInt32(sizeData, 0);
            byte[] replyData = new byte[replyDataSize];
            int bytesRead = 0;
            while (bytesRead < replyDataSize) {
                if (!Stream.IsConnected)
                    return Error("Disconnected");
                bytesRead += Stream.Read(replyData, bytesRead, replyDataSize - bytesRead);
            }

            return Encoding.UTF8.GetString(replyData);
        }

        public string RunMacroFile(string macroPath)
        {
            return LoadAndRunMacro(macroPath);
        }

        public string StoreMacro(string macroName, string macroCode)
        {
            if (string.IsNullOrEmpty(macroName))
                return Error("Invalid macro name");
            return RunMacro($"//# macro {macroName}\r\n{macroCode}");
        }

        public string StoreMacroFile(string macroName, string macroPath)
        {
            if (string.IsNullOrEmpty(macroName))
                return Error("Invalid macro name");
            return LoadAndRunMacro(macroPath, $"//# macro {macroName}");
        }

        string LoadAndRunMacro(string macroPath, string macroHeader = null)
        {
            var macroCode = File.ReadAllText(macroPath, Encoding.UTF8);
            if (string.IsNullOrEmpty(macroCode))
                return Error("Macro load failed");
            if (!string.IsNullOrEmpty(macroHeader))
                return RunMacro($"{macroHeader}\r\n{macroCode}");
            else
                return RunMacro(macroCode);
        }

        public const string MacroOk = "(ok)";
        public const string MacroWarn = "(warn)";
        public const string MacroError = "(error)";

        static string Error(string msg) => $"{MacroError}\r\n{msg}";
    }
}
