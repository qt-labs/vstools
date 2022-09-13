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
        public SetBreakpointRequest() : base()
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
        public SetBreakpointResponse() : base()
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
