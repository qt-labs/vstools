#ifndef QMAKEWRAPPER_H
#define QMAKEWRAPPER_H

#include <QtWidgets/QWidget>

class QMakeDataProvider;

class QMakeWrapper : public QWidget
{
    Q_OBJECT
    Q_CLASSINFO("ClassID", "{33BE6C6F-E878-4F76-9676-9D78C44C4086}")
    Q_CLASSINFO("InterfaceID", "{7BE63374-3234-44CC-9A68-F68E01D3BAF1}")
    Q_CLASSINFO("EventsID", "{CA15864E-AE89-409F-BE96-F212931383A9}")

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
