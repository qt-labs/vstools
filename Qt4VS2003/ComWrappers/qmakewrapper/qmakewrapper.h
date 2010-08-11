#ifndef QMAKEWRAPPER_H
#define QMAKEWRAPPER_H

#include <QtGui/QWidget>

class QMakeDataProvider;

class QMakeWrapper : public QWidget
{
    Q_OBJECT
    Q_CLASSINFO("ClassID", "{3DE52853-C379-41ac-8FB8-41FE8DEE6389}")
    Q_CLASSINFO("InterfaceID", "{3B12CAFA-2475-4696-A42C-A330A44C75CA}")
    Q_CLASSINFO("EventsID", "{D56442BF-E71C-4b3d-A50A-2960D218F72A}")

public:
    QMakeWrapper(QWidget *parent = 0);

public slots:
    bool readFile(const QString &fileName);
    void setQtDir(const QString&);
    QStringList sourceFiles() const;
    QStringList headerFiles() const;
    QStringList formFiles() const;
    QStringList resourceFiles() const;
    bool isValid() const;
    bool isFlat() const;

private:
    QMakeDataProvider *m_qmakeDataProvider;
};

#endif
