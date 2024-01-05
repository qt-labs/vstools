/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    sealed class Scope
    {
        //  { "index"      : <index of this scope in the scope chain. Index 0 is the top scope
        //                    and the global scope will always have the highest index for a
        //                    frame>,
        //    "frameIndex" : <index of the frame>,
        //    "type"       : <type of the scope:
        //                     0: Global
        //                     1: Local
        //                     2: With
        //                     3: Closure
        //                     4: Catch >,
        //    "object"     : <the scope object defining the content of the scope.
        //                    For local and closure scopes this is transient objects,
        //                    which has a negative handle value>
        //  }
        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "frameIndex")]
        public int FrameIndex { get; set; }

        [DataContract]
        public enum ScopeType
        {
            Global = 0,
            Local = 1,
            With = 2,
            Closure = 3,
            Catch = 4
        }

        [DataMember(Name = "type")]
        public ScopeType Type { get; set; }

        [DataMember(Name = "object")]
        public DeferredObject<JsValue> Object { get; set; }
    }

    [DataContract]
    sealed class ScopeRequest : Request<ScopeResponse, ScopeRequest.ArgumentsStruct>
    {
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "scope",
        //    "arguments" : { "number" : <scope number>
        //                    "frameNumber" : <frame number, optional uses selected
        //                                    frame if missing>
        //                  }
        //  }
        public const string REQ_COMMAND = "scope";
        public ScopeRequest()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "number")]
            public int ScopeNumber { get; set; }

            [DataMember(Name = "frameNumber", EmitDefaultValue = false)]
            public int? FrameNumber { get; set; }
        }
    }

    [DataContract]
    sealed class ScopeResponse : Response
    {
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "scope",
        //    "body"        : { "index"      : <index of this scope in the scope chain. Index 0 is
        //                                      the top scope
        //                                      and the global scope will always have the highest
        //                                      index for a
        //                                      frame>,
        //                      "frameIndex" : <index of the frame>,
        //                      "type"       : <type of the scope:
        //                                       0: Global
        //                                       1: Local
        //                                       2: With
        //                                       3: Closure
        //                                       4: Catch >,
        //                      "object"     : <the scope object defining the content of the scope.
        //                                      For local and closure scopes this is transient
        //                                      objects,
        //                                      which has a negative handle value>
        //                    }
        //    "running"     : <is the VM running after sending this response>
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = ScopeRequest.REQ_COMMAND;
        public ScopeResponse()
        {
            Command = REQ_COMMAND;
        }

        [DataMember(Name = "body")]
        public Scope Scope { get; set; }
    }
}
