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
