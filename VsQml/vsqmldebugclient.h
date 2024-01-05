/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
#pragma once

#include <QObject>

class VsQmlDebugClientPrivate;

class VsQmlDebugClient : public QObject
{
    Q_OBJECT

public:
    VsQmlDebugClient(QObject *parent = nullptr);
    ~VsQmlDebugClient() override;

public slots:
    void connectToHost(const QString &hostName, quint16 port);
    void startLocalServer(const QString &fileName);
    void disconnectFromHost();
    void sendMessage(const QByteArray &messageType, const QByteArray &messageParams);

signals:
    void connected();
    void disconnected();
    void messageReceived(const QByteArray &messageType, const QByteArray &messageParams);

private:
    VsQmlDebugClientPrivate *d;
    friend class VsQmlDebugClientPrivate;
};
