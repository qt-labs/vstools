/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
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
                return (LastTimestamp = t);
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
