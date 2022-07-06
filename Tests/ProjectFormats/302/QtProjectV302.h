#pragma once

#include <QtWidgets/QMainWindow>
#include "ui_QtProjectV302.h"

class QtProjectV302 : public QMainWindow
{
    Q_OBJECT

public:
    QtProjectV302(QWidget *parent = Q_NULLPTR);

private:
    Ui::QtProjectV302Class ui;
};
