/**************************************************************************
**
** This file is part of the Qt VS Add-in
**
** Copyright (c) 2011 Nokia Corporation and/or its subsidiary(-ies).
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

#include <QFile>
#include <QFileInfo>
#include <QDomDocument>
#include <QDebug>

static QString sourceDir;
static QList<QDomNode> killList;

static void showUsage()
{
    printf("Usage: qhprepair <foo.qhp>\n\n");
}

static void fixRefAttribute(QDomNode node)
{
    if (!node.isElement())
        return;

    QDomElement e = node.toElement();
    QString ref = e.attribute("ref");

    int idx = ref.lastIndexOf('#');
    if (idx >= 0)
        ref.truncate(idx);

    if (!QFile::exists(sourceDir + ref)) {
        printf("removing <%s ref=\"%s\">\n", qPrintable(e.tagName()), qPrintable(ref));
        killList.append(node);
    }
}

int main(int argc, char *argv[])
{
    if (argc < 2) {
        showUsage();
        return 0;
    }

    QString targetFileName(argv[1]);
    QString backupFileName(targetFileName + ".bak");
    QFile::remove(backupFileName);

    QFile file(targetFileName);
    file.rename(backupFileName);
    if (!file.open(QFile::ReadOnly)) {
        fprintf(stderr, "Can't open file %s.", argv[1]);
        return 128;
    }

    sourceDir = QFileInfo(file.fileName()).path();
    if (!sourceDir.endsWith('/'))
        sourceDir.append('/');

    printf("reading %s...\n", qPrintable(targetFileName));
    QDomDocument document("qhp");
    if (!document.setContent(&file)) {
        fprintf(stderr, "Can't read XML data from %s.", argv[1]);
        return 128;
    }
    file.close();

    printf("fixing QHP content...\n");
    QDomNodeList lst = document.elementsByTagName("section");
    int count = lst.count();
    for (int i=0; i < count; ++i) {
        fixRefAttribute(lst.item(i));
    }

    lst = document.elementsByTagName("keyword");
    count = lst.count();
    for (int i=0; i < count; ++i) {
        fixRefAttribute(lst.item(i));
    }

    lst = document.elementsByTagName("file");
    count = lst.count();
    for (int i=0; i < count; ++i) {
        QDomNode node = lst.item(i);
        if (!node.isElement())
            continue;

        QDomElement e = node.toElement();
        if (!QFile::exists(sourceDir + e.text())) {
            printf("removing <%s ref=\"%s\">\n", qPrintable(e.tagName()), qPrintable(e.text()));
            killList.append(node);
        }
    }

    foreach (QDomNode node, killList)
        node.parentNode().removeChild(node);

    printf("writing QHP...\n");
    file.setFileName(targetFileName);
    if (!file.open(QFile::WriteOnly | QFile::Truncate)) {
        fprintf(stderr, "Can't open file for writing.");
        return 128;
    }
    file.write(document.toByteArray());
    file.close();

    return 0;
}
