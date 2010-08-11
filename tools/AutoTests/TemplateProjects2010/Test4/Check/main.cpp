#include "test.h"
#include <QtGui/QApplication>
#include "foo.h"

class Bar : public QWidget
{
	Q_OBJECT

public:
	Bar(QWidget *parent = 0, Qt::WFlags flags = 0);
	~Bar();

private:
	Ui::FooClass ui;
};

int main(int argc, char *argv[])
{
	QApplication a(argc, argv);
	Foo w;
	w.show();
	return a.exec();
}
