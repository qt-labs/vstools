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
        public ContinueRequest() : base()
        {
            Command = REQ_COMMAND;
        }

        public enum StepAction
        {
            [EnumString(default(string))] Continue = 0,
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
        public ContinueResponse() : base()
        {
            Command = REQ_COMMAND;
        }
    }
}
