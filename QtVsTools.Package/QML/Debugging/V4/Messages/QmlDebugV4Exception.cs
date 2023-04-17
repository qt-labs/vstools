/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    sealed class ExceptionEvent : Event<ExceptionEvent.BodyStruct>
    {
        //  "v8message"
        //  { "seq"   : <number>,
        //    "type"  : "event",
        //    "event" : "break",
        //    "body"  : { "sourceLine" : <int>,
        //                "script"     : { "name" : <string>
        //                },
        //                "text"       : <string>,
        //                "exception"  : <object>
        //              }
        //  }
        public const string EV_TYPE = "exception";
        public ExceptionEvent()
        {
            EventType = EV_TYPE;
        }

        [DataContract]
        public class BodyStruct
        {
            [DataMember(Name = "sourceLine")]
            public int SourceLine { get; set; }

            [DataMember(Name = "script")]
            public ScriptStruct Script { get; set; }

            [DataMember(Name = "text")]
            public string Text { get; set; }

            [DataMember(Name = "exception")]
            public DeferredObject<JsValue> Exception { get; set; }

            [DataContract]
            public class ScriptStruct
            {
                [DataMember(Name = "name")]
                public string Name { get; set; }
            }
        }
    }
}
