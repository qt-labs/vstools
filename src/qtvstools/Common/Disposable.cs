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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace QtVsTools
{
    /// <summary>
    /// Implemented by objects that wish to receive notifications of disposal of other objects
    /// </summary>
    ///
    public interface IDisposableEventSink
    {
        void NotifyDisposing(IDisposable obj);
        void NotifyDisposed(IDisposable obj);
    }

    /// <summary>
    /// Base class of object that implement the Dispose Pattern in a thread-safe manner
    /// cf. https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
    /// </summary>
    ///
    [DataContract]
    abstract class Disposable : Concurrent, IDisposable
    {
        protected const bool DisposingFrom_ObjectFinalizer = false;
        protected const bool DisposingFrom_DisposeInterface = true;

        private HashSet<IDisposableEventSink> eventSinks = null;
        private HashSet<IDisposableEventSink> EventSinks
        {
            get
            {
                return ThreadSafe(() => eventSinks != null ? eventSinks
                        : eventSinks = new HashSet<IDisposableEventSink>());
            }
        }

        public bool Disposed { get; private set; }

        public bool Disposing { get; private set; }

        public void AdviseDispose(IDisposableEventSink sink)
        {
            ThreadSafe(() => EventSinks.Add(sink));
        }

        public void UnadviseDispose(IDisposableEventSink sink)
        {
            ThreadSafe(() => EventSinks.Remove(sink));
        }

        public void Dispose()
        {
            Dispose(DisposingFrom_DisposeInterface);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override to dispose managed objects
        /// </summary>
        ///
        protected virtual void DisposeManaged()
        { }

        /// <summary>
        /// Override to dispose unmanaged resources
        /// </summary>
        ///
        protected virtual void DisposeUnmanaged()
        { }

        /// <summary>
        /// Override for clean-up procedure at the end of the disposal process
        /// </summary>
        ///
        protected virtual void DisposeFinally()
        { }

        /// <summary>
        /// Override to block disposal
        /// </summary>
        ///
        public virtual bool CanDispose { get { return true; } }

        protected virtual void Dispose(bool disposingFrom)
        {
            if (!Atomic(() => CanDispose && !Disposed && !Disposing, () => Disposing = true))
                return;

            ThreadSafe(() => EventSinks.ToList())
                .ForEach(sink => sink.NotifyDisposing(this));

            if (disposingFrom == DisposingFrom_DisposeInterface)
                DisposeManaged();

            DisposeUnmanaged();

            Disposed = true;

            DisposeFinally();

            ThreadSafe(() => EventSinks.ToList())
                .ForEach(sink => sink.NotifyDisposed(this));
        }
    }

    /// <summary>
    /// Base class of disposable objects that need a finalizer method
    /// cf. https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern#finalizable-types
    /// </summary>
    ///
    [DataContract]
    abstract class Finalizable : Disposable
    {
        ~Finalizable()
        {
            Dispose(DisposingFrom_ObjectFinalizer);
        }
    }
}
