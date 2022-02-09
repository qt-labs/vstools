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

/// This file contains the integration with the Qt Declarative parser.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace QtVsTools.Qml
{
    using Syntax;

    /// <summary>
    /// Implements the integration with the Qt declarative parser, including:
    ///   * Managed-unmanaged interop with the vsqml DLL;
    ///   * Unmarshaling of syntax element data types (e.g. AST nodes);
    ///   * Visitor role for AST traversal.
    /// </summary>
    class Parser : IDisposable
    {
        internal static class NativeMethods
        {
            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlGetTokens")]
            internal static extern bool GetTokens(
                IntPtr qmlText,
                int qmlTextLength,
                ref IntPtr tokens,
                ref int tokensLength);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlFreeTokens")]
            internal static extern bool FreeTokens(IntPtr tokens);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlParse")]
            internal static extern bool Parse(
                IntPtr qmlText,
                int qmlTextLength,
                ref IntPtr parser,
                ref bool parsedCorrectly,
                ref IntPtr diagnosticMessages,
                ref int diagnosticMessagesLength,
                ref IntPtr comments,
                ref int commentsLength);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlFreeParser")]
            internal static extern bool FreeParser(IntPtr parser);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlFreeDiagnosticMessages")]
            internal static extern bool FreeDiagnosticMessages(IntPtr diagnosticMessages);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlFreeComments")]
            internal static extern bool FreeComments(IntPtr comments);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlGetAstVisitor")]
            internal static extern IntPtr GetAstVisitor();

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlFreeAstVisitor")]
            internal static extern bool FreeAstVisitor(IntPtr astVisitor);

            internal delegate bool Callback(
                IntPtr astVisitor,
                int nodeKind,
                IntPtr node,
                bool beginVisit,
                IntPtr nodeData,
                int nodeDataLength);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlSetAstVisitorCallback")]
            internal static extern bool SetAstVisitorCallback(
                IntPtr astVisitor,
                int nodeKindFilter,
                Callback visitCallback);

            [DllImport("vsqml",
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "qmlAcceptAstVisitor")]
            internal static extern bool AcceptAstVisitor(
                IntPtr parser,
                IntPtr node,
                IntPtr astVisitor);
        }

        /// <summary>
        /// List of "interesting" AST node types. To optimize the managed-unmanaged interop,
        /// during AST traversal, only these node types will be reported.
        /// </summary>
        static readonly List<AstNodeKind> CallbackFilters =
            new List<AstNodeKind> {
                AstNodeKind.UiImport,
                AstNodeKind.UiQualifiedId,
                AstNodeKind.UiObjectDefinition,
                AstNodeKind.UiObjectBinding,
                AstNodeKind.UiObjectInitializer,
                AstNodeKind.UiScriptBinding,
                AstNodeKind.UiArrayBinding,
                AstNodeKind.UiPublicMember,
                AstNodeKind.FieldMemberExpression,
                AstNodeKind.IdentifierExpression
            };

        IntPtr qmlTextPtr = IntPtr.Zero;
        IntPtr qmlParserPtr = IntPtr.Zero;
        readonly List<Token> tokens;
        public IEnumerable<Token> Tokens
        {
            get { return tokens; }
        }

        readonly List<DiagnosticMessage> diagnosticMessages;
        public IEnumerable<DiagnosticMessage> DiagnosticMessages
        {
            get { return diagnosticMessages; }
        }

        private int FirstErrorOffset { get; set; }

        readonly List<AstNode> visitedNodes;
        public IEnumerable<AstNode> AstNodes { get { return visitedNodes; } }

        public bool ParsedCorrectly { get; private set; }

        readonly Dictionary<IntPtr, AstNode> nodesBytPtr;
        readonly Dictionary<IntPtr, List<KeyValuePair<AstNode, PropertyInfo>>> pendingDereferences;

        Parser()
        {
            tokens = new List<Token>();
            diagnosticMessages = new List<DiagnosticMessage>();
            nodesBytPtr = new Dictionary<IntPtr, AstNode>();
            pendingDereferences = new Dictionary<IntPtr,
                List<KeyValuePair<AstNode, PropertyInfo>>>();

            visitedNodes = new List<AstNode>();
        }

        public static Parser Parse(string qmlText)
        {
            var parser = new Parser();
            parser.Work(qmlText);
            return parser;
        }

        void Work(string qmlText)
        {
            // The Qt Declarative parser ignores CR's. However, the Visual Studio editor does not.
            // To ensure that offsets are compatible, CR's are replaced with spaces.
            string qmlTextNormalized = qmlText.Replace('\r', ' ');
            var qmlTextData = Encoding.UTF8.GetBytes(qmlTextNormalized);

            qmlTextPtr = Marshal.AllocHGlobal(qmlTextData.Length);
            Marshal.Copy(qmlTextData, 0, qmlTextPtr, qmlTextData.Length);

            IntPtr tokensPtr = IntPtr.Zero;
            int tokensLength = 0;

            NativeMethods.GetTokens(qmlTextPtr, qmlTextData.Length,
                ref tokensPtr, ref tokensLength);

            if (tokensPtr != IntPtr.Zero) {
                var tokensData = new byte[tokensLength];
                Marshal.Copy(tokensPtr, tokensData, 0, tokensLength);

                using (var rdr = new BinaryReader(new MemoryStream(tokensData))) {
                    while (rdr.BaseStream.Position + (3 * sizeof(int)) <= tokensLength) {
                        int kind = rdr.ReadInt32();
                        int offset = rdr.ReadInt32();
                        int length = rdr.ReadInt32();
                        tokens.Add(Token.Create((TokenKind)kind, offset, length));
                    }
                }
                NativeMethods.FreeTokens(tokensPtr);
            }

            bool parsedCorrectly = false;
            IntPtr diagnosticMessagesPtr = IntPtr.Zero;
            int diagnosticMessagesLength = 0;
            IntPtr commentsPtr = IntPtr.Zero;
            int commentsLength = 0;

            NativeMethods.Parse(qmlTextPtr, qmlTextData.Length,
                ref qmlParserPtr, ref parsedCorrectly,
                ref diagnosticMessagesPtr, ref diagnosticMessagesLength,
                ref commentsPtr, ref commentsLength);

            ParsedCorrectly = parsedCorrectly;

            if (diagnosticMessagesPtr != IntPtr.Zero) {
                var diagnosticMessagesData = new byte[diagnosticMessagesLength];
                Marshal.Copy(
                    diagnosticMessagesPtr,
                    diagnosticMessagesData, 0, diagnosticMessagesLength);

                FirstErrorOffset = qmlTextNormalized.Length + 1;
                using (var rdr = new BinaryReader(new MemoryStream(diagnosticMessagesData))) {
                    while (rdr.BaseStream.Position + (3 * sizeof(int)) <= diagnosticMessagesLength) {
                        var kind = (DiagnosticMessageKind)rdr.ReadInt32();
                        int offset = rdr.ReadInt32();
                        int length = rdr.ReadInt32();
                        diagnosticMessages.Add(new DiagnosticMessage(kind, offset, length));
                        if (kind == DiagnosticMessageKind.Error && offset < FirstErrorOffset)
                            FirstErrorOffset = offset;
                    }
                }
                NativeMethods.FreeDiagnosticMessages(diagnosticMessagesPtr);
            }

            if (commentsPtr != IntPtr.Zero) {
                var commentsData = new byte[commentsLength];
                Marshal.Copy(commentsPtr, commentsData, 0, commentsLength);

                using (var rdr = new BinaryReader(new MemoryStream(commentsData))) {
                    while (rdr.BaseStream.Position + (2 * sizeof(int)) <= commentsLength) {
                        int offset = rdr.ReadInt32();
                        int length = rdr.ReadInt32();
                        tokens.Add(Token.Create(TokenKind.T_COMMENT, offset, length));
                    }
                }
                NativeMethods.FreeComments(commentsPtr);
            }

            var astVisitor = NativeMethods.GetAstVisitor();
            var callback = new NativeMethods.Callback(VisitorCallback);

            foreach (var callbackFilter in CallbackFilters)
                NativeMethods.SetAstVisitorCallback(astVisitor, (int)callbackFilter, callback);

            NativeMethods.AcceptAstVisitor(qmlParserPtr, IntPtr.Zero, astVisitor);

            while (pendingDereferences.Count > 0) {
                var deref = pendingDereferences.First();
                NativeMethods.AcceptAstVisitor(qmlParserPtr, deref.Key, astVisitor);
                pendingDereferences.Remove(deref.Key);
            }

            GC.KeepAlive(callback);
            NativeMethods.FreeAstVisitor(astVisitor);
        }

        SourceLocation UnmarshalLocation(BinaryReader nodeData)
        {
            try {
                return new SourceLocation
                {
                    Offset = nodeData.ReadInt32(),
                    Length = nodeData.ReadInt32(),
                };
            } catch (Exception) {
                return new SourceLocation();
            }
        }

        void UnmarshalPointer(BinaryReader nodeData, AstNode node, PropertyInfo nodeProperty)
        {
            if (nodeData == null || node == null || nodeProperty == null)
                return;

            IntPtr ptrRef;
            try {
                long ptrHi = nodeData.ReadInt32();
                long ptrLo = nodeData.ReadInt32();
                ptrRef = new IntPtr((ptrHi << 32) | (ptrLo & 0xFFFFFFFFL));
            } catch (Exception) {
                return;
            }

            if (ptrRef == IntPtr.Zero)
                return;

            if (nodesBytPtr.TryGetValue(ptrRef, out AstNode nodeRef)) {
                nodeProperty.SetValue(node, nodeRef);
            } else {
                List<KeyValuePair<AstNode, PropertyInfo>> pendingRefList;
                if (!pendingDereferences.TryGetValue(ptrRef, out pendingRefList)) {
                    pendingDereferences[ptrRef] = pendingRefList =
                        new List<KeyValuePair<AstNode, PropertyInfo>>();
                }
                pendingRefList.Add(new KeyValuePair<AstNode, PropertyInfo>(node, nodeProperty));
            }
        }

        void UnmarshalNode(BinaryReader nodeData, AstNode node)
        {
            node.FirstSourceLocation = UnmarshalLocation(nodeData);
            node.LastSourceLocation = UnmarshalLocation(nodeData);
        }

        AstNode UnmarshalNode(BinaryReader nodeData, AstNodeKind nodeKind)
        {
            var node = new AstNode(nodeKind);
            UnmarshalNode(nodeData, node);
            return node;
        }

        UiImport UnmarshalUiImport(BinaryReader nodeData)
        {
            var node = new UiImport();
            UnmarshalNode(nodeData, node);
            node.ImportToken = UnmarshalLocation(nodeData);
            node.FileNameToken = UnmarshalLocation(nodeData);
            node.VersionToken = UnmarshalLocation(nodeData);
            node.AsToken = UnmarshalLocation(nodeData);
            node.ImportIdToken = UnmarshalLocation(nodeData);
            node.SemicolonToken = UnmarshalLocation(nodeData);
            return node;
        }

        UiQualifiedId UnmarshalUiQualifiedId(BinaryReader nodeData)
        {
            var node = new UiQualifiedId();
            UnmarshalNode(nodeData, node);
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.Next));
            node.IdentifierToken = UnmarshalLocation(nodeData);
            return node;
        }

        UiObjectDefinition UnmarshalUiObjectDefinition(BinaryReader nodeData)
        {
            var node = new UiObjectDefinition();
            UnmarshalNode(nodeData, node);
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.QualifiedTypeNameId));
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.Initializer));
            return node;
        }

        UiObjectBinding UnmarshalUiObjectBinding(BinaryReader nodeData)
        {
            var node = new UiObjectBinding();
            UnmarshalNode(nodeData, node);
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.QualifiedId));
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.QualifiedTypeNameId));
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.Initializer));
            node.ColonToken = UnmarshalLocation(nodeData);
            return node;
        }

        UiScriptBinding UnmarshalUiScriptBinding(BinaryReader nodeData)
        {
            var node = new UiScriptBinding();
            UnmarshalNode(nodeData, node);
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.QualifiedId));
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.Statement));
            node.ColonToken = UnmarshalLocation(nodeData);
            return node;
        }

        UiArrayBinding UnmarshalUiArrayBinding(BinaryReader nodeData)
        {
            var node = new UiArrayBinding();
            UnmarshalNode(nodeData, node);
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.QualifiedId));
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.Members));
            node.ColonToken = UnmarshalLocation(nodeData);
            node.LBracketToken = UnmarshalLocation(nodeData);
            node.RBracketToken = UnmarshalLocation(nodeData);
            return node;
        }

        UiPublicMember UnmarshalUiPublicMember(BinaryReader nodeData)
        {
            var node = new UiPublicMember();
            UnmarshalNode(nodeData, node);
            node.Type = (UiPublicMemberType)nodeData.ReadInt32();
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.MemberType));
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.Statement));
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.Binding));
            node.IsDefaultMember = (nodeData.ReadInt32() != 0);
            node.IsReadonlyMember = (nodeData.ReadInt32() != 0);
            UnmarshalPointer(nodeData, node, GetPropertyRef(() => node.Parameters));
            node.DefaultToken = UnmarshalLocation(nodeData);
            node.ReadonlyToken = UnmarshalLocation(nodeData);
            node.PropertyToken = UnmarshalLocation(nodeData);
            node.TypeModifierToken = UnmarshalLocation(nodeData);
            node.TypeToken = UnmarshalLocation(nodeData);
            node.IdentifierToken = UnmarshalLocation(nodeData);
            node.ColonToken = UnmarshalLocation(nodeData);
            node.SemicolonToken = UnmarshalLocation(nodeData);
            return node;
        }

        /// <summary>
        /// This delegate method is called during the AST traversal when entering/exiting a node.
        /// </summary>
        /// <param name="astVisitor">Unmanaged AST visitor object ref</param>
        /// <param name="nodeKind">Type of node being visited</param>
        /// <param name="nodePtr">Node object ref</param>
        /// <param name="beginVisit">"true" when entering node, "false" when exiting</param>
        /// <param name="nodeDataPtr">Serialized content of AST node</param>
        /// <param name="nodeDataLength">Length in bytes of serialized content</param>
        /// <returns></returns>
        bool VisitorCallback(
            IntPtr astVisitor,
            int nodeKind,
            IntPtr nodePtr,
            bool beginVisit,
            IntPtr nodeDataPtr,
            int nodeDataLength)
        {
            if (!beginVisit)
                return true;

            AstNode node = null;
            var nodeData = new byte[nodeDataLength];
            Marshal.Copy(nodeDataPtr, nodeData, 0, nodeDataLength);
            using (var rdr = new BinaryReader(new MemoryStream(nodeData))) {
                switch ((AstNodeKind)nodeKind) {
                case AstNodeKind.UiImport:
                    node = UnmarshalUiImport(rdr);
                    break;
                case AstNodeKind.UiQualifiedId:
                    node = UnmarshalUiQualifiedId(rdr);
                    break;
                case AstNodeKind.UiObjectDefinition:
                    node = UnmarshalUiObjectDefinition(rdr);
                    break;
                case AstNodeKind.UiObjectBinding:
                    node = UnmarshalUiObjectBinding(rdr);
                    break;
                case AstNodeKind.UiScriptBinding:
                    node = UnmarshalUiScriptBinding(rdr);
                    break;
                case AstNodeKind.UiArrayBinding:
                    node = UnmarshalUiArrayBinding(rdr);
                    break;
                case AstNodeKind.UiPublicMember:
                    node = UnmarshalUiPublicMember(rdr);
                    break;
                default:
                    node = UnmarshalNode(rdr, (AstNodeKind)nodeKind);
                    break;
                }
            }
            if (node == null)
                return true;

            visitedNodes.Add(node);
            nodesBytPtr[nodePtr] = node;

            List<KeyValuePair<AstNode, PropertyInfo>> derefs;
            if (pendingDereferences.TryGetValue(nodePtr, out derefs)) {
                foreach (var deref in derefs) {
                    try {
                        deref.Value.SetValue(deref.Key, node);
                    } catch (Exception) { }
                }
                pendingDereferences.Remove(nodePtr);
            }

            return true;
        }

        void FreeManaged()
        {
        }

        void FreeUnmanaged()
        {
            if (qmlParserPtr != IntPtr.Zero) {
                NativeMethods.FreeParser(qmlParserPtr);
                qmlParserPtr = IntPtr.Zero;
            }
            if (qmlTextPtr != IntPtr.Zero) {
                Marshal.FreeHGlobal(qmlTextPtr);
                qmlTextPtr = IntPtr.Zero;
            }
        }

        #region IDisposable
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                FreeManaged();

            FreeUnmanaged();

            disposed = true;
        }

        ~Parser()
        {
            Dispose(false);
        }
        #endregion

        /// <summary>
        ///     Get a reference to a static or instance class member from a member access lambda.
        ///     Adapted from:
        ///         https://stackoverflow.com/questions/2820660/get-name-of-property-as-a-string
        /// </summary>
        /// <param name="memberLambda">
        ///     Lambda expression of the form: '() => Class.Member'or '() => object.Member'
        /// </param>
        /// <returns>Reference to the class member</returns>
        public static MemberInfo GetMemberRef<T>(Expression<Func<T>> memberLambda)
        {
            var me = memberLambda.Body as MemberExpression;
            if (me == null)
                return null;
            return me.Member;
        }

        public static PropertyInfo GetPropertyRef<T>(Expression<Func<T>> memberLambda)
        {
            return GetMemberRef(memberLambda) as PropertyInfo;
        }
    }
}
