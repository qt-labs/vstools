#ifndef ADDRESSBOOK_H
#define ADDRESSBOOK_H

#include <QtGui/QWidget>
#include "ui_addressbook.h"

class AddressBook : public QWidget
{
    Q_OBJECT

public:
    AddressBook(QWidget *parent = 0);
    ~AddressBook();

private:
    Ui::AddressBookClass ui;

private slots:
	void on_deleteButton_clicked();
	void on_addButton_clicked();
	void on_addressList_currentItemChanged();
};

#endif // ADDRESSBOOK_H
