/****************************************************************************
**
** Copyright (C) 2013 Digia Plc and/or its subsidiary(-ies).
** Contact: http://qt.digia.com/Digia-Legal-Notice--Privacy-Policy/
**
** This file is part of the Commercial Qt VS Add-in.
**
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia. For licensing terms and
** conditions see http://qt.digia.com/licensing. For further information
** use the contact form at http://qt.digia.com/contact-us.
**
****************************************************************************/

namespace QmlClassifier
{
    public enum TokenType
    {
        None = -1,
        Other = 0,
        Comment,
        MultilineComment,
        Type,
        Keyword,
        JsKeyword,
        String,
        Property,
    }
}
