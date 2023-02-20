/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
import QtQuick 2.9
import QtQuick.Window 2.2
import QtQuick.Controls 2.5
import QtQuick.Layouts 1.12

Rectangle {
    id: addressBookItem
    color: (index % 2) == 0 ? "dimgray" : "lightgray"
    anchors.left: parent.left
    anchors.right: parent.right
    height: itemText.height + 12

    signal removed()

    RowLayout {
        spacing: 12
        anchors.left: parent.left
        anchors.leftMargin: spacing
        RoundButton {
            id: deleteButton
            text: "🗙"
            font.pointSize: 12
            palette.buttonText: "red"
            onClicked: addressBookItem.removed()
        }
        Text {
            id: itemText
            font.pointSize: 24
            text: "<b>" + name + "</b><br><i>" + addr + "</i>"
        }
    }
}
