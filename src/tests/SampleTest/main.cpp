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

#include <QtTest>
#include <MacroClient.h>

class TestSample : public QObject
{
    Q_OBJECT

private:
    MacroClient client;

private slots:
    void initTestCase()
    {
        qint64 pid = 0;
        QVERIFY(client.connect(&pid));

        client.runMacro(QString() % "//# var QtConfPath => @\"" % QT_CONF_PATH % "\"");

        QFile macroQtVsToolsLoaded(":/QtVsToolsLoaded");
        QCOMPARE(client.runMacro(macroQtVsToolsLoaded), MACRO_OK);
    }

    void tutorial01TestCase()
    {
        QSKIP("tutorial");
        QCOMPARE(client.runMacro(QString()
            % "//# using System.Windows.Forms\r\n"
            % "MessageBox.Show(\"Hello from Visual Studio!!\");"),
            MACRO_OK);
    }

    void tutorial02TestCase()
    {
        QSKIP("tutorial");
        QCOMPARE(client.runMacro(QString()
            % "//# using System.Windows.Forms\r\n"
            % "var task = Task.Run(() => MessageBox.Show(\"Hello, close this in 15 secs!!\"));\r\n"
            % "//# wait 15000 => task.IsCompleted"),
            MACRO_OK);
    }

    void tutorial03TestCase()
    {
        QSKIP("tutorial");
        QCOMPARE(client.runMacro(
            "Result = Environment.CurrentDirectory.Replace(\"\\\\\", \"/\");"),
            QDir::currentPath());
    }

    void tutorial04TestCase()
    {
        QSKIP("tutorial");
        QCOMPARE(client.runMacro("//# var InitTime => DateTime.Now"),
            MACRO_OK);

        QCOMPARE(client.runMacro(QString()
            % "//# using System.Windows.Forms\r\n"
            % "//# var InitTime\r\n"
            % "MessageBox.Show(\"Test started at \" + InitTime);"),
            MACRO_OK);
    }

    void tutorial05TestCase()
    {
        QSKIP("tutorial");
        QCOMPARE(client.runMacro(QString()
            % "//# using System.Windows.Forms\r\n"
            % "Task.Run(() => MessageBox.Show(\"Press OK to close.\", \"Hello\"));\r\n"
            % "//# ui context DESKTOP => \"Hello\", \"OK\"\r\n"
            % "UiContext.SetFocus();\r\n"),
            MACRO_OK);
    }

    void guiAppCreate_Rebuild_Debug()
    {
        client.runMacro("//# wait 5000 => !Dte.Solution.IsOpen");
        QFile macroCreateGuiApp(":/CreateGuiApp"),
            macroRebuildSolution(":/RebuildSolution"),
            macroDebugGuiApp(":/DebugGuiApp");
        QCOMPARE(client.runMacro(macroCreateGuiApp), MACRO_OK);
        QCOMPARE(client.runMacro(macroRebuildSolution), MACRO_OK);
        QCOMPARE(client.runMacro(macroDebugGuiApp), MACRO_OK);
        client.runMacro(
            "Dte.Solution.Close(false);"                "\r\n"
            "//# wait 15000 => !Dte.Solution.IsOpen"    "\r\n");
    }

    void importProFile_Rebuild_Debug()
    {
        QSKIP("foo");
        QFile macroImportProFile(":/ImportProFile"),
            macroRebuildSolution(":/RebuildSolution"),
            macroDebugGuiApp(":/DebugGuiApp");
        QCOMPARE(client.runMacro(macroImportProFile), MACRO_OK);
        QCOMPARE(client.runMacro(macroRebuildSolution), MACRO_OK);
        QCOMPARE(client.runMacro(macroDebugGuiApp), MACRO_OK);
        client.runMacro(
            "Dte.Solution.Close(false);"                "\r\n"
            "//# wait 15000 => !Dte.Solution.IsOpen"    "\r\n");
    }

    void cleanupTestCase()
    {
#ifdef QT_NO_DEBUG
        client.runMacro("//#quit");
        client.disconnect(true);
#endif
    }
};

QTEST_MAIN(TestSample)
#include "main.moc"
