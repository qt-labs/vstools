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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace QtVsTools.Qml.Debug.AD7
{
    using V4;

    sealed partial class Property : Concurrent,

        IDebugProperty2 // "This interface represents a stack frame property, a program document
                        //  property, or some other property. The property is usually the result of
                        //  an expression evaluation."
    {
        public QmlDebugger Debugger { get; private set; }

        public QmlEngine Engine { get; private set; }
        public Program Program { get; private set; }
        public StackFrame StackFrame { get; private set; }
        public CodeContext CodeContext { get; private set; }

        public Property Parent { get; private set; }
        public SortedDictionary<string, Property> Children { get; private set; }

        public int FrameNumber { get; private set; }
        public int ScopeNumber { get; private set; }
        public JsValue JsValue { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string Type { get; private set; }
        public string Value { get; private set; }

        public static Property Create(
            StackFrame frame,
            int scopeNumber,
            JsValue value,
            Property parent = null)
        {
            var _this = new Property();
            return _this.Initialize(frame, scopeNumber, value, parent) ? _this : null;
        }

        private Property()
        { }

        private bool Initialize(
            StackFrame frame,
            int scopeNumber,
            JsValue value,
            Property parent)
        {
            StackFrame = frame;
            Engine = frame.Engine;
            Program = frame.Program;
            Debugger = frame.Debugger;
            CodeContext = frame.Context;
            FrameNumber = frame.FrameNumber;
            ScopeNumber = scopeNumber;
            Parent = parent;
            JsValue = value;

            if (Parent != null && Parent.JsValue is JsObject && ((JsObject)Parent.JsValue).IsArray)
                Name = string.Format("[{0}]", JsValue.Name);
            else
                Name = JsValue.Name;

            var nameParts = new Stack<string>(new[] { Name });
            for (var p = Parent; p != null && !string.IsNullOrEmpty(p.Name); p = p.Parent) {
                if (!nameParts.Peek().StartsWith("["))
                    nameParts.Push(".");
                nameParts.Push(p.Name);
            }
            FullName = string.Join("", nameParts);

            Type = JsValue.Type.ToString();
            Value = JsValue.ToString();

            Children = new SortedDictionary<string, Property>();
            if (JsValue is JsObject) {
                var obj = JsValue as JsObject;
                foreach (JsValue objProp in obj.Properties.Where(x => x.HasData)) {
                    Children[GetChildKey(objProp.Name)]
                        = Create(StackFrame, ScopeNumber, objProp, this);
                }
            }

            return true;
        }

        static string GetChildKey(string childName)
        {
            int childIndex;
            if (int.TryParse(childName, out childIndex))
                return string.Format("{0:D9}", childIndex);
            else
                return childName;
        }

        int IDebugProperty2.SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            string expr = string.Format("{0}=({1})", FullName, pszValue);

            var value = Debugger.Evaluate(FrameNumber, expr);
            if (value == null || value is JsError)
                return VSConstants.S_FALSE;

            Program.Refresh();
            return VSConstants.S_OK;
        }

        int IDebugProperty2.EnumChildren(
            enum_DEBUGPROP_INFO_FLAGS dwFields,
            uint dwRadix,
            ref Guid guidFilter,
            enum_DBG_ATTRIB_FLAGS dwAttribFilter,
            string pszNameFilter,
            uint dwTimeout,
            out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = null;
            if (guidFilter != Guid.Empty && !Filter.LocalsSelected(ref guidFilter))
                return VSConstants.S_OK;

            if (JsValue is JsObjectRef) {
                var obj = Debugger.Lookup(FrameNumber, ScopeNumber, JsValue as JsObjectRef);
                if (obj == null)
                    return VSConstants.S_OK;

                JsValue = obj;
                foreach (JsValue objProp in obj.Properties.Where(x => x.HasData)) {
                    Children[GetChildKey(objProp.Name)]
                        = Create(StackFrame, ScopeNumber, objProp, this);
                }
            }

            if (!Children.Any())
                return VSConstants.S_OK;

            ppEnum = PropertyEnum.Create(Children.Select(x =>
            {
                var info = new DEBUG_PROPERTY_INFO[1];
                (x.Value as IDebugProperty2).GetPropertyInfo(dwFields, dwRadix, 0,
                    new IDebugReference2[0], 0, info);
                return info[0];
            }));
            return VSConstants.S_OK;
        }


        #region //////////////////// Info /////////////////////////////////////////////////////////

        class PropertyInfo : InfoHelper<PropertyInfo>
        {
            public string FullName { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
            public enum_DBG_ATTRIB_FLAGS? Attribs { get; set; }
            public IDebugProperty2 Property { get; set; }
        }

        PropertyInfo Info
        {
            get
            {
                return new PropertyInfo
                {
                    Name = Name,
                    FullName = FullName,
                    Type = Type,
                    Value = Value,
                    Property = this,
                    Attribs = ((Children.Any() || JsValue.Type == JsValue.DataType.Object)
                        ? enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE : 0),
                };
            }
        }

        static readonly PropertyInfo.Mapping MappingToDEBUG_PROPERTY_INFO =

        #region //////////////////// DEBUG_PROPERTY_INFO <-- PropertyInfo /////////////////////////
            // r: Ref<DEBUG_PROPERTY_INFO>
            // f: enum_DEBUGPROP_INFO_FLAGS
            // i: PropertyInfo
            // v: value of i.<<property>>

            new PropertyInfo.Mapping<DEBUG_PROPERTY_INFO, enum_DEBUGPROP_INFO_FLAGS>
                ((r, bit) => r.s.dwFields |= bit)
            {
                { enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME,
                    (r, v) => r.s.bstrFullName = v, i => i.FullName },

                { enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME,
                    (r, v) => r.s.bstrName = v, i => i.Name },

                { enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE,
                    (r, v) => r.s.bstrType = v, i => i.Type },

                { enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE,
                    (r, v) => r.s.bstrValue = v, i => i.Value },

                { enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB,
                    (r, v) => r.s.dwAttrib |= v, i => i.Attribs },

                { enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP,
                    (r, v) => r.s.pProperty = v, i => i.Property },
            };

        #endregion //////////////////// DEBUG_PROPERTY_INFO <-- PropertyInfo //////////////////////


        public DEBUG_PROPERTY_INFO GetInfo(enum_DEBUGPROP_INFO_FLAGS dwFields)
        {
            DEBUG_PROPERTY_INFO info;
            Info.Map(MappingToDEBUG_PROPERTY_INFO, dwFields, out info);
            return info;
        }

        int IDebugProperty2.GetPropertyInfo(
            enum_DEBUGPROP_INFO_FLAGS dwFields,
            uint dwRadix,
            uint dwTimeout,
            IDebugReference2[] rgpArgs,
            uint dwArgCount,
            DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            Info.Map(MappingToDEBUG_PROPERTY_INFO, dwFields, out pPropertyInfo[0]);
            return VSConstants.S_OK;
        }

        int IDebugProperty2.GetParent(out IDebugProperty2 ppParent)
        {
            ppParent = Parent;
            return (Parent != null) ? VSConstants.S_OK : VSConstants.S_FALSE;
        }

        #endregion //////////////////// Info //////////////////////////////////////////////////////


        #region //////////////////// Filter ///////////////////////////////////////////////////////

        public static class Filter
        {
            public static readonly Guid Registers
                = new Guid("223ae797-bd09-4f28-8241-2763bdc5f713");

            public static readonly Guid Locals
                = new Guid("b200f725-e725-4c53-b36a-1ec27aef12ef");

            public static readonly Guid AllLocals
                = new Guid("196db21f-5f22-45a9-b5a3-32cddb30db06");

            public static readonly Guid Args
                = new Guid("804bccea-0475-4ae7-8a46-1862688ab863");

            public static readonly Guid LocalsPlusArgs
                = new Guid("e74721bb-10c0-40f5-807f-920d37f95419");

            public static readonly Guid AllLocalsPlusArgs
                = new Guid("939729a8-4cb0-4647-9831-7ff465240d5f");

            public static bool LocalsSelected(ref Guid guidFilter)
            {
                return guidFilter == Locals
                    || guidFilter == AllLocals
                    || guidFilter == LocalsPlusArgs
                    || guidFilter == AllLocalsPlusArgs;
            }
        }

        #endregion //////////////////// Filter ////////////////////////////////////////////////////
    }
}
