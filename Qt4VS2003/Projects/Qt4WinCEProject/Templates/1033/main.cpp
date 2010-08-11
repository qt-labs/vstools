#include "%INCLUDE%"
#include <QtGui/QApplication>

int main(int argc, char *argv[])
{
    QApplication a(argc, argv);
    %CLASS% w;
    w.showMaximized();
    return a.exec();
}
