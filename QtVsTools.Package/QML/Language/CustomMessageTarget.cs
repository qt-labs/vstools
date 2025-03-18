// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

using STTask = System.Threading.Tasks;

namespace QtVsTools.Package.QML.Language
{
    using Lsp;

    internal sealed class CustomMessageTarget
    {
        private readonly QmlLanguageClient client;
        private const string DidChangeWorkspaceFolders = "workspace/didChangeWorkspaceFolders";

        internal CustomMessageTarget(QmlLanguageClient client) => this.client = client;

        // The client/registerCapability request is sent from the server to the client to
        // dynamically register for the workspace/didChangeWorkspaceFolders notifications.
        [JsonRpcMethod("client/registerCapability")]
        public async STTask.Task OnRegisterCapabilityAsync(JToken arg)
        {
            if (client.InitializeMessageSent)
                return;

            var @params = arg.ToObject<RegistrationParams>();
            if (@params?.Registrations == null || @params.Registrations.Length == 0)
                return;

            if (!Array.Exists(@params.Registrations, p => p.Method == DidChangeWorkspaceFolders))
                return;

            client.WorkspaceFolders = QmlLanguageClient.GetWorkspaceFolders();

            await client.NotifyDidChangeWorkspaceFoldersParamsAsync(client.WorkspaceFolders
                .ToArray(), Array.Empty<WorkspaceFolder>());
        }

        // The workspace/configuration request is sent from the server to the client to fetch
        // configuration settings from the client. The request can fetch several configuration
        // settings in one roundtrip. The order of the returned configuration settings correspond
        // to the order of the passed ConfigurationItems (e.g. the first item in the response is
        // the result for the first configuration item in the params).
        [JsonRpcMethod("workspace/configuration")]
        public object WorkspaceConfiguration(JToken arg)
        {
            try {
                var reqParams = arg.ToObject<ConfigurationParams>();
                if (reqParams?.Items == null || reqParams.Items.Length == 0)
                    return null;
                return null; // TODO: Return a workspace/configuration for each workspace.
            } catch {
                return null;
            }
        }

        // The workspace/workspaceFolders request is sent from the server to the client to fetch
        // the current open list of workspace folders. Returns null in the response if only a single
        // file is open in the tool. Returns an empty array if a workspace is open but no folders
        // are configured.
        [JsonRpcMethod("workspace/workspaceFolders")]
        public object WorkspaceWorkspaceFolders()
        {
            return client.WorkspaceFolders.Count > 0 ? client.WorkspaceFolders : null;
        }
    }
}
