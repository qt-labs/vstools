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
    abstract class Concurrent
    {
        private readonly static object criticalSectionGlobal = new object();
        private object criticalSection = null;

        protected object CriticalSection
        {
            get
            {
                if (criticalSection == null) { // prevent global lock at every call
                    lock (criticalSectionGlobal) {
                        if (criticalSection == null) // prevent race conditions
                            criticalSection = new object();
                    }
                }
                return criticalSection;
            }
        }

        protected void EnterCriticalSection()
        {
            Monitor.Enter(CriticalSection);
        }

        protected void LeaveCriticalSection()
        {
            if (Monitor.IsEntered(CriticalSection))
                Monitor.Exit(CriticalSection);
        }

        protected void AbortCriticalSection()
        {
            while (Monitor.IsEntered(CriticalSection))
                Monitor.Exit(CriticalSection);
        }

        protected void ThreadSafe(Action action)
        {
            lock (CriticalSection)
                action();
        }

        protected T ThreadSafe<T>(Func<T> func)
        {
            lock (CriticalSection)
                return func();
        }

        protected bool Atomic(Func<bool> test, Action action, Action actionElse = null)
        {
            bool success = false;
            lock (CriticalSection) {
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
    /// Allows exclusive access to a wrapped variable. Reading access is always allowed. Concurrent
    /// write requests are protected by mutex: only the first requesting thread will be granted
    /// access; all other requests will be blocked until the value is reset (i.e. thread with
    /// write access sets the variable's default value).
    /// </summary>
    /// <typeparam name="T">Type of wrapped variable</typeparam>
    ///
    class Exclusive<T> : Concurrent
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
