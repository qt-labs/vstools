/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

#include "qmakedataprovider.h"
#include <QCoreApplication>
#include <QStringList>
#include <QFileInfo>
#include <QXmlStreamWriter>

QString toString(bool b)
{
    return b ? QStringLiteral("true") : QStringLiteral("false");
}

int main(int argc, char *argv[])
{
    if (argc < 3) {
        fputs("Usage: qmakefilereader <QtDir> <filePath>\n", stderr);
        return -1;
    }

    QCoreApplication app(argc, argv);
    const QStringList args = app.arguments();
    const QString qtDir = args.at(1);
    const QString filePath = QFileInfo(args.at(2)).absoluteFilePath();

    QMakeDataProvider dataProvider;
    dataProvider.setQtDir(qtDir);
    if (!dataProvider.readFile(filePath))
        return 1;

    QFile fout;
    if (!fout.open(stdout, QFile::WriteOnly))
        return 2;

    QXmlStreamWriter stream(&fout);
    stream.setAutoFormatting(true);
    stream.writeStartDocument();
    stream.writeStartElement("content");
    stream.writeAttribute("valid", toString(dataProvider.isValid()));
    stream.writeAttribute("flat", toString(dataProvider.isFlat()));
    stream.writeStartElement("SOURCES");
    foreach (const QString &str, dataProvider.getSourceFiles())
         stream.writeTextElement("file", str);
    stream.writeEndElement();
    stream.writeStartElement("HEADERS");
    foreach (const QString &str, dataProvider.getHeaderFiles())
         stream.writeTextElement("file", str);
    stream.writeEndElement();
    stream.writeStartElement("RESOURCES");
    foreach (const QString &str, dataProvider.getResourceFiles())
         stream.writeTextElement("file", str);
    stream.writeEndElement();
    stream.writeStartElement("FORMS");
    foreach (const QString &str, dataProvider.getFormFiles())
         stream.writeTextElement("file", str);
    stream.writeEndElement();
    stream.writeEndElement();   // content
    stream.writeEndDocument();
    return 0;
}

