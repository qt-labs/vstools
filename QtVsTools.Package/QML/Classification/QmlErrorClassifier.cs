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
