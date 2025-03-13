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

namespace QtVsTools.Package.QML.Language
{
    using Core;
    using Qml;

    [Export(typeof(ILanguageClient))]
    [ContentType(QmlContentType.Name)]
    public partial class QmlLanguageClient : ILanguageClient
    {
        public event AsyncEventHandler<EventArgs> StartAsync;
#pragma warning disable CS0067
        public event AsyncEventHandler<EventArgs> StopAsync;
#pragma warning restore CS0067
        public string Name => "QML LSP Client";

        public async Task OnLoadedAsync()
        {
            await LoadServerAsync();
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            return await ActivateServerAsync(token);
        }

        public Task OnServerInitializeFailedAsync(Exception exception)
        {
            exception.Log();
            return Task.CompletedTask;
        }

        public Task<InitializationFailureContext>
            OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            var statusMessage = initializationState.StatusMessage?.Trim(' ', '\r', '\n');

            string failureMessage = null;
            if (!string.IsNullOrWhiteSpace(statusMessage))
                failureMessage = statusMessage;

            if (initializationState.InitializationException != null) {
                var exceptionMessage = initializationState.InitializationException.Message
                    .Trim(' ', '\r', '\n');

                if (!string.IsNullOrWhiteSpace(failureMessage))
                    failureMessage += "\r\n" + exceptionMessage;
                else
                    failureMessage = exceptionMessage;
            }

            return Task.FromResult(new InitializationFailureContext
            {
                FailureMessage = failureMessage
            });
        }

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => true;

        public async Task OnServerInitializedAsync() => await Task.Yield();
    }
}
