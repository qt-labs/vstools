#include "QtWidgetsApplication2.h"
#include "QtClass.h"

QtWidgetsApplication2::QtWidgetsApplication2(QWidget *parent)
    : QMainWindow(parent)
{
    ui.setupUi(this);
    qtObject = new QtClass(this);
}
