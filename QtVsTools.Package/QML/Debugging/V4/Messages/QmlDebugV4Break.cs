/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    [DataContract]
    sealed class BreakEvent : Event<BreakEvent.BodyStruct>
    {
        //  "v8message"
        //  { "seq"   : <number>,
        //    "type"  : "event",
        //    "event" : "break",
        //    "body"  : { "invocationText" : <string>,
        //               "sourceLine"      : <int>,
        //               "sourceLineText"  : <string>,
        //               "script"          : { "name" : <string>
        //               },
        //               "breakpoints"     : [ <int>,
        //                                    ...
        //               ]
        //             }
        //  }
        public const string EV_TYPE = "break";
        public BreakEvent() : base()
        {
            EventType = EV_TYPE;
        }

        [DataContract]
        public class BodyStruct
        {
            [DataMember(Name = "invocationText")]
            public string InvocationText { get; set; }

            [DataMember(Name = "sourceLine")]
            public int SourceLine { get; set; }

            [DataMember(Name = "sourceLineText")]
            public string SourceLineText { get; set; }

            [DataMember(Name = "script")]
            public ScriptStruct Script { get; set; }

            [DataMember(Name = "breakpoints")]
            public List<int> Breakpoints { get; set; }

            [DataContract]
            public class ScriptStruct
            {
                [DataMember(Name = "name")]
                public string Name { get; set; }
            }
        }
    }
}
