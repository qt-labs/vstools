/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
** Contact: http://www.qt.io/licensing/
**
** This file is part of the QtSensors module of the Qt Toolkit.
**
** $QT_BEGIN_LICENSE:BSD$
** You may use this file under the terms of the BSD license as follows:
**
** "Redistribution and use in source and binary forms, with or without
** modification, are permitted provided that the following conditions are
** met:
**   * Redistributions of source code must retain the above copyright
**     notice, this list of conditions and the following disclaimer.
**   * Redistributions in binary form must reproduce the above copyright
**     notice, this list of conditions and the following disclaimer in
**     the documentation and/or other materials provided with the
**     distribution.
**   * Neither the name of The Qt Company Ltd nor the names of its
**     contributors may be used to endorse or promote products derived
**     from this software without specific prior written permission.
**
**
** THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
** "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
** LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
** A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
** OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
** SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
** LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
** DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
** THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
** (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
** OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE."
**
** $QT_END_LICENSE$
**
****************************************************************************/

#include "addressbook.h"
#include "adddialog.h"

AddressBook::AddressBook(QWidget *parent)
    : QWidget(parent)
{
    ui.setupUi(this);
}

AddressBook::~AddressBook()
{
}

void AddressBook::on_addButton_clicked()
{
    AddDialog dialog(this);

    if (dialog.exec()) {
        QString name = dialog.nameEdit->text();
        QString email = dialog.emailEdit->text();

        if (!name.isEmpty() && !email.isEmpty()) {
            QListWidgetItem *item = new QListWidgetItem(name, ui.addressList);
            item->setData(Qt::UserRole, email);
            ui.addressList->setCurrentItem(item);
        }
    }
}

void AddressBook::on_addressList_currentItemChanged()
{
    QListWidgetItem *curItem = ui.addressList->currentItem();

    if (curItem) {
        ui.nameLabel->setText("Name: " + curItem->text());
        ui.emailLabel->setText("Email: " + curItem->data(Qt::UserRole).toString());
    } else {
        ui.nameLabel->setText("<No item selected>");
        ui.emailLabel->clear();
    }
}

void AddressBook::on_deleteButton_clicked()
{
    QListWidgetItem *curItem = ui.addressList->currentItem();

    if (curItem) {
        int row = ui.addressList->row(curItem);
        ui.addressList->takeItem(row);
        delete curItem;

        if (ui.addressList->count() > 0)
            ui.addressList->setCurrentRow(0);
        else
            on_addressList_currentItemChanged();
    }
}
