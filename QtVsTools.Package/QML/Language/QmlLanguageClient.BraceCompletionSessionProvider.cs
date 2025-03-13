// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;

namespace QtVsTools.Package.QML.Language
{
    [Export(typeof(IBraceCompletionSessionProvider))]
    [BracePair('{', '}')]
    public partial class QmlLanguageClient : IBraceCompletionSessionProvider
    {
        public bool TryCreateSession(ITextView textView, SnapshotPoint openingPoint,
            char openingBrace, char closingBrace, out IBraceCompletionSession session)
        {
            session = null;
            return true;
        }
    }
}
