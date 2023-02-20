/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
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
