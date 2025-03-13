// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools.Core.Common
{
    public static partial class Utils
    {
        public enum StreamAction
        {
            KeepStreaming,
            StopStreaming
        }

        public class StreamDataEventArgs : EventArgs
        {
            public byte[] Data { get; }

            public StreamDataEventArgs(byte[] data, int count)
            {
                Data = new byte[count];
                Buffer.BlockCopy(data, 0, Data, 0, count);
            }
        }

        public class StreamMonitor : Stream
        {
            private readonly Stream monitoredStream;

            public bool LoggingEnabled { get; set; }

            public delegate void DataReceivedDelegate(StreamDataEventArgs args);
            public event DataReceivedDelegate DataReceived;

            public delegate (StreamData Data, StreamAction action)
                StreamModifierDelegate(StreamData data);
            public StreamModifierDelegate StreamModifier { get; set; }

            public bool IsConnected => monitoredStream is { CanRead: true } or { CanWrite: true };

            public StreamMonitor(Stream stream)
            {
                monitoredStream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var bytesRead = monitoredStream.Read(buffer, offset, count);
                if (LoggingEnabled && bytesRead > 0)
                    _ = Task.Run(() => DataReceived?.Invoke(new StreamDataEventArgs(buffer, bytesRead)));
                return bytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (StreamModifier != null) {
                    var streamData = new StreamData
                    {
                        Bytes = buffer,
                        Offset = offset,
                        Count = count
                    };

                    var (modifiedData, action) = StreamModifier(streamData);
                    buffer = modifiedData.Bytes;
                    count = modifiedData.Count;

                    if (action == StreamAction.StopStreaming)
                        StreamModifier = null;
                }

                monitoredStream.Write(buffer, offset, count);

                if (LoggingEnabled && count > 0)
                    _ = Task.Run(() => DataReceived?.Invoke(new StreamDataEventArgs(buffer, count)));
            }

            public override bool CanRead => monitoredStream.CanRead;

            public override bool CanSeek => monitoredStream.CanSeek;

            public override bool CanWrite => monitoredStream.CanWrite;

            public override long Length => monitoredStream.Length;

            public override long Position
            {
                get => monitoredStream.Position;
                set => monitoredStream.Position = value;
            }

            public override void Flush() => monitoredStream.Flush();

            public override long Seek(long offset, SeekOrigin origin)
                => monitoredStream.Seek(offset, origin);

            public override void SetLength(long value) => monitoredStream.SetLength(value);

            #region IDisposable

            private bool disposed;

            protected override void Dispose(bool disposing)
            {
                if (!disposed) {
                    if (disposing)
                        monitoredStream?.Dispose();

                    disposed = true;
                    GC.SuppressFinalize(this);
                }

                base.Dispose(disposing);
            }

            ~StreamMonitor()
            {
                Dispose(false);
            }

            #endregion IDisposable
        }
    }
}
