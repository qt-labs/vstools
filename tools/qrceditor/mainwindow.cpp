/****************************************************************************
**
** Copyright (C) 2012 Digia Plc and/or its subsidiary(-ies).
** Contact: http://www.qt-project.org/legal
**
** This file is part of the Qt VS Add-in.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and Digia.  For licensing terms and
** conditions see http://qt.digia.com/licensing.  For further information
** use the contact form at http://qt.digia.com/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 as published by the Free Software
** Foundation and appearing in the file LICENSE.LGPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU Lesser General Public License version 2.1 requirements
** will be met: http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** In addition, as a special exception, Digia gives you certain additional
** rights.  These rights are described in the Digia Qt LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3.0 as published by the Free Software
** Foundation and appearing in the file LICENSE.GPL included in the
** packaging of this file.  Please review the following information to
** ensure the GNU General Public License version 3.0 requirements will be
** met: http://www.gnu.org/copyleft/gpl.html.
**
**
** $QT_END_LICENSE$
**
****************************************************************************/

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
        statusBar()->showMessage(tr("Unable to open %1!").arg(fileName));
}

void MainWindow::slotOpen()
{
    const QString fileName = QFileDialog::getOpenFileName(this, tr("Choose resource file"),
                                                          QString(),
                                                          tr("Resource files (*.qrc)"));
    this->openFile(fileName);
}

void MainWindow::slotSave()
{
    const QString oldFileName = m_qrcEditor->fileName();
    QString fileName = oldFileName;

    if (fileName.isEmpty()) {
        fileName = QFileDialog::getSaveFileName(this, tr("Save resource file"),
                                                QString(),
                                                tr("Resource files (*.qrc)"));
        if (fileName.isEmpty())
            return;
    }

    m_qrcEditor->setFileName(fileName);
    if (m_qrcEditor->save()) {
        statusBar()->showMessage(tr("%1 written").arg(fileName));
        sendFileNameToQtAppWrapper();
    } else {
        statusBar()->showMessage(tr("Unable to write %1!").arg(fileName));
        m_qrcEditor->setFileName(oldFileName);
    }
}

void MainWindow::slotAbout()
{
    QMessageBox::about(this, tr("About Qt Resource Editor"),
        tr("Qt Resource Editor") + "\n\n" + tr("Copyright (C) 2013 Digia Plc and/or its subsidiary(-ies)"));
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
    message.setInformativeText(tr("Do you want the changes to be saved?"));
    message.setStandardButtons(QMessageBox::Yes | QMessageBox::No | QMessageBox::Cancel);
    message.setDefaultButton(QMessageBox::Yes);
    return message.exec();
}

void MainWindow::sendFileNameToQtAppWrapper()
{
    if (m_qtAppWrapperPath.isNull()) {
        // Try to find qtappwrapper.exe
        m_qtAppWrapperPath = QCoreApplication::applicationDirPath();
        m_qtAppWrapperPath += QLatin1String("/qtappwrapper.exe");
        if (!QFile::exists(m_qtAppWrapperPath)) {
            m_qtAppWrapperPath.clear();
            qWarning("Can't locate qtappwrapper.exe.");
            return;
        }
    }

    if (m_devenvPIDArg.isNull()) {
        HANDLE hSnapShot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (hSnapShot == INVALID_HANDLE_VALUE) {
            qWarning("CreateToolhelp32Snapshot failed.");
            return;
        }
        BOOL bSuccess;
        const DWORD dwThisPID = QCoreApplication::applicationPid();
        PROCESSENTRY32 processEntry;
        processEntry.dwSize = sizeof(processEntry);
        bSuccess = Process32First(hSnapShot, &processEntry);
        while (bSuccess) {
            if (processEntry.th32ProcessID == dwThisPID) {
                m_devenvPIDArg = QLatin1String("-pid ");
                m_devenvPIDArg += QString::number(processEntry.th32ParentProcessID);
                break;
            }
            bSuccess = Process32Next(hSnapShot, &processEntry);
        }
        CloseHandle(hSnapShot);

        if (m_devenvPIDArg.isNull()) {
            qWarning("Couldn't determine parent's process id.");
            return;
        }
    }

    if (!QProcess::startDetached(m_qtAppWrapperPath, QStringList() << m_qrcEditor->fileName() << m_devenvPIDArg)) {
        qWarning("Couldn't start qtappwrapper.exe.");
    }
}
