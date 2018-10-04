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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace QtVsTools.Qml.Debug.AD7
{
    sealed partial class QmlEngine
    {
        #region //////////////////// IDebugEngine2 ////////////////////////////////////////////////

        int IDebugEngine2.SetLocale(ushort wLangID)
        {
            return VSConstants.S_OK;
        }

        int IDebugEngine2.SetRegistryRoot(string pszRegistryRoot)
        {
            return VSConstants.S_OK;
        }

        int IDebugEngine2.SetMetric(string pszMetric, object varValue)
        {
            return VSConstants.S_OK;
        }

        int IDebugEngine2.EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        { throw new NotImplementedException(); }

        int IDebugEngine2.SetException(EXCEPTION_INFO[] pException)
        { throw new NotImplementedException(); }

        int IDebugEngine2.RemoveSetException(EXCEPTION_INFO[] pException)
        { throw new NotImplementedException(); }

        int IDebugEngine2.RemoveAllSetExceptions(ref Guid guidType)
        { throw new NotImplementedException(); }

        int IDebugEngine2.DestroyProgram(IDebugProgram2 pProgram)
        { throw new NotImplementedException(); }

        int IDebugEngine2.CauseBreak()
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugEngine2 /////////////////////////////////////////////
    }

    sealed partial class ProgramProvider
    {
        #region //////////////////// IDebugProgramProvider2 ///////////////////////////////////////

        int IDebugProgramProvider2.GetProviderProcessData(
            enum_PROVIDER_FLAGS Flags,
            IDebugDefaultPort2 pPort,
            AD_PROCESS_ID ProcessId,
            CONST_GUID_ARRAY EngineFilter,
            PROVIDER_PROCESS_DATA[] pProcess)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProgramProvider2.GetProviderProgramNode(
            enum_PROVIDER_FLAGS Flags,
            IDebugDefaultPort2 pPort,
            AD_PROCESS_ID ProcessId,
            ref Guid guidEngine,
            ulong programId,
            out IDebugProgramNode2 ppProgramNode)
        {
            ppProgramNode = null;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProgramProvider2.WatchForProviderEvents(
            enum_PROVIDER_FLAGS Flags,
            IDebugDefaultPort2 pPort,
            AD_PROCESS_ID ProcessId,
            CONST_GUID_ARRAY EngineFilter,
            ref Guid guidLaunchingEngine,
            IDebugPortNotify2 pEventCallback)
        {
            return VSConstants.S_OK;
        }

        int IDebugProgramProvider2.SetLocale(ushort wLangID)
        {
            return VSConstants.S_OK;
        }

        #endregion //////////////////// IDebugProgramProvider2 ////////////////////////////////////
    }

    sealed partial class Program
    {
        #region //////////////////// IDebugProgramNode2 ///////////////////////////////////////////

        int IDebugProgramNode2.GetHostName(
            enum_GETHOSTNAME_TYPE dwHostNameType,
            out string pbstrHostName)
        {
            pbstrHostName = string.Empty;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProgramNode2.GetHostMachineName_V7(out string pbstrHostMachineName)
        { throw new NotImplementedException(); }

        int IDebugProgramNode2.Attach_V7(
            IDebugProgram2 pMDMProgram,
            IDebugEventCallback2 pCallback,
            uint dwReason)
        { throw new NotImplementedException(); }

        int IDebugProgramNode2.DetachDebugger_V7()
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugProgramNode2 ////////////////////////////////////////


        #region //////////////////// IDebugProgram3 ///////////////////////////////////////////////

        public int /*IDebugProgram3*/ Terminate()
        {
            return VSConstants.S_OK;
        }

        public int /*IDebugProgram3*/ CauseBreak()
        {
            return VSConstants.S_OK;
        }

        public int /*IDebugProgram3*/ CanDetach()
        {
            return VSConstants.S_OK;
        }

        public int /*IDebugProgram3*/ Detach()
        {
            return VSConstants.S_OK;
        }

        public int /*IDebugProgram3*/ GetProcess(out IDebugProcess2 ppProcess)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ Attach(IDebugEventCallback2 pCallback)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ GetDebugProperty(out IDebugProperty2 ppProperty)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ Execute()
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ GetEngineInfo(out string pbstrEngine, out Guid pguidEngine)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ EnumCodeContexts(
            IDebugDocumentPosition2 pDocPos,
            out IEnumDebugCodeContexts2 ppEnum)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ GetDisassemblyStream(
            enum_DISASSEMBLY_STREAM_SCOPE dwScope,
            IDebugCodeContext2 pCodeContext,
            out IDebugDisassemblyStream2 ppDisassemblyStream)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ GetENCUpdate(out object ppUpdate)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ EnumCodePaths(string pszHint,
            IDebugCodeContext2 pStart,
            IDebugStackFrame2 pFrame,
            int fSource,
            out IEnumCodePaths2 ppEnum,
            out IDebugCodeContext2 ppSafety)
        { throw new NotImplementedException(); }

        public int /*IDebugProgram3*/ WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugProgram3 ////////////////////////////////////////////


        #region //////////////////// IDebugModule3 ////////////////////////////////////////////////

        public int /*IDebugModule3*/ IsUserCode(out int pfUser)
        {
            pfUser = 1;
            return VSConstants.S_OK;
        }

        public int /*IDebugModule3*/ ReloadSymbols_Deprecated(
            string pszUrlToSymbols,
            out string pbstrDebugMessage)
        { throw new NotImplementedException(); }

        public int /*IDebugModule3*/ GetSymbolInfo(
            enum_SYMBOL_SEARCH_INFO_FIELDS dwFields,
            MODULE_SYMBOL_SEARCH_INFO[] pinfo)
        { throw new NotImplementedException(); }

        public int /*IDebugModule3*/ LoadSymbols()
        { throw new NotImplementedException(); }

        public int /*IDebugModule3*/ SetJustMyCodeState(int fIsUserCode)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugModule3 /////////////////////////////////////////////


        #region //////////////////// IDebugProcess2 ///////////////////////////////////////////////

        int IDebugProcess2.GetInfo(enum_PROCESS_INFO_FIELDS Fields, PROCESS_INFO[] pProcessInfo)
        { throw new NotImplementedException(); }

        int IDebugProcess2.GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
        { throw new NotImplementedException(); }

        int IDebugProcess2.GetServer(out IDebugCoreServer2 ppServer)
        { throw new NotImplementedException(); }

        int IDebugProcess2.Terminate()
        { throw new NotImplementedException(); }

        int IDebugProcess2.EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        { throw new NotImplementedException(); }

        int IDebugProcess2.Attach(
            IDebugEventCallback2 pCallback,
            Guid[] rgguidSpecificEngines,
            uint celtSpecificEngines,
            int[] rghrEngineAttach)
        { throw new NotImplementedException(); }

        int IDebugProcess2.GetAttachedSessionName(out string pbstrSessionName)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugProcess2 ////////////////////////////////////////////


        #region //////////////////// IDebugThread2 ////////////////////////////////////////////////

        int IDebugThread2.SetThreadName(string pszName)
        { throw new NotImplementedException(); }

        int IDebugThread2.GetProgram(out IDebugProgram2 ppProgram)
        { throw new NotImplementedException(); }

        int IDebugThread2.CanSetNextStatement(
            IDebugStackFrame2 pStackFrame,
            IDebugCodeContext2 pCodeContext)
        { throw new NotImplementedException(); }

        int IDebugThread2.SetNextStatement(IDebugStackFrame2 pStackFrame,
            IDebugCodeContext2 pCodeContext)
        { throw new NotImplementedException(); }

        int IDebugThread2.Suspend(out uint pdwSuspendCount)
        { throw new NotImplementedException(); }

        int IDebugThread2.Resume(out uint pdwSuspendCount)
        { throw new NotImplementedException(); }

        int IDebugThread2.GetLogicalThread(IDebugStackFrame2 pStackFrame,
            out IDebugLogicalThread2 ppLogicalThread)
        { throw new NotImplementedException(); }

        int IDebugThread100.GetFlags(out uint pFlags)
        { throw new NotImplementedException(); }

        int IDebugThread100.SetFlags(uint flags)
        { throw new NotImplementedException(); }

        int IDebugThread100.CanDoFuncEval()
        { throw new NotImplementedException(); }

        int IDebugThread100.GetThreadDisplayName(out string bstrDisplayName)
        { throw new NotImplementedException(); }

        int IDebugThread100.SetThreadDisplayName(string bstrDisplayName)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugThread2 /////////////////////////////////////////////
    }

    sealed partial class CodeContext
    {
        #region //////////////////// IDebugDocumentContext2 ///////////////////////////////////////
        int IDebugDocumentContext2.GetDocument(out IDebugDocument2 ppDocument)
        {
            ppDocument = null;
            return VSConstants.E_FAIL;
        }
        int IDebugDocumentContext2.GetSourceRange(
            TEXT_POSITION[] pBegPosition,
            TEXT_POSITION[] pEndPosition)
        { throw new NotImplementedException(); }

        int IDebugDocumentContext2.Compare(
            enum_DOCCONTEXT_COMPARE Compare,
            IDebugDocumentContext2[] rgpDocContextSet,
            uint dwDocContextSetLen,
            out uint pdwDocContext)
        {
            dwDocContextSetLen = 0;
            pdwDocContext = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugDocumentContext2.Seek(int nCount, out IDebugDocumentContext2 ppDocContext)
        {
            ppDocContext = null;
            return VSConstants.E_NOTIMPL;
        }

        #endregion //////////////////// IDebugDocumentContext2 ////////////////////////////////////


        #region //////////////////// IDebugCodeContext2 ///////////////////////////////////////////

        public int /*IDebugCodeContext2*/ Add(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
        { throw new NotImplementedException(); }

        public int /*IDebugCodeContext2*/ Subtract(
            ulong dwCount,
            out IDebugMemoryContext2 ppMemCxt)
        { throw new NotImplementedException(); }

        public int /*IDebugCodeContext2*/ GetName(out string pbstrName)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugCodeContext2 ////////////////////////////////////////
    }

    sealed partial class PendingBreakpoint
    {
        #region //////////////////// IDebugPendingBreakpoint2 /////////////////////////////////////

        int IDebugPendingBreakpoint2.CanBind(out IEnumDebugErrorBreakpoints2 ppErrorEnum)
        {
            ppErrorEnum = null;
            return VSConstants.S_OK;
        }

        int IDebugPendingBreakpoint2.Virtualize(int fVirtualize)
        {
            return VSConstants.S_OK;
        }

        int IDebugPendingBreakpoint2.EnumErrorBreakpoints(
            enum_BP_ERROR_TYPE bpErrorType,
            out IEnumDebugErrorBreakpoints2 ppEnum)
        {
            ppEnum = null;
            return VSConstants.S_OK;
        }

        int IDebugPendingBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        { throw new NotImplementedException(); }

        int IDebugPendingBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugPendingBreakpoint2 //////////////////////////////////
    }

    sealed partial class Breakpoint
    {
        #region //////////////////// IDebugBoundBreakpoint2 ///////////////////////////////////////

        int IDebugBoundBreakpoint2.GetHitCount(out uint pdwHitCount)
        {
            pdwHitCount = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugBoundBreakpoint2.SetHitCount(uint dwHitCount)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IDebugBoundBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        { throw new NotImplementedException(); }

        int IDebugBoundBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugBoundBreakpoint2 ////////////////////////////////////
    }

    sealed partial class StackFrame
    {
        #region //////////////////// IDebugExpressionContext2 /////////////////////////////////////

        int IDebugExpressionContext2.GetName(out string pbstrName)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugExpressionContext2 //////////////////////////////////


        #region //////////////////// IDebugProperty2 //////////////////////////////////////////////

        int IDebugProperty2.GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            ppDerivedMost = null;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.GetExtendedInfo(ref Guid guidExtendedInfo, out object pExtendedInfo)
        {
            pExtendedInfo = null;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            ppMemoryBytes = null;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            ppMemory = null;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.GetParent(out IDebugProperty2 ppParent)
        {
            ppParent = null;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.GetPropertyInfo(
            enum_DEBUGPROP_INFO_FLAGS dwFields,
            uint dwRadix,
            uint dwTimeout,
            IDebugReference2[] rgpArgs,
            uint dwArgCount,
            DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.GetReference(out IDebugReference2 ppReference)
        {
            ppReference = null;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.GetSize(out uint pdwSize)
        {
            pdwSize = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.SetValueAsReference(
            IDebugReference2[] rgpArgs,
            uint dwArgCount,
            IDebugReference2 pValue,
            uint dwTimeout)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IDebugProperty2.SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion //////////////////// IDebugProperty2 ///////////////////////////////////////////
    }

    sealed partial class Property
    {
        #region //////////////////// IDebugProperty2 //////////////////////////////////////////////

        int IDebugProperty2.SetValueAsReference(
            IDebugReference2[] rgpArgs,
            uint dwArgCount,
            IDebugReference2 pValue,
            uint dwTimeout)
        { throw new NotImplementedException(); }

        int IDebugProperty2.GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        { throw new NotImplementedException(); }

        int IDebugProperty2.GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        { throw new NotImplementedException(); }

        int IDebugProperty2.GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        { throw new NotImplementedException(); }

        int IDebugProperty2.GetSize(out uint pdwSize)
        { throw new NotImplementedException(); }

        int IDebugProperty2.GetReference(out IDebugReference2 ppReference)
        { throw new NotImplementedException(); }

        int IDebugProperty2.GetExtendedInfo(ref Guid guidExtendedInfo, out object pExtendedInfo)
        { throw new NotImplementedException(); }

        #endregion //////////////////// IDebugProperty2 ///////////////////////////////////////////
    }

    sealed partial class Expression
    {
        #region //////////////////// IDebugExpression2 ////////////////////////////////////////////

        int IDebugExpression2.Abort()
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion //////////////////// IDebugExpression2 /////////////////////////////////////////
    }
}
