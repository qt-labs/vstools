#include <QtGui/QWidget>

class Bar : public QWidget
{
    //Q_OBJECT_HERE

public:
	Bar(QWidget *parent = 0, Qt::WFlags flags = 0);
	~Bar();
};

//#include "bar.moc"