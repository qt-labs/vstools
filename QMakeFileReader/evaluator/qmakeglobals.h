/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#ifndef QMAKEGLOBALS_H
#define QMAKEGLOBALS_H

#include "qmake_global.h"
#include "proitems.h"

#ifdef QT_BUILD_QMAKE
#  include <property.h>
#endif

#include <qhash.h>
#include <qstringlist.h>
#ifndef QT_BOOTSTRAPPED
# include <qprocess.h>
#endif
#ifdef PROEVALUATOR_THREAD_SAFE
# include <qmutex.h>
# include <qwaitcondition.h>
#endif

QT_BEGIN_NAMESPACE

class QMakeEvaluator;

class QMakeBaseKey
{
public:
    QMakeBaseKey(const QString &_root, bool _hostBuild);

    QString root;
    bool hostBuild;
};

uint qHash(const QMakeBaseKey &key);
bool operator==(const QMakeBaseKey &one, const QMakeBaseKey &two);

class QMakeBaseEnv
{
public:
    QMakeBaseEnv();
    ~QMakeBaseEnv();

#ifdef PROEVALUATOR_THREAD_SAFE
    QMutex mutex;
    QWaitCondition cond;
    bool inProgress;
    // The coupling of this flag to thread safety exists because for other
    // use cases failure is immediately fatal anyway.
    bool isOk;
#endif
    QMakeEvaluator *evaluator;
};

class QMAKE_EXPORT QMakeCmdLineParserState
{
public:
    QMakeCmdLineParserState(const QString &_pwd) : pwd(_pwd), after(false) {}
    QString pwd;
    QStringList precmds, preconfigs, postcmds, postconfigs;
    bool after;
};

class QMAKE_EXPORT QMakeGlobals
{
public:
    QMakeGlobals();
    ~QMakeGlobals();

    bool do_cache;
    QString dir_sep;
    QString dirlist_sep;
    QString cachefile;
#ifdef PROEVALUATOR_SETENV
    QProcessEnvironment environment;
#endif
    QString qmake_abslocation;

    QString qmakespec, xqmakespec;
    QString user_template, user_template_prefix;
    QString precmds, postcmds;

#ifdef PROEVALUATOR_DEBUG
    int debugLevel;
#endif

    enum ArgumentReturn { ArgumentUnknown, ArgumentMalformed, ArgumentsOk };
    ArgumentReturn addCommandLineArguments(QMakeCmdLineParserState &state,
                                           QStringList &args, int *pos);
    void commitCommandLineArguments(QMakeCmdLineParserState &state);
    void setCommandLineArguments(const QString &pwd, const QStringList &args);
    void useEnvironment();
    void setDirectories(const QString &input_dir, const QString &output_dir);
#ifdef QT_BUILD_QMAKE
    void setQMakeProperty(QMakeProperty *prop) { property = prop; }
    ProString propertyValue(const ProKey &name) const { return property->value(name); }
#else
#  ifdef PROEVALUATOR_INIT_PROPS
    bool initProperties();
#  else
    void setProperties(const QHash<QString, QString> &props);
#  endif
    ProString propertyValue(const ProKey &name) const { return properties.value(name); }
#endif

    QString expandEnvVars(const QString &str) const;
    QString shadowedPath(const QString &fileName) const;

private:
    QString getEnv(const QString &) const;
    QStringList getPathListEnv(const QString &var) const;

    QString cleanSpec(QMakeCmdLineParserState &state, const QString &spec);

    QString source_root, build_root;

#ifdef QT_BUILD_QMAKE
    QMakeProperty *property;
#else
    QHash<ProKey, ProString> properties;
#endif

#ifdef PROEVALUATOR_THREAD_SAFE
    QMutex mutex;
#endif
    QHash<QMakeBaseKey, QMakeBaseEnv *> baseEnvs;

    friend class QMakeEvaluator;
};

QT_END_NAMESPACE

#endif // QMAKEGLOBALS_H
