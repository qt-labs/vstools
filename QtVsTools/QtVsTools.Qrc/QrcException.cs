// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;

namespace QtVsTools.Qrc
{
    /// <summary>
    /// Thrown when a .qrc file cannot be read or written.
    /// </summary>
    public class QrcException : Exception
    {
        public QrcException(string message)
            : base(message)
        { }

        public QrcException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
