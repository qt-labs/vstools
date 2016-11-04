/****************************************************************************
**
** Copyright (C) 2016 The Qt Company Ltd.
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

namespace QtProjectLib
{
    public enum QtModule
    {
        Invalid = -1,
        Core = 1,
        Xml = 2,
        Sql = 3,
        OpenGL = 4,
        Network = 5,
        // Compat = 6,
        Gui = 7,
        ActiveQtS = 8,
        ActiveQtC = 9,
        Main = 10,
        // Qt3Library = 11,    // ### unused
        // Qt3Main = 12,       // ### unused
        Svg = 13,
        Designer = 14,
        Test = 15,
        Script = 16,
        Help = 17,
        WebKit = 18,
        XmlPatterns = 19,
        Enginio = 20,
        Multimedia = 21,
        Declarative = 22,
        ScriptTools = 23,
        UiTools = 24,

        Widgets = 25,
        ThreeD = 26,
        Location = 27,
        Nfc = 28,
        Qml = 29,
        Bluetooth = 30,
        Positioning = 31,
        SerialPort = 32,
        PrintSupport = 33,
        WebChannel = 34,
        WebSockets = 35,
        Sensors = 36,
        WindowsExtras = 37,
        QuickWidgets = 38,
        // JSBackend = 39,
        Quick = 40,
        ThreeDQuick = 41,
        // Feedback = 42,
        // QA = 43,
        // QLALR = 44,
        // RepoTools = 45,
        // Translations = 46,
        // CLucene = 48,
        // DesignerComponents = 49,
        WebkitWidgets = 50,
        Concurrent = 51,
        MultimediaWidgets = 52
    }
}
