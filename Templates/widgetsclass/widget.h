#pragma once

#include <$baseclass$>
#include "$ui_hdr$"
$forward_declare_class$
$namespacebegin$class $classname$ : public $baseclass$$multiple_inheritance$
{$qobject$
public:
    $classname$(QWidget *parent = nullptr);
    ~$classname$();

private:
    $ui_classname$ $asterisk$$member$$semicolon$
};
$namespaceend$