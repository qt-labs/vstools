/****************************************************************************
**
** Copyright (C) 2021 The Qt Company Ltd.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QtVsTools
{
    public class PriorityQueue<T, TPriority> : BasePriorityQueue<T, TPriority>
        where TPriority : IComparable<TPriority>
    {
        public PriorityQueue() : base()
        { }

        public PriorityQueue(Func<T, object> getItemKey) : base(getItemKey)
        { }

        public new void Enqueue(T item, TPriority priority)
        {
            base.Enqueue(item, priority);
        }
    }

    public abstract class BasePriorityQueue<T, TPriority> : Concurrent, IEnumerable<T>
        where TPriority : IComparable<TPriority>
    {
        SortedDictionary<TPriority, T> ItemsByPriority { get; set; }
        Dictionary<object, TPriority> ItemPriority { get; set; }
        T Head { get; set; }

        IEnumerable<T> Items => ThreadSafe(() => ItemsByPriority.Values.ToList());
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        Func<T, object> GetItemKey { get; set; }

        public BasePriorityQueue()
        {
            Clear();
            GetItemKey = (x => x);
        }

        public BasePriorityQueue(Func<T, object> getItemKey)
        {
            Clear();
            GetItemKey = getItemKey;
        }

        public void Clear()
        {
            lock (CriticalSection) {
                ItemsByPriority = new SortedDictionary<TPriority, T>();
                ItemPriority = new Dictionary<object, TPriority>();
                Head = default(T);
            }
        }

        public bool Contains(T item)
        {
            lock (CriticalSection) {
                return ItemPriority.ContainsKey(GetItemKey(item));
            }
        }

        // Base Enqueue() is protected to allow specialized implementations to
        // hide the concept of priority (e.g. PunisherQueue).
        //
        protected void Enqueue(T item, TPriority priority)
        {
            if (item == null)
                throw new InvalidOperationException("Item cannot be null.");
            lock (CriticalSection) {
                T oldItem;
                if (ItemsByPriority.TryGetValue(priority, out oldItem) && !item.Equals(oldItem))
                    throw new InvalidOperationException("An item with the same priority exists.");
                TPriority oldPriority;
                if (ItemPriority.TryGetValue(GetItemKey(item), out oldPriority))
                    ItemsByPriority.Remove(oldPriority);
                ItemPriority[GetItemKey(item)] = priority;
                ItemsByPriority[priority] = item;
                Head = ItemsByPriority.First().Value;
            }
        }

        public bool TryPeek(out T result)
        {
            lock (CriticalSection) {
                result = Head;
                return ItemsByPriority.Any();
            }
        }

        public T Peek()
        {
            lock (CriticalSection) {
                T result;
                if (!TryPeek(out result))
                    throw new InvalidOperationException("Queue is empty.");
                return result;
            }
        }

        public bool TryDequeue(out T result)
        {
            lock (CriticalSection) {
                result = Head;
                if (!ItemsByPriority.Any())
                    return false;
                ItemsByPriority.Remove(ItemPriority[GetItemKey(result)]);
                ItemPriority.Remove(GetItemKey(result));
                if (ItemsByPriority.Any())
                    Head = ItemsByPriority.First().Value;
                else
                    Head = default(T);
                return true;
            }
        }

        public T Dequeue()
        {
            lock (CriticalSection) {
                T result;
                if (!TryDequeue(out result))
                    throw new InvalidOperationException("Queue is empty.");
                return result;
            }
        }
    }
}
