#pragma once

#include <QtWidgets/QMainWindow>
#include "ui_QtProjectV303.h"

class QtProjectV303 : public QMainWindow
{
    Q_OBJECT

public:
    QtProjectV303(QWidget *parent = Q_NULLPTR);

private:
    Ui::QtProjectV303Class ui;
};
