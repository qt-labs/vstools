/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#ifndef QMAKEDATAPROVIDER_H
#define QMAKEDATAPROVIDER_H

#include <QtCore/QString>
#include <QtCore/QStringList>

class QMakeDataProviderPrivate;

class QMakeDataProvider {

    QMakeDataProviderPrivate * const d;

public:
    QMakeDataProvider();
    ~QMakeDataProvider();

    bool readFile(const QString &fileName);
    void setQtDir(const QString &qtdir);
    QStringList getFormFiles() const;
    QStringList getHeaderFiles() const;
    QStringList getResourceFiles() const;
    QStringList getSourceFiles() const;
    bool isFlat() const;
    bool isValid() const;
};

#endif // QMAKEDATAPROVIDER_H

