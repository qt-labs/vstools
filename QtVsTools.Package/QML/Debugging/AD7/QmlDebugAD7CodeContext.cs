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
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace QtVsTools.Qml.Debug.AD7
{
    sealed partial class CodeContext :

        IDebugDocumentContext2, // "This interface represents a position in a source file document."

        IDebugCodeContext2,     // "This interface represents the starting position of a code
        IDebugCodeContext100    //  instruction. For most run-time architectures today, a code
                                //  context can be thought of as an address in a program's execution
                                //  stream."
    {
        public QmlEngine Engine { get; private set; }
        public Program Program { get; private set; }

        public string FilePath { get; private set; }
        public uint FileLine { get; private set; }

        public enum Language { QML, JavaScript, Other }
        public Language FileType
        {
            get
            {
                var ext = Path.GetExtension(FilePath);
                if (string.Equals(ext, ".qml", StringComparison.InvariantCultureIgnoreCase))
                    return Language.QML;
                if (string.Equals(ext, ".js", StringComparison.InvariantCultureIgnoreCase))
                    return Language.JavaScript;
                return Language.Other;
            }
        }

        public static CodeContext Create(
            QmlEngine engine, Program program,
            string filePath, uint fileLine)
        {
            return new CodeContext()
            {
                Engine = engine,
                Program = program,
                FilePath = filePath,
                FileLine = fileLine
            };
        }

        private CodeContext()
        { }

        class CodeContextInfo : InfoHelper<CodeContextInfo>
        {
            public string Address { get; set; }
        }

        CodeContextInfo Info =>
            new CodeContextInfo
            {
                Address = FileLine.ToString()
            };

        static readonly CodeContextInfo.Mapping MappingToCONTEXT_INFO =

        #region //////////////////// CONTEXT_INFO <-- CodeContextInfo /////////////////////////////
            // r: Ref<CONTEXT_INFO>
            // f: enum_CONTEXT_INFO_FIELDS
            // i: CodeContextInfo
            // v: value of i.<<property>>

            new CodeContextInfo.Mapping<CONTEXT_INFO, enum_CONTEXT_INFO_FIELDS>
                ((r, f) => r.s.dwFields |= f)
            {
                { enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS,
                    (r, v) => r.s.bstrAddress = v, i => i.Address }
            };

        #endregion //////////////////// CONTEXT_INFO <-- CodeContextInfo //////////////////////////


        public int /*IDebugCodeContext2*/ GetInfo(
            enum_CONTEXT_INFO_FIELDS dwFields,
            CONTEXT_INFO[] pinfo)
        {
            Info.Map(MappingToCONTEXT_INFO, dwFields, out pinfo[0]);
            return VSConstants.S_OK;
        }

        public int /*IDebugCodeContext2*/ Compare(
            enum_CONTEXT_COMPARE Compare,
            IDebugMemoryContext2[] rgpMemoryContextSet,
            uint dwMemoryContextSetLen,
            out uint pdwMemoryContext)
        {
            pdwMemoryContext = uint.MaxValue;
            if (Compare != enum_CONTEXT_COMPARE.CONTEXT_EQUAL)
                return VSConstants.E_NOTIMPL;

            for (uint i = 0; i < dwMemoryContextSetLen; ++i) {
                var that = rgpMemoryContextSet[i] as CodeContext;
                if (that == null)
                    continue;
                if (this.Engine != that.Engine)
                    continue;
                if (this.Program != that.Program)
                    continue;
                if (!string.Equals(
                    Path.GetFullPath(this.FilePath), Path.GetFullPath(that.FilePath),
                    StringComparison.InvariantCultureIgnoreCase)) {
                    continue;
                }
                if (this.FileLine != that.FileLine)
                    continue;

                pdwMemoryContext = i;
                return VSConstants.S_OK;
            }

            return VSConstants.S_FALSE;
        }

        int IDebugDocumentContext2.GetName(enum_GETNAME_TYPE gnType, out string pbstrFileName)
        {
            pbstrFileName = FilePath;
            return VSConstants.S_OK;
        }

        int IDebugDocumentContext2.GetStatementRange(
            TEXT_POSITION[] pBegPosition,
            TEXT_POSITION[] pEndPosition)
        {
            pBegPosition[0].dwLine = FileLine;
            pBegPosition[0].dwColumn = 0;
            pEndPosition[0].dwLine = FileLine;
            pEndPosition[0].dwColumn = 0;
            return VSConstants.S_OK;
        }

        int IDebugDocumentContext2.EnumCodeContexts(out IEnumDebugCodeContexts2 ppEnumCodeCxts)
        {
            ppEnumCodeCxts = CodeContextEnum.Create(this);
            return VSConstants.S_OK;
        }

        int IDebugDocumentContext2.GetLanguageInfo(
            ref string pbstrLanguage,
            ref Guid pguidLanguage)
        {
            pbstrLanguage = "C++";
            pguidLanguage = NativeEngine.IdLanguageCpp;
            return VSConstants.S_OK;
        }

        int IDebugCodeContext2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            return (this as IDebugDocumentContext2)
                .GetLanguageInfo(ref pbstrLanguage, ref pguidLanguage);
        }

        int IDebugCodeContext2.GetDocumentContext(out IDebugDocumentContext2 ppSrcCxt)
        {
            ppSrcCxt = this;
            return VSConstants.S_OK;
        }

        int IDebugCodeContext100.GetProgram(out IDebugProgram2 ppProgram)
        {
            ppProgram = Program;
            return VSConstants.S_OK;
        }
    }
}
