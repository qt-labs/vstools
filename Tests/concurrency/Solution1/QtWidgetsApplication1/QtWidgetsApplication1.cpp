#include "QtWidgetsApplication1.h"
#include "QtClass.h"
#include "Header.h"

QtWidgetsApplication1::QtWidgetsApplication1(QWidget *parent)
    : QMainWindow(parent)
{
    ui.setupUi(this);
    qtObject = new QtClass(this);
    foobar();
}
