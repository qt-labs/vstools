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
    abstract class Prototyped<TBase> : Concurrent
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
                .Where(x => x.GetParameters().Length == 0)
                .FirstOrDefault();

            if (ctorInfo == null)
                throw new NotSupportedException("No default constructor: " + type.Name);

            return ctorInfo.Invoke(new object[0]) as TBase;
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

        public TBase Prototype
        {
            get { return ThisClass.Prototype; }
        }

        SubClass thisClass = null;
        protected SubClass ThisClass
        {
            get
            {
                Atomic(() => thisClass == null, () => thisClass = SubClass.Get(GetType()));
                return thisClass;
            }
        }

        protected static SubClass BaseClass
        {
            get { return SubClass.baseClass; }
        }

        public static TBase BasePrototype
        {
            get { return BaseClass.Prototype; }
        }

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
            public IEnumerable<SubClass> SubClasses
            {
                get
                {
                    return SubTypes.Select(x => Get(x)).Where(x => x != null);
                }
            }

            static readonly object classCriticalSection = new object();

            static readonly Dictionary<Type, List<Type>> types = GetTypeHierarchy(typeof(TBase));

            static Dictionary<Type, List<Type>> GetTypeHierarchy(Type baseType)
            {
                var subTypes = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(x => baseType.IsAssignableFrom(x)
                        && x.IsAbstract == false
                        && x.ContainsGenericParameters == false)
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
                return new SubClass()
                {
                    Type = type,
                    SubTypes = subTypes,
                    Prototype = CreatePrototype(type),
                };
            }

            public static SubClass Get(Type type)
            {
                if (!typeof(TBase).IsAssignableFrom(type))
                    return null;

                lock (classCriticalSection) {

                    SubClass subClass = null;
                    if (!classes.TryGetValue(type, out subClass)) {

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
