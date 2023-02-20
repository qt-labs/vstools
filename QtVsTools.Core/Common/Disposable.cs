/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
    public abstract class Disposable : Concurrent, IDisposable
    {
        protected const bool DisposingFrom_ObjectFinalizer = false;
        protected const bool DisposingFrom_DisposeInterface = true;

        private HashSet<IDisposableEventSink> eventSinks = null;
        private HashSet<IDisposableEventSink> EventSinks =>
            ThreadSafe(() => eventSinks ?? (eventSinks = new HashSet<IDisposableEventSink>()));

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
        public virtual bool CanDispose => true;

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
    public abstract class Finalizable : Disposable
    {
        ~Finalizable()
        {
            Dispose(DisposingFrom_ObjectFinalizer);
        }
    }
}
