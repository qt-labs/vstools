#include "%INCLUDE%"

#include <ActiveQt/QAxFactory>

%CLASS%::%CLASS%(QWidget *parent)
    : QWidget(parent)
{
    ui.setupUi(this);
}

QAXFACTORY_DEFAULT(%CLASS%,
	   "{%GUID0%}",
	   "{%GUID1%}",
	   "{%GUID2%}",
	   "{%GUID3%}",
	   "{%GUID4%}")