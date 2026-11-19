// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace QtVsTools.Qml.Classification
{
    [Export(typeof(ISmartIndentProvider))]
    [Name(nameof(QmlSmartIndentProvider))]
    [ContentType(QmlContentType.Name)]
    internal sealed class QmlSmartIndentProvider : ISmartIndentProvider
    {
        public ISmartIndent CreateSmartIndent(ITextView textView)
        {
            return textView == null ? null : new QmlSmartIndent(textView);
        }

        private sealed class QmlSmartIndent : ISmartIndent
        {
            private readonly ITextView textView;

            public QmlSmartIndent(ITextView textView)
            {
                this.textView = textView;
            }

            public int? GetDesiredIndentation(ITextSnapshotLine line)
            {
                if (line == null)
                    return null;

                if (line.LineNumber <= 0)
                    return 0;

                var previousLine = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
                var previousText = previousLine.GetText();
                var tabSize = textView.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);

                var indent = 0;
                foreach (var ch in previousText) {
                    switch (ch) {
                    case ' ':
                        indent++;
                        break;
                    case '\t':
                        indent += tabSize;
                        break;
                    default:
                        return indent;
                    }
                }

                return indent;
            }

            public void Dispose()
            {
            }
        }
    }
}
