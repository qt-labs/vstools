/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    sealed class ContinueRequest : Request<ContinueResponse, ContinueRequest.ArgumentsStruct>
    {
        //  "v8request"
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "continue",
        //    "arguments" : { "stepaction" : <"in", "next" or "out">,
        //                    "stepcount"  : <number of steps (default 1)>
        //                  }
        //  }
        public const string REQ_COMMAND = "continue";
        public ContinueRequest()
        {
            Command = REQ_COMMAND;
        }

        public enum StepAction
        {
            [EnumString(default)] Continue = 0,
            [EnumString("in")] StepIn,
            [EnumString("next")] Next,
            [EnumString("out")] StepOut
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "stepaction", EmitDefaultValue = false)]
            string StepActionString { get; set; }

            public StepAction StepAction
            {
                get => SerializableEnum.Deserialize<StepAction>(StepActionString);
                set => StepActionString = SerializableEnum.Serialize(value);
            }

            [DataMember(Name = "stepcount", EmitDefaultValue = false)]
            public int? StepCount { get; set; }
        }
    }

    [DataContract]
    sealed class ContinueResponse : Response
    {
        //  "v8message"
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "continue",
        //    "running"     : <is the VM running after sending this response>,
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = ContinueRequest.REQ_COMMAND;
        public ContinueResponse()
        {
            Command = REQ_COMMAND;
        }
    }
}
