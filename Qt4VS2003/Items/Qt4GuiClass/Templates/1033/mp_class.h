#ifndef %PRE_DEF%
#define %PRE_DEF%

#include <%BASECLASS%>
namespace Ui {class %CLASS%Class;};

%NAMESPACE_BEGIN%class %CLASS% : public %BASECLASS%
{
    Q_OBJECT

public:
    %CLASS%(QWidget *parent = 0);
    ~%CLASS%();

private:
    Ui::%CLASS%Class *ui;
};

%NAMESPACE_END%#endif // %PRE_DEF%
