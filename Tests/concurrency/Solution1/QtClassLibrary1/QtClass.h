#pragma once
#include "qtclasslibrary1_global.h"

#include <QObject>

class QTCLASSLIBRARY1_EXPORT QtClass : public QObject
{
    Q_OBJECT

public:
    QtClass(QObject *parent);
    ~QtClass();
};
