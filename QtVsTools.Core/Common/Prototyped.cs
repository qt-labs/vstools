/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace QtVsTools
{
    /// <summary>
    /// Class hierarchies derived from Prototyped<T> will have, for each class in the hierarchy, a
    /// special prototype instance that will represent that class. This is especially useful when
    /// traversing a class hierarchy looking for an appropriate class, e.g. for deserializing data.
    ///
    /// To qualify as prototyped, the base class cannot be generic, no class in the hierarchy can
    /// be abstract and all classes must define a default constructor.
    /// </summary>
    /// <typeparam name="TBase">Base class of the prototyped class hierarchy</typeparam>
    ///
    [DataContract]
    public abstract class Prototyped<TBase> : Concurrent
        where TBase : Prototyped<TBase>
    {
        protected bool IsPrototype { get; private set; }

        protected static TBase CreateInstance(Type type)
        {
            if (typeof(TBase).ContainsGenericParameters)
                throw new NotSupportedException("Generic base class: " + typeof(TBase).Name);

            if (!typeof(TBase).IsAssignableFrom(type))
                throw new NotSupportedException("Not a derived type: " + type.Name);

            if (type.IsAbstract)
                throw new NotSupportedException("Abstract class: " + type.Name);

            if (type.ContainsGenericParameters)
                throw new NotSupportedException("Generic class: " + type.Name);

            var ctorInfo = ((TypeInfo)type).DeclaredConstructors
                .FirstOrDefault(x => x.GetParameters().Length == 0);

            if (ctorInfo == null)
                throw new NotSupportedException("No default constructor: " + type.Name);

            return ctorInfo.Invoke(Array.Empty<object>()) as TBase;
        }

        private static TBase CreatePrototype(Type type)
        {
            var obj = CreateInstance(type);
            obj.IsPrototype = true;
            obj.InitializePrototype();
            return obj;
        }

        protected virtual void InitializePrototype()
        {
            System.Diagnostics.Debug.Assert(IsPrototype);
        }

        public TBase Prototype => ThisClass.Prototype;

        SubClass thisClass;
        protected SubClass ThisClass
        {
            get
            {
                Atomic(() => thisClass == null, () => thisClass = SubClass.Get(GetType()));
                return thisClass;
            }
        }

        protected static SubClass BaseClass => SubClass.baseClass;

        public static TBase BasePrototype => BaseClass.Prototype;

        #region //////////////////// SubClass /////////////////////////////////////////////////////

        /// <summary>
        /// Each class in the prototyped hierarchy will have a SubClass object that represents it.
        /// This object contains the class Type, its sub classes and the prototype instance.
        /// </summary>
        ///
        protected sealed class SubClass
        {
            public Type Type { get; set; }
            public TBase Prototype { get; set; }
            public IEnumerable<Type> SubTypes { get; set; }
            public IEnumerable<SubClass> SubClasses => SubTypes.Select(Get).Where(x => x != null);

            static readonly object classCriticalSection = new();

            static readonly Dictionary<Type, List<Type>> types = GetTypeHierarchy(typeof(TBase));

            static Dictionary<Type, List<Type>> GetTypeHierarchy(Type baseType)
            {
                var subTypes = baseType.Assembly.GetTypes()
                    .Where(x => baseType.IsAssignableFrom(x)
                        && x is { IsAbstract: false, ContainsGenericParameters: false })
                    .ToDictionary(x => x, x => new List<Type>());

                var toDo = new Queue<Type>(subTypes.Keys);
                var seen = new HashSet<Type>(subTypes.Keys);

                while (toDo.Count > 0) {
                    var type = toDo.Dequeue();

                    if (!typeof(TBase).IsAssignableFrom(type.BaseType))
                        continue;

                    if (type.BaseType.IsAbstract)
                        throw new NotSupportedException("Abstract class: " + type.BaseType.Name);

                    if (type.BaseType.ContainsGenericParameters)
                        throw new NotSupportedException("Generic class: " + type.BaseType.Name);

                    if (!subTypes.ContainsKey(type.BaseType))
                        subTypes.Add(type.BaseType, new List<Type>());

                    subTypes[type.BaseType].Add(type);

                    if (seen.Contains(type.BaseType))
                        continue;

                    toDo.Enqueue(type.BaseType);
                    seen.Add(type.BaseType);
                }

                return subTypes;
            }

            static readonly Dictionary<Type, SubClass> classes = types
                .ToDictionary(x => x.Key, x => Create(x.Key, x.Value));

            static SubClass Create(Type type, IEnumerable<Type> subTypes)
            {
                return new SubClass
                {
                    Type = type,
                    SubTypes = subTypes,
                    Prototype = CreatePrototype(type)
                };
            }

            public static SubClass Get(Type type)
            {
                if (!typeof(TBase).IsAssignableFrom(type))
                    return null;

                lock (classCriticalSection) {

                    if (!classes.TryGetValue(type, out SubClass subClass)) {

                        var newTypes = GetTypeHierarchy(type)
                            .Where(x => !classes.ContainsKey(x.Key));

                        foreach (var newType in newTypes) {
                            var newClass = Create(newType.Key, newType.Value);
                            classes.Add(newType.Key, newClass);

                            if (type == newType.Key)
                                subClass = newClass;
                        }
                    }

                    return subClass;
                }
            }

            public static readonly SubClass baseClass = Get(typeof(TBase));
        }

        #endregion //////////////////// SubClass //////////////////////////////////////////////////

    }
}
