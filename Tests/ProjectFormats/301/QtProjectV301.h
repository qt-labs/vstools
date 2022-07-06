#pragma once

#include <QtWidgets/QMainWindow>
#include "ui_QtProjectV301.h"

class QtProjectV301 : public QMainWindow
{
	Q_OBJECT

public:
	QtProjectV301(QWidget *parent = Q_NULLPTR);

private:
	Ui::QtProjectV301Class ui;
};
