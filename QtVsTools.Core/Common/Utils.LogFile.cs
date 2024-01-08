/**************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace QtVsTools.Core.Common
{
    public static partial class Utils
    {
        /// <summary>
        /// Auto-rotating log file.
        /// </summary>
        public class LogFile : Concurrent
        {
            public string FilePath { get; }
            public int MaxSize { get; }
            public int TruncSize { get; }
            public List<byte[]> Delimiters { get; }

            /// <summary>
            /// Create auto-rotating log file. Upon reaching <see cref="maxSize"/> the file is
            /// truncated to <see cref="truncSize"/>. If any <see cref="delimiters"/> are specified,
            /// the log is further truncated to align with the first record delimiter.
            /// </summary>
            /// <param name="path">
            ///     Path to log file
            /// </param>
            /// <param name="maxSize">
            ///     Maximum size of log file.
            ///     Log is truncated if it grows to a length of 'maxSize' or greater.
            /// </param>
            /// <param name="truncSize">
            ///     Size to which the log file is truncated to.
            /// </param>
            /// <param name="delimiters">
            ///     Log record delimiter(s).
            ///     After truncating, the start of file will align with the first delimiter.
            /// </param>
            /// <exception cref="ArgumentException">
            ///     * <see cref="path"/> contains invalid characters.
            /// </exception>
            /// <exception cref="ArgumentOutOfRangeException">
            ///     * <see cref="maxSize"/> is zero or negative.
            ///     * <see cref="truncSize"/> is zero or negative.
            ///     * <see cref="truncSize"/> is greater than <see cref="maxSize"/>.
            /// </exception>
            public LogFile(string path, int maxSize, int truncSize, params string[] delimiters)
            {
                FilePath = path switch
                {
                    { Length: > 0 } when !Path.GetInvalidPathChars().Any(path.Contains) => path,
                    _ => throw new ArgumentException(nameof(path))
                };
                MaxSize = maxSize switch
                {
                    > 0 => maxSize,
                    _ => throw new ArgumentOutOfRangeException(nameof(maxSize))
                };
                TruncSize = truncSize switch
                {
                    > 0 when truncSize < maxSize => truncSize,
                    _ => throw new ArgumentOutOfRangeException(nameof(truncSize))
                };
                Delimiters = delimiters
                    ?.Select(Encoding.UTF8.GetBytes)
                    ?.ToList()
                    ?? new();
            }

            public void Write(string logEntry)
            {
                var data = Encoding.UTF8.GetBytes(logEntry);
                lock (CriticalSection) {
                    try {
                        using var log = new FileStream(FilePath,
                            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, MaxSize);
                        log.Seek(0, SeekOrigin.End);
                        log.Write(data, 0, data.Length);
                        if (log.Length > MaxSize)
                            Rotate(log);
                        log.Flush();
                    } catch (Exception e) {
                        e.Log();
                    }
                }
            }

            private void Rotate(FileStream log)
            {
                var data = new byte[TruncSize];
                log.Seek(-TruncSize, SeekOrigin.End);
                log.Read(data, 0, TruncSize);
                log.Seek(0, SeekOrigin.Begin);
                var idxStart = Delimiters switch
                {
                    { Count: > 0 } => Delimiters
                        .Select(data.IndexOfArray)
                        .Where(x => x >= 0)
                        .Append(TruncSize)
                        .Min(),
                    _ => 0
                };
                if (idxStart < TruncSize)
                    log.Write(data, idxStart, TruncSize - idxStart);
                log.SetLength(TruncSize - idxStart);
            }
        }
    }
}
