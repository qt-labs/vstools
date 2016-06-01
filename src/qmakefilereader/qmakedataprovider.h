/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

