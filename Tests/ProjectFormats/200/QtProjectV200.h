#pragma once

#include <QtWidgets/QMainWindow>
#include "ui_QtProjectV200.h"

class QtProjectV200 : public QMainWindow
{
	Q_OBJECT

public:
	QtProjectV200(QWidget *parent = Q_NULLPTR);

private:
	Ui::QtProjectV200Class ui;
};
