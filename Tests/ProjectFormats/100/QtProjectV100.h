#pragma once

#include <QtWidgets/QMainWindow>
#include "ui_QtProjectV100.h"

class QtProjectV100 : public QMainWindow
{
    Q_OBJECT

public:
    QtProjectV100(QWidget *parent = nullptr);
    ~QtProjectV100();

private:
    Ui::QtProjectV100Class ui;
};
