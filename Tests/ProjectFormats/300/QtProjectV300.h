#pragma once

#include <QtWidgets/QMainWindow>
#include "ui_QtProjectV300.h"

class QtProjectV300 : public QMainWindow
{
	Q_OBJECT

public:
	QtProjectV300(QWidget *parent = Q_NULLPTR);

private:
	Ui::QtProjectV300Class ui;
};
