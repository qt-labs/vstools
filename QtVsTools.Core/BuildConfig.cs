// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Threading;

namespace QtVsTools.Core
{
    using VisualStudio;

    public struct BuildConfig
    {

        private static readonly Lazy<string> PlatformToolsetLazy =
            new(() =>
            {
#if VS2019 || VS2022 || VS2026
                var version = VsShell.GetReleaseString();

                if (version.StartsWith("16.", StringComparison.Ordinal))
                    return "142";
                if (version.StartsWith("17.", StringComparison.Ordinal))
                    return "143";
                if (version.StartsWith("18.", StringComparison.Ordinal))
                    return "145";

                throw new Exception($"Unsupported Visual Studio version: {version}");
#else
#error Unknown Visual Studio version!
#endif
            }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static string PlatformToolset => PlatformToolsetLazy.Value;

        // Since Visual Studio 2019: WindowsTargetPlatformVersion=10.0
        // will be treated as "use latest installed Windows 10 SDK".
        // https://developercommunity.visualstudio.com/comments/407752/view.html
        public static string WindowsTargetPlatformVersion = "10.0";
    }
}
