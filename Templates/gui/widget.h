#pragma once

#include <QtWidgets/$baseclass$>
#include "$ui_hdr$"
$forward_declare_class$
$namespacebegin$class $classname$ : public $baseclass$$multiple_inheritance$
{
    Q_OBJECT

public:
    $classname$(QWidget *parent = nullptr);
    ~$classname$();

private:
    $ui_classname$ $asterisk$$member$$semicolon$
};
$namespaceend$