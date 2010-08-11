#ifndef FOO_H
#define FOO_H

#include <QtGui/QWidget>
#include "ui_foo.h"

class Foo : public QWidget
{
	//Q_OBJECT_HERE

public:
	Foo(QWidget *parent = 0, Qt::WFlags flags = 0);
	~Foo();

private:
	Ui::FooClass ui;
};

#endif // FOO_H
