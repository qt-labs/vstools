/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    sealed class EvaluateRequest : Request<EvaluateResponse, EvaluateRequest.ArgumentsStruct>
    {
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "evaluate",
        //    "arguments" : { "expression"    : <expression to evaluate>,
        //                    "frame"         : <number>,
        //                    "global"        : <boolean>,
        //                    "disable_break" : <boolean>,
        //                    "context"       : <object id>
        //                  }
        //  }
        public const string REQ_COMMAND = "evaluate";
        public EvaluateRequest()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "expression")]
            public string Expression { get; set; }

            [DataMember(Name = "frame")]
            public int Frame { get; set; }

            [DataMember(Name = "global", EmitDefaultValue = false)]
            public bool? Global { get; set; }

            [DataMember(Name = "disable_break", EmitDefaultValue = false)]
            public bool? DisableBreak { get; set; }
        }
    }

    [DataContract]
    sealed class EvaluateResponse : Response
    {
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "evaluate",
        //    "body"        : ...
        //    "running"     : <is the VM running after sending this response>
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = EvaluateRequest.REQ_COMMAND;
        public EvaluateResponse()
        {
            Command = REQ_COMMAND;
        }

        [DataMember(Name = "body")]
        public DeferredObject<JsValue> Result { get; set; }
    }
}
