/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;

namespace QtVsTools
{
    public class PunisherQueue<T> : BasePriorityQueue<T, long>
    {
        public PunisherQueue() : base()
        { }

        public PunisherQueue(Func<T, object> getItemKey) : base(getItemKey)
        { }

        /// <summary>
        /// Enqueue/re-queue moves item to back of the queue, effectively "punishing" items that
        /// were already in the queue.
        /// </summary>
        ///
        public void Enqueue(T item)
        {
            lock (CriticalSection) {
                Enqueue(item, Timestamp.Next());
            }
        }
    }
}
