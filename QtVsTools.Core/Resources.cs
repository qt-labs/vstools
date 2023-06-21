/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace QtVsTools.Core
{
    /// <summary>
    /// Summary description for Resources.
    /// </summary>
    public static class Resources
    {
        // Project properties labels
        public const string projLabelQtSettings = "QtSettings";

        public const string registryRootPath = "Digia";

#if (VS2019 || VS2022)
        public const string registryPackagePath = registryRootPath + "\\Qt5VS2017";
#else
#error Unknown Visual Studio version!
#endif
        public const string registryVersionPath = registryRootPath + "\\Versions";
    }
}
