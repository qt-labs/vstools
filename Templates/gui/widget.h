#pragma once

#include <QtWidgets/$baseclass$>
#include "$ui_hdr$"

class $classname$ : public $baseclass$
{
    Q_OBJECT

public:
    $classname$(QWidget *parent = nullptr);

private:
    Ui::$classname$Class ui;
};
