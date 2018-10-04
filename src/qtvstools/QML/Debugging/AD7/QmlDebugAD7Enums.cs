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
    /// <summary>
    /// Abstraction of AD7 enum interfaces, e.g. IEnumDebugPrograms2
    /// (cf. https://docs.microsoft.com/en-us/visualstudio/extensibility/debugger/reference/ienumdebugprograms2)
    /// </summary>
    ///
    class Enum<T, TEnum, IEnum>
        where TEnum : Enum<T, TEnum, IEnum>, new()
        where IEnum : class
    {
        int index;
        IList<T> list;

        public static TEnum Create(IEnumerable<T> data)
        {
            return new TEnum
            {
                index = 0,
                list = new List<T>(data)
            };
        }

        public static TEnum Create(T singleElement)
        {
            return new TEnum
            {
                index = 0,
                list = new List<T>() { singleElement }
            };
        }

        public static TEnum Create()
        {
            return new TEnum
            {
                index = 0,
                list = new List<T>()
            };
        }

        protected Enum()
        { }

        /// <summary>
        /// Returns the next set of elements from the enumeration.
        /// </summary>
        /// <param name="numElems">The number of elements to retrieve.</param>
        /// <returns>
        /// Collection of retrieved elements.
        /// </returns>
        public IEnumerable<T> Next(uint numElems)
        {
            int oldIndex = index;
            int maxIndex = Math.Min(list.Count, oldIndex + (int)numElems);
            for (; index < maxIndex; ++index)
                yield return list[index];
        }

        /// <summary>
        /// Returns the next set of elements from the enumeration.
        /// </summary>
        /// <param name="numElems">
        /// The number of elements to retrieve.
        /// </param>
        /// <param name="elems">
        /// Array of elements to be filled in.
        /// </param>
        /// <param name="numElemsFetched">
        /// Returns the number of elements actually returned in elems.
        /// </param>
        /// <returns>
        /// If successful, returns S_OK. Returns S_FALSE if fewer than the requested number of
        /// elements could be returned.
        /// </returns>
        public int Next(uint numElems, T[] elems, ref uint numElemsFetched)
        {
            var next = Next(numElems).ToArray();
            Array.Copy(next, elems, next.Length);
            numElemsFetched = (uint)next.Length;
            if (numElemsFetched < numElems)
                return VSConstants.S_FALSE;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Skips over the specified number of elements.
        /// </summary>
        /// <param name="numElems">Number of elements to skip.</param>
        /// <returns>
        /// If successful, returns S_OK. Returns S_FALSE if numElems is greater than the number of
        /// remaining elements; otherwise, returns an error code.
        /// </returns>
        /// <remarks>
        /// If numElems specifies a value greater than the number of remaining elements, the
        /// enumeration is set to the end and S_FALSE is returned.
        /// </remarks>
        public int Skip(uint numElems)
        {
            if ((ulong)index + numElems > Int32.MaxValue)
                return VSConstants.E_INVALIDARG;
            if (index + numElems > list.Count) {
                index = list.Count;
                return VSConstants.S_FALSE;
            }

            index += (int)numElems;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Resets the enumeration to the first element.
        /// </summary>
        /// <returns>
        /// If successful, returns S_OK; otherwise, returns an error code.
        /// </returns>
        public int Reset()
        {
            index = 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the number of elements in the enumeration.
        /// </summary>
        /// <param name="numElems">Returns the number of elements in the enumeration.</param>
        /// <returns>
        /// If successful, returns S_OK; otherwise, returns an error code.
        /// </returns>
        public int GetCount(out uint numElems)
        {
            numElems = (uint)list.Count;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns a copy of the current enumeration as a separate object.
        /// </summary>
        /// <param name="clonedEnum">Returns the clone of this enumeration.</param>
        /// <returns>
        /// If successful, returns S_OK; otherwise, returns an error code.
        /// </returns>
        /// <remarks>
        /// The copy of the enumeration has the same state as the original at the time this method
        /// is called. However, the copy's and the original's states are separate and can be
        /// changed individually.
        /// </remarks>
        public int Clone(out IEnum clonedEnum)
        {
            var clone = new TEnum();
            clone.index = index;
            clone.list = new List<T>(list);
            clonedEnum = clone as IEnum;
            return VSConstants.S_OK;
        }
    }

    class ProgramEnum :
        Enum<IDebugProgram2, ProgramEnum, IEnumDebugPrograms2>,
        IEnumDebugPrograms2
    { }

    class FrameInfoEnum :
        Enum<FRAMEINFO, FrameInfoEnum, IEnumDebugFrameInfo2>,
        IEnumDebugFrameInfo2
    { }

    class ThreadEnum :
        Enum<IDebugThread2, ThreadEnum, IEnumDebugThreads2>,
        IEnumDebugThreads2
    { }

    class ModuleEnum :
        Enum<IDebugModule2, ModuleEnum, IEnumDebugModules2>,
        IEnumDebugModules2
    { }

    class CodeContextEnum :
        Enum<IDebugCodeContext2, CodeContextEnum, IEnumDebugCodeContexts2>,
        IEnumDebugCodeContexts2
    { }

    class BoundBreakpointsEnum :
        Enum<IDebugBoundBreakpoint2, BoundBreakpointsEnum, IEnumDebugBoundBreakpoints2>,
        IEnumDebugBoundBreakpoints2
    { }

    class ErrorBreakpointsEnum :
        Enum<IDebugErrorBreakpoint2, ErrorBreakpointsEnum, IEnumDebugErrorBreakpoints2>,
        IEnumDebugErrorBreakpoints2
    { }

    class PropertyEnum :
        Enum<DEBUG_PROPERTY_INFO, PropertyEnum, IEnumDebugPropertyInfo2>,
        IEnumDebugPropertyInfo2
    {
        public int Next(uint celt, DEBUG_PROPERTY_INFO[] rgelt, out uint pceltFetched)
        {
            pceltFetched = 0;
            return Next(celt, rgelt, ref pceltFetched);
        }
    }
}
