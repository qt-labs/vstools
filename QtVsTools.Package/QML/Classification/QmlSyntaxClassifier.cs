/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

/// This file implements the actual highlighting of the text according to the
/// classification of the syntax elements recognized by the QML parser.

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
    [TagType(typeof(ClassificationTag))]
    internal sealed class QmlSyntaxClassifierProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService classificationTypeRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            QmlClassificationType.InitClassificationTypes(classificationTypeRegistry);
            return new QmlSyntaxClassifier(buffer) as ITagger<T>;
        }
    }

    internal sealed class QmlSyntaxClassifier : QmlAsyncClassifier<ClassificationTag>
    {
        internal QmlSyntaxClassifier(
            ITextBuffer buffer)
            : base("Syntax", buffer)
        {
        }

        protected override ClassificationRefresh ProcessText(
            ITextSnapshot snapshot,
            Parser parseResult,
            SharedTagList tagList,
            bool writeAccess)
        {
            if (writeAccess) {
                foreach (var token in parseResult.Tokens) {
                    tagList.AddRange(this, QmlSyntaxTag.GetClassification(snapshot, token));
                }
                foreach (var node in parseResult.AstNodes) {
                    tagList.AddRange(this, QmlSyntaxTag.GetClassification(snapshot, node));
                }
            }

            var parsedCorrectly = parseResult.ParsedCorrectly;
            return parsedCorrectly ? ClassificationRefresh.FullText : ClassificationRefresh.TagsOnly;
        }

        protected override ClassificationTag GetClassification(TrackingTag tag)
        {
            if (tag is QmlSyntaxTag {ClassificationType: {}} syntaxTag)
                return new ClassificationTag(syntaxTag.ClassificationType);
            return null;

        }
    }
}
