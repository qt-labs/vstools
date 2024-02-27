/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;

namespace QtVsTools
{
    public class PunisherQueue<T> : BasePriorityQueue<T, long>
    {
        public PunisherQueue()
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
