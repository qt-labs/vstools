// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Qrc
{
    using QtVsTools.Qrc;

    [TestClass]
    public class Test_QrcPath
    {
        [TestMethod]
        [DataRow(null, "/")]
        [DataRow("", "/")]
        [DataRow("/", "/")]
        [DataRow("images", "/images")]
        [DataRow("/images/", "/images")]
        [DataRow("//images//icons//", "/images/icons")]
        public void FixPrefix(string prefix, string expected)
        {
            Assert.AreEqual(expected, QrcPath.FixPrefix(prefix));
        }

        [TestMethod]
        [DataRow(@"C:\project\images\logo.png", "images/logo.png")]
        [DataRow(@"C:\project\logo.png", "logo.png")]
        [DataRow(@"C:\other\logo.png", "../other/logo.png")]
        [DataRow(@"c:\PROJECT\Images\logo.png", "Images/logo.png")]
        [DataRow(@"D:\shared\logo.png", "D:/shared/logo.png")]
        [DataRow("images/logo.png", "images/logo.png")]
        public void ToRelative(string path, string expected)
        {
            Assert.AreEqual(expected, QrcPath.ToRelative(@"C:\project", path));
        }

        [TestMethod]
        [DataRow("images/logo.png", @"C:\project\images\logo.png")]
        [DataRow("../other/./logo.png", @"C:\other\logo.png")]
        [DataRow(@"D:\shared\logo.png", @"D:\shared\logo.png")]
        public void ToAbsolute(string path, string expected)
        {
            Assert.AreEqual(expected, QrcPath.ToAbsolute(@"C:\project", path));
        }

        [TestMethod]
        [DataRow("bad|file.png")]
        [DataRow("what?.png")]
        [DataRow(@"C:\a:b")]
        [DataRow(@"img\a:b.png")]
        public void ToAbsolute_InvalidPath_Throws(string path)
        {
            Assert.ThrowsExactly<ArgumentException>(() => QrcPath.ToAbsolute(@"C:\project", path));
        }

        [TestMethod]
        [DataRow("images/logo.png", "images/logo.png")]
        [DataRow("../shared/icon.png", "shared/icon.png")]
        [DataRow("../../a/../b.png", "b.png")]
        [DataRow("a/./b//c.png", "a/b/c.png")]
        [DataRow(@"a\b.png", "a/b.png")]
        [DataRow("a/../../c.png", "c.png")]
        public void ToResourceName(string name, string expected)
        {
            Assert.AreEqual(expected, QrcPath.ToResourceName(name));
        }

        [TestMethod]
        [DataRow(@"C:\project\images\logo.png", false)]
        [DataRow(@"C:\project\logo.png", false)]
        [DataRow(@"C:\other\logo.png", true)]
        [DataRow(@"C:\project2\logo.png", true)]
        [DataRow(@"D:\project\logo.png", true)]
        public void IsOutside(string path, bool expected)
        {
            Assert.AreEqual(expected, QrcPath.IsOutside(@"C:\project", path));
        }
    }
}
