#pragma once

#include <QtWidgets/QWidget>
#include <ActiveQt/QAxBindable>

#include "$ui_hdr$"

class $classname$ : public QWidget, public QAxBindable
{
    Q_OBJECT

public:
    $classname$(QWidget *parent = Q_NULLPTR);

private:
    Ui::$classname$Class ui;
};
