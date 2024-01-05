/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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

