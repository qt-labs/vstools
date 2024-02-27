/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    [DataContract]
    sealed class VersionRequest : Request<VersionResponse>
    {
        //  "v8request"
        //  { "seq"     : <number>,
        //    "type"    : "request",
        //    "command" : "version"
        //  }
        public const string REQ_COMMAND = "version";
        public VersionRequest()
        {
            Command = REQ_COMMAND;
        }
    }

    [DataContract]
    sealed class VersionResponse : Response<VersionResponse.BodyStruct>
    {
        //  "v8message"
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "version",
        //    "body"        : { "UnpausedEvaluate" : <bool>,
        //                      "ContextEvaluate"  : <bool>,
        //                      "V8Version"        : <string>
        //                    },
        //    "running"     : <is the VM running after sending this response>,
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = VersionRequest.REQ_COMMAND;
        public VersionResponse()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class BodyStruct
        {
            [DataMember(Name = "UnpausedEvaluate")]
            public bool UnpausedEvaluate { get; set; }

            [DataMember(Name = "ContextEvaluate")]
            public bool ContextEvaluate { get; set; }

            [DataMember(Name = "V8Version")]
            public string Version { get; set; }
        }
    }
}
