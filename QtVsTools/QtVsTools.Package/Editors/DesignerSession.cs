// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace QtVsTools.Package.Editors
{
    internal sealed class DesignerSession : IDisposable
    {
        public Process Process { get; set; }
        public TcpListener Listener { get; } = new(IPAddress.Loopback, 0);
        public NetworkStream Stream { get; set; }

        public void Dispose()
        {
            try {
                Stream?.Dispose();
            } catch { /* ignored */ }

            try {
                Listener.Stop();
            } catch { /* ignored */ }
        }
    }
}
