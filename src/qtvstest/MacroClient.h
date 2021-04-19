/****************************************************************************
**
** Copyright (C) 2019 The Qt Company Ltd.
** Contact: https://www.qt.io/licensing/
**
** This file is part of the Qt VS Tools.
**
** $QT_BEGIN_LICENSE:GPL-EXCEPT$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see https://www.qt.io/terms-conditions. For further
** information use the contact form at https://www.qt.io/contact-us.
**
** GNU General Public License Usage
** Alternatively, this file may be used under the terms of the GNU
** General Public License version 3 as published by the Free Software
** Foundation with exceptions as appearing in the file LICENSE.GPL3-EXCEPT
** included in the packaging of this file. Please review the following
** information to ensure the GNU General Public License requirements will
** be met: https://www.gnu.org/licenses/gpl-3.0.html.
**
** $QT_END_LICENSE$
**
****************************************************************************/

#ifndef MACROCLIENT_H
#define MACROCLIENT_H

#include <QDir>
#include <QFile>
#include <QString>

#include <QElapsedTimer>
#include <QLocalSocket>
#include <QProcess>
#include <QThread>

#include <process.h>

#define MACRO_OK                    QStringLiteral("(ok)")
#define MACRO_ERROR                 QStringLiteral("(error)")
#define MACRO_ERROR_MSG(msg)        QStringLiteral("(error)\r\n" msg)

#define MACRO_ASSERT_OK(result)     QCOMPARE(result, MACRO_OK)

#define MACRO_GLOBALS(globalVars)           "//# macro Globals\r\n" globalVars
#define MACRO_GLOBAL_VAR(varName, varValue) "//# var string " varName " => " varValue "\r\n"


inline bool macroResultOk(QString result)
{
    return result == MACRO_OK;
}

inline bool macroResultError(QString result)
{
    return result.startsWith(MACRO_ERROR);
}

class MacroClient
{

public:
    MacroClient()
    {}

    ~MacroClient()
    {
        disconnect(false);
    }

    bool connect(qint64 *refPid = 0)
    {
        qint64 pid = 0;
        if (!refPid)
            refPid = &pid;

        if (*refPid == 0) {
            vsProcess.setProgram("devenv.exe");
            if (vsProcess.state() != QProcess::Running) {
                vsProcess.start();
                if (!vsProcess.waitForStarted())
                    return false;
            }
            *refPid = vsProcess.processId();
        } else if (!vsProcess.setProcessId(*refPid)) {
            return false;
        }

        QString pipeName = QStringLiteral("QtVSTest_%1").arg(*refPid);

        QElapsedTimer timer;
        timer.start();

        while (!timer.hasExpired(30000) && socket.state() != QLocalSocket::ConnectedState) {
            socket.connectToServer(pipeName, QIODevice::ReadWrite);
            if (socket.state() != QLocalSocket::ConnectedState) {
                socket.abort();
                QThread::usleep(100);
            }
        }

        return (socket.state() == QLocalSocket::ConnectedState);
    }

    void disconnect(bool closeVs)
    {
        if (socket.state() == QLocalSocket::ConnectedState)
            socket.disconnectFromServer();

        if (vsProcess.isRunning()) {
            if (closeVs)
                vsProcess.kill();
            else
                vsProcess.detach();
        }
    }

    QString runMacro(QString macroCode)
    {
        if (socket.state() != QLocalSocket::ConnectedState && !connect())
            return MACRO_ERROR_MSG("Disconnected");

        QByteArray data = macroCode.toUtf8();
        int size = data.size();

        socket.write(reinterpret_cast<const char *>(&size), sizeof(int));
        socket.write(data);
        socket.flush();

        if (socket.state() != QLocalSocket::ConnectedState)
            return MACRO_ERROR_MSG("Disconnected");

        while (socket.state() == QLocalSocket::ConnectedState && socket.bytesToWrite())
            socket.waitForBytesWritten(15000);
        if (socket.state() != QLocalSocket::ConnectedState)
            return MACRO_ERROR_MSG("Disconnected");

        while (socket.state() == QLocalSocket::ConnectedState && socket.bytesAvailable() < 4)
            socket.waitForReadyRead(15000);
        if (socket.state() != QLocalSocket::ConnectedState)
            return MACRO_ERROR_MSG("Disconnected");

        size = *reinterpret_cast<int *>(socket.read(4).data());

        while (socket.state() == QLocalSocket::ConnectedState && socket.bytesAvailable() < size)
            socket.waitForReadyRead(15000);
        if (socket.state() != QLocalSocket::ConnectedState)
            return MACRO_ERROR_MSG("Disconnected");

        data = socket.read(size);
        return QString::fromUtf8(data);
    }

    QString runMacro(QFile &macroFile)
    {
        return loadAndRunMacro(macroFile);
    }

    QString loadMacro(QFile &macroFile, QString macroName)
    {
        if (macroName.isNull() || macroName.isEmpty())
            return MACRO_ERROR_MSG("Invalid macro name");
        return loadAndRunMacro(macroFile, QString("//#macro %1").arg(macroName));
    }

    QString loadAndRunMacro(QFile &macroFile, QString macroHeader = QString())
    {
        if (!macroFile.open(QIODevice::ReadOnly | QIODevice::Text))
            return MACRO_ERROR_MSG("Macro load failed");
        QString macroCode = QString::fromUtf8(macroFile.readAll());
        macroFile.close();
        if (macroCode.isEmpty())
            return MACRO_ERROR_MSG("Macro load failed");
        if (!macroHeader.isNull())
            return runMacro(macroHeader + "\r\n" + macroCode);
        else
            return runMacro(macroCode);
    }

private:
    class QDetachableProcess : public QProcess
    {
    public:
        QDetachableProcess(QObject *parent = 0) : QProcess(parent), detachedPid(0)
        { }
        void detach()
        {
            if (isAttached()) {
                detachedPid = QProcess::processId();
                waitForStarted();
                setProcessState(QProcess::NotRunning);
            }
        }
        qint64 processId()
        {
            if (isAttached())
                return QProcess::processId();
            return detachedPid;
        }
        bool setProcessId(qint64 pid)
        {
            if (isAttached())
                return false;
            else if (!detachedIsRunning(pid))
                return false;
            detachedPid = pid;
            return true;
        }
        void kill()
        {
            if (isAttached()) {
                terminate();
                if (!waitForFinished(3000))
                    QProcess::kill();
            } else {
                killDetached();
            }
        }
        bool isRunning()
        {
            return (isAttached() || detachedIsRunning(detachedPid));
        }
    private:
        qint64 detachedPid;
        bool isAttached()
        {
            return (state() == QProcess::Running);
        }
        bool detachedIsRunning(qint64 pid)
        {
            if (pid == 0)
                return false;
            int errorLevel = system(qPrintable(QString(
                "tasklist /FI \"PID eq %1\" /FO LIST | find /I /N \"%1\" > NUL 2>&1")
                .arg(pid)));
            return (errorLevel == 0);
        }
        void killDetached()
        {
            if (detachedPid == 0)
                return;
            system(qPrintable(QString(
                "taskkill /PID %1 > NUL 2>&1")
                .arg(detachedPid)));
            detachedPid = 0;
        }
    };

    QDetachableProcess vsProcess;
    QLocalSocket socket;

}; // class MacroClient

#endif // MACROCLIENT_H
