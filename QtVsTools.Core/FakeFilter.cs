/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace QtVsTools.Core
{
    public class FakeFilter
    {
        public string Name { get; set; }
        public string Filter { get; set; }
        public string UniqueIdentifier { get; set; }

        public bool ParseFiles { get; set; } = true;
    }
}
