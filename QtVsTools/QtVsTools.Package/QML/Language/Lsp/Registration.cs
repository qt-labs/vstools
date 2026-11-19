// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QtVsTools.Package.QML.Language.Lsp
{
    [DataContract]
    internal sealed class Registration
    {
        [DataMember(Name = "id")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [DataMember(Name = "method")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Method { get; set; }

        [DataMember(Name = "registerOptions")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object RegisterOptions { get; set; }
    }

    [DataContract]
    internal sealed class RegistrationParams
    {
        /// <summary>
        /// </summary>
        [DataMember(Name = "registrations")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Registration[] Registrations { get; set; }
    }
}
