/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace QtVsTools.Common
{
    public class LazyFactory
    {
        private Lazy<ConcurrentDictionary<PropertyInfo, Lazy<object>>> LazyObjs { get; }
        private ConcurrentDictionary<PropertyInfo, Lazy<object>> Objs => LazyObjs.Value;

        public LazyFactory()
        {
            LazyObjs = new Lazy<ConcurrentDictionary<PropertyInfo, Lazy<object>>>();
        }

        public T Get<T>(Expression<Func<T>> propertyRef, Func<T> initFunc) where T : class
        {
            var lazyPropertyExpr = propertyRef?.Body as MemberExpression;
            var lazyProperty = lazyPropertyExpr?.Member as PropertyInfo;
            if (lazyProperty == null)
                throw new ArgumentException("Invalid property reference", "propertyRef");
            var lazyObj = Objs.GetOrAdd(lazyProperty, _ => new Lazy<object>(initFunc));
            return lazyObj.Value as T;
        }
    }
}
