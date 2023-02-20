/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
#pragma once

#include "vsqml_global.h"

extern "C" VSQML_EXPORT bool qmlGetTokens(
    const char *qmlText,
    int qmlTextLength,
    int **tokens,
    int *tokensLength);

extern "C" VSQML_EXPORT bool qmlParse(
    const char *qmlText,
    int qmlTextLength,
    void **parser,
    bool *parsedCorrectly,
    int **diagnosticMessages,
    int *diagnosticMessagesLength,
    int **comments,
    int *commentsLength);

extern "C" VSQML_EXPORT void *qmlGetAstVisitor();

typedef bool(__stdcall *Callback)(
    void *astVisitor,
    int nodeKind,
    void *node,
    bool beginVisit,
    int *nodeData,
    int nodeDataLength);

extern "C" VSQML_EXPORT bool qmlSetAstVisitorCallback(
    void *astVisitor,
    int nodeKindFilter,
    Callback visitCallback);

extern "C" VSQML_EXPORT bool qmlAcceptAstVisitor(
    void *parser,
    void *node,
    void *astVisitor);

extern "C" VSQML_EXPORT bool qmlFreeTokens(int *tokenData);

extern "C" VSQML_EXPORT bool qmlFreeParser(void *parser);

extern "C" VSQML_EXPORT bool qmlFreeDiagnosticMessages(int *diagnosticMessages);

extern "C" VSQML_EXPORT bool qmlFreeComments(int *commentData);

extern "C" VSQML_EXPORT bool qmlFreeAstVisitor(void *astVisitor);

typedef void(__stdcall *QmlDebugClientCreated)(void *qmlDebugClient);

typedef void(__stdcall *QmlDebugClientDestroyed)(void *qmlDebugClient);

typedef void(__stdcall *QmlDebugClientConnected)(void *qmlDebugClient);

typedef void(__stdcall *QmlDebugClientDisconnected)(void *qmlDebugClient);

typedef void(__stdcall *QmlDebugClientMessageReceived)(
    void *qmlDebugClient,
    const char *messageTypeData,
    int messageTypeLength,
    const char *messageParamsData,
    int messageParamsLength);

extern "C" VSQML_EXPORT bool qmlDebugClientThread(
    QmlDebugClientCreated clientCreated,
    QmlDebugClientDestroyed clientDestroyed,
    QmlDebugClientConnected clientConnected,
    QmlDebugClientDisconnected clientDisconnected,
    QmlDebugClientMessageReceived clientMessageReceived);

extern "C" VSQML_EXPORT bool qmlDebugClientConnect(
    void *qmlDebugClient,
    const char *hostNameData,
    int hostNameLength,
    unsigned short hostPort);

extern "C" VSQML_EXPORT bool qmlDebugClientStartLocalServer(
    void *qmlDebugClient,
    const char *fileNameData,
    int fileNameLength);

extern "C" VSQML_EXPORT bool qmlDebugClientDisconnect(void *qmlDebugClient);

extern "C" VSQML_EXPORT bool qmlDebugClientSendMessage(
    void *qmlDebugClient,
    const char *messageTypeData,
    int messageTypeLength,
    const char *messageParamsData,
    int messageParamsLength);

extern "C" VSQML_EXPORT bool qmlDebugClientShutdown(void *qmlDebugClient);
