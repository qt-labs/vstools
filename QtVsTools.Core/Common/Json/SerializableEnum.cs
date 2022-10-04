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
                return default(TEnum);

            var members = typeof(TEnum).GetMembers();
            if (members.Length == 0)
                return default(TEnum);

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
                return default(TEnum);

            var objValue = field.GetValue(null);
            if (!(objValue is TEnum))
                return default(TEnum);

            return (TEnum)objValue;
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
