/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    [DataContract]
    sealed class BacktraceRequest : Request<BacktraceResponse, BacktraceRequest.ArgumentsStruct>
    {
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "backtrace",
        //    "arguments" : { "fromFrame" : <number>
        //                    "toFrame" : <number>
        //                    "bottom" : <boolean, set to true if the bottom of the
        //                        stack is requested>
        //                  }
        //  }
        public const string REQ_COMMAND = "backtrace";
        public BacktraceRequest() : base()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "fromFrame")]
            public int FromFrame { get; set; }

            [DataMember(Name = "toFrame")]
            public int ToFrame { get; set; }

            [DataMember(Name = "bottom")]
            public bool Bottom { get; set; }
        }
    }

    [DataContract]
    sealed class BacktraceResponse : Response<BacktraceResponse.BodyStruct>
    {
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "backtrace",
        //    "body"        : { "fromFrame" : <number>
        //                      "toFrame" : <number>
        //                      "totalFrames" : <number>
        //                      "frames" : <array of frames - see frame request for details>
        //                    }
        //    "running"     : <is the VM running after sending this response>
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = BacktraceRequest.REQ_COMMAND;
        public BacktraceResponse() : base()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class BodyStruct
        {
            [DataMember(Name = "fromFrame")]
            public int FromFrame { get; set; }

            [DataMember(Name = "toFrame")]
            public int ToFrame { get; set; }

            [DataMember(Name = "totalFrames")]
            public int TotalFrames { get; set; }

            [DataMember(Name = "frames")]
            public List<Frame> Frames { get; set; }
        }
    }
}
