#ifndef %PRE_DEF%
#define %PRE_DEF%

#include <QtWidgets/QWidget>
#include <ActiveQt/QAxBindable>

#include "%UI_HDR%"

class %CLASS% : public QWidget, public QAxBindable
{
    Q_OBJECT

public:
    %CLASS%(QWidget *parent = 0);

private:
    Ui::%CLASS%Class ui;
};

#endif // %PRE_DEF%