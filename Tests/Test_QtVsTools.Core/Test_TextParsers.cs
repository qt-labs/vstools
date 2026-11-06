// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Core
{
    using QtVsTools.Core.Common;

    /// <summary>
    /// Pins the behavior of the small text parsers, independent of how they are implemented.
    /// </summary>
    [TestClass]
    public class Test_TextParsers
    {
        #region Utils.ParseEnvironment (debugging environment of a project)

        [TestMethod]
        public void ParseEnvironment_NameValueLines()
        {
            var env = Utils.ParseEnvironment("PATH=C:\\Qt\\bin;%PATH%\r\nQT_DEBUG_PLUGINS=1\r\n");

            Assert.HasCount(2, env);
            Assert.AreEqual("C:\\Qt\\bin;%PATH%", env["PATH"]);
            Assert.AreEqual("1", env["QT_DEBUG_PLUGINS"]);
        }

        [TestMethod]
        public void ParseEnvironment_ValueContainsEqualSign()
        {
            var env = Utils.ParseEnvironment("QT_LOGGING_RULES=qt.qml=true\n");
            Assert.AreEqual("qt.qml=true", env["QT_LOGGING_RULES"]);
        }

        [TestMethod]
        public void ParseEnvironment_EmptyValue()
        {
            var env = Utils.ParseEnvironment("EMPTY=\nOTHER=x");
            Assert.AreEqual("", env["EMPTY"]);
            Assert.AreEqual("x", env["OTHER"]);
        }

        [TestMethod]
        public void ParseEnvironment_NamesIgnoreCase_LastWins()
        {
            var env = Utils.ParseEnvironment("Path=a\npath=b\n");
            Assert.HasCount(1, env);
            Assert.AreEqual("b", env["PATH"]);
        }

        [TestMethod]
        public void ParseEnvironment_EmptyLinesAreIgnored()
        {
            var env = Utils.ParseEnvironment("\r\nA=1\r\n\r\nB=2");
            Assert.HasCount(2, env);
            Assert.AreEqual("2", env["B"]);
        }

        #endregion

        #region qmake -query

        [TestMethod]
        public void ParseQueryProperties_QMakeOutput()
        {
            var props = TextParsers.ParseQueryProperties(
                "QT_SYSROOT:\r\n"
                + "QT_INSTALL_PREFIX:C:/Qt/6.8.0/msvc2022_64\r\n"
                + "QT_INSTALL_PREFIX/raw:C:/Qt/6.8.0/msvc2022_64\r\n"
                + "QT_VERSION:6.8.0\r\n"
                + "QMAKE_XSPEC:win32-msvc\r\n");

            Assert.HasCount(5, props);
            Assert.AreEqual("", props["QT_SYSROOT"]);
            Assert.AreEqual("C:/Qt/6.8.0/msvc2022_64", props["QT_INSTALL_PREFIX"]);
            Assert.AreEqual("C:/Qt/6.8.0/msvc2022_64", props["QT_INSTALL_PREFIX/raw"]);
            Assert.AreEqual("6.8.0", props["QT_VERSION"]);
            Assert.AreEqual("win32-msvc", props["QMAKE_XSPEC"]);
        }

        [TestMethod]
        public void ParseQueryProperties_LastValueWins_LfLineEndings()
        {
            var props = TextParsers.ParseQueryProperties("QT_VERSION:6.7.0\nQT_VERSION:6.8.0\n");
            Assert.AreEqual("6.8.0", props["QT_VERSION"]);
        }

        [TestMethod]
        public void ParseQueryProperties_NoTrailingLineBreak()
        {
            var props = TextParsers.ParseQueryProperties("QT_VERSION:6.8.0");
            Assert.AreEqual("6.8.0", props["QT_VERSION"]);
        }

        #endregion

        #region vcvars environment

        [TestMethod]
        public void ParseVcVarsEnvironment_SplitsValuesAtSemicolons()
        {
            var env = TextParsers.ParseVcVarsEnvironment(
                "INCLUDE=C:\\VC\\include;C:\\SDK\\ucrt;\r\n"
                + "Platform=x64\r\n"
                + "VSCMD_ARG_TGT_ARCH=x64\r\n"
                + "EMPTY=\r\n");

            CollectionAssert.AreEqual(new[] { "C:\\VC\\include", "C:\\SDK\\ucrt" },
                env["INCLUDE"]);
            CollectionAssert.AreEqual(new[] { "x64" }, env["platform"]);
            Assert.IsEmpty(env["EMPTY"]);
        }

        [TestMethod]
        public void ParseVcVarsEnvironment_SkipsEmptyParts()
        {
            var env = TextParsers.ParseVcVarsEnvironment("PATH=;a;;b;\n");
            CollectionAssert.AreEqual(new[] { "a", "b" }, env["PATH"]);
        }

        [TestMethod]
        public void ParseVcVarsEnvironment_ValueContainsEqualSign()
        {
            var env = TextParsers.ParseVcVarsEnvironment("CL=/D X=1\r\n");
            CollectionAssert.AreEqual(new[] { "/D X=1" }, env["CL"]);
        }

        #endregion

        #region Dev releases

        [TestMethod]
        public void ParseDevReleaseVersions_HtmlListing()
        {
            var versions = TextParsers.ParseDevReleaseVersions(
                "<html><body><pre>\n"
                + "<a href=\"3.4.1/\">3.4.1/</a>   12-Sep-2025 10:00\n"
                + "<a href=\"3.5.0/\">3.5.0/</a>   01-Nov-2026 10:00\n"
                + "<a href=\"3.6.0\">3.6.0</a>\n"
                + "<a href=\"1.2.3.4/\">1.2.3.4/</a>\n"
                + "<a href=\"readme.txt\">readme.txt</a>\n"
                + "</pre></body></html>\n").ToList();

            CollectionAssert.AreEqual(new[] {
                new System.Version(3, 4, 1),
                new System.Version(3, 5, 0),
                new System.Version(3, 6, 0)
            }, versions);
        }

        [TestMethod]
        public void ParseDevReleaseVersions_NoVersions()
        {
            Assert.IsEmpty(TextParsers.ParseDevReleaseVersions("<html></html>").ToList());
        }

        #endregion

        #region CMake

        [TestMethod]
        [DataRow("find_package(Qt6 REQUIRED COMPONENTS Core Gui)", true)]
        [DataRow("find_package(Qt5 COMPONENTS Widgets REQUIRED)", true)]
        [DataRow("find_package(QT NAMES Qt6 Qt5 REQUIRED COMPONENTS Core)", true)]
        [DataRow("find_package( Qt6 )", true)]
        [DataRow("project(app)\nfind_package(\n    Qt6\n    REQUIRED)\n", true)]
        [DataRow("find_package(Qt6Core)", false)]
        [DataRow("find_package(Qt4)", false)]
        [DataRow("find_package(Boost REQUIRED)", false)]
        [DataRow("my_find_package(Qt6)", false)]
        [DataRow("", false)]
        public void HasQtFindPackage(string cmakeLists, bool expected)
        {
            Assert.AreEqual(expected, TextParsers.HasQtFindPackage(cmakeLists));
        }

        #endregion

        #region WSL paths

        [TestMethod]
        [DataRow("/mnt/c/Users/dev/app", @"c:\Users/dev/app")]
        [DataRow("/mnt/d/a", @"d:\a")]
        [DataRow("/home/dev/app", null)]
        [DataRow("x/mnt/c/app", null)]
        [DataRow(@"C:\dev\app.exe", null)]
        public void ConvertWslPath(string path, string expected)
        {
            Assert.AreEqual(expected, TextParsers.ConvertWslPath(path));
        }

        #endregion

        #region QML debugger

        [TestMethod]
        public void ParseQmlDebuggerArgs_PortOnly()
        {
            var args = TextParsers.ParseQmlDebuggerArgs("-qmljsdebugger=port:3768");
            Assert.AreEqual((ushort)3768, args.Port);
            Assert.IsNull(args.MaxPort);
            Assert.IsNull(args.Host);
            Assert.IsNull(args.File);
            Assert.IsFalse(args.Block);
        }

        [TestMethod]
        public void ParseQmlDebuggerArgs_AllParameters()
        {
            var args = TextParsers.ParseQmlDebuggerArgs(
                "app.exe -platform windows -qmljsdebugger=port:3768,3800,host:127.0.0.1,block -x");
            Assert.AreEqual((ushort)3768, args.Port);
            Assert.AreEqual((ushort?)3800, args.MaxPort);
            Assert.AreEqual("127.0.0.1", args.Host);
            Assert.IsTrue(args.Block);
        }

        [TestMethod]
        public void ParseQmlDebuggerArgs_HostBeforePort()
        {
            var args = TextParsers.ParseQmlDebuggerArgs("-qmljsdebugger=host:localhost,port:1234");
            Assert.AreEqual((ushort)1234, args.Port);
            Assert.AreEqual("localhost", args.Host);
        }

        [TestMethod]
        public void ParseQmlDebuggerArgs_LocalFile()
        {
            var args = TextParsers.ParseQmlDebuggerArgs("-qmljsdebugger=file:qmldebug,block");
            Assert.AreEqual("qmldebug", args.File);
            Assert.IsTrue(args.Block);
        }

        [TestMethod]
        [DataRow("app.exe -platform windows")]
        [DataRow("-qmljsdebugger=")]
        [DataRow("-qmljsdebugger=port:abc")]
        [DataRow("")]
        public void ParseQmlDebuggerArgs_NoParameters(string commandLine)
        {
            Assert.IsNull(TextParsers.ParseQmlDebuggerArgs(commandLine));
        }

        #endregion

        #region Qt/MSBuild projects

        [TestMethod]
        [DataRow("QtVS_v304", 304)]
        [DataRow("QtVS_v100", 100)]
        [DataRow("QtVS_v3041", 304)]
        [DataRow("Keyword QtVS_v301", 301)]
        [DataRow("QtVS_v30", null)]
        [DataRow("Qt4VSv1.0", null)]
        [DataRow("", null)]
        public void ParseProjectFormatVersion(string text, int? expected)
        {
            Assert.AreEqual(expected, TextParsers.ParseProjectFormatVersion(text));
        }

        [TestMethod]
        public void CreatePathRegex_IgnoresCaseAndSeparators()
        {
            var regex = TextParsers.CreatePathRegex("C:/dev/Qt/6.8.0", "../Qt/6.8.0");

            Assert.AreEqual("$(QTDIR)\\include",
                regex.Replace(@"c:\DEV\qt\6.8.0\include", "$(QTDIR)"));
            Assert.AreEqual("$(QTDIR)/bin", regex.Replace("../qt/6.8.0/bin", "$(QTDIR)"));
            Assert.AreEqual(@"$(QTDIR)\lib;$(QTDIR)\bin",
                regex.Replace(@"C:\dev\Qt\6.8.0\lib;..\Qt\6.8.0\bin", "$(QTDIR)"));
            Assert.AreEqual(@"C:\dev\Qt\6.7.0", regex.Replace(@"C:\dev\Qt\6.7.0", "$(QTDIR)"));
        }

        [TestMethod]
        public void CreatePathRegex_EscapesSpecialCharacters()
        {
            var regex = TextParsers.CreatePathRegex("C:/a+b/(x)");
            Assert.IsTrue(regex.IsMatch(@"c:\A+B\(X)\y"));
            Assert.IsFalse(regex.IsMatch(@"c:\aab\x\y"));
        }

        #endregion
    }
}
