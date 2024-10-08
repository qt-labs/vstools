/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#nullable enable

namespace QtVsTools.Package
{
    /// <summary>Defines constants from the <c>source.extension.vsixmanifest</c> file.</summary>
    internal sealed partial class Vsix
    {
        /// <summary>The author of the extension.</summary>
        public const string Author = "The Qt Company Ltd.";

        /// <summary>The description of the extension.</summary>
        public const string Description = "This official Qt Group extension, Qt Visual Studio Tools, integrates Qt development tools into Microsoft Visual Studio 2019, enabling developers to use their familiar Windows environment without worrying about Qt-related build steps or tools.";

        /// <summary>The extension identifier.</summary>
        public const string Id = "QtVsTools.bf3c71c0-ab41-4427-ada9-9b3813d89ff5";

        /// <summary>The default language for the extension.</summary>
        public const string Language = "en-US";

        /// <summary>The name of the extension.</summary>
        public const string Name = "Qt Visual Studio Tools";

        /// <summary>The version of the extension.</summary>
        public const string Version = QtVsTools.Core.Version.PRODUCT_VERSION;
    }
}
