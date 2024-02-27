/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    sealed class LookupRequest : Request<LookupResponse, LookupRequest.ArgumentsStruct>
    {
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "lookup",
        //    "arguments" : { "handles"       : <array of handles>,
        //                    "includeSource" : <boolean indicating whether
        //                                       the source will be included when
        //                                       script objects are returned>,
        //                  }
        //  }
        public const string REQ_COMMAND = "lookup";
        public LookupRequest()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "handles")]
            public List<int> Handles { get; set; }

            [DataMember(Name = "includeSource", EmitDefaultValue = false)]
            public bool? IncludeSource { get; set; }
        }
    }

    [DataContract]
    sealed class LookupResponse : Response
    {
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "lookup",
        //    "body"        : <array of serialized objects indexed using their handle>
        //    "running"     : <is the VM running after sending this response>
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = LookupRequest.REQ_COMMAND;
        public LookupResponse()
        {
            Command = REQ_COMMAND;
        }

        [DataMember(Name = "body")]
        public Dictionary<string, DeferredObject<JsValue>> Objects { get; set; }
    }
}
