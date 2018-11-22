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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    class JsValue : Serializable<JsValue>
    {
        //  { "handle" : <handle>,
        //    "type"   : <"undefined", "null", "boolean", "number", "string", "object", "function"
        //               or "frame">
        //  }
        protected JsValue()
        { }

        [DataMember(Name = "handle")]
        public int Handle { get; set; }

        public enum DataType
        {
            [EnumString("undefined")] Undefined = 0,
            [EnumString("null")] Null,
            [EnumString("boolean")] Boolean,
            [EnumString("number")] Number,
            [EnumString("string")] String,
            [EnumString("object")] Object,
            [EnumString("function")] Function,
            [EnumString("frame")] Frame
        }

        [DataMember(Name = "type")]
        protected string TypeString { get; set; }

        public DataType Type
        {
            get { return SerializableEnum.Deserialize<DataType>(TypeString); }
            set { TypeString = SerializableEnum.Serialize<DataType>(value); }
        }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Empty;
        }

        protected override bool? IsCompatible(JsValue that)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(that) == false)
                return false;

            if (that == null)
                return false;
            if (string.IsNullOrEmpty(TypeString))
                return null;
            return (this.TypeString == that.TypeString);
        }

        public static JsValue Create<T>(T value)
        {
            return (JsPrimitive<T>)value;
        }

        protected static readonly CodeDomProvider JScriptProvider
            = CodeDomProvider.CreateProvider("JScript");
    }

    [DataContract]
    [SkipDeserialization]
    class JsError : JsValue
    {
        public string Message { get; set; }

        public JsError()
        {
            Type = DataType.Undefined;
        }

        protected override bool? IsCompatible(JsValue that)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            return false;
        }

        public override string ToString()
        {
            return Message;
        }
    }

    [DataContract]
    [SkipDeserialization]
    class JsPrimitive : JsValue
    {
        public static string Format(object obj)
        {
            using (var writer = new StringWriter()) {
                JScriptProvider.GenerateCodeFromExpression(
                    new CodePrimitiveExpression(obj), writer, null);
                return writer.ToString();
            }
        }
    }

    [DataContract]
    class JsUndefined : JsPrimitive
    {
        //  {"handle":<handle>,"type":"undefined"}
        public JsUndefined()
        {
            Type = DataType.Undefined;
        }

        public override string ToString()
        {
            return "undefined";
        }
    }

    [DataContract]
    class JsNull : JsPrimitive
    {
        //  {"handle":<handle>,"type":"null"}
        public JsNull()
        {
            Type = DataType.Null;
        }

        public override string ToString()
        {
            return "null";
        }
    }

    [DataContract]
    class JsNumberSymbolic : JsPrimitive
    {
        //  {"handle":<handle>,"type":"null"}
        public JsNumberSymbolic()
        {
            Type = DataType.Number;
        }

        [DataMember(Name = "value")]
        public virtual string Value { get; set; }

        protected override bool? IsCompatible(JsValue obj)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(obj) == false)
                return false;

            var that = obj as JsNumberSymbolic;
            if (that == null)
                return null;

            var symbolicValues = new[] { "NaN", "Infinity", "+Infinity", "-Infinity" };
            return symbolicValues.Contains(that.Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    [DataContract]
    [SkipDeserialization]
    class JsPrimitive<T> : JsPrimitive
    {
        //  { "handle" : <handle>,
        //    "type"   : <"boolean", "number" or "string">
        //    "value"  : <JSON encoded value>
        //  }
        protected JsPrimitive()
        { }

        [DataMember(Name = "value")]
        public virtual T Value { get; set; }

        public static implicit operator JsPrimitive<T>(T value)
        {
            foreach (var subType in SubClass.Get(typeof(JsPrimitive<T>)).SubTypes) {
                var valueType = subType.GetGenericArguments().FirstOrDefault();
                if (valueType.IsAssignableFrom(typeof(T))) {
                    var _this = CreateInstance(subType) as JsPrimitive<T>;
                    if (_this == null)
                        return null;
                    _this.Value = value;
                    return _this;
                }
            }
            return null;
        }

        public static implicit operator T(JsPrimitive<T> _this)
        {
            return _this.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    [DataContract]
    class JsBoolean : JsPrimitive<bool>
    {
        public JsBoolean()
        {
            Type = DataType.Boolean;
        }

        public override string ToString()
        {
            return Value ? "true" : "false";
        }
    }

    [DataContract]
    class JsNumber : JsPrimitive<decimal>
    {
        public JsNumber()
        {
            Type = DataType.Number;
        }

        public override string ToString()
        {
            if (Value - Math.Floor(Value) == 0)
                return string.Format("{0:0}", Value);
            return Format(Value);
        }
    }

    [DataContract]
    class JsString : JsPrimitive<string>
    {
        public JsString()
        {
            Type = DataType.String;
        }

        public override string ToString()
        {
            return Format(Value);
        }
    }
}
