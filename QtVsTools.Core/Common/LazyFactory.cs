/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace QtVsTools.Common
{
    public class LazyFactory : Concurrent
    {
        private ConcurrentDictionary<PropertyInfo, object> Objs { get; } = new();

        public T Get<T>(Expression<Func<T>> propertyRef, Func<T> initFunc) where T : class
        {
            if (propertyRef?.Body is not MemberExpression lazyPropertyExpr)
                throw new ArgumentException("Expected lambda member expression", "propertyRef");
            if (lazyPropertyExpr?.Member is not PropertyInfo lazyProperty)
                throw new ArgumentException("Invalid property reference", "propertyRef");
            lock (CriticalSection) {
                if (!Objs.TryGetValue(lazyProperty, out var lazyObject))
                    Objs[lazyProperty] = lazyObject = initFunc();
                return lazyObject as T;
            }
        }
    }
}
