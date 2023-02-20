/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

/// Highlighting of syntax errors

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace QtVsTools.Qml.Classification
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("qml")]
    [TagType(typeof(ErrorTag))]
    internal sealed class QmlErrorClassifierProvider : IViewTaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService classificationTypeRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            QmlClassificationType.InitClassificationTypes(classificationTypeRegistry);
            return new QmlErrorClassifier(textView, buffer) as ITagger<T>;
        }
    }

    internal sealed class QmlErrorClassifier : QmlAsyncClassifier<ErrorTag>
    {
        internal QmlErrorClassifier(
            ITextView textView,
            ITextBuffer buffer)
            : base("Error", textView, buffer)
        {
        }

        protected override ClassificationRefresh ProcessText(
            ITextSnapshot snapshot,
            Parser parseResult,
            SharedTagList tagList,
            bool writeAccess)
        {
            if (writeAccess) {
                foreach (var diag in parseResult.DiagnosticMessages) {
                    tagList.Add(this, new QmlDiagnosticsTag(snapshot, diag));
                }
            }
            return ClassificationRefresh.FullText;
        }

        protected override ErrorTag GetClassification(TrackingTag tag)
        {
            return new ErrorTag("ERROR");
        }
    }
}
