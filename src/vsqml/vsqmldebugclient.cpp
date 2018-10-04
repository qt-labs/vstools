/****************************************************************************
**
** Copyright (C) 2018 The Qt Company Ltd.
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
#include "vsqmldebugclient.h"

#include <QMutex>
#include <QMutexLocker>
#include <QTimer>

#include <QtQmlDebug/private/qqmldebugclient_p.h>
#include <QtQmlDebug/private/qqmldebugconnection_p.h>
#include <QtPacketProtocol/private/qpacket_p.h>

class VsQmlDebugClientPrivate : public QQmlDebugClient
{
    Q_OBJECT

public:
    VsQmlDebugClient *q;
    QString hostName;
    quint16 hostPort;
    QTimer checkConnection;

    VsQmlDebugClientPrivate(VsQmlDebugClient *debugClient, QQmlDebugConnection *debugConnection)
        : q(debugClient),
        QQmlDebugClient("V8Debugger", debugConnection)
    {
        connect(this, &QQmlDebugClient::stateChanged, this, &VsQmlDebugClientPrivate::stateChanged);

        checkConnection.setInterval(1000);
        checkConnection.setSingleShot(false);
        connect(&checkConnection, &QTimer::timeout,
            this, &VsQmlDebugClientPrivate::checkConnectionState);
    }

    void messageReceived(const QByteArray &data) override;

public slots:
    void stateChanged(State state);
    void checkConnectionState();
};

VsQmlDebugClient::VsQmlDebugClient(QObject *parent)
    : d(new VsQmlDebugClientPrivate(this, new QQmlDebugConnection(this))),
    QObject(parent)
{ }

VsQmlDebugClient::~VsQmlDebugClient()
{
    delete d;
}

void VsQmlDebugClient::connectToHost(const QString &hostName, quint16 port)
{
    if (!d->connection()->isConnected()) {
        d->hostName = hostName;
        d->hostPort = port;
        d->connection()->connectToHost(hostName, port);
    }
}

void VsQmlDebugClient::disconnectFromHost()
{
    d->connection()->close();
    emit disconnected();
}

void VsQmlDebugClient::sendMessage(
    const QByteArray &messageType,
    const QByteArray &messageParams)
{
    QByteArray packetType = "V8DEBUG";
    QPacket messageEnvelope(d->connection()->currentDataStreamVersion());
    messageEnvelope << packetType << messageType << messageParams;

    d->sendMessage(messageEnvelope.data());
}

void VsQmlDebugClientPrivate::messageReceived(const QByteArray &data)
{
    QPacket messageEnvelope(connection()->currentDataStreamVersion(), data);
    QByteArray packetType;
    messageEnvelope >> packetType;
    if (packetType == "V8DEBUG") {
        QByteArray messageType;
        QByteArray messageParams;
        messageEnvelope >> messageType >> messageParams;
        emit q->messageReceived(messageType, messageParams);
    }
}

void VsQmlDebugClientPrivate::stateChanged(State state)
{
    switch (state)
    {
    case QQmlDebugClient::Unavailable:
    case QQmlDebugClient::NotConnected:
        emit q->disconnected();
        break;
    case QQmlDebugClient::Enabled:
        checkConnection.start();
        emit q->connected();
        break;
    default:
        break;
    }
}

void VsQmlDebugClientPrivate::checkConnectionState()
{
    if (!connection()->isConnected()) {
        checkConnection.stop();
        emit q->disconnected();
    }
}


#include "vsqmldebugclient.moc"
