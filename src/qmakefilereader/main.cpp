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

