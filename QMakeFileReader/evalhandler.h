/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#ifndef EVALHANDLER_H
#define EVALHANDLER_H

#include <qmakeevaluator.h>

/**
 * Dummy handler to please qmake's parser.
 */
class EvalHandler : public QMakeHandler
{
public:
    void message(int type, const QString &msg, const QString &fileName, int lineNo);
    void fileMessage(const QString &msg);
    void aboutToEval(ProFile *parent, ProFile *proFile, EvalFileType type);
    void doneWithEval(ProFile *parent);
};

#endif // EVALHANDLER_H
