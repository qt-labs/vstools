/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace QtVsTools.Qml.Classification
{
    using QtVsTools.VisualStudio;
    using Syntax;

    [Export(typeof(IViewTaggerProvider))]
    [ContentType("qml")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class QmlExpressionEvalProvider : IViewTaggerProvider
    {
        public const string ClassificationType = QmlExpressionEval.ClassificationType;

        [Import]
        internal IClassificationTypeRegistryService classificationTypeRegistry = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(ClassificationType)]
        internal static ClassificationTypeDefinition qmlDebug = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            QmlClassificationType.InitClassificationTypes(classificationTypeRegistry);
            if (!QmlClassificationType.ClassificationTypes.ContainsKey(ClassificationType)) {
                QmlClassificationType.ClassificationTypes.Add(ClassificationType,
                    classificationTypeRegistry.GetClassificationType(ClassificationType));
            }

            return QmlExpressionEval.Create(textView, buffer) as ITagger<T>;
        }
    }

    internal sealed class QmlExpressionEval :
        QmlAsyncClassifier<ClassificationTag>,
        IOleCommandTarget,
        IVsTextViewFilter
    {
        public const string ClassificationType = "expr_eval.qml";

        ITextView textView;
        ITextBuffer buffer;
        IVsDebugger debugger;
        IVsTextView vsTextView;
        IVsTextLines textLines;
        IOleCommandTarget nextTarget;

        public static QmlExpressionEval Create(ITextView textView, ITextBuffer buffer)
        {
            var _this = new QmlExpressionEval(textView, buffer);
            return _this.Initialize(textView, buffer) ? _this : null;
        }

        private QmlExpressionEval(ITextView textView, ITextBuffer buffer)
            : base(ClassificationType, textView, buffer)
        { }

        private bool Initialize(ITextView textView, ITextBuffer buffer)
        {
            this.textView = textView;
            this.buffer = buffer;

            debugger = VsServiceProvider.GetService<IVsDebugger>();
            if (debugger == null)
                return false;

            var componentModel = VsServiceProvider
                .GetService<SComponentModel, IComponentModel>();
            if (componentModel == null)
                return false;

            var editorFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            if (editorFactory == null)
                return false;

            vsTextView = editorFactory.GetViewAdapter(textView);
            if (vsTextView == null)
                return false;

            if (vsTextView.GetBuffer(out textLines) != VSConstants.S_OK)
                return false;

            if (vsTextView.AddCommandFilter(this, out nextTarget) != VSConstants.S_OK)
                return false;

            textView.Closed += TextView_Closed;

            return true;
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            vsTextView.RemoveCommandFilter(this);
        }

        protected override ClassificationRefresh ProcessText(
            ITextSnapshot snapshot,
            Parser parseResult,
            SharedTagList tagList,
            bool writeAccess)
        {
            if (writeAccess) {
                var expressions = parseResult.AstNodes
                    .Where(x => x.Kind == AstNodeKind.FieldMemberExpression
                             || x.Kind == AstNodeKind.IdentifierExpression)
                    .GroupBy(x => x.FirstSourceLocation.Offset)
                    .Select(x => new
                    {
                        Offset = x.Key,
                        Length = x.Max(y =>
                            y.LastSourceLocation.Offset + y.LastSourceLocation.Length) - x.Key,
                        List = x.OrderBy(y =>
                            y.LastSourceLocation.Offset + y.LastSourceLocation.Length)
                    });
                tagList.AddRange(this, expressions
                    .Select(x => new ExprTrackingTag(snapshot, x.Offset, x.Length, x.List)));
            }
            return ClassificationRefresh.FullText;
        }

        protected override ClassificationTag GetClassification(TrackingTag tag)
        {
            if (tag is ExprTrackingTag debugTag)
                return new ExprTag(debugTag.Exprs, QmlClassificationType.Get(ClassificationType));
            return null;
        }

        int IVsTextViewFilter.GetDataTipText(TextSpan[] pSpan, out string pbstrText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            pbstrText = "";
            var dbgMode = new DBGMODE[1];
            if (debugger.GetMode(dbgMode) != VSConstants.S_OK
                || dbgMode[0] != DBGMODE.DBGMODE_Break) {
                return VSConstants.S_FALSE;
            }

            var startLine = buffer.CurrentSnapshot.GetLineFromLineNumber(pSpan[0].iStartLine);
            var offset = startLine.Start.Position + pSpan[0].iStartIndex;

            var spans = new NormalizedSnapshotSpanCollection(
                new SnapshotSpan(buffer.CurrentSnapshot, offset, 1));

            var tags = GetTags(spans).Select(x => x.Tag).Cast<ExprTag>();

            var expr = tags.SelectMany(x => x.Exprs)
                .Where(x => offset < x.LastSourceLocation.Offset + x.LastSourceLocation.Length)
                .FirstOrDefault();

            if (expr == null)
                return VSConstants.S_FALSE;

            var exprSpan = new Span(expr.FirstSourceLocation.Offset,
                expr.LastSourceLocation.Offset + expr.LastSourceLocation.Length
                - expr.FirstSourceLocation.Offset);
            var exprText = buffer.CurrentSnapshot.GetText(exprSpan);

            return debugger.GetDataTipValue(textLines, pSpan, exprText, out pbstrText);
        }

        int IVsTextViewFilter.GetWordExtent(int iLine, int iIndex, uint dwFlags, TextSpan[] pSpan)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTextViewFilter.GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IOleCommandTarget.QueryStatus(
            ref Guid pguidCmdGroup,
            uint cCmds,
            OLECMD[] prgCmds,
            IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return nextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        int IOleCommandTarget.Exec(
            ref Guid pguidCmdGroup,
            uint nCmdID,
            uint nCmdexecopt,
            IntPtr pvaIn,
            IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        class ExprTrackingTag : TrackingTag
        {
            public IOrderedEnumerable<AstNode> Exprs { get; }

            public ExprTrackingTag(
                ITextSnapshot snapshot,
                int offset,
                int length,
                IOrderedEnumerable<AstNode> exprs)
                : base(snapshot, offset, length)
            {
                Exprs = exprs;
            }
        }

        class ExprTag : ClassificationTag
        {
            public IOrderedEnumerable<AstNode> Exprs { get; }

            public ExprTag(IOrderedEnumerable<AstNode> exprs, IClassificationType type)
                : base(type)
            {
                Exprs = exprs;
            }
        }
    }
}
