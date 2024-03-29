/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QString>
#include <QCloseEvent>

namespace SharedTools {
    class QrcEditor;
}

class MainWindow : public QMainWindow
{
    Q_OBJECT

public:
    MainWindow();
    void openFile(QString fileName);

protected:
    void closeEvent(QCloseEvent *e);

private slots:
    void slotOpen();
    void slotSave();
    void slotAbout();
    void slotAboutQt();

private:
    int fileChangedDialog();

private:
    SharedTools::QrcEditor *m_qrcEditor;
    QString                 m_devenvPIDArg;
};

#endif // MAINWINDOW_H
