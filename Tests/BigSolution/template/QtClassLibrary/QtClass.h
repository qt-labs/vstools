/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
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
