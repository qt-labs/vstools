/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    sealed class SetBreakpointRequest
        : Request<SetBreakpointResponse, SetBreakpointRequest.ArgumentsStruct>
    {
        //  "v8request"
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "setbreakpoint",
        //    "arguments" : { "type"        : <"function" or "script" or "scriptId"
        //                                    or "scriptRegExp">,
        //                    "target"      : <function expression or script identification>,
        //                    "line"        : <line in script or function>,
        //                    "column"      : <character position within the line>,
        //                    "enabled"     : <initial enabled state. True or false, default is
        //                                    true>,
        //                    "condition"   : <string with break point condition>,
        //                    "ignoreCount" : <number specifying the number of break point hits to
        //                                    ignore, default value is 0>
        //                  }
        //  }
        public const string REQ_COMMAND = "setbreakpoint";
        public SetBreakpointRequest()
        {
            Command = REQ_COMMAND;
        }

        public enum TargetType
        {
            [EnumString("function")] Function = 0,
            [EnumString("script")] Script,
            [EnumString("scriptId")] ScriptId,
            [EnumString("scriptRegExp")] ScriptRegExp
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "type")]
            private string TargetTypeString { get; set; }

            public TargetType TargetType
            {
                get => SerializableEnum.Deserialize<TargetType>(TargetTypeString);
                set => TargetTypeString = SerializableEnum.Serialize(value);
            }

            [DataMember(Name = "target")]
            public string Target { get; set; }

            [DataMember(Name = "line")]
            public int Line { get; set; }

            [DataMember(Name = "column", EmitDefaultValue = false)]
            public int? Column { get; set; }

            [DataMember(Name = "condition", EmitDefaultValue = false)]
            public string Condition { get; set; }

            [DataMember(Name = "enabled")]
            public bool Enabled { get; set; }

            [DataMember(Name = "ignoreCount", EmitDefaultValue = false)]
            public int? IgnoreCount { get; set; }

            public ArgumentsStruct()
            {
                Enabled = true;
            }
        }
    }

    [DataContract]
    sealed class SetBreakpointResponse : Response<SetBreakpointResponse.BodyStruct>
    {
        //  "v8message"
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "setbreakpoint",
        //    "body"        : { "type"       : <"function" or "script">,
        //                      "breakpoint" : <break point number of the new break point>,
        //                    },
        //    "running"     : <is the VM running after sending this response>,
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = SetBreakpointRequest.REQ_COMMAND;
        public SetBreakpointResponse()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class BodyStruct
        {
            [DataMember(Name = "type")]
            public string Type { get; set; }

            [DataMember(Name = "breakpoint")]
            public int Breakpoint { get; set; }
        }
    }
}
