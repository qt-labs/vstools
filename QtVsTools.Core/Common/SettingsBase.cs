// ************************************************************************************************
// Copyright (C) 2024 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
// ************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace QtVsTools.Core.Common
{
    using Options;

    public abstract class SettingsBase<T>
    {
        public TV GetValue<TV>(Expression<Func<TV>> propertyByRef)
        {
            var property = GetPropertyInfo(propertyByRef);
            if (!TryGetAttributeKey(property, out (string Key, object) tuple))
                return default;
            if (!Settings.TryGetValue(tuple.Key, out var value))
                return default;

            if (Equals<TV, bool>() && value is int number)
                return (TV)(object)(number == 1);
            if (Equals<TV, QtOptionsPage.Timeout>() && value is int timeout)
                return (TV)(object)(QtOptionsPage.Timeout)timeout;
            if (typeof(TV).IsEnum && value is string enumerator)
                return (TV)Enum.Parse(typeof(TV), enumerator);
            return value is TV defaultValue ? defaultValue : default;
        }

        public void SetValue<TV>(Expression<Func<TV>> propertyByRef, object value)
        {
            var property = GetPropertyInfo(propertyByRef);
            if (TryGetAttributeKey(property, out (string Key, object) tuple))
                Settings[tuple.Key] = value;
        }

        public abstract void SaveSettings();
        protected abstract Dictionary<string, object> LoadSettings();

        protected void Initialize()
        {
            Settings = new ConcurrentDictionary<string, object>(LoadSettings());
            InitializedEvent.Set();
        }

        protected static bool TryGetAttributeKey(MemberInfo property, out (string, object) tuple)
        {
            tuple = (null, null);
            if (Attribute.IsDefined(property, typeof(SettingsAttribute))) {
                var attributes = property.GetCustomAttributes(typeof(SettingsAttribute), false)
                    .OfType<SettingsAttribute>().ToList();
                if (attributes.FirstOrDefault() is { } attribute)
                    tuple = (attribute.Key, attribute.DefaultValue);
            }
            return !string.IsNullOrEmpty(tuple.Item1);
        }

        protected readonly ManualResetEventSlim InitializedEvent = new(false);
        protected ConcurrentDictionary<string, object> Settings { get; private set; }

        private static bool Equals<T1, T2>()
        {
            return typeof(T1) == typeof(T2);
        }

        private static PropertyInfo GetPropertyInfo<TV>(Expression<Func<TV>> propertyByRef)
        {
            if (propertyByRef.Body is not MemberExpression memberExpression)
                throw new ArgumentException("Expression must be a member access expression.");

            if (memberExpression.Member is not PropertyInfo property)
                throw new ArgumentException("Expression must be a property access expression.");

            var declaringType = property.DeclaringType;
            if (declaringType == null || !typeof(T).IsAssignableFrom(declaringType))
                throw new ArgumentException($"Property must be declared in a subclass of {nameof(T)}.");

            return property;
        }
    }
}
