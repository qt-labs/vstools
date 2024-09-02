/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
        protected const bool DisposingFromObjectFinalizer = false;
        protected const bool DisposingFromDisposeInterface = true;

        private HashSet<IDisposableEventSink> eventSinks;
        private HashSet<IDisposableEventSink> EventSinks =>
            ThreadSafe(() => eventSinks ??= new HashSet<IDisposableEventSink>());

        public bool Disposed { get; private set; }

        public bool Disposing { get; private set; }

        public void Dispose()
        {
            Dispose(DisposingFromDisposeInterface);
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

            if (disposingFrom == DisposingFromDisposeInterface)
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
            Dispose(DisposingFromObjectFinalizer);
        }
    }
}
