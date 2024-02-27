/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
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
