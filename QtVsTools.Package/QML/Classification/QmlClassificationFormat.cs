/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

/// This file contains the definition of syntax highlighting formats.
/// These definitions can be modified at run-time by the user.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace QtVsTools.Qml.Classification
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = QmlSyntaxTag.Keyword)]
    [Name(QmlSyntaxTag.Keyword)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class QmlKeywordFormat : ClassificationFormatDefinition
    {
        public QmlKeywordFormat()
        {
            DisplayName = "QML Keyword";
            ForegroundColor = Color.FromRgb(86, 156, 214);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = QmlSyntaxTag.Numeric)]
    [Name(QmlSyntaxTag.Numeric)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class QmlNumberFormat : ClassificationFormatDefinition
    {
        public QmlNumberFormat()
        {
            DisplayName = "QML Number";
            ForegroundColor = Color.FromRgb(181, 206, 168);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = QmlSyntaxTag.String)]
    [Name(QmlSyntaxTag.String)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class QmlStringFormat : ClassificationFormatDefinition
    {
        public QmlStringFormat()
        {
            DisplayName = "QML String";
            ForegroundColor = Color.FromRgb(214, 157, 133);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = QmlSyntaxTag.TypeName)]
    [Name(QmlSyntaxTag.TypeName)]
    [UserVisible(true)]
    [Order(Before = Priority.Default, After = QmlSyntaxTag.Keyword)]
    internal sealed class QmlTypeNameFormat : ClassificationFormatDefinition
    {
        public QmlTypeNameFormat()
        {
            DisplayName = "QML Type Name";
            ForegroundColor = Color.FromRgb(78, 201, 176);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = QmlSyntaxTag.Binding)]
    [Name(QmlSyntaxTag.Binding)]
    [UserVisible(true)]
    [Order(Before = Priority.Default, After = QmlSyntaxTag.Keyword)]
    internal sealed class QmlBindingFormat : ClassificationFormatDefinition
    {
        public QmlBindingFormat()
        {
            DisplayName = "QML Binding";
            ForegroundColor = Color.FromRgb(183, 153, 185);
        }
    }
}
