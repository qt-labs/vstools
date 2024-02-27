/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Diagnostics;

namespace QtVsTools
{
    using Common;

    public class Timestamp : Concurrent<Timestamp>
    {
        static LazyFactory StaticLazy { get; } = new();

        long LastTimestamp { get; set; }
        long GetStrictMonotonicTimestamp()
        {
            lock (CriticalSection) {
                long t = Stopwatch.GetTimestamp();
                if (t <= LastTimestamp)
                    t = LastTimestamp + 1;
                return LastTimestamp = t;
            }
        }

        static Timestamp Instance => StaticLazy.Get(() =>
            Instance, () => new Timestamp());

        public static long Next()
        {
            return Instance.GetStrictMonotonicTimestamp();
        }
    }
}
