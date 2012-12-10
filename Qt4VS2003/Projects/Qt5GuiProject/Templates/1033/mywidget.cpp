#include "%INCLUDE%"

%CLASS%::%CLASS%(QWidget *parent, Qt::WFlags flags)
    : %BASECLASS%(parent, flags)
{
    ui.setupUi(this);
}

%CLASS%::~%CLASS%()
{

}
