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
