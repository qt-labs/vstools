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
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace QtVsTools.Qml.Debug.AD7
{
    using V4;

    sealed partial class Expression :

        IDebugExpression2 // "This interface represents a parsed expression ready for binding
                          //  and evaluating."
    {
        public string ExpressionString { get; private set; }

        public StackFrame StackFrame { get; private set; }
        public QmlEngine Engine { get; private set; }
        public Program Program { get; private set; }
        public QmlDebugger Debugger { get; private set; }
        public CodeContext CodeContext { get; private set; }

        public static Expression Create(StackFrame frame, string expr)
        {
            return new Expression
            {
                ExpressionString = expr,
                StackFrame = frame,
                Engine = frame.Engine,
                Program = frame.Program,
                Debugger = frame.Debugger,
                CodeContext = frame.Context,
            };
        }

        private Expression()
        { }

        int IDebugExpression2.EvaluateSync(
            enum_EVALFLAGS dwFlags,
            uint dwTimeout,
            IDebugEventCallback2 pExprCallback,
            out IDebugProperty2 ppResult)
        {
            ppResult = null;
            var value = Debugger.Evaluate(StackFrame.FrameNumber, ExpressionString);
            if (value == null || value is JsError)
                return VSConstants.S_FALSE;

            Program.Refresh();

            value.Name = ExpressionString;
            ppResult = Property.Create(StackFrame, 0, value);
            return VSConstants.S_OK;
        }

        int IDebugExpression2.EvaluateAsync(
            enum_EVALFLAGS dwFlags,
            IDebugEventCallback2 pExprCallback)
        {
            Task.Run(() =>
            {
                var value = Debugger.Evaluate(StackFrame.FrameNumber, ExpressionString);
                if (value != null)
                    value.Name = ExpressionString;

                Program.Refresh();

                DebugEvent.Send(new ExpressionEvaluationCompleteEvent(
                    pExprCallback, this, Property.Create(StackFrame, 0, value)));
            });
            return VSConstants.S_OK;
        }
    }
}
