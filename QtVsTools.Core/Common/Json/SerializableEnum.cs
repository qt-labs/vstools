/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Linq;
using System.Reflection;

namespace QtVsTools.Json
{
    /// <summary>
    /// Provide serialization/deserialization of enum values marked with the [EnumString] attribute
    /// </summary>
    public static class SerializableEnum
    {
        public static string Serialize<TEnum>(TEnum enumValue)
            where TEnum : struct
        {
            var type = enumValue.GetType();
            if (!type.IsEnum)
                return enumValue.ToString();

            var member = type.GetMember(enumValue.ToString());
            if (member.Length == 0)
                return enumValue.ToString();

            var attribs = member[0].GetCustomAttributes(typeof(EnumStringAttribute), false);
            if (attribs.Length == 0)
                return enumValue.ToString();

            if (attribs.FirstOrDefault(x => x is EnumStringAttribute) is EnumStringAttribute a)
                return a.ValueString;

            return enumValue.ToString();
        }

        public static TEnum Deserialize<TEnum>(string stringValue)
            where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
                return default;

            var members = typeof(TEnum).GetMembers();
            if (members.Length == 0)
                return default;

            var member = members.Where(x =>
            {
                var attribs = x.GetCustomAttributes(typeof(EnumStringAttribute), false);
                if (attribs.Length == 0)
                    return false;

                if (attribs.FirstOrDefault(y => y is EnumStringAttribute) is EnumStringAttribute a)
                    return a.ValueString == stringValue;

                return false;
            }).FirstOrDefault();

            var field = member as FieldInfo;
            if (field == null)
                return default;

            var objValue = field.GetValue(null);
            return objValue is not TEnum value ? default : value;
        }

    }

    public class EnumStringAttribute : Attribute
    {
        public string ValueString { get; }

        public EnumStringAttribute(string enumValueString)
        {
            ValueString = enumValueString;
        }
    }
}
