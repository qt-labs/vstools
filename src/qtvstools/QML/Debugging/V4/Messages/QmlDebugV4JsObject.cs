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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    class JsRef<TJsObject> : JsValue
        where TJsObject : JsRef<TJsObject>
    {
        protected JsRef()
        {
            Type = DataType.Object;
            Ref = null;
        }

        [DataMember(Name = "ref")]
        public int? Ref { get; set; }

        [DataMember(Name = "value")]
        public int PropertyCount { get; set; }

        protected override bool? IsCompatible(JsValue obj)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(obj) == false)
                return false;

            var _that = obj as JsRef<TJsObject>;
            if (_that == null)
                return null;

            return true;
        }
    }

    [DataContract]
    class JsObjectRef : JsRef<JsObjectRef>
    {
        public JsObjectRef()
        { }

        protected override bool? IsCompatible(JsValue obj)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(obj) == false)
                return false;

            var _that = obj as JsObjectRef;
            if (_that == null)
                return null;

            return ((JsRef<JsObjectRef>)_that).Ref.HasValue;
        }

        public new int Ref
        {
            get { return base.Ref.HasValue ? base.Ref.Value : 0; }
            set { base.Ref = value; }
        }
    }

    [DataContract]
    class JsObject : JsRef<JsObject>
    {
        //  { "handle"              : <handle>,
        //    "type"                : "object",
        //    "className"           : <Class name, ECMA-262 property [[Class]]>,
        //    "constructorFunction" : {"ref":<handle>},
        //    "protoObject"         : {"ref":<handle>},
        //    "prototypeObject"     : {"ref":<handle>},
        //    "properties" : [ {"name" : <name>,
        //                      "ref"  : <handle>
        //                     },
        //                     ...
        //                   ]
        //  }
        public JsObject()
        { }

        protected override bool? IsCompatible(JsValue obj)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(obj) == false)
                return false;

            var _that = obj as JsObject;
            if (_that == null)
                return null;

            return !_that.Ref.HasValue;
        }

        [DataMember(Name = "className")]
        public string ClassName { get; set; }

        [DataMember(Name = "constructorFunction")]
        public DeferredObject<JsValue> Constructor { get; set; }

        [DataMember(Name = "protoObject")]
        public DeferredObject<JsValue> ProtoObject { get; set; }

        [DataMember(Name = "prototypeObject")]
        public DeferredObject<JsValue> PrototypeObject { get; set; }

        [DataMember(Name = "properties")]
        public List<DeferredObject<JsValue>> Properties { get; set; }

        public IDictionary<string, JsValue> PropertiesByName
        {
            get
            {
                if (Properties == null)
                    return null;

                return Properties
                    .Where(x => x.Object != null
                        && !string.IsNullOrEmpty(x.Object.Name))
                    .Select(x => x.Object)
                    .GroupBy(x => x.Name)
                    .ToDictionary(x => x.Key, x => x.First());
            }
        }

        public bool IsArray
        {
            get
            {
                return !Properties.Where((x, i) => x.HasData
                    && ((JsValue)x).Name != i.ToString()).Any();
            }
        }
    }

    [DataContract]
    class FunctionStruct : JsObject
    {
        //  { "handle" : <handle>,
        //    "type"                : "function",
        //    "className"           : "Function",
        //    "constructorFunction" : {"ref":<handle>},
        //    "protoObject"         : {"ref":<handle>},
        //    "prototypeObject"     : {"ref":<handle>},
        //    "name"                : <function name>,
        //    "inferredName"        : <inferred function name for anonymous functions>
        //    "source"              : <function source>,
        //    "script"              : <reference to function script>,
        //    "scriptId"            : <id of function script>,
        //    "position"            : <function begin position in script>,
        //    "line"                : <function begin source line in script>,
        //    "column"              : <function begin source column in script>,
        //    "properties" : [ {"name" : <name>,
        //                      "ref"  : <handle>
        //                     },
        //                     ...
        //                   ]
        //  }
        public FunctionStruct()
        {
            Type = DataType.Function;
        }

        [DataMember(Name = "inferredName")]
        public string InferredName { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }

        [DataMember(Name = "script")]
        public string Script { get; set; }

        [DataMember(Name = "scriptId")]
        public string ScriptId { get; set; }

        [DataMember(Name = "position")]
        public string Position { get; set; }

        [DataMember(Name = "line")]
        public int Line { get; set; }

        [DataMember(Name = "column")]
        public int Column { get; set; }
    }
}
