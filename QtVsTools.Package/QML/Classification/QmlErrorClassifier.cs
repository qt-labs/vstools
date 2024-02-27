/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
    [Export(typeof(ITaggerProvider))]
    [ContentType(QmlContentType.Name)]
    [TagType(typeof(ErrorTag))]
    internal sealed class QmlErrorClassifierProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService classificationTypeRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            QmlClassificationType.InitClassificationTypes(classificationTypeRegistry);
            return new QmlErrorClassifier(buffer) as ITagger<T>;
        }
    }

    internal sealed class QmlErrorClassifier : QmlAsyncClassifier<ErrorTag>
    {
        internal QmlErrorClassifier(
            ITextBuffer buffer)
            : base("Error", buffer)
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
