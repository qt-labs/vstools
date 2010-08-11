/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2010 Nokia Corporation and/or its subsidiary(-ies).
**
** Contact: Nokia Corporation (qt-info@nokia.com)
**
** Commercial Usage
**
** Licensees holding valid Qt Commercial licenses may use this file in
** accordance with the Qt Commercial License Agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Nokia.
**
** GNU Lesser General Public License Usage
**
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** If you are unsure which license is appropriate for your use, please
** contact the sales department at http://qt.nokia.com/contact.
**
**************************************************************************/

#include "qmakedataprovider.h"
#include <QDebug>
#include <QStringList>

void printList(const QString section, const QStringList values)
{
    qDebug() << section;
    foreach(const QString i, values) {
        qDebug() << "  *" << i;
    }
}

int main(int argc, char *argv[])
{
    if (argc < 2)
        return -1;

    QMakeDataProvider dataProvider;
    dataProvider.readFile(QString::fromLocal8Bit(argv[1]));

    qDebug() << "valid ==" << dataProvider.isValid();
    qDebug() << "flat ==" << dataProvider.isFlat();
    qDebug() << "";

    printList("Source files", dataProvider.getSourceFiles());
    printList("Header files", dataProvider.getHeaderFiles());
    printList("Resource files", dataProvider.getResourceFiles());
    printList("Form files", dataProvider.getFormFiles());

    return 0;
}

