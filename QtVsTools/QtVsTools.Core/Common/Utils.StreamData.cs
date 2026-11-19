// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

namespace QtVsTools.Core.Common
{
    public sealed class StreamData
    {
        public byte[] Bytes { get; set; }
        public int Offset { get; set; }
        public int Count { get; set; }
    }
}
