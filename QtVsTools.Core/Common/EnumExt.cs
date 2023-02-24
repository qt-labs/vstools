/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace QtVsTools.Common
{
    /// <summary>
    /// Extended enum support:
    ///   * Customized cast of enum values to arbitrary types
    /// </summary>
    public static class EnumExt
    {
        static LazyFactory StaticLazy { get; } = new LazyFactory();

        /// <summary>
        /// Wrapper for enum cast values.
        /// </summary>
        /// <typeparam name="T">Type of cast output</typeparam>
        /// <remarks>
        /// Cast attributes associated with enum values must implement this interface.
        /// </remarks>
        public interface ICast<T>
        {
            T Value { get; }
        }

        /// <summary>
        /// String cast attribute associated to an enum value.
        /// </summary>
        /// <example>
        ///     enum Foobar {
        ///         Foo,
        ///         [EnumExt.String("Bahr")] Bar
        ///     }
        /// </example>
        [AttributeUsage(AttributeTargets.All)]
        public sealed class StringAttribute : Attribute, ICast<string>
        {
            public string Value { get; }
            public StringAttribute(string str) { Value = str; }
        }

        /// <summary>
        /// Cast enum value to type T.
        /// </summary>
        /// <typeparam name="T">Cast output type.</typeparam>
        /// <param name="value">Input enum value.</param>
        /// <returns>
        /// Value of type T associated to the enum value by an Attribute implementing
        /// ICast<typeparamref name="T"/>. If no attribute is found, returns a default value.
        /// </returns>
        /// <example>
        ///     enum Foobar
        ///     {
        ///         Foo,
        ///         [EnumExt.String("Bahr")] Bar
        ///     }
        ///     Foobar foo = Foobar.Foo;
        ///     Foobar bar = Foobar.Bar;
        ///     string fooCastString = foo.Cast<string>(); // "Foo"
        ///     string barCastString = bar.Cast<string>(); // "Bahr"
        ///     string fooToString = foo.ToString();       // "Foo"
        ///     string barToString = bar.ToString();       // "Bar"
        /// </example>
        public static T Cast<T>(this Enum value)
        {
            if (FindCastAttrib<T>(value) is {} cast)
                return cast.Value;
            return Default<T>(value);
        }

        /// <summary>
        /// Compare enum value with instance/value of type T.
        /// </summary>
        /// <typeparam name="T">Cast/comparison type.</typeparam>
        /// <param name="valueT">Instance/value of type T to compare with.</param>
        /// <param name="valueEnum">Enum value to compare with.</param>
        /// <returns>true if cast of valueEnum is equal to valueT, false otherwise</returns>
        public static bool EqualTo<T>(this T valueT, Enum valueEnum)
        {
            return valueT.Equals(valueEnum.Cast<T>());
        }

        /// <summary>
        /// Convert type T to enum
        /// </summary>
        public static bool TryCast<T, TEnum>(this T valueT, out TEnum value) where TEnum : struct
        {
            value = default(TEnum);
            IEnumerable<Enum> enumValues = Enum.GetValues(typeof(TEnum)).OfType<Enum>()
                .Where(valueEnum => valueEnum.Cast<T>().Equals(valueT))
                .ToList();
            if (enumValues.Any())
                value = (TEnum)Enum.ToObject(typeof(TEnum), enumValues.FirstOrDefault());
            return enumValues.Any();
        }

        /// <summary>
        /// Convert type T to enum
        /// </summary>
        public static TEnum Cast<T, TEnum>(this T valueT, TEnum defaultValue) where TEnum : struct
        {
            return TryCast(valueT, out TEnum value) ? value : defaultValue;
        }

        /// <summary>
        /// Get list of values of enum type
        /// </summary>
        public static IEnumerable<TEnum> GetValues<TEnum>() where TEnum : struct
        {
            Debug.Assert(typeof(TEnum).IsEnum);
            return Enum.GetValues(typeof(TEnum)).OfType<TEnum>();
        }

        /// <summary>
        /// Get list of values of enum type converted to type T
        /// </summary>
        public static IEnumerable<T> GetValues<T>(Type enumType)
        {
            return Enum.GetValues(enumType).OfType<Enum>()
                .Select((Enum value) => value.Cast<T>());
        }

        /// <summary>
        /// Default cast of enum value to type T.
        /// </summary>
        /// <typeparam name="T">Cast output type.</typeparam>
        /// <param name="value">Input enum value.</param>
        /// <returns>
        /// Default value of type T associated with the enum value:
        ///   * if T is string: returns the enum value name as string;
        ///   * if T is an integer type: returns the underlying enum integer value;
        ///   * otherwise: default value for type T (e.g. null for reference types).
        /// </returns>
        static T Default<T>(Enum value)
        {
            Type enumType = value.GetType();
            Type baseType = Enum.GetUnderlyingType(enumType);
            Type outputType = typeof(T);
            if (outputType.IsAssignableFrom(enumType) || outputType.IsAssignableFrom(baseType))
                return (T)(object)value;
            else if (outputType == typeof(string))
                return (T)(object)Enum.GetName(value.GetType(), value);
            else
                return default(T);
        }

        /// <summary>
        /// Find cast attribute.
        /// </summary>
        /// <typeparam name="T">Cast output type.</typeparam>
        /// <param name="value">Input enum value.</param>
        /// <returns>
        /// First cast attribute of type T found associated with the enum value, or null in case a
        /// suitable attribute is not found.
        /// </returns>
        static ICast<T> FindCastAttrib<T>(Enum value)
        {
            Type enumType = value.GetType();

            string valueName = Enum.GetName(enumType, value);
            if (string.IsNullOrEmpty(valueName))
                return null;

            FieldInfo enumField = enumType.GetField(valueName);
            if (enumField == null)
                return null;

            return CastAttribTypes
                .Where(type => typeof(ICast<T>).IsAssignableFrom(type))
                .Select(type => Attribute.GetCustomAttribute(enumField, type) as ICast<T>)
                .FirstOrDefault();
        }

        /// <summary>
        /// List of cast attribute types.
        /// </summary>
        /// <remarks>
        /// Future cast attribute types need to be added to this list.
        /// </remarks>
        static IEnumerable<Type> CastAttribTypes => StaticLazy.Get(() =>
            CastAttribTypes, () => new[]
            {
                typeof(StringAttribute)
            });

    }
}
