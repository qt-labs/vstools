#include <QObject>

class Sub: public QObject
{
    //Q_OBJECT_HERE
public:
    Sub(QObject *parent = 0)
        : QObject(parent)
    {}
};

//#include "sub.moc"