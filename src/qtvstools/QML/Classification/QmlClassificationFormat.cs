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
