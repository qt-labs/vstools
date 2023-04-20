/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
#pragma once

#include "vsqml.h"

#include <QtQml/private/qqmljslexer_p.h>
#include <QtQml/private/qqmljsparser_p.h>
#include <QtQml/private/qqmljssourcelocation_p.h>

class AstVisitorPrivate;

class AstVisitor {
public:
    AstVisitor();
    ~AstVisitor();
    void setCallback(Callback visitCallback);
    void setCallback(int nodeKindFilter, Callback visitCallback);
    QQmlJS::AST::Visitor *GetVisitor();

private:
    AstVisitorPrivate *d_ptr;
};
