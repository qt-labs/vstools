/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QtVsTools
{
    public class PriorityQueue<T, TPriority> : BasePriorityQueue<T, TPriority>
        where TPriority : IComparable<TPriority>
    {
        public PriorityQueue()
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
        SortedDictionary<TPriority, T> ItemsByPriority { get; }
        Dictionary<object, TPriority> ItemPriority { get; }
        T Head { get; set; }
        public int Count { get; private set; }
        public bool IsEmpty => Count == 0;

        IEnumerable<T> Items => ThreadSafe(() => ItemsByPriority.Values.ToList());
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        Func<T, object> GetItemKey { get; }

        public BasePriorityQueue() : this(x => x)
        { }

        public BasePriorityQueue(Func<T, object> getItemKey)
        {
            ItemsByPriority = new SortedDictionary<TPriority, T>();
            ItemPriority = new Dictionary<object, TPriority>();
            Head = default;
            Count = 0;
            GetItemKey = getItemKey;
        }

        public void Clear()
        {
            lock (CriticalSection) {
                if (Count == 0)
                    return;
                ItemsByPriority.Clear();
                ItemPriority.Clear();
                Head = default;
                Count = 0;
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
                if (ItemsByPriority.TryGetValue(priority, out T oldItem) && !item.Equals(oldItem))
                    throw new InvalidOperationException("An item with the same priority exists.");

                if (ItemPriority.TryGetValue(GetItemKey(item), out TPriority oldPriority)) {
                    ItemsByPriority.Remove(oldPriority);
                    --Count;
                }
                ItemPriority[GetItemKey(item)] = priority;
                ItemsByPriority[priority] = item;
                Head = ItemsByPriority.First().Value;
                ++Count;
            }
        }

        public bool TryPeek(out T result)
        {
            lock (CriticalSection) {
                result = Head;
                return Count > 0;
            }
        }

        public T Peek()
        {
            lock (CriticalSection) {
                if (!TryPeek(out T result))
                    throw new InvalidOperationException("Queue is empty.");
                return result;
            }
        }

        public void Remove(T item)
        {
            lock (CriticalSection) {
                object key = GetItemKey(item);
                if (key == null)
                    return;
                ItemsByPriority.Remove(ItemPriority[key]);
                ItemPriority.Remove(key);
                --Count;
                if (key == GetItemKey(Head))
                    Head = IsEmpty ? default : ItemsByPriority.First().Value;
            }
        }

        public bool TryDequeue(out T result)
        {
            lock (CriticalSection) {
                result = Head;
                if (IsEmpty)
                    return false;
                Remove(Head);
                return true;
            }
        }

        public T Dequeue()
        {
            lock (CriticalSection) {
                if (!TryDequeue(out T result))
                    throw new InvalidOperationException("Queue is empty.");
                return result;
            }
        }
    }
}
