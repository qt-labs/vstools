#ifndef ADDDIALOG_H
#define ADDDIALOG_H

#include <QDialog>
#include "ui_adddialog.h"

using namespace Ui;

class AddDialog : public QDialog, public AddDialogClass
{
    Q_OBJECT

public:
    AddDialog(QWidget *parent = 0);
    ~AddDialog();
};

#endif // ADDDIALOG_H
