#ifndef %PRE_DEF%
#define %PRE_DEF%

#include <%BASECLASS%>
namespace Ui {class %CLASS%;};

%NAMESPACE_BEGIN%class %CLASS% : public %BASECLASS%
{
    Q_OBJECT

public:
    %CLASS%(QWidget *parent = 0);
    ~%CLASS%();

private:
    Ui::%CLASS% *ui;
};

%NAMESPACE_END%#endif // %PRE_DEF%
