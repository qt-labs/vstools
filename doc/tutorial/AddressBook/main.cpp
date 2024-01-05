/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: BSD-3-Clause-Clear
***************************************************************************************************/

#include <QApplication>
#include "addressbook.h"

int main(int argc, char *argv[])
{
    QApplication a(argc, argv);
    AddressBook w;
    w.show();
    return a.exec();
}
