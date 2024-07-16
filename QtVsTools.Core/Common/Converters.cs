/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace QtVsTools.Core.Common
{
    public static class Converters
    {
        public class EnableDisableConverter : BooleanConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture,
                object value)
            {
                return string.Equals(value as string, "Enable", Utils.IgnoreCase);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture,
                object value, Type destinationType)
            {
                if (value is bool b && destinationType == typeof(string))
                    return b ? "Enable" : "Disable";
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public class CollectionConverter<T> : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext _) => true;
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext _) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext _)
                => new(GetCollection);

            protected virtual T[] GetCollection => Array.Empty<T>();
        }

        public abstract class QtVersionFilter : CollectionConverter<object>
        {
            protected abstract bool IsCompatible(string qtVersion);
        }

        public abstract class QtVersionConverter : QtVersionFilter
        {
            protected override object[] GetCollection =>
                QtVersionManager.GetVersions()
                    .Where(IsCompatible)
                    .Prepend("$(DefaultQtVersion)")
                    .Cast<object>()
                    .ToArray();
        }
    }
}
