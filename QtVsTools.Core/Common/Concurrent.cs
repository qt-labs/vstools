// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Concurrent;
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
        protected static object StaticCriticalSection { get; } = new();

        protected object CriticalSection { get; } = new();

        protected static ConcurrentDictionary<string, object> Resources { get; } = new();

        protected static object Alloc(string resourceName)
        {
            return Resources.GetOrAdd(resourceName, _ => new object());
        }

        protected static void Free(string resourceName)
        {
            Resources.TryRemove(resourceName, out _);
        }

        protected static bool Get(string resourceName, int timeout = -1)
        {
            var resource = Alloc(resourceName);

            var lockTaken = false;
            try {
                // Attempt to enter the critical section
                if (timeout >= 0) {
                    lockTaken = Monitor.TryEnter(resource, timeout);
                } else {
                    Monitor.Enter(resource);
                    lockTaken = true;
                }

                return lockTaken;
            } catch {
                if (lockTaken)
                    Monitor.Exit(resource);
                throw;
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
            if (Monitor.IsEntered(resource))
                Monitor.Exit(resource);
        }

        protected T ThreadSafeInit<T>(Func<T> getValue, Action init)
            where T : class
        {
            return StaticThreadSafeInit(getValue, init, this);
        }

        protected static T StaticThreadSafeInit<T>(
                Func<T> getValue,
                Action init,
                Concurrent<TSubClass> _this = null)
            where T : class
        {
            // prevent global lock at every call
            T value = getValue();
            if (value != null)
                return value;
            lock (_this?.CriticalSection ?? StaticCriticalSection) {
                // prevent race conditions
                value = getValue();
                if (value == null) {
                    init();
                    value = getValue();
                }
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

        protected static void EnterStaticCriticalSection(Concurrent<TSubClass> _this = null)
        {
            Monitor.Enter(_this?.CriticalSection ?? StaticCriticalSection);
        }

        protected static bool TryEnterStaticCriticalSection(Concurrent<TSubClass> _this = null)
        {
            return Monitor.TryEnter(_this?.CriticalSection ?? StaticCriticalSection);
        }

        protected void LeaveCriticalSection()
        {
            LeaveStaticCriticalSection(this);
        }

        protected static void LeaveStaticCriticalSection(Concurrent<TSubClass> _this = null)
        {
            if (Monitor.IsEntered(_this?.CriticalSection ?? StaticCriticalSection))
                Monitor.Exit(_this?.CriticalSection ?? StaticCriticalSection);
        }

        protected void AbortCriticalSection()
        {
            AbortStaticCriticalSection(this);
        }

        protected static void AbortStaticCriticalSection(Concurrent<TSubClass> _this = null)
        {
            while (Monitor.IsEntered(_this?.CriticalSection ?? StaticCriticalSection))
                Monitor.Exit(_this?.CriticalSection ?? StaticCriticalSection);
        }

        protected void ThreadSafe(Action action)
        {
            StaticThreadSafe(action, this);
        }

        protected static void StaticThreadSafe(Action action, Concurrent<TSubClass> _this = null)
        {
            lock (_this?.CriticalSection ?? StaticCriticalSection) {
                action();
            }
        }

        protected T ThreadSafe<T>(Func<T> func)
        {
            return StaticThreadSafe(func, this);
        }

        protected static T StaticThreadSafe<T>(Func<T> func, Concurrent<TSubClass> _this = null)
        {
            lock (_this?.CriticalSection ?? StaticCriticalSection) {
                return func();
            }
        }

        protected bool Atomic(Func<bool> test, Action action)
        {
            return StaticAtomic(test, action, _this: this);
        }

        protected bool Atomic(Func<bool> test, Action action, Action actionElse)
        {
            return StaticAtomic(test, action, actionElse, this);
        }

        protected static bool StaticAtomic(
            Func<bool> test,
            Action action,
            Action actionElse = null,
            Concurrent<TSubClass> _this = null)
        {
            bool success;
            lock (_this?.CriticalSection ?? StaticCriticalSection) {
                success = test();
                if (success)
                    action();
                else {
                    actionElse?.Invoke();
                }
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

        public static new object Alloc(string resourceName)
        {
            return Concurrent.Alloc(resourceName);
        }

        public static new void Free(string resourceName)
        {
            Concurrent.Free(resourceName);
        }

        public static new bool Get(string resourceName, int timeout = -1)
        {
            return Concurrent.Get(resourceName, timeout);
        }

        public static new async Task<bool> GetAsync(string resName, int timeout = -1)
        {
            return await Concurrent.GetAsync(resName, timeout);
        }

        public static new void Release(string resourceName)
        {
            Concurrent.Release(resourceName);
        }
    }

    /// <summary>
    /// Allows exclusive access to a wrapped variable. Reading access is always allowed. Concurrent
    /// write requests are protected by mutex: only the first requesting thread will be granted
    /// access; all other requests will be blocked until the value is reset (i.e. thread with
    /// write access sets the variable's default value).
    /// </summary>
    /// <typeparam name="T">Type of wrapped variable</typeparam>
    ///
    public class Exclusive<T> : Concurrent
    {
        private T value;

        public void Set(T newValue)
        {
            EnterCriticalSection();
            if (IsNull(value) && !IsNull(newValue)) {
                value = newValue;

            } else if (!IsNull(value) && !IsNull(newValue)) {
                value = newValue;
                LeaveCriticalSection();

            } else if (!IsNull(value) && IsNull(newValue)) {
                value = default;
                LeaveCriticalSection();
                LeaveCriticalSection();

            } else {
                LeaveCriticalSection();

            }
        }

        bool IsNull(T value)
        {
            if (typeof(T).IsValueType)
                return value.Equals(default(T));
            return value == null;
        }

        public void Release()
        {
            Set(default);
        }

        public static implicit operator T(Exclusive<T> _this)
        {
            return _this.value;
        }
    }
}
