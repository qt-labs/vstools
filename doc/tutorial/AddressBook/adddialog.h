/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: BSD-3-Clause-Clear
***************************************************************************************************/

#pragma once

#include <QDialog>
#include "ui_adddialog.h"

class AddDialog : public QDialog, public Ui::AddDialog
{
    Q_OBJECT

public:
    AddDialog(QWidget *parent = nullptr);
    ~AddDialog();
};
