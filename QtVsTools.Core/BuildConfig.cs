/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace QtVsTools.Core
{
    public struct BuildConfig
    {
        public static string PlatformToolset =>
            // TODO: Find a proper way to return the PlatformToolset version.
#if VS2019
            "142";
#elif VS2022
            "143";
#else
#error Unknown Visual Studio version!
#endif

        // Since Visual Studio 2019: WindowsTargetPlatformVersion=10.0
        // will be treated as "use latest installed Windows 10 SDK".
        // https://developercommunity.visualstudio.com/comments/407752/view.html
        public static string WindowsTargetPlatformVersion = "10.0";
    }
}
