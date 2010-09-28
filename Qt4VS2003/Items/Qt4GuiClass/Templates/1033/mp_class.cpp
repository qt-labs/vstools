#include "%INCLUDE%"
#include "%UI_HDR%"

%NAMESPACE_BEGIN%%CLASS%::%CLASS%(QWidget *parent)
    : %BASECLASS%(parent)
{
	ui = new Ui::%CLASS%();
	ui->setupUi(this);
}

%CLASS%::~%CLASS%()
{
	delete ui;
}
%NAMESPACE_END%
