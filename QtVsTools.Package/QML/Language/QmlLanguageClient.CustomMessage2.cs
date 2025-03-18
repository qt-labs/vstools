// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using StreamJsonRpc;

namespace QtVsTools.Package.QML.Language
{
    using Lsp;

    public partial class QmlLanguageClient : ILanguageClientCustomMessage2
    {
        internal JsonRpc JsonRpc { get; set; }

        public Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            JsonRpc = rpc;
            CustomMessageTarget = new CustomMessageTarget(this);

            return Task.CompletedTask;
        }

        public object MiddleLayer => null;

        public object CustomMessageTarget { get; private set; }
    }
}
