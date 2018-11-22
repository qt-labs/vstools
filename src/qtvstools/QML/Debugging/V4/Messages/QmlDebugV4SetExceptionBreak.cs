/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    public enum ExceptionBreakType
    {
        [EnumString("all")] All = 0,
        [EnumString("uncaught")] Uncaught,
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
        public SetExceptionBreakRequest() : base()
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
                get { return SerializableEnum.Deserialize<ExceptionBreakType>(TypeString); }
                set { TypeString = SerializableEnum.Serialize<ExceptionBreakType>(value); }
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
        public SetExceptionBreakResponse() : base()
        {
            Command = REQ_COMMAND;
        }
    }
}
