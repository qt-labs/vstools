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

#include <QString>
#include <QByteArray>
#include <QFileInfo>
#include <QUrl>
#include <QTime>
#include <QVariant>
#include <QStringList>
#include <QMap>
#include <QList>
#include <QQueue>
#include <QLinkedList>
#include <QVector>
#include <QStack>
#include <QHash>
#include <QMultiHash>
#include <QSet>
#include <QPalette>
#include <QBrush>
#include <QTransform>
#include <QMatrix>
#include <QPolygon>
#include <QPolygonF>

struct BigStruct
{
    int a;
    int b;
    int c;

    BigStruct()
    {
        a = b = c = 0;
    }

    BigStruct(int i)
    {
        a = i++;
        b = i++;
        c = i;
    }
};

static bool operator==(const BigStruct& lhs, const BigStruct& rhs)
{
    return rhs.a == lhs.a && rhs.b == lhs.b && rhs.c == lhs.c;
}

static uint qHash(const BigStruct& bs)
{
    return qHash(bs.a + bs.b*10 + bs.c*100);
}

int main(int argc, char *argv[])
{
    QString str = QLatin1String("This is a string.");
    QByteArray byteArray = "This is a zero-terminated string.";
    QFileInfo fileInfo(argv[0]);
    QUrl url = "http://qt.nokia.com/";
    QTime currentTime = QTime::currentTime();

    QStringList stringList;
    stringList << "one" << "two" << "three";

    QVariant v_bool(true);
    QVariant v_int(int(-156));
    QVariant v_uint(unsigned int(156));
    QVariant v_longlong(long long(-156));
    QVariant v_ulonglong(unsigned long long(156));
    QVariant v_double(123.456);
    QVariant v_char('A');
    QMap<QString,QVariant> vMap;
    vMap["foo"] = 2;
    QVariant v_map(vMap);
    QVariant v_string(QString(QLatin1String("This is a string.")));
    QVariant v_stringList(stringList);
    QVariant v_url(QUrl("http://qt.nokia.com/"));

    QList<int> lst_int;
    lst_int << 1 << 2 << 3;
    QList<int>::iterator lst_int_it = lst_int.begin();
    QListIterator<int> lst_int_it2(lst_int);

    QList<BigStruct> lst_big;
    lst_big << BigStruct(1) << BigStruct(2) << BigStruct(3);
    QList<BigStruct>::iterator lst_big_it = lst_big.begin();
    QListIterator<BigStruct> lst_big_it2(lst_big);

    QQueue<int> queue_int;
    queue_int << 1 << 2 << 3;
    QQueue<int>::iterator queue_int_it = queue_int.begin();

    QQueue<BigStruct> queue_big;
    queue_big << BigStruct(1) << BigStruct(2) << BigStruct(3);
    QQueue<BigStruct>::iterator queue_big_it = queue_big.begin();

    QLinkedList<int> lnklst_int;
    lnklst_int << 1 << 2 << 3;
    QLinkedList<int>::iterator lnklst_int_it = lnklst_int.begin();

    QLinkedList<BigStruct> lnklst_big;
    lnklst_big << BigStruct(1) << BigStruct(2) << BigStruct(3);
    QLinkedList<BigStruct>::iterator lnklst_big_it = lnklst_big.begin();

    QVector<int> vec_int;
    vec_int << 1 << 2 << 3;

    QVector<BigStruct> vec_big;
    vec_big << BigStruct(1) << BigStruct(2) << BigStruct(3);

    QStack<int> stack_int;
    stack_int << 1 << 2 << 3;

    QStack<BigStruct> stack_big;
    stack_big << BigStruct(1) << BigStruct(2) << BigStruct(3);

    QHash<QString, int> hash;
    hash["one"] = 1;
    hash["two"] = 2;
    hash["three"] = 3;

    QHash<QString, int>::iterator hash_it = hash.begin();
    QHashIterator<QString, int> hash_it2(hash);

    QMultiHash<QString, int> multihash;
    multihash.insert("ones", 1);
    multihash.insert("ones", 11);
    multihash.insert("ones", 111);
    multihash.insert("twos", 2);
    multihash.insert("threes", 3);
    multihash.insert("threes", 33);

    QSet<int> set_int;
    set_int << 1 << 2 << 3;

    QSet<BigStruct> set_big;
    set_big << BigStruct(1) << BigStruct(2) << BigStruct(3);

    QPalette palette;
    QBrush brush(Qt::red);
    QColor color(Qt::blue);
    QTransform transform;
    transform.translate(50, 50);
    transform.rotate(45);
    transform.scale(0.5, 1.0);
    QMatrix matrix(1, 0, 0, 1, 50.0, 50.0);
    QPolygon polygon;
    polygon << QPoint(10, 20) << QPoint(20, 30);
    QPolygonF polygonf;
    polygonf << QPointF(10.1, 20.1) << QPointF(20.1, 30.1);

    return 0;
}
