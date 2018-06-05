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
#include "vsqml.h"
#include "astvisitor.h"

#include <QtQml/private/qqmljslexer_p.h>
#include <QtQml/private/qqmljsparser_p.h>

using namespace QQmlJS;
using namespace QQmlJS::AST;

struct State
{
    Engine *engine;
    Lexer *lexer;
    Parser *parser;
};

bool qmlGetTokens(const char *qmlText, int qmlTextLength, int **tokens, int *tokensLength)
{
    if (!qmlText || !tokens || !tokensLength)
        return false;

    Engine engine;
    Lexer lexer(&engine);
    Parser parser(&engine);

    lexer.setCode(QString::fromUtf8(qmlText, qmlTextLength), 1, true);

    QVector<int> tokenValues;
    for (lexer.lex(); lexer.tokenKind(); lexer.lex()) {
        tokenValues.append(lexer.tokenKind());
        tokenValues.append(lexer.tokenOffset());
        tokenValues.append(lexer.tokenLength());
    }
    *tokensLength = tokenValues.count() * sizeof(int);
    *tokens = new int[tokenValues.count()];
    memcpy(*tokens, tokenValues.data(), *tokensLength);

    return true;
}

bool qmlFreeTokens(int *tokens)
{
    if (!tokens)
        return false;

    delete[] tokens;
    return true;
}

bool qmlParse(
    const char *qmlText,
    int qmlTextLength,
    void **parser,
    bool *parsedCorrectly,
    int **diagnosticMessages,
    int *diagnosticMessagesLength,
    int **comments,
    int *commentsLength)
{
    if (!qmlText || !parser)
        return false;

    *parser = 0;

    if (parsedCorrectly)
        *parsedCorrectly = false;
    if (diagnosticMessages)
        *diagnosticMessages = 0;
    if (diagnosticMessagesLength)
        *diagnosticMessagesLength = 0;
    if (comments)
        *comments = 0;
    if (commentsLength)
        *commentsLength = 0;

    auto s = new State;
    s->engine = new Engine();
    s->lexer = new Lexer(s->engine);
    s->parser = new Parser(s->engine);
    *parser = s;

    s->lexer->setCode(QString::fromUtf8(qmlText, qmlTextLength), 1, true);
    bool ok = s->parser->parse();
    if (parsedCorrectly)
        *parsedCorrectly = ok;

    if (diagnosticMessages && diagnosticMessagesLength) {
        QVector<int> diagValues;
        for (auto diag : s->parser->diagnosticMessages()) {
            diagValues.append(diag.kind);
            diagValues.append(diag.loc.offset);
            diagValues.append(diag.loc.length);
        }
        *diagnosticMessagesLength = diagValues.count() * sizeof(int);
        *diagnosticMessages = new int[diagValues.count()];
        memcpy(*diagnosticMessages, diagValues.data(), *diagnosticMessagesLength);
    }

    if (comments && commentsLength) {
        QVector<int> commentValues;
        for (auto comment : s->engine->comments()) {
            commentValues.append(comment.offset);
            commentValues.append(comment.length);
        }
        *commentsLength = commentValues.count() * sizeof(int);
        *comments = new int[commentValues.count()];
        memcpy(*comments, commentValues.data(), *commentsLength);
    }

    return true;
}

bool qmlFreeParser(void *parser)
{
    if (!parser)
        return false;

    auto s = reinterpret_cast<State*>(parser);
    delete s->parser;
    delete s->lexer;
    delete s->engine;
    delete s;

    return true;
}

bool qmlFreeDiagnosticMessages(int *diagnosticMessages)
{
    if (!diagnosticMessages)
        return false;

    delete[] diagnosticMessages;
    return true;
}

bool qmlFreeComments(int *comments)
{
    if (!comments)
        return false;

    delete[] comments;
    return true;
}

void *qmlGetAstVisitor()
{
    return new AstVisitor();
}

bool qmlFreeAstVisitor(void *astVisitor)
{
    if (!astVisitor)
        return false;

    delete reinterpret_cast<AstVisitor*>(astVisitor);
    return true;
}

bool qmlSetAstVisitorCallback(void *astVisitor, int nodeKindFilter, Callback visitCallback)
{
    if (!astVisitor)
        return false;

    auto visitor = reinterpret_cast<AstVisitor*>(astVisitor);
    if (nodeKindFilter <= Node::Kind_Undefined)
        visitor->setCallback(visitCallback);
    else
        visitor->setCallback(nodeKindFilter, visitCallback);

    return true;
}

bool qmlAcceptAstVisitor(void *parser, void *node, void *astVisitor)
{
    if (!parser || !astVisitor)
        return false;

    auto s = reinterpret_cast<State*>(parser);
    auto visitor = reinterpret_cast<AstVisitor*>(astVisitor);

    Node *visitNode = 0;
    if (node)
        visitNode = reinterpret_cast<Node*>(node);
    else
        visitNode = s->parser->rootNode();

    if (!visitNode)
        return false;

    visitNode->accept(visitor->GetVisitor());

    return true;
}
