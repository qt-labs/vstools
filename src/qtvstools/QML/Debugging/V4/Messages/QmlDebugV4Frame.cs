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
        public FrameRequest() : base()
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
        public FrameResponse() : base()
        {
            Command = REQ_COMMAND;
        }

        [DataMember(Name = "body")]
        public Frame Frame { get; set; }
    }
}
