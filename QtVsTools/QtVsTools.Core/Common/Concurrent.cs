// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace QtVsTools
{
    /// <summary>
    /// Base class of objects requiring thread-safety features
    /// </summary>
    ///
    [DataContract]
    public abstract class Concurrent<TSubClass>
        where TSubClass : Concurrent<TSubClass>
    {
        // Shared static critical section used by non-resource methods (like ThreadSafeInit).
        protected static object StaticCriticalSection { get; } = new();

        // Per-instance critical section to lock per instance rather than global.
        protected object CriticalSection { get; } = new();

        protected static ConcurrentDictionary<string, Resource> Resources { get; } = new();

        protected sealed class Resource
        {
            public object SyncRoot { get; } = new();
            public bool IsLocked { get; set; }
        }

        // Optionally remove the resource from the dictionary entirely.
        protected static void Free(string resourceName)
        {
            Resources.TryRemove(resourceName, out _);
        }

        protected static bool Get(string resourceName, int timeout = -1)
        {
            var resource = Resources.GetOrAdd(resourceName, _ => new Resource());

            lock (resource.SyncRoot) {
                if (timeout < 0) {
                    // Infinite wait
                    while (resource.IsLocked)
                        Monitor.Wait(resource.SyncRoot);

                    resource.IsLocked = true;
                    return true;
                }

                // Timed wait
                var startTime = Environment.TickCount;
                while (resource.IsLocked) {
                    var elapsed = Environment.TickCount - startTime;
                    // Guard against overflow
                    if (elapsed < 0)
                        elapsed = int.MaxValue;

                    var remaining = timeout - elapsed;
                    if (remaining <= 0 || !Monitor.Wait(resource.SyncRoot, remaining))
                        return false; // Timed out
                }

                resource.IsLocked = true;
                return true;
            }
        }

        protected static async Task<bool> GetAsync(string resourceName, int timeout = -1)
        {
            return await Task.Run(() => Get(resourceName, timeout));
        }

        protected static void Release(string resourceName)
        {
            if (!Resources.TryGetValue(resourceName, out var resource))
                return;

            lock (resource.SyncRoot) {
                if (!resource.IsLocked)
                    return;
                resource.IsLocked = false;
                Monitor.Pulse(resource.SyncRoot);
            }
        }

        protected T ThreadSafeInit<T>(Func<T> getValue, Action init)
            where T : class
        {
            return StaticThreadSafeInit(getValue, init, this);
        }

        protected static T StaticThreadSafeInit<T>(
                Func<T> getValue,
                Action init,
                Concurrent<TSubClass> instance = null)
            where T : class
        {
            // prevent global lock at every call
            var value = getValue();
            if (value != null)
                return value;

            lock (instance?.CriticalSection ?? StaticCriticalSection) {
                // prevent race conditions
                value = getValue();
                if (value != null)
                    return value;
                init();
                value = getValue();
                return value;
            }
        }

        protected void EnterCriticalSection()
        {
            EnterStaticCriticalSection(this);
        }

        protected bool TryEnterCriticalSection()
        {
            return TryEnterStaticCriticalSection(this);
        }

        protected static void EnterStaticCriticalSection(Concurrent<TSubClass> instance = null)
        {
            Monitor.Enter(instance?.CriticalSection ?? StaticCriticalSection);
        }

        protected static bool TryEnterStaticCriticalSection(Concurrent<TSubClass> instance = null)
        {
            return Monitor.TryEnter(instance?.CriticalSection ?? StaticCriticalSection);
        }

        protected void LeaveCriticalSection()
        {
            LeaveStaticCriticalSection(this);
        }

        protected static void LeaveStaticCriticalSection(Concurrent<TSubClass> instance = null)
        {
            if (Monitor.IsEntered(instance?.CriticalSection ?? StaticCriticalSection))
                Monitor.Exit(instance?.CriticalSection ?? StaticCriticalSection);
        }

        protected void AbortCriticalSection()
        {
            AbortStaticCriticalSection(this);
        }

        protected static void AbortStaticCriticalSection(Concurrent<TSubClass> instance = null)
        {
            while (Monitor.IsEntered(instance?.CriticalSection ?? StaticCriticalSection))
                Monitor.Exit(instance?.CriticalSection ?? StaticCriticalSection);
        }

        protected void ThreadSafe(Action action)
        {
            StaticThreadSafe(action, this);
        }

        protected static void StaticThreadSafe(Action action, Concurrent<TSubClass> instance = null)
        {
            lock (instance?.CriticalSection ?? StaticCriticalSection) {
                action();
            }
        }

        protected T ThreadSafe<T>(Func<T> func)
        {
            return StaticThreadSafe(func, this);
        }

        protected static T StaticThreadSafe<T>(Func<T> func, Concurrent<TSubClass> instance = null)
        {
            lock (instance?.CriticalSection ?? StaticCriticalSection) {
                return func();
            }
        }

        protected bool Atomic(Func<bool> test, Action action)
        {
            return StaticAtomic(test, action, instance: this);
        }

        protected bool Atomic(Func<bool> test, Action action, Action actionElse)
        {
            return StaticAtomic(test, action, actionElse, this);
        }

        protected static bool StaticAtomic(
            Func<bool> test,
            Action action,
            Action actionElse = null,
            Concurrent<TSubClass> instance = null)
        {
            bool success;
            lock (instance?.CriticalSection ?? StaticCriticalSection) {
                success = test();
                if (success)
                    action();
                else
                    actionElse?.Invoke();
            }
            return success;
        }
    }

    /// <summary>
    /// Base class of objects requiring thread-safety features
    /// Sub-classes will share the same static critical section
    /// </summary>
    ///
    [DataContract]
    public class Concurrent : Concurrent<Concurrent>
    {
    }

    /// <summary>
    /// Simplify use of synchronization features in classes that are not Concurrent-based.
    /// </summary>
    ///
    public sealed class Synchronized : Concurrent<Synchronized>
    {
        private Synchronized() { }

        public static new bool Atomic(Func<bool> test, Action action)
        {
            return StaticAtomic(test, action);
        }

        public static new bool Atomic(Func<bool> test, Action action, Action actionElse)
        {
            return StaticAtomic(test, action, actionElse);
        }

        public static new void ThreadSafe(Action action)
        {
            StaticThreadSafe(action);
        }

        public static new T ThreadSafe<T>(Func<T> func)
        {
            return StaticThreadSafe(func);
        }

        public static new void Free(string resourceName)
        {
            Concurrent.Free(resourceName);
        }

        public static new bool Get(string resourceName, int timeout = -1)
        {
            return Concurrent.Get(resourceName, timeout);
        }

        public static new Task<bool> GetAsync(string resourceName, int timeout = -1)
        {
            return Concurrent.GetAsync(resourceName, timeout);
        }

        public static new void Release(string resourceName)
        {
            Concurrent.Release(resourceName);
        }

        // Expose the base critical section if needed
        public static new object StaticCriticalSection => Concurrent.StaticCriticalSection;
    }

    /// <summary>
    /// Allows exclusive access to a wrapped variable. Reading access is always allowed. Concurrent
    /// write requests are protected by instance-based "critical sections." Once a thread sets a
    /// non-default value, it effectively 'holds' it until it sets it back to default.
    /// </summary>
    /// <typeparam name="T">Type of wrapped variable</typeparam>
    ///
    [DataContract]
    public class Exclusive<T> : Concurrent
    {
        private T value;

        public void Set(T newValue)
        {
            EnterCriticalSection();
            if (IsNull(value) && !IsNull(newValue)) {
                // Acquiring
                value = newValue;

            } else if (!IsNull(value) && !IsNull(newValue)) {
                // Already held, update in place
                value = newValue;
                LeaveCriticalSection();

            } else if (!IsNull(value) && IsNull(newValue)) {
                // Releasing
                value = default;
                LeaveCriticalSection();
                // This class uses nested calls, so we call LeaveCriticalSection() once more
                LeaveCriticalSection();

            } else {
                // Edge case: was null, setting null => no change
                LeaveCriticalSection();
            }
        }

        private static readonly EqualityComparer<T> EqualityComparer = EqualityComparer<T>.Default;
        private static bool IsNull(T val) => EqualityComparer.Equals(val, default);

        // Sets value to default => releases one level of lock
        // plus the additional "extra" lock if it was currently held.
        public void Release()
        {
            Set(default);
        }

        public static implicit operator T(Exclusive<T> instance)
        {
            return instance.value;
        }
    }
}
