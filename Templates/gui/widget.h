#pragma once

#include <QtWidgets/$baseclass$>
#include "$ui_hdr$"

class $classname$ : public $baseclass$
{
    Q_OBJECT

public:
    $classname$(QWidget *parent = Q_NULLPTR);

private:
    Ui::$classname$Class ui;
};
