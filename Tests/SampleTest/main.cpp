/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

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
