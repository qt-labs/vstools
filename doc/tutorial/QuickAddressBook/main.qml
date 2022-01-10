/****************************************************************************
**
** Copyright (C) 2022 The Qt Company Ltd.
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
import QtQuick 2.9
import QtQuick.Window 2.2
import QtQuick.Controls 2.5
import QtQuick.Layouts 1.12
import "QuickAddressBookTypes"

Window {
    id: mainWindow
    visible: true
    width: 480
    height: 640
    color: "darkgray"
    title: qsTr("Address Book")

    ListModel {
        id: addressList
    }

    NewAddressPopup {
        id: newAddressPopup
        onAddressAdded: addressList.append({name: newName, addr: newAddr})
    }

    ColumnLayout {
        id: mainWindowLayout
        anchors.left: parent.left; anchors.right: parent.right
        spacing: 0
        Button {
            id: addButton
            anchors.left: parent.left
            anchors.right: parent.right
            text: "Add..."
            font.pointSize: 24
            onClicked: newAddressPopup.open()
        }
        Repeater {
            id: addressListViewer
            model: addressList
            anchors.left: parent.left
            anchors.right: parent.right
            AddressBookItem {
                id: addressBookItem
                onRemoved: addressList.remove(index)
            }
        }
    }
}
