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
#include "vsqmldebugclient.h"

#include <QtQml/private/qqmljslexer_p.h>
#include <QtQml/private/qqmljsparser_p.h>
#include <QtQml/private/qqmljsglobal_p.h>
#include <QtQml/private/qqmljsgrammar_p.h>

#include <QCoreApplication>

#include <Windows.h>

using namespace QQmlJS;
using namespace QQmlJS::AST;

QCoreApplication *app = nullptr;

BOOL WINAPI DllMain(HINSTANCE instance, DWORD reason, LPVOID)
{
    switch (reason) {
    case DLL_PROCESS_DETACH:
        if (app) {
            delete app;
            app = nullptr;
        }
        break;
    default:
        break;
    }
    return TRUE;
}

QCoreApplication *GetQtApplication()
{
    if (app == nullptr) {
        static int argc = 1;
        static const char *argv[] = { "vsqml", nullptr };
        app = new QCoreApplication(argc, const_cast<char **>(argv));
    }

    return app;
}

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
        for (auto &diag : s->parser->diagnosticMessages()) {
            diagValues.append(diag.type >= QtCriticalMsg ? 1 : 0);
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

bool qmlDebugClientThread(
    QmlDebugClientCreated clientCreated,
    QmlDebugClientDestroyed clientDestroyed,
    QmlDebugClientConnected clientConnected,
    QmlDebugClientDisconnected clientDisconnected,
    QmlDebugClientMessageReceived clientMessageReceived)
{
    GetQtApplication();

    QEventLoop eventLoop;
    VsQmlDebugClient client(&eventLoop);

    if (clientCreated)
        clientCreated(&client);

    QObject::connect(&client, &VsQmlDebugClient::connected, [&client, clientConnected]()
    {
        if (clientConnected)
            clientConnected(&client);
    });

    QObject::connect(&client, &VsQmlDebugClient::disconnected, [&client, clientDisconnected]()
    {
        if (clientDisconnected)
            clientDisconnected(&client);
    });

    QObject::connect(&client, &VsQmlDebugClient::messageReceived,
        [&client, clientMessageReceived](
            const QByteArray &messageType,
            const QByteArray &messageParams)
    {
        if (clientMessageReceived)
            clientMessageReceived(&client,
                messageType.constData(), messageType.size(),
                messageParams.constData(), messageParams.size());
    });

    int exitCode = eventLoop.exec();

    if (clientDestroyed)
        clientDestroyed(&client);

    return (exitCode == 0);
}

bool qmlDebugClientConnect(
    void *qmlDebugClient,
    const char *hostNameData,
    int hostNameLength,
    unsigned short hostPort)
{
    if (!qmlDebugClient)
        return false;

    auto client = reinterpret_cast<VsQmlDebugClient*>(qmlDebugClient);
    QString hostName = QString::fromUtf8(hostNameData, hostNameLength);

    return QMetaObject::invokeMethod(client, "connectToHost", Qt::QueuedConnection,
        Q_ARG(QString, hostName), Q_ARG(quint16, hostPort));
}

bool qmlDebugClientStartLocalServer(
    void *qmlDebugClient,
    const char *fileNameData,
    int fileNameLength)
{
    if (!qmlDebugClient)
        return false;

    auto client = reinterpret_cast<VsQmlDebugClient *>(qmlDebugClient);
    QString fileName = QString::fromUtf8(fileNameData, fileNameLength);

    return QMetaObject::invokeMethod(client, "startLocalServer", Qt::QueuedConnection,
        Q_ARG(QString, fileName));
}

bool qmlDebugClientDisconnect(void *qmlDebugClient)
{
    if (!qmlDebugClient)
        return false;

    auto client = reinterpret_cast<VsQmlDebugClient*>(qmlDebugClient);

    return QMetaObject::invokeMethod(client, "disconnectFromHost", Qt::QueuedConnection);
}

bool qmlDebugClientSendMessage(
    void *qmlDebugClient,
    const char *messageTypeData,
    int messageTypeLength,
    const char *messageParamsData,
    int messageParamsLength)
{
    if (!qmlDebugClient)
        return false;

    auto client = reinterpret_cast<VsQmlDebugClient*>(qmlDebugClient);
    QByteArray messageType = QByteArray::fromRawData(messageTypeData, messageTypeLength);
    QByteArray messageParams = QByteArray::fromRawData(messageParamsData, messageParamsLength);

    return QMetaObject::invokeMethod(client, "sendMessage", Qt::QueuedConnection,
        Q_ARG(QByteArray, messageType), Q_ARG(QByteArray, messageParams));
}

bool qmlDebugClientShutdown(void *qmlDebugClient)
{
    if (!qmlDebugClient)
        return false;

    auto client = reinterpret_cast<VsQmlDebugClient*>(qmlDebugClient);
    return QMetaObject::invokeMethod(client->parent(), "quit", Qt::QueuedConnection);
}
