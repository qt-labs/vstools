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

        object IDeferredObject.Object
        {
            get { return Object; }
        }

        public bool HasData
        {
            get { return Object != null; }
        }

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
