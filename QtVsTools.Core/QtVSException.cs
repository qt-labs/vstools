/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;

namespace QtVsTools.Core
{
    [Serializable]
    public class QtVSException : ApplicationException
    {
        public QtVSException(string message)
            : base(message)
        {
        }

        public QtVSException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
