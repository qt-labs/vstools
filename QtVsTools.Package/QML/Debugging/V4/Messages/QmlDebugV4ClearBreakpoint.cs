/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    [DataContract]
    sealed class ClearBreakpointRequest
        : Request<ClearBreakpointResponse, ClearBreakpointRequest.ArgumentsStruct>
    {
        //  { "seq"       : <number>,
        //    "type"      : "request",
        //    "command"   : "clearbreakpoint",
        //    "arguments" : { "breakpoint" : <number of the break point to clear>
        //                  }
        //  }
        public const string REQ_COMMAND = "clearbreakpoint";
        public ClearBreakpointRequest()
        {
            Command = REQ_COMMAND;
        }

        [DataContract]
        public class ArgumentsStruct
        {
            [DataMember(Name = "breakpoint")]
            public int Breakpoint { get; set; }
        }
    }

    [DataContract]
    sealed class ClearBreakpointResponse : Response
    {
        public const string REQ_COMMAND = ClearBreakpointRequest.REQ_COMMAND;
        public ClearBreakpointResponse()
        {
            Command = REQ_COMMAND;
        }
    }
}
