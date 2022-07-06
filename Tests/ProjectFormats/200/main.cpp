#include "QtProjectV200.h"
#include <QtWidgets/QApplication>

int main(int argc, char *argv[])
{
	QApplication a(argc, argv);
	QtProjectV200 w;
	w.show();
	return a.exec();
}
