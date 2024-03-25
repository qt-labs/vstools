// ************************************************************************************************
// Copyright (C) 2024 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
// ************************************************************************************************

using System;

namespace QtVsTools.Core.Common
{
    using QtVsTools.Common;

    internal sealed class SettingsAttribute : Attribute
    {
        public SettingsAttribute(object key, object defaultValue)
        {
            DefaultValue = defaultValue;
            if (key.GetType().BaseType != typeof(Enum))
                throw new ArgumentException("The provided argument must be an Enum type.");
            Key = ((Enum) key).Cast<string>();
        }

        public string Key { get; }
        public object DefaultValue { get; }
    }
}
