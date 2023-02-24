/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

/// This file contains the classification of the syntax elements recognized by the QML parser.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace QtVsTools.Qml.Classification
{
    using Syntax;
    using VisualStudio.Text.Extensions;

    /// <summary>
    /// Represents a classification tag that can be mapped onto future versions of the source code
    /// </summary>
    public class TrackingTag : ITag
    {
        public ITextSnapshot Snapshot { get; }
        public int Start { get; }
        private int Length { get; }
        public ITrackingSpan Span { get; }
        public TrackingTag(ITextSnapshot snapshot, int start, int length)
        {
            Snapshot = snapshot;
            Start = start;
            Length = length;
            Span = snapshot.CreateTrackingSpan(start, length, SpanTrackingMode.EdgeExclusive);
        }
        public ITagSpan<TrackingTag> MapToSnapshot(ITextSnapshot snapshot)
        {
            return new TagSpan<TrackingTag>(Span.GetSpan(snapshot), this);
        }
    }

    /// <summary>
    /// Represents the classification of a QML syntax element
    /// </summary>
    public class QmlSyntaxTag : TrackingTag
    {
        public const string Keyword = "keyword.qml";
        public const string Numeric = "numeric.qml";
        public const string String = "string.qml";
        public const string Comment = "comment.qml";
        public const string TypeName = "typename.qml";
        public const string Binding = "binding.qml";

        private SyntaxElement SyntaxElement { get; }
        private SourceLocation SourceLocation { get; }
        public IClassificationType ClassificationType { get; }

        private QmlSyntaxTag(ITextSnapshot snapshot, SourceLocation location)
            : base(snapshot, location.Offset, location.Length)
        {
            SourceLocation = location;
        }

        public QmlSyntaxTag(
            ITextSnapshot snapshot,
            SyntaxElement element,
            string classificationType,
            SourceLocation location)
            : this(snapshot, location)
        {
            SyntaxElement = element;
            ClassificationType = QmlClassificationType.Get(classificationType);
        }

        static QmlSyntaxTag GetClassificationTag(
            ITextSnapshot snapshot,
            AstNode parentNode,
            string classificationType,
            UiQualifiedId qualifiedId)
        {
            var firstName = qualifiedId.IdentifierToken;
            var lastName = qualifiedId.IdentifierToken;
            while (qualifiedId.Next != null) {
                qualifiedId = qualifiedId.Next;
                lastName = qualifiedId.IdentifierToken;
            }
            var fullNameLocation = new SourceLocation
            {
                Offset = firstName.Offset,
                Length = lastName.Offset + lastName.Length - firstName.Offset
            };

            return new QmlSyntaxTag(snapshot, parentNode, classificationType, fullNameLocation);
        }

        private static readonly HashSet<string> QmlBasicTypes = new HashSet<string> {
            "bool", "double", "enumeration", "int",
            "list", "real", "string", "url", "var",
            "date", "point", "rect", "size", "alias"
        };

        public static IEnumerable<QmlSyntaxTag> GetClassification(
            ITextSnapshot snapshot,
            SyntaxElement element)
        {
            var tags = new List<QmlSyntaxTag>();

            switch (element) {
            case KeywordToken token:
                tags.Add(new QmlSyntaxTag(snapshot, token, Keyword, token.Location));
                break;
            case NumberToken token:
                tags.Add(new QmlSyntaxTag(snapshot, token, Numeric, token.Location));
                break;
            case StringToken token:
                tags.Add(new QmlSyntaxTag(snapshot, token, String, token.Location));
                break;
            case CommentToken token: {
                    // QML parser does not report the initial/final tokens of comments
                    var commentStart = snapshot.GetText(token.Location.Offset - 2, 2);
                    var commentLocation = token.Location;
                    commentLocation.Offset -= 2;
                    commentLocation.Length += commentStart == "//" ? 2 : 4;
                    tags.Add(new QmlSyntaxTag(snapshot, token, Comment, commentLocation));
                    break;
                }
            case UiImport node:
                if (node.ImportIdToken.Length > 0)
                    tags.Add(new QmlSyntaxTag(snapshot, node, TypeName, node.ImportIdToken));
                break;
            case UiObjectDefinition node:
                if (node.QualifiedTypeNameId != null) {
                    var name = snapshot.GetText(node.QualifiedTypeNameId.IdentifierToken);
                    // an UiObjectDefinition may be used to group property bindings
                    // think anchors { ... }
                    bool isGroupedBinding = !string.IsNullOrEmpty(name) && char.IsLower(name[0]);
                    if (!isGroupedBinding) {
                        tags.Add(GetClassificationTag(
                            snapshot, node, TypeName, node.QualifiedTypeNameId));
                    } else {
                        tags.Add(GetClassificationTag(
                            snapshot, node, Binding, node.QualifiedTypeNameId));
                    }
                }
                break;
            case UiObjectBinding node:
                if (node.QualifiedId != null) {
                    tags.Add(GetClassificationTag(
                        snapshot, node, Binding, node.QualifiedId));
                }
                if (node.QualifiedTypeNameId != null) {
                    tags.Add(GetClassificationTag(
                        snapshot, node, TypeName, node.QualifiedTypeNameId));
                }
                break;
            case UiScriptBinding node: {
                    var qualifiedId = node.QualifiedId;
                    while (qualifiedId != null) {
                        tags.Add(GetClassificationTag(snapshot, node, Binding, qualifiedId));
                        qualifiedId = qualifiedId.Next;
                    }
                    break;
                }
            case UiArrayBinding node: {
                    var qualifiedId = node.QualifiedId;
                    while (qualifiedId != null) {
                        tags.Add(GetClassificationTag(snapshot, node, Binding, qualifiedId));
                        qualifiedId = qualifiedId.Next;
                    }
                    break;
                }
            case UiPublicMember node:
                if (node is { Type: UiPublicMemberType.Property, TypeToken: { Length: > 0 } }) {
                    var typeName = snapshot.GetText(node.TypeToken);
                    if (QmlBasicTypes.Contains(typeName))
                        tags.Add(new QmlSyntaxTag(snapshot, node, Keyword, node.TypeToken));
                    else
                        tags.Add(new QmlSyntaxTag(snapshot, node, TypeName, node.TypeToken));
                }
                if (node.IdentifierToken.Length > 0)
                    tags.Add(new QmlSyntaxTag(snapshot, node, Binding, node.IdentifierToken));
                break;
            }

            return tags;
        }
    }

    public class QmlDiagnosticsTag : TrackingTag
    {
        private DiagnosticMessage DiagnosticMessage { get; }
        public QmlDiagnosticsTag(ITextSnapshot snapshot, DiagnosticMessage diagnosticMessage)
            : base(snapshot, diagnosticMessage.Location.Offset, diagnosticMessage.Location.Length)
        {
            DiagnosticMessage = diagnosticMessage;
        }
    }

    internal static class QmlClassificationType
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlSyntaxTag.Keyword)]
        internal static ClassificationTypeDefinition qmlKeyword = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlSyntaxTag.Numeric)]
        internal static ClassificationTypeDefinition qmlNumber = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlSyntaxTag.String)]
        internal static ClassificationTypeDefinition qmlString = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlSyntaxTag.Comment)]
        internal static ClassificationTypeDefinition qmlComment = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlSyntaxTag.TypeName)]
        internal static ClassificationTypeDefinition qmlTypeName = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlSyntaxTag.Binding)]
        internal static ClassificationTypeDefinition qmlBinding = null;

        public static IDictionary<string, IClassificationType> ClassificationTypes
        {
            get; private set;
        }

        public static void InitClassificationTypes(IClassificationTypeRegistryService typeService)
        {
            if (ClassificationTypes != null)
                return;
            ClassificationTypes = new Dictionary<string, IClassificationType>
            {
                { QmlSyntaxTag.Keyword,
                    typeService.GetClassificationType(QmlSyntaxTag.Keyword) },
                { QmlSyntaxTag.Numeric,
                    typeService.GetClassificationType(QmlSyntaxTag.Numeric) },
                { QmlSyntaxTag.String,
                    typeService.GetClassificationType(QmlSyntaxTag.String) },
                { QmlSyntaxTag.TypeName,
                    typeService.GetClassificationType(QmlSyntaxTag.TypeName) },
                { QmlSyntaxTag.Binding,
                    typeService.GetClassificationType(QmlSyntaxTag.Binding) },

                // QML comments are mapped to the Visual Studio pre-defined comment classification
                { QmlSyntaxTag.Comment,
                    typeService.GetClassificationType(PredefinedClassificationTypeNames.Comment) }
            };
        }
        public static IClassificationType Get(string classificationType)
        {
            return ClassificationTypes?[classificationType];
        }
    }

    namespace VisualStudio.Text.Extensions
    {
        public static class TextSnapshotExtensions
        {
            public static string GetText(this ITextSnapshot _this, SourceLocation sourceLocation)
            {
                if (sourceLocation.Length == 0)
                    return string.Empty;
                if (_this.Length < sourceLocation.Offset + sourceLocation.Length)
                    return string.Empty;
                try {
                    return _this.GetText(sourceLocation.Offset, sourceLocation.Length);
                } catch (Exception) {
                    return string.Empty;
                }
            }
        }
    }
}
