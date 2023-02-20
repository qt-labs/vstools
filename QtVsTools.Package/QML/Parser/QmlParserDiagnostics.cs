/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace QtVsTools.Qml
{
    using Syntax;

    public enum DiagnosticMessageKind { Warning, Error }

    /// <summary>
    /// Represents a syntax error issued by the QML parser
    /// </summary>
    public class DiagnosticMessage
    {
        private DiagnosticMessageKind Kind { get; }
        public SourceLocation Location { get; }
        public DiagnosticMessage(DiagnosticMessageKind kind, int offset, int length)
        {
            Kind = kind;
            Location = new SourceLocation
            {
                Offset = offset,
                Length = length
            };
        }
    }
}
