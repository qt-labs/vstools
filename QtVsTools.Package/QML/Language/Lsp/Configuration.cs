// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QtVsTools.Package.QML.Language.Lsp
{
    [DataContract]
    internal sealed class ConfigurationItem
    {
        [DataMember(Name = "scopeUri")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ScopeUri { get; set; }

        [DataMember(Name = "section")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Section { get; set; }
    }

    [DataContract]
    internal sealed class ConfigurationParams
    {
        [DataMember(Name = "items")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationItem[] Items { get; set; }
    }

    [DataContract]
    internal sealed class DidChangeConfigurationParams
    {
        [DataMember(Name = "settings")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Settings { get; set; }
    }
}
