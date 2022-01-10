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

Popup {
    id: newAddressPopup
    modal: true
    focus: true
    width: parent.width * 0.9
    x: (parent.width - width) / 2
    y: 35
    onOpened: {
        nameField.text = "";
        addrField.text = "";
        nameField.focus = true;
    }

    signal addressAdded(string newName, string newAddr)

    ColumnLayout {
        anchors.fill: parent
        TextField {
            id: nameField
            placeholderText: qsTr("Name")
            font.pointSize: 24
            background: Rectangle { color: "lightgray" }
            Layout.preferredWidth: newAddressPopup / 2
            Layout.fillWidth: true
        }
        TextField {
            id: addrField
            placeholderText: qsTr("E-Mail Address")
            font.pointSize: 24
            background: Rectangle { color: "lightgray" }
            Layout.preferredWidth: newAddressPopup / 2
            Layout.fillWidth: true
        }
        RowLayout {
            anchors.left: parent.left; anchors.right: parent.right
            Button {
                text: "Add"
                enabled: nameField.length > 0 && addrField.length > 0
                font.pointSize: 24
                Layout.preferredWidth: newAddressPopup / 2
                Layout.fillWidth: true
                onClicked: {
                    newAddressPopup.addressAdded(nameField.text, addrField.text)
                    newAddressPopup.close()
                }
            }
            Button {
                text: "Cancel"
                font.pointSize: 24
                Layout.preferredWidth: newAddressPopup / 2
                Layout.fillWidth: true
                onClicked: newAddressPopup.close()
            }
        }
    }
}
