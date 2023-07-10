/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;

namespace QtVsTools.Core
{
    public class QtModule
    {
        public string Name;
        public bool Selectable;
        public List<string> Defines = new();
        public string LibraryPrefix = string.Empty;
        public List<string> AdditionalLibraries = new();
        public List<string> AdditionalLibrariesDebug = new();
        public List<string> IncludePath = new();
        public string proVarQT;
        private string majorVersion;

        public string LibRelease =>
            LibraryPrefix.StartsWith("Qt", StringComparison.Ordinal)
                ? "Qt" + majorVersion + LibraryPrefix.Substring(2) + ".lib"
                : LibraryPrefix + ".lib";

        public string LibDebug =>
            LibraryPrefix.StartsWith("Qt", StringComparison.Ordinal)
                ? "Qt" + majorVersion + LibraryPrefix.Substring(2) + "d.lib"
                : LibraryPrefix + "d.lib";

        public QtModule(string major)
        {
            majorVersion = major;
        }
    }
}
