/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
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
using System.Runtime.Serialization;
using System.Threading;

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
        protected static object StaticCriticalSection { get; } = new object();

        protected object CriticalSection { get; } = new object();

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
            StaticEnterCriticalSection(this);
        }

        protected static void StaticEnterCriticalSection(Concurrent<TSubClass> _this = null)
        {
            Monitor.Enter(_this?.CriticalSection ?? StaticCriticalSection);
        }

        protected void LeaveCriticalSection()
        {
            StaticLeaveCriticalSection(this);
        }

        protected static void StaticLeaveCriticalSection(Concurrent<TSubClass> _this = null)
        {
            if (Monitor.IsEntered(_this?.CriticalSection ?? StaticCriticalSection))
                Monitor.Exit(_this?.CriticalSection ?? StaticCriticalSection);
        }

        protected void AbortCriticalSection()
        {
            StaticAbortCriticalSection(this);
        }

        protected static void StaticAbortCriticalSection(Concurrent<TSubClass> _this = null)
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
            bool success = false;
            lock (_this?.CriticalSection ?? StaticCriticalSection) {
                success = test();
                if (success)
                    action();
                else if (actionElse != null)
                    actionElse();
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
                value = default(T);
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
            else
                return value == null;
        }

        public void Release()
        {
            Set(default(T));
        }

        public static implicit operator T(Exclusive<T> _this)
        {
            return _this.value;
        }
    }
}
