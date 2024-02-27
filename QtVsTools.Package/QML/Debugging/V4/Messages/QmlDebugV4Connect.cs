/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    [DataContract]
    sealed class ConnectMessage : Message
    {
        //  "connect"
        //  { "redundantRefs"  : <bool>,
        //    "namesAsObjects" : <bool>
        //  }
        public const string MSG_TYPE = "connect";
        public ConnectMessage()
        {
            Type = MSG_TYPE;
        }

        [DataMember(Name = "redundantRefs")]
        public bool RedundantRefs { get; set; }

        [DataMember(Name = "namesAsObjects")]
        public bool NamesAsObjects { get; set; }
    }
}
