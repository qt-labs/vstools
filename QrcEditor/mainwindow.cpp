/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include "mainwindow.h"
#include "qrceditor.h"

#include <QAction>
#include <QDebug>
#include <QFileDialog>
#include <QMenuBar>
#include <QStatusBar>
#include <QVBoxLayout>
#include <QMessageBox>
#include <QToolBar>
#include <QProcess>

#include <windows.h>
#include <Tlhelp32.h>

MainWindow::MainWindow() :
    m_qrcEditor(new  SharedTools::QrcEditor())
{
    m_qrcEditor->setResourceDragEnabled(true);
    setWindowTitle(tr("Qt Resource Editor"));
    QMenu* fMenu = menuBar()->addMenu(tr("&File"));
    QToolBar* tb = new QToolBar("Title", this);
    tb->setMovable(false);
    addToolBar(Qt::TopToolBarArea, tb);

    QAction* oa = fMenu->addAction(tr("&Open..."));
    oa->setShortcut(tr("Ctrl+O", "File|Open"));
    oa->setIcon(style()->standardIcon(QStyle::SP_DialogOpenButton));
    tb->addAction(oa);
    connect(oa, SIGNAL(triggered()), this, SLOT(slotOpen()));

    QAction* sa = fMenu->addAction(tr("&Save"));
    sa->setShortcut(tr("Ctrl+S", "File|Save"));
    sa->setIcon(style()->standardIcon(QStyle::SP_DialogSaveButton));
    tb->addAction(sa);
    connect(sa, SIGNAL(triggered()), this, SLOT(slotSave()));

    fMenu->addSeparator();

    QAction* xa = fMenu->addAction(tr("E&xit"));
    xa->setIcon(style()->standardIcon(QStyle::SP_DialogCloseButton));
    connect(xa, SIGNAL(triggered()), this, SLOT(close()));

    QMenu* hMenu = menuBar()->addMenu(tr("&Help"));
    QAction* actionAbout = hMenu->addAction(tr("&About"));
    connect(actionAbout, SIGNAL(triggered()), this, SLOT(slotAbout()));

    QAction* actionAboutQt = hMenu->addAction(tr("A&bout Qt"));
    connect(actionAboutQt, SIGNAL(triggered()), this, SLOT(slotAboutQt()));

    QWidget *cw = new QWidget();
    setCentralWidget(cw);
    QVBoxLayout *lt = new QVBoxLayout(cw);
    lt->addWidget(m_qrcEditor);
    setMinimumSize(QSize(500, 500));
}

void MainWindow::openFile(QString fileName)
{
    if (fileName.isEmpty())
    return;

    if (m_qrcEditor->isDirty()) {
        int ret = fileChangedDialog();
        switch (ret) {
            case QMessageBox::Yes:
                slotSave();
            case QMessageBox::No:
            break;
            default:
                return;
            break;
        }
    }

    if (m_qrcEditor->load(fileName)) {
        statusBar()->showMessage(tr("%1 opened").arg(fileName));
        QFileInfo fi(fileName);
        setWindowTitle(tr("Qt Resource Editor") + " - " + fi.fileName());
    }
    else
        statusBar()->showMessage(tr("Unable to open %1.").arg(fileName));
}

void MainWindow::slotOpen()
{
    const QString fileName = QFileDialog::getOpenFileName(this, tr("Choose Resource File"),
                                                          QString(),
                                                          tr("Resource files (*.qrc)"));
    this->openFile(fileName);
}

void MainWindow::slotSave()
{
    const QString oldFileName = m_qrcEditor->fileName();
    QString fileName = oldFileName;

    if (fileName.isEmpty()) {
        fileName = QFileDialog::getSaveFileName(this, tr("Save Resource File"),
                                                QString(),
                                                tr("Resource files (*.qrc)"));
        if (fileName.isEmpty())
            return;
    }

    m_qrcEditor->setFileName(fileName);
    if (m_qrcEditor->save()) {
        statusBar()->showMessage(tr("%1 written").arg(fileName));
    } else {
        statusBar()->showMessage(tr("Unable to write %1.").arg(fileName));
        m_qrcEditor->setFileName(oldFileName);
    }
}

void MainWindow::slotAbout()
{
    QMessageBox::about(this, tr("About Qt Resource Editor"),
        tr("Qt Resource Editor") + "\n\n" + tr("Copyright (C) 2016 The Qt Company Ltd."));
}

void MainWindow::slotAboutQt()
{
    QMessageBox::aboutQt(this);
}

void MainWindow::closeEvent(QCloseEvent *e)
{
    if (m_qrcEditor->isDirty()) {
        int ret = fileChangedDialog();
        switch (ret) {
            case QMessageBox::Yes:
                slotSave();
            case QMessageBox::No:
                QMainWindow::close();
            break;
            default:
                e->ignore();
                return;
            break;
        }
    }
    e->accept();
}

int MainWindow::fileChangedDialog()
{
    QMessageBox message(this);
    message.setText(tr("The .qrc file has been modified."));
    message.setWindowTitle("Qt Resource Editor");
    message.setInformativeText(tr("Do you want to save the changes?"));
    message.setStandardButtons(QMessageBox::Yes | QMessageBox::No | QMessageBox::Cancel);
    message.setDefaultButton(QMessageBox::Yes);
    return message.exec();
}
