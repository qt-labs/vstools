/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
        private string ExpressionString { get; set; }

        private StackFrame StackFrame { get; set; }
        public QmlEngine Engine { get; private set; }
        public Program Program { get; private set; }
        private QmlDebugger Debugger { get; set; }
        private CodeContext CodeContext { get; set; }

        public static Expression Create(StackFrame frame, string expr)
        {
            return new Expression
            {
                ExpressionString = expr,
                StackFrame = frame,
                Engine = frame.Engine,
                Program = frame.Program,
                Debugger = frame.Debugger,
                CodeContext = frame.Context
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
            if (value is null or JsError)
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
            _ = Task.Run(() =>
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
