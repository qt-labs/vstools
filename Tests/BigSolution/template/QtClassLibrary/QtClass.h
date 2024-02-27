/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#pragma once
#include "qtclasslibrary_global.h"

#include <QObject>

class QTCLASSLIBRARY_EXPORT QtClass : public QObject
{
    Q_OBJECT

public:
    QtClass(QObject *parent);
    ~QtClass();
};
