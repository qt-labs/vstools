//#include "test.h"
#include <QObject>

class Sub: public QObject
{
    Q_OBJECT
public:
    Sub(QObject *parent = 0)
        : QObject(parent)
    {}
};

#include "sub.moc"