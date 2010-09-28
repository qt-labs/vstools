#ifndef %PRE_DEF%
#define %PRE_DEF%

#include <%BASECLASS%>
#include "%UI_HDR%"

%NAMESPACE_BEGIN%class %CLASS% : public %BASECLASS%, public Ui::%CLASS%
{
    Q_OBJECT

public:
    %CLASS%(QWidget *parent = 0);
    ~%CLASS%();
};

%NAMESPACE_END%#endif // %PRE_DEF%
