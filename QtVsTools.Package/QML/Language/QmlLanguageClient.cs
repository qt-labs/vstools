// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using Task = System.Threading.Tasks.Task;

namespace QtVsTools
{
    using Package.QML.Language;

    public static partial class Instances
    {
        public static QmlLanguageClient QmlLanguageClient => QmlLanguageClient.Instance;
    }
}

namespace QtVsTools.Package.QML.Language
{
    using Qml;

    [Export(typeof(ILanguageClient))]
    [ContentType(QmlContentType.Name)]
    public partial class QmlLanguageClient : ILanguageClient, IDisposable
    {
        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;
        public string Name => "QML LSP Client";

        public static QmlLanguageClient Instance { get; private set; }

        public QmlLanguageClient()
        {
            Instance = this;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public async Task OnLoadedAsync()
        {
            await LoadServerAsync();
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            return await ActivateServerAsync(token);
        }

#if VS2019
        public async Task OnServerInitializeFailedAsync(Exception e)
        {
            await Task.Yield();
        }
#else
        public async Task<InitializationFailureContext> OnServerInitializeFailedAsync(
            ILanguageClientInitializationInfo initializationState)
        {
            await Task.Yield();
            return new InitializationFailureContext
            {
                FailureMessage = initializationState.StatusMessage
            };
        }
#endif

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => false;

        public async Task OnServerInitializedAsync() => await Task.Yield();
    }
}
