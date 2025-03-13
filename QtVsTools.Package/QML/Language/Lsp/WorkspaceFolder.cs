// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QtVsTools.Package.QML.Language.Lsp
{
    [DataContract]
    public sealed class WorkspaceFolder
    {
        /// <summary>
        /// Gets or sets the URI of the workspace folder.
        /// </summary>
        [DataMember(Name = "uri")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the name of the workspace folder.
        /// </summary>
        [DataMember(Name = "name")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }
}
