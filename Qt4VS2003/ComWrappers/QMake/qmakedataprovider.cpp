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

#include "qmakedataprovider.h"
#include <project.h>
#include <option.h>
#include <QString>
#include <QStringList>
#include <QMap>
#include <QDir>

class QMakeDataProviderPrivate
{
public:
    QMap<QString, QStringList> m_vars;
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
        Option::js_ext = ".js";
        Option::mkfile::do_cache = true;
        Option::mkfile::cachefile = m_qtdir + "\\.qmake.cache";

        // Enable 'flat' option for Visual Studio
        QMap<QString, QStringList> preDefs;
        preDefs.insert("CONFIG", QStringList("flat"));

        QMakeProject project(preDefs);
        m_valid = project.read(fileName);
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

