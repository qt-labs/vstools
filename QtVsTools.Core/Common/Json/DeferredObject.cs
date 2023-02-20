/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Json
{
    /// <summary>
    /// Public interface of objects that allow deferred deserialization of their data.
    /// </summary>
    /// <typeparam name="TBase">Base type of deferred data</typeparam>
    ///
    public interface IDeferrable<TBase>
    {
        TBase Deserialize(IJsonData jsonData);
    }

    /// <summary>
    /// Provides deferred deserialization of a wrapped object, given the base class of a
    /// prototyped class hierarchy that will be searched for the actual deserialization class.
    /// </summary>
    /// <typeparam name="TBase">Base of deferrable class hierarchy</typeparam>
    ///
    [DataContract]
    public class DeferredObject<TBase> : Disposable, IDeferredObject
        where TBase : Prototyped<TBase>, IDeferrable<TBase>
    {
        private IJsonData jsonData;

        public TBase Object { get; private set; }

        object IDeferredObject.Object => Object;

        public bool HasData => Object != null;

        /// <summary>
        /// This constructor is used when serializing, to directly wrap an existing object.
        /// </summary>
        /// <param name="obj">Object to wrap</param>
        ///
        public DeferredObject(TBase obj)
        {
            Object = obj;
        }

        [OnDeserializing] // <-- Invoked by serializer before deserializing this object
        void OnDeserializing(StreamingContext context)
        {
            // Store JSON data corresponding to this object
            jsonData = Serializer.GetCurrentJsonData();
        }

        /// <summary>
        /// Performs a deferred deserialization to obtain a new wrapped object corresponding to the
        /// contents of the stored JSON data. The actual deserialization is delegated to the base
        /// prototype of the class hierarchy. This prototype is then responsible to find an
        /// appropriate class in the hierarchy and map the JSON data to an instance of the class.
        /// </summary>
        ///
        public void Deserialize()
        {
            Atomic(() => Object == null && jsonData != null, () =>
            {
                Object = Prototyped<TBase>.BasePrototype.Deserialize(jsonData);
                jsonData.Dispose();
                jsonData = null;
            });
        }

        protected override void DisposeManaged()
        {
            if (jsonData != null)
                jsonData.Dispose();
        }

        public static implicit operator TBase(DeferredObject<TBase> _this)
        {
            return _this.Object;
        }
    }
}
