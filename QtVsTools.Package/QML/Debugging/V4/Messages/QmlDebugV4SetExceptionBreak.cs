/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    public enum ExceptionBreakType
    {
        [EnumString("all")] All = 0,
        [EnumString("uncaught")] Uncaught
    }

    [DataContract]
    sealed class SetExceptionBreakRequest
        : Request<SetExceptionBreakResponse, SetExceptionBreakRequest.ArgumentsStruct>
    {
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "setexceptionbreak",
        //    "arguments" : { "type"    : <string: "all", or "uncaught">,
        //                    "enabled" : <optional bool: enables the break type if true>
        //                  }
        //  }
        public const string REQ_COMMAND = "setexceptionbreak";
        public SetExceptionBreakRequest()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "type")]
            private string TypeString { get; set; }

            public ExceptionBreakType ExceptionBreakType
            {
                get => SerializableEnum.Deserialize<ExceptionBreakType>(TypeString);
                set => TypeString = SerializableEnum.Serialize(value);
            }

            [DataMember(Name = "enabled")]
            public bool Enabled { get; set; }
        }
    }

    [DataContract]
    sealed class SetExceptionBreakResponse : Response<SetExceptionBreakRequest.ArgumentsStruct>
    {
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "setexceptionbreak",
        //    "body"        : { "type"    : <string: "all" or "uncaught" corresponding to the
        //                                  request.>,
        //                      "enabled" : <bool: true if the break type is currently enabled
        //                                  as a result of the request>
        //                    }
        //    "running"     : true
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = SetExceptionBreakRequest.REQ_COMMAND;
        public SetExceptionBreakResponse()
        {
            Command = REQ_COMMAND;
        }
    }
}
