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

/// The data types in this file represent nodes in the QML abstract sytax tree (AST).
/// They correspond to classes defined in qqmljsast_p.h

namespace QtVsTools.Qml.Syntax
{
    public enum AstNodeKind
    {
        #region Copied from qqmljsast_p.h
        Undefined,

        ArgumentList,
        ArrayPattern,
        ArrayMemberExpression,
        BinaryExpression,
        Block,
        BreakStatement,
        CallExpression,
        CaseBlock,
        CaseClause,
        CaseClauses,
        Catch,
        ConditionalExpression,
        ContinueStatement,
        DebuggerStatement,
        DefaultClause,
        DeleteExpression,
        DoWhileStatement,
        ElementList,
        Elision,
        EmptyStatement,
        Expression,
        ExpressionStatement,
        FalseLiteral,
        SuperLiteral,
        FieldMemberExpression,
        Finally,
        ForEachStatement,
        ForStatement,
        FormalParameterList,
        FunctionBody,
        FunctionDeclaration,
        FunctionExpression,
        ClassExpression,
        ClassDeclaration,
        IdentifierExpression,
        IdentifierPropertyName,
        ComputedPropertyName,
        IfStatement,
        LabelledStatement,
        NameSpaceImport,
        ImportSpecifier,
        ImportsList,
        NamedImports,
        ImportClause,
        FromClause,
        ImportDeclaration,
        Module,
        ExportSpecifier,
        ExportsList,
        ExportClause,
        ExportDeclaration,
        NewExpression,
        NewMemberExpression,
        NotExpression,
        NullExpression,
        YieldExpression,
        NumericLiteral,
        NumericLiteralPropertyName,
        ObjectPattern,
        PostDecrementExpression,
        PostIncrementExpression,
        PreDecrementExpression,
        PreIncrementExpression,
        Program,
        PropertyDefinitionList,
        PropertyGetterSetter,
        PropertyName,
        PropertyNameAndValue,
        RegExpLiteral,
        ReturnStatement,
        StatementList,
        StringLiteral,
        StringLiteralPropertyName,
        SwitchStatement,
        TemplateLiteral,
        TaggedTemplate,
        ThisExpression,
        ThrowStatement,
        TildeExpression,
        TrueLiteral,
        TryStatement,
        TypeOfExpression,
        UnaryMinusExpression,
        UnaryPlusExpression,
        VariableDeclaration,
        VariableDeclarationList,
        VariableStatement,
        VoidExpression,
        WhileStatement,
        WithStatement,
        NestedExpression,
        ClassElementList,
        PatternElement,
        PatternElementList,
        PatternProperty,
        PatternPropertyList,


        UiArrayBinding,
        UiImport,
        UiObjectBinding,
        UiObjectDefinition,
        UiObjectInitializer,
        UiObjectMemberList,
        UiArrayMemberList,
        UiPragma,
        UiProgram,
        UiParameterList,
        UiPublicMember,
        UiQualifiedId,
        UiScriptBinding,
        UiSourceElement,
        UiHeaderItemList,
        UiEnumDeclaration,
        UiEnumMemberList
        #endregion
    }

    public class AstNode : SyntaxElement
    {
        public AstNodeKind Kind { get; private set; }
        public AstNode(AstNodeKind kind) { Kind = kind; }
        public SourceLocation FirstSourceLocation { get; set; }
        public SourceLocation LastSourceLocation { get; set; }
    }

    public class UiImport : AstNode
    {
        public UiImport() : base(AstNodeKind.UiImport) { }
        public SourceLocation ImportToken { get; set; }
        public SourceLocation FileNameToken { get; set; }
        public SourceLocation VersionToken { get; set; }
        public SourceLocation AsToken { get; set; }
        public SourceLocation ImportIdToken { get; set; }
        public SourceLocation SemicolonToken { get; set; }
    }

    public class UiQualifiedId : AstNode
    {
        public UiQualifiedId() : base(AstNodeKind.UiQualifiedId) { }
        public SourceLocation IdentifierToken { get; set; }
        public UiQualifiedId Next { get; set; }
    }

    public class UiObjectDefinition : AstNode
    {
        public UiObjectDefinition() : base(AstNodeKind.UiObjectDefinition) { }
        public UiQualifiedId QualifiedTypeNameId { get; set; }
        public AstNode /*UiObjectInitializer*/ Initializer { get; set; }
    }

    public class UiObjectBinding : AstNode
    {
        public UiObjectBinding() : base(AstNodeKind.UiObjectBinding) { }
        public UiQualifiedId QualifiedId { get; set; }
        public UiQualifiedId QualifiedTypeNameId { get; set; }
        public AstNode /*UiObjectInitializer*/ Initializer { get; set; }
        public SourceLocation ColonToken { get; set; }
    }

    public class UiScriptBinding : AstNode
    {
        public UiScriptBinding() : base(AstNodeKind.UiScriptBinding) { }
        public UiQualifiedId QualifiedId { get; set; }
        public AstNode /*Statement*/ Statement { get; set; }
        public SourceLocation ColonToken { get; set; }
    }

    public class UiArrayBinding : AstNode
    {
        public UiArrayBinding() : base(AstNodeKind.UiArrayBinding) { }
        public UiQualifiedId QualifiedId { get; set; }
        public AstNode /*UiArrayMemberList*/ Members { get; set; }
        public SourceLocation ColonToken { get; set; }
        public SourceLocation LBracketToken { get; set; }
        public SourceLocation RBracketToken { get; set; }
    }

    public enum UiPublicMemberType { Signal, Property };

    public class UiPublicMember : AstNode
    {
        public UiPublicMember() : base(AstNodeKind.UiPublicMember) { }
        public UiPublicMemberType Type { get; set; }
        public UiQualifiedId MemberType { get; set; }
        public AstNode /*Statement*/ Statement { get; set; }
        public AstNode /*UiObjectMember*/ Binding { get; set; }
        public bool IsDefaultMember { get; set; }
        public bool IsReadonlyMember { get; set; }
        public AstNode /*UiParameterList*/ Parameters { get; set; }
        public SourceLocation DefaultToken { get; set; }
        public SourceLocation ReadonlyToken { get; set; }
        public SourceLocation PropertyToken { get; set; }
        public SourceLocation TypeModifierToken { get; set; }
        public SourceLocation TypeToken { get; set; }
        public SourceLocation IdentifierToken { get; set; }
        public SourceLocation ColonToken { get; set; }
        public SourceLocation SemicolonToken { get; set; }
    }

}
