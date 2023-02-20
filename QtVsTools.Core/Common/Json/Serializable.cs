/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace QtVsTools.Json
{
    /// <summary>
    /// Classes in an hierarchy derived from Serializable<T> represent objects that can be mapped
    /// to and from JSON data using the DataContractJsonSerializer class. When deserializing, the
    /// class hierarchy will be searched for the derived class best suited to the data.
    /// </summary>
    /// <typeparam name="TBase">Base of the class hierarchy</typeparam>
    [DataContract]
    public abstract class Serializable<TBase> :
        Prototyped<TBase>,
        IDeferrable<TBase>,
        IDeferredObjectContainer
        where TBase : Serializable<TBase>
    {
        #region //////////////////// Prototype ////////////////////////////////////////////////////

        private Serializer Serializer { get; set; }

        protected Serializable()
        { }

        protected sealed override void InitializePrototype()
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            // Create serializer for this particular type
            Serializer = Serializer.Create(GetType());
        }

        /// <summary>
        /// Check if this class is suited as target type for deserialization, based on the
        /// information already deserialized. Prototypes of derived classes will override this to
        /// implement the local type selection rules.
        /// </summary>
        /// <param name="that">Object containing the data deserialized so far</param>
        /// <returns>
        ///     true  ::= class is suitable and can be used as target type for deserialization
        ///
        ///     false ::= class is not suitable for deserialization
        ///
        ///     null  ::= a derived class of this class might be suitable; search for target type
        ///               should be expanded to include all classes derived from this class
        /// </returns>
        ///
        protected virtual bool? IsCompatible(TBase that)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            return null;
        }

        /// <summary>
        /// Check if this class is marked with the [SkipDeserialization] attribute, which signals
        /// that deserialization of this class is to be skipped while traversing the class
        /// hierarchy looking for a suitable target type for deserialization.
        /// </summary>
        ///
        bool SkipDeserialization
        {
            get
            {
                System.Diagnostics.Debug.Assert(IsPrototype);

                return GetType()
                    .GetCustomAttributes(typeof(SkipDeserializationAttribute), false).Any();
            }
        }

        /// <summary>
        /// Perform a deferred deserialization based on this class hierarchy.
        /// </summary>
        /// <param name="jsonData">Data to deserialize</param>
        /// <returns>Deserialized object</returns>
        ///
        TBase IDeferrable<TBase>.Deserialize(IJsonData jsonData)
        {
            System.Diagnostics.Debug.Assert(this == BasePrototype);

            return DeserializeClassHierarchy(null, jsonData);
        }

        #endregion //////////////////// Prototype /////////////////////////////////////////////////


        #region //////////////////// Deferred Objects /////////////////////////////////////////////

        List<IDeferredObject> deferredObjects = null;
        List<IDeferredObject> DeferredObjects
        {
            get
            {
                Atomic(() => deferredObjects == null,
                    () => deferredObjects = new List<IDeferredObject>());

                return deferredObjects;
            }
        }

        public IEnumerable<IDeferredObject> PendingObjects => DeferredObjects.Where(x => !x.HasData);

        void IDeferredObjectContainer.Add(IDeferredObject item)
        {
            ThreadSafe(() => DeferredObjects.Add(item));
        }

        protected void Add(IDeferredObject item)
        {
            ((IDeferredObjectContainer)this).Add(item);
        }

        #endregion //////////////////// Deferred Objects //////////////////////////////////////////


        /// <summary>
        /// Initialize new instance. Derived classes override this to implement their own
        /// initializations.
        /// </summary>
        ///
        protected virtual void InitializeObject(object initArgs)
        { }

        /// <summary>
        /// Serialize object.
        /// </summary>
        /// <returns>Raw JSON data</returns>
        ///
        public byte[] Serialize(bool indent = false)
        {
            return ThreadSafe(() => Prototype.Serializer.Serialize(this, indent).GetBytes());
        }

        /// <summary>
        /// Serialize object.
        /// </summary>
        /// <returns>JSON string</returns>
        ///
        public string ToJsonString(bool indent = true)
        {
            return ThreadSafe(() => Prototype.Serializer.Serialize(this, indent).GetString());
        }

        /// <summary>
        /// Deserialize object using this class hierarchy. After selecting the most suitable derived
        /// class as target type and deserializing an instance of that class, any deferred objects
        /// are also deserialized using their respective class hierarchies.
        /// </summary>
        /// <param name="initArgs">Additional arguments required for object initialization</param>
        /// <param name="data">Raw JSON data</param>
        /// <returns>Deserialized object, or null if deserialization failed</returns>
        ///
        public static TBase Deserialize(object initArgs, byte[] data)
        {
            TBase obj = DeserializeClassHierarchy(initArgs, Serializer.Parse(data));
            if (obj == null)
                return null;

            var toDo = new Queue<IDeferredObjectContainer>();
            if (obj.PendingObjects.Any())
                toDo.Enqueue(obj);

            while (toDo.Count > 0) {
                var container = toDo.Dequeue();
                foreach (var defObj in container.PendingObjects) {
                    defObj.Deserialize();
                    if (defObj.Object is IDeferredObjectContainer subContainer
                        && subContainer.PendingObjects.Any()) {
                        toDo.Enqueue(subContainer);

                    }
                }
            }
            return obj;
        }

        public static TBase Deserialize(byte[] data)
        {
            return Deserialize(null, data);
        }

        /// <summary>
        /// Traverse this class hierarchy looking for the most suitable derived class that can be
        /// used as target type for the deserialization of the JSON data provided.
        /// </summary>
        /// <param name="initArgs">Additional arguments required for object initialization</param>
        /// <param name="jsonData">Parsed JSON data</param>
        /// <returns>Deserialized object, or null if deserialization failed</returns>
        ///
        protected static TBase DeserializeClassHierarchy(object initArgs, IJsonData jsonData)
        {
            //  PSEUDOCODE:
            //
            //  Nodes to visit := base of class hierarchy.
            //  While there are still nodes to visit
            //      Current node ::= Extract next node to visit.
            //      Tentative object := Deserialize using current node as target type.
            //      If deserialization failed
            //          Skip branch, continue (with next node, if any).
            //      Else
            //          Test compatibility of current node with tentative object.
            //          If not compatible
            //              Skip branch, continue (with next node, if any).
            //          If compatible
            //              If leaf node
            //                  Found suitable node!!
            //                  Return tentative object as final result of deserialization.
            //              Else
            //                  Save tentative object as last sucessful deserialization.
            //                  Add child nodes to the nodes to visit.
            //          If inconclusive (i.e. a child node might be compatible)
            //              Add child nodes to the nodes to visit.
            //  If no suitable node was found
            //      Return last sucessful deserialization as final result of deserialization.

            lock (BaseClass.Prototype.CriticalSection) {

                var toDo = new Queue<SubClass>(new[] { BaseClass });
                TBase lastCompatibleObj = null;

                // Traverse class hierarchy tree looking for a compatible leaf node
                // i.e. compatible class without any sub-classes
                while (toDo.Count > 0) {
                    var subClass = toDo.Dequeue();

                    // Try to deserialize as sub-class
                    TBase tryObj;
                    if (jsonData.IsEmpty())
                        tryObj = CreateInstance(subClass.Type);
                    else
                        tryObj = subClass.Prototype.Serializer.Deserialize(jsonData) as TBase;

                    if (tryObj == null)
                        continue; // Not deserializable as this type

                    tryObj.InitializeObject(initArgs);

                    // Test compatbility
                    var isCompatible = subClass.Prototype.IsCompatible(tryObj);

                    if (isCompatible == false) {
                        // Incompatible
                        continue;

                    } else if (isCompatible == true) {
                        // Compatible

                        if (!subClass.SubTypes.Any())
                            return tryObj; // Found compatible leaf node!

                        // Non-leaf node; continue searching
                        lastCompatibleObj = tryObj;
                        PotentialSubClasses(subClass, tryObj)
                            .ForEach(x => toDo.Enqueue(x));
                        continue;

                    } else {
                        // Maybe has compatible derived class

                        if (subClass.SubTypes.Any()) {
                            // Non-leaf node; continue searching
                            PotentialSubClasses(subClass, tryObj)
                                .ForEach(x => toDo.Enqueue(x));
                        }
                        continue;
                    }
                }

                // No compatible leaf node found
                // Use last successful (non-leaf) deserializtion, if any
                return lastCompatibleObj;
            }
        }

        /// <summary>
        /// Get list of sub-classes of a particular class that are potentially suitable to the
        /// deserialized data. Sub-classes marked with the [SkipDeserialization] attribute will not
        /// be returned; their own sub-sub-classes will be tested for compatibility and returned in
        /// case they are potentially suitable (i.e.: IsCompatible == true || IsCompatible == null)
        /// </summary>
        /// <param name="subClass">Class whose sub-classes are to be tested</param>
        /// <param name="tryObj">Deserialized data</param>
        /// <returns>List of sub-classes that are potentially suitable for deserialization</returns>
        static List<SubClass> PotentialSubClasses(SubClass subClass, TBase tryObj)
        {
            if (subClass == null || tryObj == null)
                return new List<SubClass>();

            var potential = new List<SubClass>();
            var toDo = new Queue<SubClass>(subClass.SubClasses);
            while (toDo.Count > 0) {
                subClass = toDo.Dequeue();

                if (subClass.Prototype.IsCompatible(tryObj) == false)
                    continue;

                if (subClass.Prototype.SkipDeserialization && subClass.SubClasses.Any()) {
                    foreach (var subSubClass in subClass.SubClasses)
                        toDo.Enqueue(subSubClass);

                    continue;
                }

                potential.Add(subClass);
            }

            return potential;
        }
    }

    public class SkipDeserializationAttribute : Attribute
    { }
}
