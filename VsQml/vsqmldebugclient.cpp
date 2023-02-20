/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
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

void VsQmlDebugClient::startLocalServer(const QString &fileName)
{
    if (!d->connection()->isConnected())
        d->connection()->startLocalServer(fileName);
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
