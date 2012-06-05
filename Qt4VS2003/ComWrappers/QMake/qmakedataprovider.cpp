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
** a written agreement between you and Digia. For licensing terms and
** conditions see http://qt.digia.com/licensing. For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights. These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file. Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

#include "qmakedataprovider.h"
#include <project.h>
#include <property.h>
#include <option.h>
#include <QString>
#include <QStringList>
#include <QHash>
#include <QDir>

class QMakeDataProviderPrivate
{
public:
    QHash<QString, QStringList> m_vars;
    bool m_valid;
    bool m_flat;
    QString m_qtdir;

    QMakeDataProviderPrivate(const QString fileName)
    {
        readFile(fileName);
    }

    bool readFile(const QString &fileName)
    {
        m_vars.clear();
        m_valid = false;
        m_flat = true;

        if (fileName.isEmpty())
            return false;

        // NOTE: needed to make QMake code work
        Option::mkfile::do_cache = true;
        Option::mkfile::cachefile = m_qtdir + "\\.qmake.cache";

        QMakeProperty prop;
        QMakeProject project(&prop);

        m_valid = project.read(fileName, QMakeProject::ReadProFile);
        if (m_valid) {
            m_vars = project.variables();
            m_flat = project.isActiveConfig("flat");
        }
        return m_valid;
    }
};

QMakeDataProvider::QMakeDataProvider(const QString fileName)
        : d(new QMakeDataProviderPrivate(fileName))
{
    // noop
}

QMakeDataProvider::~QMakeDataProvider()
{
    delete d;
}

bool QMakeDataProvider::readFile(const QString &fileName)
{
    return d->readFile(fileName);
}

void QMakeDataProvider::setQtDir(const QString &qtdir)
{
    d->m_qtdir = qtdir;
}

QStringList QMakeDataProvider::getFormFiles() const
{
    return d->m_vars.value("FORMS");
}

QStringList QMakeDataProvider::getHeaderFiles() const
{
    return d->m_vars.value("HEADERS");
}

QStringList QMakeDataProvider::getResourceFiles() const
{
    return d->m_vars.value("RESOURCES");
}

QStringList QMakeDataProvider::getSourceFiles() const
{
    return d->m_vars.value("SOURCES");
}

bool QMakeDataProvider::isFlat() const
{
    return d->m_flat;
}

bool QMakeDataProvider::isValid() const
{
    return d->m_valid;
}

////////////////////////////////////////////////////////////////////////////////
// BEGIN -- Modified copy from QMake's main.cpp
Q_GLOBAL_STATIC(QString, globalPwd);

QString qmake_getpwd()
{
    QString & pwd = *(globalPwd());
    if(pwd.isNull())
        pwd = QDir::currentPath();
    return pwd;
}

bool qmake_setpwd(const QString &p)
{
    if(QDir::setCurrent(p)) {
        QString & pwd = *(globalPwd());
        pwd = QDir::currentPath();
        return true;
    }
    return false;
}
// END -- from QMake's main.cpp
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// BEGIN -- Copy from QMake's generators/projectgenerator.cpp
QString project_builtin_regx() //calculate the builtin regular expression..
{
    QString ret;
    QStringList builtin_exts;
    builtin_exts << Option::c_ext << Option::ui_ext << Option::yacc_ext << Option::lex_ext << ".ts" << ".xlf" << ".qrc";
    builtin_exts += Option::h_ext + Option::cpp_ext;
    for(int i = 0; i < builtin_exts.size(); ++i) {
        if(!ret.isEmpty())
            ret += "; ";
        ret += QString("*") + builtin_exts[i];
    }
    return ret;
}
// END -- from QMake's generators/projectgenerator.cpp
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// BEGIN -- Modified copy from QLibraryInfo
QString
QLibraryInfo::rawLocation(LibraryLocation loc, PathGroup group)
{
    return ""; // Modified to return empty string
}

// END -- from QLibraryInfo
////////////////////////////////////////////////////////////////////////////////
