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
#include "astvisitor.h"

using namespace QQmlJS::AST;

class AstVisitorPrivate : public QQmlJS::AST::Visitor
{
private:
    Callback callbackUnfiltered;
    QMap<int, Callback> callbackFiltered;

    Callback getCallback(Node *node) {
        auto callback = callbackUnfiltered;
        auto itFilter = callbackFiltered.find(node->kind);
        if (itFilter != callbackFiltered.end())
            callback = itFilter.value();
        return callback;
    }

    bool invokeCallback(Callback callback, QVector<int> &nodeData, Node *node, bool beginVisit)
    {
        if (!callback)
            return true;
        bool result = callback(this, node->kind, node,
            beginVisit, nodeData.data(), nodeData.count() * sizeof(int));

        nodeData.clear();
        return result;
    }

    void marshalInt(QVector<int> &nodeData, int n)
    {
        nodeData.append(n);
    }

    void marshalLocation(QVector<int> &nodeData, SourceLocation &location)
    {
        nodeData.append(location.offset);
        nodeData.append(location.length);
    }

    void marshalPointer(QVector<int> &nodeData, void *ptr)
    {
        auto ptrRef = reinterpret_cast<long long>(ptr);
        auto ptrHi = ptrRef >> 32;
        auto ptrLo = ptrRef & 0xFFFFFFFFLL;
        nodeData.append(int(ptrHi));
        nodeData.append(int(ptrLo));
    }

    void marshalNode(QVector<int> &nodeData, Node *node)
    {
        marshalLocation(nodeData, node->firstSourceLocation());
        marshalLocation(nodeData, node->lastSourceLocation());
    }

    bool visitCallback(UiImport *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        marshalLocation(nodeData, node->importToken);
        marshalLocation(nodeData, node->fileNameToken);
        marshalLocation(nodeData, node->versionToken);
        marshalLocation(nodeData, node->asToken);
        marshalLocation(nodeData, node->importIdToken);
        marshalLocation(nodeData, node->semicolonToken);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

    bool visitCallback(UiQualifiedId *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        marshalPointer(nodeData, node->next);
        marshalLocation(nodeData, node->identifierToken);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

    bool visitCallback(UiObjectDefinition *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        marshalPointer(nodeData, node->qualifiedTypeNameId);
        marshalPointer(nodeData, node->initializer);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

    bool visitCallback(UiObjectBinding *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        marshalPointer(nodeData, node->qualifiedId);
        marshalPointer(nodeData, node->qualifiedTypeNameId);
        marshalPointer(nodeData, node->initializer);
        marshalLocation(nodeData, node->colonToken);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

    bool visitCallback(UiObjectInitializer *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

    bool visitCallback(UiScriptBinding *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        marshalPointer(nodeData, node->qualifiedId);
        marshalPointer(nodeData, node->statement);
        marshalLocation(nodeData, node->colonToken);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

    bool visitCallback(UiArrayBinding *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        marshalPointer(nodeData, node->qualifiedId);
        marshalPointer(nodeData, node->members);
        marshalLocation(nodeData, node->colonToken);
        marshalLocation(nodeData, node->lbracketToken);
        marshalLocation(nodeData, node->rbracketToken);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

    bool visitCallback(UiPublicMember *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        marshalInt(nodeData, node->type);
        marshalPointer(nodeData, node->memberType);
        marshalPointer(nodeData, node->statement);
        marshalPointer(nodeData, node->binding);
        marshalInt(nodeData, node->isDefaultMember);
        marshalInt(nodeData, node->isReadonlyMember);
        marshalPointer(nodeData, node->parameters);
        marshalLocation(nodeData, node->defaultToken);
        marshalLocation(nodeData, node->readonlyToken);
        marshalLocation(nodeData, node->propertyToken);
        marshalLocation(nodeData, node->typeModifierToken);
        marshalLocation(nodeData, node->typeToken);
        marshalLocation(nodeData, node->identifierToken);
        marshalLocation(nodeData, node->colonToken);
        marshalLocation(nodeData, node->semicolonToken);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

    bool visitCallback(Node *node, bool beginVisit)
    {
        auto callback = getCallback(node);
        if (!callback)
            return true;
        QVector<int> nodeData;
        marshalNode(nodeData, node);
        return invokeCallback(callback, nodeData, node, beginVisit);
    }

public:
    AstVisitorPrivate()
    {
        callbackUnfiltered = 0;
    }

    ~AstVisitorPrivate()
    {
    }

    void setCallback(Callback visitCallback)
    {
        callbackUnfiltered = visitCallback;
    }

    void setCallback(int nodeKindFilter, Callback visitCallback)
    {
        callbackFiltered[nodeKindFilter] = visitCallback;
    }

    // Copied from qqmljsastvisitor_p.h

    virtual bool visit(UiProgram *node) { return visitCallback(node, true); }
    virtual bool visit(UiHeaderItemList *node) { return visitCallback(node, true); }
    virtual bool visit(UiPragma *node) { return visitCallback(node, true); }
    virtual bool visit(UiImport *node) { return visitCallback(node, true); }
    virtual bool visit(UiPublicMember *node) { return visitCallback(node, true); }
    virtual bool visit(UiSourceElement *node) { return visitCallback(node, true); }
    virtual bool visit(UiObjectDefinition *node) { return visitCallback(node, true); }
    virtual bool visit(UiObjectInitializer *node) { return visitCallback(node, true); }
    virtual bool visit(UiObjectBinding *node) { return visitCallback(node, true); }
    virtual bool visit(UiScriptBinding *node) { return visitCallback(node, true); }
    virtual bool visit(UiArrayBinding *node) { return visitCallback(node, true); }
    virtual bool visit(UiParameterList *node) { return visitCallback(node, true); }
    virtual bool visit(UiObjectMemberList *node) { return visitCallback(node, true); }
    virtual bool visit(UiArrayMemberList *node) { return visitCallback(node, true); }
    virtual bool visit(UiQualifiedId *node) { return visitCallback(node, true); }
    virtual bool visit(UiEnumDeclaration *node) { return visitCallback(node, true); }
    virtual bool visit(UiEnumMemberList *node) { return visitCallback(node, true); }

    virtual void endVisit(UiProgram *node) { visitCallback(node, false); }
    virtual void endVisit(UiImport *node) { visitCallback(node, false); }
    virtual void endVisit(UiHeaderItemList *node) { visitCallback(node, false); }
    virtual void endVisit(UiPragma *node) { visitCallback(node, false); }
    virtual void endVisit(UiPublicMember *node) { visitCallback(node, false); }
    virtual void endVisit(UiSourceElement *node) { visitCallback(node, false); }
    virtual void endVisit(UiObjectDefinition *node) { visitCallback(node, false); }
    virtual void endVisit(UiObjectInitializer *node) { visitCallback(node, false); }
    virtual void endVisit(UiObjectBinding *node) { visitCallback(node, false); }
    virtual void endVisit(UiScriptBinding *node) { visitCallback(node, false); }
    virtual void endVisit(UiArrayBinding *node) { visitCallback(node, false); }
    virtual void endVisit(UiParameterList *node) { visitCallback(node, false); }
    virtual void endVisit(UiObjectMemberList *node) { visitCallback(node, false); }
    virtual void endVisit(UiArrayMemberList *node) { visitCallback(node, false); }
    virtual void endVisit(UiQualifiedId *node) { visitCallback(node, false); }
    virtual void endVisit(UiEnumDeclaration *node) { visitCallback(node, false); }
    virtual void endVisit(UiEnumMemberList *node) { visitCallback(node, false); }

    // QQmlJS
    virtual bool visit(ThisExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(ThisExpression *node) { visitCallback(node, false); }

    virtual bool visit(IdentifierExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(IdentifierExpression *node) { visitCallback(node, false); }

    virtual bool visit(NullExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(NullExpression *node) { visitCallback(node, false); }

    virtual bool visit(TrueLiteral *node) { return visitCallback(node, true); }
    virtual void endVisit(TrueLiteral *node) { visitCallback(node, false); }

    virtual bool visit(FalseLiteral *node) { return visitCallback(node, true); }
    virtual void endVisit(FalseLiteral *node) { visitCallback(node, false); }

    virtual bool visit(SuperLiteral *node) { return visitCallback(node, true); }
    virtual void endVisit(SuperLiteral *node) { visitCallback(node, false); }

    virtual bool visit(StringLiteral *node) { return visitCallback(node, true); }
    virtual void endVisit(StringLiteral *node) { visitCallback(node, false); }

    virtual bool visit(TemplateLiteral *node) { return visitCallback(node, true); }
    virtual void endVisit(TemplateLiteral *node) { visitCallback(node, false); }

    virtual bool visit(NumericLiteral *node) { return visitCallback(node, true); }
    virtual void endVisit(NumericLiteral *node) { visitCallback(node, false); }

    virtual bool visit(RegExpLiteral *node) { return visitCallback(node, true); }
    virtual void endVisit(RegExpLiteral *node) { visitCallback(node, false); }

    virtual bool visit(ArrayPattern *node) { return visitCallback(node, true); }
    virtual void endVisit(ArrayPattern *node) { visitCallback(node, false); }

    virtual bool visit(ObjectPattern *node) { return visitCallback(node, true); }
    virtual void endVisit(ObjectPattern *node) { visitCallback(node, false); }

    virtual bool visit(PatternElementList *node) { return visitCallback(node, true); }
    virtual void endVisit(PatternElementList *node) { visitCallback(node, false); }

    virtual bool visit(PatternPropertyList *node) { return visitCallback(node, true); }
    virtual void endVisit(PatternPropertyList *node) { visitCallback(node, false); }

    virtual bool visit(PatternElement *node) { return visitCallback(node, true); }
    virtual void endVisit(PatternElement *node) { visitCallback(node, false); }

    virtual bool visit(PatternProperty *node) { return visitCallback(node, true); }
    virtual void endVisit(PatternProperty *node) { visitCallback(node, false); }

    virtual bool visit(Elision *node) { return visitCallback(node, true); }
    virtual void endVisit(Elision *node) { visitCallback(node, false); }

    virtual bool visit(NestedExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(NestedExpression *node) { visitCallback(node, false); }

    virtual bool visit(IdentifierPropertyName *node) { return visitCallback(node, true); }
    virtual void endVisit(IdentifierPropertyName *node) { visitCallback(node, false); }

    virtual bool visit(StringLiteralPropertyName *node) { return visitCallback(node, true); }
    virtual void endVisit(StringLiteralPropertyName *node) { visitCallback(node, false); }

    virtual bool visit(NumericLiteralPropertyName *node) { return visitCallback(node, true); }
    virtual void endVisit(NumericLiteralPropertyName *node) { visitCallback(node, false); }

    virtual bool visit(ComputedPropertyName *node) { return visitCallback(node, true); }
    virtual void endVisit(ComputedPropertyName *node) { visitCallback(node, false); }

    virtual bool visit(ArrayMemberExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(ArrayMemberExpression *node) { visitCallback(node, false); }

    virtual bool visit(FieldMemberExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(FieldMemberExpression *node) { visitCallback(node, false); }

    virtual bool visit(TaggedTemplate *node) { return visitCallback(node, true); }
    virtual void endVisit(TaggedTemplate *node) { visitCallback(node, false); }

    virtual bool visit(NewMemberExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(NewMemberExpression *node) { visitCallback(node, false); }

    virtual bool visit(NewExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(NewExpression *node) { visitCallback(node, false); }

    virtual bool visit(CallExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(CallExpression *node) { visitCallback(node, false); }

    virtual bool visit(ArgumentList *node) { return visitCallback(node, true); }
    virtual void endVisit(ArgumentList *node) { visitCallback(node, false); }

    virtual bool visit(PostIncrementExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(PostIncrementExpression *node) { visitCallback(node, false); }

    virtual bool visit(PostDecrementExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(PostDecrementExpression *node) { visitCallback(node, false); }

    virtual bool visit(DeleteExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(DeleteExpression *node) { visitCallback(node, false); }

    virtual bool visit(VoidExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(VoidExpression *node) { visitCallback(node, false); }

    virtual bool visit(TypeOfExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(TypeOfExpression *node) { visitCallback(node, false); }

    virtual bool visit(PreIncrementExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(PreIncrementExpression *node) { visitCallback(node, false); }

    virtual bool visit(PreDecrementExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(PreDecrementExpression *node) { visitCallback(node, false); }

    virtual bool visit(UnaryPlusExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(UnaryPlusExpression *node) { visitCallback(node, false); }

    virtual bool visit(UnaryMinusExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(UnaryMinusExpression *node) { visitCallback(node, false); }

    virtual bool visit(TildeExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(TildeExpression *node) { visitCallback(node, false); }

    virtual bool visit(NotExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(NotExpression *node) { visitCallback(node, false); }

    virtual bool visit(BinaryExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(BinaryExpression *node) { visitCallback(node, false); }

    virtual bool visit(ConditionalExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(ConditionalExpression *node) { visitCallback(node, false); }

    virtual bool visit(Expression *node) { return visitCallback(node, true); }
    virtual void endVisit(Expression *node) { visitCallback(node, false); }

    virtual bool visit(Block *node) { return visitCallback(node, true); }
    virtual void endVisit(Block *node) { visitCallback(node, false); }

    virtual bool visit(StatementList *node) { return visitCallback(node, true); }
    virtual void endVisit(StatementList *node) { visitCallback(node, false); }

    virtual bool visit(VariableStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(VariableStatement *node) { visitCallback(node, false); }

    virtual bool visit(VariableDeclarationList *node) { return visitCallback(node, true); }
    virtual void endVisit(VariableDeclarationList *node) { visitCallback(node, false); }

    virtual bool visit(EmptyStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(EmptyStatement *node) { visitCallback(node, false); }

    virtual bool visit(ExpressionStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(ExpressionStatement *node) { visitCallback(node, false); }

    virtual bool visit(IfStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(IfStatement *node) { visitCallback(node, false); }

    virtual bool visit(DoWhileStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(DoWhileStatement *node) { visitCallback(node, false); }

    virtual bool visit(WhileStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(WhileStatement *node) { visitCallback(node, false); }

    virtual bool visit(ForStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(ForStatement *node) { visitCallback(node, false); }

    virtual bool visit(ForEachStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(ForEachStatement *node) { visitCallback(node, false); }

    virtual bool visit(ContinueStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(ContinueStatement *node) { visitCallback(node, false); }

    virtual bool visit(BreakStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(BreakStatement *node) { visitCallback(node, false); }

    virtual bool visit(ReturnStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(ReturnStatement *node) { visitCallback(node, false); }

    virtual bool visit(YieldExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(YieldExpression *node) { visitCallback(node, false); }

    virtual bool visit(WithStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(WithStatement *node) { visitCallback(node, false); }

    virtual bool visit(SwitchStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(SwitchStatement *node) { visitCallback(node, false); }

    virtual bool visit(CaseBlock *node) { return visitCallback(node, true); }
    virtual void endVisit(CaseBlock *node) { visitCallback(node, false); }

    virtual bool visit(CaseClauses *node) { return visitCallback(node, true); }
    virtual void endVisit(CaseClauses *node) { visitCallback(node, false); }

    virtual bool visit(CaseClause *node) { return visitCallback(node, true); }
    virtual void endVisit(CaseClause *node) { visitCallback(node, false); }

    virtual bool visit(DefaultClause *node) { return visitCallback(node, true); }
    virtual void endVisit(DefaultClause *node) { visitCallback(node, false); }

    virtual bool visit(LabelledStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(LabelledStatement *node) { visitCallback(node, false); }

    virtual bool visit(ThrowStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(ThrowStatement *node) { visitCallback(node, false); }

    virtual bool visit(TryStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(TryStatement *node) { visitCallback(node, false); }

    virtual bool visit(Catch *node) { return visitCallback(node, true); }
    virtual void endVisit(Catch *node) { visitCallback(node, false); }

    virtual bool visit(Finally *node) { return visitCallback(node, true); }
    virtual void endVisit(Finally *node) { visitCallback(node, false); }

    virtual bool visit(FunctionDeclaration *node) { return visitCallback(node, true); }
    virtual void endVisit(FunctionDeclaration *node) { visitCallback(node, false); }

    virtual bool visit(FunctionExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(FunctionExpression *node) { visitCallback(node, false); }

    virtual bool visit(FormalParameterList *node) { return visitCallback(node, true); }
    virtual void endVisit(FormalParameterList *node) { visitCallback(node, false); }

    virtual bool visit(ClassExpression *node) { return visitCallback(node, true); }
    virtual void endVisit(ClassExpression *node) { visitCallback(node, false); }

    virtual bool visit(ClassDeclaration *node) { return visitCallback(node, true); }
    virtual void endVisit(ClassDeclaration *node) { visitCallback(node, false); }

    virtual bool visit(ClassElementList *node) { return visitCallback(node, true); }
    virtual void endVisit(ClassElementList *node) { visitCallback(node, false); }

    virtual bool visit(Program *node) { return visitCallback(node, true); }
    virtual void endVisit(Program *node) { visitCallback(node, false); }

    virtual bool visit(NameSpaceImport *node) { return visitCallback(node, true); }
    virtual void endVisit(NameSpaceImport *node) { visitCallback(node, false); }

    virtual bool visit(ImportSpecifier *node) { return visitCallback(node, true); }
    virtual void endVisit(ImportSpecifier *node) { visitCallback(node, false); }

    virtual bool visit(ImportsList *node) { return visitCallback(node, true); }
    virtual void endVisit(ImportsList *node) { visitCallback(node, false); }

    virtual bool visit(NamedImports *node) { return visitCallback(node, true); }
    virtual void endVisit(NamedImports *node) { visitCallback(node, false); }

    virtual bool visit(FromClause *node) { return visitCallback(node, true); }
    virtual void endVisit(FromClause *node) { visitCallback(node, false); }

    virtual bool visit(ImportClause *node) { return visitCallback(node, true); }
    virtual void endVisit(ImportClause *node) { visitCallback(node, false); }

    virtual bool visit(ImportDeclaration *node) { return visitCallback(node, true); }
    virtual void endVisit(ImportDeclaration *node) { visitCallback(node, false); }

    virtual bool visit(ExportSpecifier *node) { return visitCallback(node, true); }
    virtual void endVisit(ExportSpecifier *node) { visitCallback(node, false); }

    virtual bool visit(ExportsList *node) { return visitCallback(node, true); }
    virtual void endVisit(ExportsList *node) { visitCallback(node, false); }

    virtual bool visit(ExportClause *node) { return visitCallback(node, true); }
    virtual void endVisit(ExportClause *node) { visitCallback(node, false); }

    virtual bool visit(ExportDeclaration *node) { return visitCallback(node, true); }
    virtual void endVisit(ExportDeclaration *node) { visitCallback(node, false); }

    virtual bool visit(ESModule *node) { return visitCallback(node, true); }
    virtual void endVisit(ESModule *node) { visitCallback(node, false); }

    virtual bool visit(DebuggerStatement *node) { return visitCallback(node, true); }
    virtual void endVisit(DebuggerStatement *node) { visitCallback(node, false); }

    virtual void throwRecursionDepthError() { /* TODO: Anything we should do here? */ }
};

AstVisitor::AstVisitor() : d_ptr(new AstVisitorPrivate)
{
}

AstVisitor::~AstVisitor()
{
    delete d_ptr;
}

void AstVisitor::setCallback(Callback visitCallback)
{
    d_ptr->setCallback(visitCallback);
}

void AstVisitor::setCallback(int nodeKindFilter, Callback visitCallback)
{
    d_ptr->setCallback(nodeKindFilter, visitCallback);
}

Visitor *AstVisitor::GetVisitor()
{
    return d_ptr;
}
