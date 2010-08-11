#ifndef %PRE_DEF%
#define %PRE_DEF%

#include <%BASECLASS%>
#include "%UI_HDR%"

using namespace Ui;

%NAMESPACE_BEGIN%class %CLASS% : public %BASECLASS%, public %CLASS%Class
{
    Q_OBJECT

public:
    %CLASS%(QWidget *parent = 0);
    ~%CLASS%();
};

%NAMESPACE_END%#endif // %PRE_DEF%
