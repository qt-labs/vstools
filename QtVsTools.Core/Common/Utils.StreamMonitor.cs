/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
**************************************************************************************************/

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace QtVsTools.Core.Common
{
    public static partial class Utils
    {
        public class StreamDataEventArgs : EventArgs
        {
            public byte[] Data { get; set; }
        }

        public class StreamMonitor : IDisposable
        {
            public Stream Stream { get; private set; }
            public static implicit operator Stream(StreamMonitor self) => self.Proxy ?? self.Stream;
            public bool IsConnected => Pipe is { IsConnected: true }
                && Stream is { CanRead: true } or { CanWrite: true };

            public delegate void StreamDataDelegate(object sender, StreamDataEventArgs args);
            public event StreamDataDelegate StreamData;

            public delegate void DisconnectedDelegate(object sender, EventArgs args);
            public event DisconnectedDelegate Disconnected;

            private string PipeName { get; }
            private PipeSecurity PipeSecurity { get;  }
            private NamedPipeServerStream Pipe { get; set; }
            private NamedPipeClientStream Proxy { get; set; }

            public StreamMonitor()
            {
                PipeName = $"{typeof(StreamMonitor).FullName}.{Path.GetRandomFileName()}";
                PipeSecurity = new PipeSecurity();
                PipeSecurity.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }

            public void Dispose()
            {
                Stream = null;

                var oldProxy = Proxy;
                Proxy = null;

                var oldPipe = Pipe;
                Pipe = null;

                oldProxy?.Dispose();
                oldPipe?.Dispose();
            }

            private void NotifyStreamData(byte[] data, int size)
            {
                if (StreamData is null)
                    return;
                var args = new StreamDataEventArgs { Data = new byte[size] };
                Array.Copy(data, args.Data, size);
                StreamData.Invoke(this, args);
            }

            private void NotifyDisconnected()
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }

            public void SetStream(StreamReader rdr) => SetStream(rdr.BaseStream);

            public void SetStream(StreamWriter wri) => SetStream(wri.BaseStream);

            public void SetStream(Stream stream)
            {
                Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            public async Task ConnectAsync(StreamReader rdr) => await ConnectAsync(rdr.BaseStream);

            public async Task ConnectAsync(StreamWriter wri) => await ConnectAsync(wri.BaseStream);

            public async Task ConnectAsync(Stream stream)
            {
                Stream = stream ?? throw new ArgumentNullException(nameof(stream));
                try {
                    Pipe = new NamedPipeServerStream(PipeName,
                        Stream.CanWrite ? PipeDirection.In : PipeDirection.Out, 1,
                        PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, PipeSecurity);
                    Proxy = new NamedPipeClientStream(".", PipeName,
                        Stream.CanWrite ? PipeDirection.Out : PipeDirection.In);
                    await Task.WhenAll(Pipe.WaitForConnectionAsync(), Proxy.ConnectAsync());
                } catch (Exception ex) {
                    ex.Log();
                    Dispose();
                    return;
                }

                _ = Task.Run(MonitorAsync);
            }

            private async Task MonitorAsync()
            {
                var data = new byte[4096];

                Stream source, target;
                if (Stream.CanWrite) {
                    source = Pipe;
                    target = Stream;
                } else {
                    source = Stream;
                    target = Pipe;
                }

                try {
                    while (IsConnected) {
                        var size = await source.ReadAsync(data, 0, data.Length);
                        if (!IsConnected)
                            break;
                        await target.WriteAsync(data, 0, size);
                        await target.FlushAsync();
                        NotifyStreamData(data, size);
                    }
                } catch (Exception ex) {
                    if (Pipe is not null)
                        ex.Log();
                } finally {
                    Dispose();
                    NotifyDisconnected();
                }
            }
        }
    }
}
