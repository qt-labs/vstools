/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Diagnostics;

namespace QtVsTools
{
    public class ConcurrentStopwatch : Concurrent<ConcurrentStopwatch>
    {
        Stopwatch Stopwatch { get; }

        public ConcurrentStopwatch()
        {
            Stopwatch = new Stopwatch();
        }

        public static ConcurrentStopwatch StartNew()
        {
            ConcurrentStopwatch s = new ConcurrentStopwatch();
            s.Start();
            return s;
        }

        public static long Frequency => Stopwatch.Frequency;
        public static bool IsHighResolution => Stopwatch.IsHighResolution;
        public bool IsRunning => ThreadSafe(() => Stopwatch.IsRunning);
        public TimeSpan Elapsed => ThreadSafe(() => Stopwatch.Elapsed);
        public long ElapsedMilliseconds => ThreadSafe(() => Stopwatch.ElapsedMilliseconds);
        public long ElapsedTicks => ThreadSafe(() => Stopwatch.ElapsedTicks);
        public void Reset() => ThreadSafe(() => Stopwatch.Reset());
        public void Restart() => ThreadSafe(() => Stopwatch.Restart());
        public void Start() => ThreadSafe(() => Stopwatch.Start());
        public void Stop() => ThreadSafe(() => Stopwatch.Stop());
    }
}
