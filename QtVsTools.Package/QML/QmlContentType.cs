/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace QtVsTools.Qml
{
    internal sealed class QmlContentType
    {
        public const string Name = "qml";

        [Export]
        [Name(Name)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition qmlContentType = null;

        [Export]
        [FileExtension(".qml")]
        [ContentType(Name)]
        internal static FileExtensionToContentTypeDefinition qmlFileType = null;

        [Export]
        [FileExtension(".qmlproject")]
        [ContentType(Name)]
        internal static FileExtensionToContentTypeDefinition qmlprojectFileType = null;
    }
}
