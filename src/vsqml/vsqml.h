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
