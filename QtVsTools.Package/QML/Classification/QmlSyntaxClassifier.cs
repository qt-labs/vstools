/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
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
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("qml")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class QmlSyntaxClassifierProvider : IViewTaggerProvider
    {
        [Export]
        [Name("qml")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition qmlContentType = null;

        [Export]
        [FileExtension(".qml")]
        [ContentType("qml")]
        internal static FileExtensionToContentTypeDefinition qmlFileType = null;

        [Export]
        [FileExtension(".qmlproject")]
        [ContentType("qml")]
        internal static FileExtensionToContentTypeDefinition qmlprojectFileType = null;

        [Import]
        internal IClassificationTypeRegistryService classificationTypeRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            QmlClassificationType.InitClassificationTypes(classificationTypeRegistry);
            return new QmlSyntaxClassifier(textView, buffer) as ITagger<T>;
        }
    }

    internal sealed class QmlSyntaxClassifier : QmlAsyncClassifier<ClassificationTag>
    {
        internal QmlSyntaxClassifier(
            ITextView textView,
            ITextBuffer buffer)
            : base("Syntax", textView, buffer)
        {
        }

        protected override ClassificationRefresh ProcessText(
            ITextSnapshot snapshot,
            Parser parseResult,
            SharedTagList tagList,
            bool writeAccess)
        {
            bool parsedCorrectly = parseResult.ParsedCorrectly;

            if (writeAccess) {
                foreach (var token in parseResult.Tokens) {
                    tagList.AddRange(this, QmlSyntaxTag.GetClassification(snapshot, token));
                }
                foreach (var node in parseResult.AstNodes) {
                    tagList.AddRange(this, QmlSyntaxTag.GetClassification(snapshot, node));
                }
            }

            if (parsedCorrectly)
                return ClassificationRefresh.FullText;
            else
                return ClassificationRefresh.TagsOnly;
        }

        protected override ClassificationTag GetClassification(TrackingTag tag)
        {
            if (tag is QmlSyntaxTag syntaxTag && syntaxTag.ClassificationType != null)
                return new ClassificationTag(syntaxTag.ClassificationType);
            return null;

        }
    }
}
