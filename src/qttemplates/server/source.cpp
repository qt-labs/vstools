#include "$include$"

#include <ActiveQt/QAxFactory>

$classname$::$classname$(QWidget *parent)
    : QWidget(parent)
{
    ui.setupUi(this);
}

QAXFACTORY_DEFAULT($classname$,
    "{$guid1$}",
    "{$guid2$}",
    "{$guid3$}",
    "{$guid4$}",
    "{$guid5$}"
)
