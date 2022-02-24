/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
            var lazyObj = Objs.GetOrAdd(lazyProperty, (_) => new Lazy<object>(() => initFunc()));
            return lazyObj.Value as T;
        }
    }
}
