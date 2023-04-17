/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    [DataContract]
    sealed class DisconnectRequest : Request<DisconnectResponse>
    {
        //  "v8request"
        //  { "seq"     : <number>,
        //    "type"    : "request",
        //    "command" : "disconnect"
        //  }
        public const string REQ_COMMAND = "disconnect";
        public DisconnectRequest()
        {
            Command = REQ_COMMAND;
        }
    }

    [DataContract]
    sealed class DisconnectResponse : Response
    {
        //  "v8message"
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : "disconnect",
        //    "running"     : <is the VM running after sending this response>,
        //    "success"     : true
        //  }
        public const string REQ_COMMAND = DisconnectRequest.REQ_COMMAND;
        public DisconnectResponse()
        {
            Command = REQ_COMMAND;
        }
    }
}
