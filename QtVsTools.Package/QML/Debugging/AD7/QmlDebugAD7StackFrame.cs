/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Threading;

namespace QtVsTools.Qml.Debug.AD7
{
    sealed partial class StackFrame : Concurrent,

        IDebugStackFrame2,        // "This interface represents a single stack frame in a call
                                  //  stack in a particular thread."

        IDebugExpressionContext2, // "This interface represents a context for expression evaluation"

        IDebugProperty2           // "This interface represents a stack frame property, a program
                                  //  document property, or some other property. The property is
                                  //  usually the result of an expression evaluation."
    {
        public QmlDebugger Debugger { get; private set; }

        public QmlEngine Engine { get; private set; }
        public Program Program { get; private set; }

        public CodeContext Context { get; private set; }
        private Dictionary<int, Dictionary<string, Property>> Properties { get; set; }

        private string Name { get; set; }
        public int FrameNumber { get; set; }
        private IEnumerable<int> Scopes { get; set; }
        private JoinableTask InitThread { get; set; }

        public static StackFrame Create(
            string name,
            int number,
            IEnumerable<int> scopes,
            CodeContext context)
        {
            var _this = new StackFrame();
            return _this.Initialize(name, number, scopes, context) ? _this : null;
        }

        private StackFrame()
        { }

        private bool Initialize(
            string name,
            int number,
            IEnumerable<int> scopes,
            CodeContext context)
        {
            Context = context;
            Engine = context.Engine;
            Program = context.Program;
            Debugger = Program.Debugger;
            Name = $"{name}@{context.FilePath}:{context.FileLine + 1}";
            FrameNumber = number;
            Scopes = scopes;
            InitThread = QtVsToolsPackage.Instance.JoinableTaskFactory.RunAsync(async () =>
            {
                InitializeProperties();
                await Task.Yield();
            });
            return true;
        }

        private void InitializeProperties(bool forceScope = false)
        {
            Properties = Scopes.ToDictionary(x => x, x => new Dictionary<string, Property>());
            foreach (var scopeNumber in Scopes) {
                var scopeVars = Debugger.RefreshScope(FrameNumber, scopeNumber, forceScope);
                foreach (var scopeVar in scopeVars) {
                    Properties[scopeNumber]
                        .Add(scopeVar.Name, Property.Create(this, scopeNumber, scopeVar));
                }
            }
        }

        public void Refresh()
        {
            InitializeProperties(true);
        }

        int IDebugExpressionContext2.ParseText(
            string pszCode,
            enum_PARSEFLAGS dwFlags,
            uint nRadix,
            out IDebugExpression2 ppExpr,
            out string pbstrError,
            out uint pichError)
        {
            pbstrError = "";
            pichError = 0;
            ppExpr = Expression.Create(this, pszCode);
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.EnumProperties(
            enum_DEBUGPROP_INFO_FLAGS dwFields,
            uint nRadix,
            ref Guid guidFilter,
            uint dwTimeout,
            out uint pcelt,
            out IEnumDebugPropertyInfo2 ppEnum)
        {
            pcelt = 0;
            ppEnum = null;

            if (guidFilter != Guid.Empty && !Property.Filter.LocalsSelected(ref guidFilter))
                return VSConstants.S_OK;

            InitThread.Join();
            pcelt = 0;
            ppEnum = PropertyEnum.Create(Properties
                .SelectMany(x => x.Value
                    .Select(y => y.Value.GetInfo(dwFields))));

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
            return ((IDebugStackFrame2)this)
                .EnumProperties(dwFields, dwRadix, guidFilter, dwTimeout, out _, out ppEnum);
        }

        #region //////////////////// Info /////////////////////////////////////////////////////////

        class StackFrameInfo : InfoHelper<StackFrameInfo>
        {
            public string FunctionName { get; set; }
            public string ReturnType { get; set; }
            public string Arguments { get; set; }
            public string Language { get; set; }
            public string ModuleName { get; set; }
            public ulong? MinAddress { get; set; }
            public ulong? MaxAddress { get; set; }
            public IDebugStackFrame2 Frame { get; set; }
            public IDebugModule2 Module { get; set; }
            public int? HasDebugInfo { get; set; }
            public int? StaleCode { get; set; }
        }

        StackFrameInfo Info => new()
        {
            FunctionName = Name,
            ReturnType = "",
            Arguments = "",
            Language = Context.FileType.ToString(),
            ModuleName = "",
            MinAddress = 0,
            MaxAddress = 9999,
            Frame = this,
            Module = Program,
            HasDebugInfo = 1,
            StaleCode = 0,
        };

        static readonly StackFrameInfo.Mapping MappingToFRAMEINFO =

        #region //////////////////// FRAMEINFO <-- StackFrameInfo /////////////////////////////////
            // r: Ref<FRAMEINFO>
            // f: enum_FRAMEINFO_FLAGS
            // i: StackFrameInfo
            // v: value of i.<<property>>

            new StackFrameInfo.Mapping<FRAMEINFO, enum_FRAMEINFO_FLAGS>
                ((r, bit) => r.s.m_dwValidFields |= bit)
            {
                { enum_FRAMEINFO_FLAGS.FIF_FUNCNAME,
                    (r, v) => r.s.m_bstrFuncName = v, i => i.FunctionName },

                { enum_FRAMEINFO_FLAGS.FIF_RETURNTYPE,
                    (r, v) => r.s.m_bstrReturnType = v, i => i.ReturnType },

                { enum_FRAMEINFO_FLAGS.FIF_ARGS,
                    (r, v) => r.s.m_bstrArgs = v, i => i.Arguments },

                { enum_FRAMEINFO_FLAGS.FIF_LANGUAGE,
                    (r, v) => r.s.m_bstrLanguage = v, i => i.Language },

                { enum_FRAMEINFO_FLAGS.FIF_MODULE,
                    (r, v) => r.s.m_bstrModule = v, i => i.ModuleName },

                { enum_FRAMEINFO_FLAGS.FIF_STACKRANGE,
                    (r, v) => r.s.m_addrMin = v, i => i.MinAddress },

                { enum_FRAMEINFO_FLAGS.FIF_STACKRANGE,
                    (r, v) => r.s.m_addrMax = v, i => i.MaxAddress },

                { enum_FRAMEINFO_FLAGS.FIF_FRAME,
                    (r, v) => r.s.m_pFrame = v, i => i.Frame },

                { enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP,
                    (r, v) => r.s.m_pModule = v, i => i.Module },

                { enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO,
                    (r, v) => r.s.m_fHasDebugInfo = v, i => i.HasDebugInfo },

                { enum_FRAMEINFO_FLAGS.FIF_STALECODE,
                    (r, v) => r.s.m_fStaleCode = v, i => i.StaleCode },
            };

        #endregion //////////////////// FRAMEINFO <-- StackFrameInfo //////////////////////////////


        int IDebugStackFrame2.GetInfo(
            enum_FRAMEINFO_FLAGS dwFieldSpec,
            uint nRadix,
            FRAMEINFO[] pFrameInfo)
        {
            Info.Map(MappingToFRAMEINFO, dwFieldSpec, out pFrameInfo[0]);
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.GetCodeContext(out IDebugCodeContext2 ppCodeCxt)
        {
            ppCodeCxt = Context;
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.GetDocumentContext(out IDebugDocumentContext2 ppCxt)
        {
            ppCxt = Context;
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.GetName(out string pbstrName)
        {
            pbstrName = Name;
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.GetPhysicalStackRange(out ulong paddrMin, out ulong paddrMax)
        {
            paddrMin = ulong.MinValue;
            paddrMax = ulong.MaxValue;
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.GetExpressionContext(out IDebugExpressionContext2 ppExprCxt)
        {
            ppExprCxt = this;
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            pbstrLanguage = "C++";
            pguidLanguage = NativeEngine.IdLanguageCpp;
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.GetThread(out IDebugThread2 ppThread)
        {
            ppThread = Program;
            return VSConstants.S_OK;
        }

        int IDebugStackFrame2.GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            ppProperty = this;
            return VSConstants.S_OK;
        }

        #endregion //////////////////// Info //////////////////////////////////////////////////////

    }
}
