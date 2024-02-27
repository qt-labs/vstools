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
    sealed class Frame
    {
        //  { "index"          : <frame number>,
        //    "receiver"       : <frame receiver>,
        //    "func"           : <function invoked>,
        //    "script"         : <script for the function>,
        //    "constructCall"  : <boolean indicating whether the function was called as
        //                       constructor>,
        //    "debuggerFrame"  : <boolean indicating whether this is an internal debugger frame>,
        //    "arguments"      : [ { name: <name of the argument - missing of anonymous argument>,
        //                           value: <value of the argument>
        //                         },
        //                         ... <the array contains all the arguments>
        //                       ],
        //    "locals"         : [ { name: <name of the local variable>,
        //                           value: <value of the local variable>
        //                         },
        //                         ... <the array contains all the locals>
        //                       ],
        //    "position"       : <source position>,
        //    "line"           : <source line>,
        //    "column"         : <source column within the line>,
        //    "sourceLineText" : <text for current source line>,
        //    "scopes"         : [ <array of scopes, see scope request below for format> ],
        //  }
        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "receiver")]
        public DeferredObject<JsValue> Receiver { get; set; }

        [DataMember(Name = "func")]
        public string Function { get; set; }

        [DataMember(Name = "script")]
        public string Script { get; set; }

        [DataMember(Name = "constructCall")]
        public bool IsConstructCall { get; set; }

        [DataMember(Name = "debuggerFrame")]
        public bool IsDebuggerFrame { get; set; }

        [DataMember(Name = "arguments")]
        public List<VariableStruct> Arguments { get; set; }

        [DataMember(Name = "locals")]
        public List<VariableStruct> Locals { get; set; }

        [DataMember(Name = "position")]
        public string Position { get; set; }

        [DataMember(Name = "line")]
        public int Line { get; set; }

        [DataMember(Name = "column")]
        public int Column { get; set; }

        [DataMember(Name = "sourceLineText")]
        public string SourceLineText { get; set; }

        [DataMember(Name = "scopes")]
        public List<Scope> Scopes { get; set; }

        [DataContract]
        public sealed class VariableStruct
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "value")]
            public string Value { get; set; }
        }

    }

    [DataContract]
    sealed class FrameRequest : Request<FrameResponse, FrameRequest.ArgumentsStruct>
    {
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "frame",
        //    "arguments" : { "number" : <frame number> }
        //  }
        public const string REQ_COMMAND = "frame";
        public FrameRequest()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "number")]
            public int FrameNumber { get; set; }
        }
    }

    [DataContract]
    sealed class FrameResponse : Response
    {
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "frame",
        //    "body"        : { "index"          : <frame number>,
        //                      "receiver"       : <frame receiver>,
        //                      "func"           : <function invoked>,
        //                      "script"         : <script for the function>,
        //                      "constructCall"  : <boolean indicating whether the function was
        //                                          called as constructor>,
        //                      "debuggerFrame"  : <boolean indicating whether this is an internal
        //                                          debugger frame>,
        //                      "arguments"      : [ { name: <name of the argument - missing of
        //                                          anonymous argument>,
        //                                             value: <value of the argument>
        //                                           },
        //                                           ... <the array contains all the arguments>
        //                                         ],
        //                      "locals"         : [ { name: <name of the local variable>,
        //                                             value: <value of the local variable>
        //                                           },
        //                                           ... <the array contains all the locals>
        //                                         ],
        //                      "position"       : <source position>,
        //                      "line"           : <source line>,
        //                      "column"         : <source column within the line>,
        //                      "sourceLineText" : <text for current source line>,
        //                      "scopes"         : [ <array of scopes, see scope request below for
        //                                          format> ],
        //                    }
        //    "running"     : <is the VM running after sending this response>
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = FrameRequest.REQ_COMMAND;
        public FrameResponse()
        {
            Command = REQ_COMMAND;
        }

        [DataMember(Name = "body")]
        public Frame Frame { get; set; }
    }
}
