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
    /// Represents the classification of a QML syntax element
    /// </summary>
    public class QmlTag : ITag
    {
        public const string Keyword = "keyword.qml";
        public const string Numeric = "numeric.qml";
        public const string String = "string.qml";
        public const string Comment = "comment.qml";
        public const string TypeName = "typename.qml";
        public const string Binding = "binding.qml";

        public SyntaxElement SyntaxElement { get; private set; }
        public SourceLocation SourceLocation { get; private set; }
        public ITrackingSpan Span { get; private set; }
        public IClassificationType ClassificationType { get; private set; }

        private QmlTag(ITextSnapshot snapshot, SourceLocation location)
        {
            SourceLocation = location;
            Span = snapshot.CreateTrackingSpan(
                location.Offset, location.Length, SpanTrackingMode.EdgeExclusive);
        }

        public QmlTag(
            ITextSnapshot snapshot,
            SyntaxElement element,
            string classificationType,
            SourceLocation location)
            : this(snapshot, location)
        {
            SyntaxElement = element;
            ClassificationType = QmlClassificationType.Get(classificationType);
        }

        public ITagSpan<QmlTag> ToTagSpan(ITextSnapshot snapshot)
        {
            return new TagSpan<QmlTag>(Span.GetSpan(snapshot), this);
        }

        static QmlTag GetClassificationTag(
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

            return new QmlTag(snapshot, parentNode, classificationType, fullNameLocation);
        }

        public static readonly HashSet<string> QmlBasicTypes = new HashSet<string> {
            "bool", "double", "enumeration", "int",
            "list", "real", "string", "url", "var",
            "date", "point", "rect", "size", "alias"
        };

        public static IEnumerable<QmlTag> GetClassification(
            ITextSnapshot snapshot,
            SyntaxElement element)
        {
            var tags = new List<QmlTag>();

            if (element is KeywordToken) {
                var token = element as KeywordToken;
                tags.Add(new QmlTag(snapshot, token, Keyword, token.Location));

            } else if (element is NumberToken) {
                var token = element as NumberToken;
                tags.Add(new QmlTag(snapshot, token, Numeric, token.Location));

            } else if (element is StringToken) {
                var token = element as StringToken;
                tags.Add(new QmlTag(snapshot, token, String, token.Location));

            } else if (element is CommentToken) {
                var token = element as CommentToken;
                // QML parser does not report the initial/final tokens of comments
                var commentStart = snapshot.GetText(token.Location.Offset - 2, 2);
                var commentLocation = token.Location;
                if (commentStart == "//") {
                    commentLocation.Offset -= 2;
                    commentLocation.Length += 2;
                } else {
                    commentLocation.Offset -= 2;
                    commentLocation.Length += 4;
                }
                tags.Add(new QmlTag(snapshot, token, Comment, commentLocation));

            } else if (element is UiImport) {
                var node = element as UiImport;
                if (node.ImportIdToken.Length > 0)
                    tags.Add(new QmlTag(snapshot, node, TypeName, node.ImportIdToken));

            } else if (element is UiObjectDefinition) {
                var node = element as UiObjectDefinition;
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

            } else if (element is UiObjectBinding) {
                var node = element as UiObjectBinding;
                if (node.QualifiedId != null) {
                    tags.Add(GetClassificationTag(
                        snapshot, node, Binding, node.QualifiedId));
                }
                if (node.QualifiedTypeNameId != null) {
                    tags.Add(GetClassificationTag(
                        snapshot, node, TypeName, node.QualifiedTypeNameId));
                }

            } else if (element is UiScriptBinding) {
                var node = element as UiScriptBinding;
                var qualifiedId = node.QualifiedId;
                while (qualifiedId != null) {
                    tags.Add(GetClassificationTag(snapshot, node, Binding, qualifiedId));
                    qualifiedId = qualifiedId.Next;
                }

            } else if (element is UiArrayBinding) {
                var node = element as UiArrayBinding;
                var qualifiedId = node.QualifiedId;
                while (qualifiedId != null) {
                    tags.Add(GetClassificationTag(snapshot, node, Binding, qualifiedId));
                    qualifiedId = qualifiedId.Next;
                }

            } else if (element is UiPublicMember) {
                var node = element as UiPublicMember;
                if (node.Type == UiPublicMemberType.Property && node.TypeToken.Length > 0) {
                    var typeName = snapshot.GetText(node.TypeToken);
                    if (QmlBasicTypes.Contains(typeName))
                        tags.Add(new QmlTag(snapshot, node, Keyword, node.TypeToken));
                    else
                        tags.Add(new QmlTag(snapshot, node, TypeName, node.TypeToken));
                }
                if (node.IdentifierToken.Length > 0)
                    tags.Add(new QmlTag(snapshot, node, Binding, node.IdentifierToken));

            }
            return tags;
        }
    }

    public class QmlDiagnosticsTag : ITag
    {
        public DiagnosticMessage DiagnosticMessage { get; private set; }
        public ITrackingSpan Span { get; private set; }
        public QmlDiagnosticsTag(ITextSnapshot snapshot, DiagnosticMessage diagnosticMessage)
        {
            DiagnosticMessage = diagnosticMessage;
            Span = snapshot.CreateTrackingSpan(
                diagnosticMessage.Location.Offset, diagnosticMessage.Location.Length,
                SpanTrackingMode.EdgeExclusive);
        }
        public ITagSpan<QmlDiagnosticsTag> ToTagSpan(ITextSnapshot snapshot)
        {
            return new TagSpan<QmlDiagnosticsTag>(Span.GetSpan(snapshot), this);
        }
    }

    internal static class QmlClassificationType
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlTag.Keyword)]
        internal static ClassificationTypeDefinition qmlKeyword = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlTag.Numeric)]
        internal static ClassificationTypeDefinition qmlNumber = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlTag.String)]
        internal static ClassificationTypeDefinition qmlString = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlTag.Comment)]
        internal static ClassificationTypeDefinition qmlComment = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlTag.TypeName)]
        internal static ClassificationTypeDefinition qmlTypeName = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(QmlTag.Binding)]
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
                { QmlTag.Keyword, typeService.GetClassificationType(QmlTag.Keyword) },
                { QmlTag.Numeric, typeService.GetClassificationType(QmlTag.Numeric) },
                { QmlTag.String, typeService.GetClassificationType(QmlTag.String) },
                { QmlTag.TypeName, typeService.GetClassificationType(QmlTag.TypeName) },
                { QmlTag.Binding, typeService.GetClassificationType(QmlTag.Binding) },

                // QML comments are mapped to the Visual Studio pre-defined comment classification
                { QmlTag.Comment,
                    typeService.GetClassificationType(PredefinedClassificationTypeNames.Comment) }
            };
        }
        public static IClassificationType Get(string classificationType)
        {
            if (ClassificationTypes == null)
                return null;
            return ClassificationTypes[classificationType];
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
