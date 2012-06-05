#include "%INCLUDE%"

%NAMESPACE_BEGIN%%CLASS%::%CLASS%(QWidget *parent)
    : %BASECLASS%(parent)
{
	ui.setupUi(this);
}

%CLASS%::~%CLASS%()
{

}
%NAMESPACE_END%