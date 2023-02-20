/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: BSD-3-Clause-Clear
***************************************************************************************************/

#pragma once

#include <QWidget>
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
