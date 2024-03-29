/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#include "evalhandler.h"

void EvalHandler::message(int type, const QString &msg, const QString &fileName, int lineNo)
{
    Q_UNUSED(type);
    Q_UNUSED(msg);
    Q_UNUSED(fileName);
    Q_UNUSED(lineNo);
}

void EvalHandler::fileMessage(const QString &msg)
{
    Q_UNUSED(msg);
}

void EvalHandler::aboutToEval(ProFile *parent, ProFile *proFile, EvalFileType type)
{
    Q_UNUSED(parent);
    Q_UNUSED(proFile);
    Q_UNUSED(type);
}

void EvalHandler::doneWithEval(ProFile *parent)
{
    Q_UNUSED(parent);
}
