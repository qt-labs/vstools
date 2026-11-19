// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Qrc
{
    using QtVsTools.Qrc;

    [TestClass]
    public class Test_QrcDocument
    {
        private const string QrcPath = @"C:\project\resources.qrc";

        private static string Lf(string text) => text.Replace("\r\n", "\n");

        [TestMethod]
        public void Parse_ReadsPrefixesFilesAndAliases()
        {
            var document = QrcDocument.Parse(Lf(@"<RCC>
    <qresource prefix=""/images"">
        <file alias=""logo.png"">img/logo.png</file>
        <file>../shared/icon.png</file>
    </qresource>
    <qresource prefix=""/"" lang=""de"">
        <file>i18n/app_de.qm</file>
    </qresource>
</RCC>
"), QrcPath);

            Assert.HasCount(2, document.Prefixes);

            var images = document.Prefixes[0];
            Assert.AreEqual("/images", images.Name);
            Assert.AreEqual("", images.Language);
            Assert.HasCount(2, images.Files);
            Assert.AreEqual(@"C:\project\img\logo.png", images.Files[0].Path);
            Assert.AreEqual("logo.png", images.Files[0].Alias);
            Assert.AreEqual("img/logo.png (logo.png)", images.Files[0].DisplayName);
            Assert.AreEqual(":/images/logo.png", images.Files[0].ResourcePath);
            Assert.AreEqual(@"C:\shared\icon.png", images.Files[1].Path);
            Assert.AreEqual("../shared/icon.png", images.Files[1].RelativePath);
            Assert.AreEqual(":/images/shared/icon.png", images.Files[1].ResourcePath);

            var root = document.Prefixes[1];
            Assert.AreEqual("/", root.Name);
            Assert.AreEqual("de", root.Language);
            Assert.AreEqual("/ (de)", root.DisplayName);
            Assert.AreEqual(":/i18n/app_de.qm", root.Files[0].ResourcePath);

            Assert.IsFalse(document.IsDirty);
        }

        [TestMethod]
        public void Parse_NormalizesPrefixes()
        {
            var document = QrcDocument.Parse(
                "<RCC><qresource><file>a.txt</file></qresource>"
                + "<qresource prefix=\"icons//dark/\"/></RCC>", QrcPath);

            Assert.AreEqual("/", document.Prefixes[0].Name);
            Assert.AreEqual("/icons/dark", document.Prefixes[1].Name);
        }

        [TestMethod]
        public void Parse_KeepsSamePrefixWithDifferentLanguagesApart()
        {
            var document = QrcDocument.Parse(Lf(@"<RCC>
    <qresource prefix=""/i18n"">
        <file>app.qm</file>
    </qresource>
    <qresource prefix=""/i18n"" lang=""de"">
        <file>app_de.qm</file>
    </qresource>
</RCC>
"), QrcPath);

            Assert.HasCount(2, document.Prefixes);
            Assert.AreEqual("", document.Prefixes[0].Language);
            Assert.AreEqual("de", document.Prefixes[1].Language);
        }

        [TestMethod]
        public void Parse_InvalidXml_Throws()
        {
            var exception = Assert.ThrowsExactly<QrcException>(
                () => QrcDocument.Parse("<RCC><qresource></RCC>", QrcPath));
            StringAssert.StartsWith(exception.Message, "XML error");
        }

        [TestMethod]
        public void Parse_MissingRootElement_Throws()
        {
            var exception = Assert.ThrowsExactly<QrcException>(
                () => QrcDocument.Parse("<RESOURCES/>", QrcPath));
            Assert.AreEqual("The <RCC> root element is missing.", exception.Message);
        }

        [TestMethod]
        public void Parse_InvalidFilePath_Throws()
        {
            var exception = Assert.ThrowsExactly<QrcException>(() => QrcDocument.Parse(
                "<RCC><qresource><file>bad|file.png</file></qresource></RCC>", QrcPath));
            StringAssert.Contains(exception.Message, "bad|file.png");
        }

        [TestMethod]
        public void ToXml_UsesQtFormatting()
        {
            var document = QrcDocument.Parse(
                "<RCC><qresource prefix=\"/\"><file>main.qml</file></qresource>"
                + "<qresource prefix=\"/empty\"/></RCC>", QrcPath);

            Assert.AreEqual(Lf(@"<RCC>
    <qresource prefix=""/"">
        <file>main.qml</file>
    </qresource>
    <qresource prefix=""/empty"" />
</RCC>
"), document.ToXml());
        }

        [TestMethod]
        public void ToXml_KeepsDocTypeAndUnknownAttributes()
        {
            var document = QrcDocument.Parse(Lf(@"<!DOCTYPE RCC><RCC version=""1.0"">
<qresource prefix=""/"" lang=""en"" extra=""1"">
    <file compress=""9"" alias=""a.png"" threshold=""0"">images/a.png</file>
</qresource>
</RCC>
"), QrcPath);

            Assert.AreEqual(Lf(@"<!DOCTYPE RCC>
<RCC version=""1.0"">
    <qresource prefix=""/"" lang=""en"" extra=""1"">
        <file alias=""a.png"" compress=""9"" threshold=""0"">images/a.png</file>
    </qresource>
</RCC>
"), document.ToXml());
        }

        [TestMethod]
        public void ToXml_KeepsXmlDeclarationAndLineEndings()
        {
            var document = QrcDocument.Parse(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<RCC>\r\n"
                + "<qresource prefix=\"/\"><file>a.txt</file></qresource>\r\n</RCC>\r\n",
                QrcPath);

            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<RCC>\r\n"
                + "    <qresource prefix=\"/\">\r\n        <file>a.txt</file>\r\n"
                + "    </qresource>\r\n</RCC>\r\n", document.ToXml());
        }

        [TestMethod]
        public void ToXml_KeepsXmlDeclarationAndDocTypeOnSeparateLines()
        {
            var document = QrcDocument.Parse(Lf(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!DOCTYPE RCC>
<RCC>
<qresource prefix=""/""><file>a.txt</file></qresource>
</RCC>
"), QrcPath);

            Assert.AreEqual(Lf(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!DOCTYPE RCC>
<RCC>
    <qresource prefix=""/"">
        <file>a.txt</file>
    </qresource>
</RCC>
"), document.ToXml());
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip()
        {
            using var temp = new TempDirectory();
            var qrcFile = Path.Combine(temp.Path, "app.qrc");
            File.WriteAllText(qrcFile, "<RCC><qresource prefix=\"/\"/></RCC>");

            var document = QrcDocument.Load(qrcFile);
            document.AddFiles(document.Prefixes[0],
                new[] { Path.Combine(temp.Path, "qml", "main.qml") });
            Assert.IsTrue(document.IsDirty);

            document.Save();
            Assert.IsFalse(document.IsDirty);

            var reloaded = QrcDocument.Load(qrcFile);
            Assert.AreEqual("qml/main.qml", reloaded.Prefixes[0].Files.Single().RelativePath);
            Assert.AreEqual(document.ToXml(), File.ReadAllText(qrcFile));
        }

        [TestMethod]
        public void Save_KeepsByteOrderMark()
        {
            using var temp = new TempDirectory();
            var withBom = Path.Combine(temp.Path, "bom.qrc");
            var withoutBom = Path.Combine(temp.Path, "nobom.qrc");
            const string xml = "<RCC><qresource prefix=\"/\"/></RCC>";
            File.WriteAllText(withBom, xml, new UTF8Encoding(true));
            File.WriteAllText(withoutBom, xml, new UTF8Encoding(false));

            QrcDocument.Load(withBom).Save();
            QrcDocument.Load(withoutBom).Save();

            CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF },
                File.ReadAllBytes(withBom).Take(3).ToArray());
            Assert.AreEqual((byte)'<', File.ReadAllBytes(withoutBom)[0]);
        }

        [TestMethod]
        public void LoadAndSave_ConvertsDeclaredEncodingToUtf8()
        {
            using var temp = new TempDirectory();
            var latin1 = Path.Combine(temp.Path, "latin1.qrc");
            var utf16 = Path.Combine(temp.Path, "utf16.qrc");
            const string body = "<RCC><qresource prefix=\"/\">"
                + "<file alias=\"m\u00E4in\">a.txt</file></qresource></RCC>";
            File.WriteAllText(latin1, "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>" + body,
                Encoding.GetEncoding("ISO-8859-1"));
            File.WriteAllText(utf16, "<?xml version=\"1.0\" encoding=\"UTF-16\"?>" + body,
                Encoding.Unicode);

            foreach (var file in new[] { latin1, utf16 }) {
                var document = QrcDocument.Load(file);
                Assert.AreEqual("m\u00E4in", document.Prefixes[0].Files[0].Alias, file);
                document.Save();

                var text = File.ReadAllText(file, new UTF8Encoding(false));
                StringAssert.StartsWith(text,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>", file);
                StringAssert.Contains(text, "alias=\"m\u00E4in\"", file);
                Assert.AreEqual("m\u00E4in",
                    QrcDocument.Load(file).Prefixes[0].Files[0].Alias, file);
            }
        }

        [TestMethod]
        public void Load_UnsupportedEncoding_Throws()
        {
            using var temp = new TempDirectory();
            var file = Path.Combine(temp.Path, "app.qrc");
            File.WriteAllText(file, "<?xml version=\"1.0\" encoding=\"no-such-encoding\"?><RCC/>");
            Assert.ThrowsExactly<QrcException>(() => QrcDocument.Load(file));
        }

        [TestMethod]
        public void ToXml_KeepsFileTextAsWritten()
        {
            // rcc names the resources after this text, so saving must not rewrite it.
            var document = QrcDocument.Parse("<RCC><qresource prefix=\"/\">"
                + "<file>C:/project/img/a.png</file>"
                + "<file>sub/../img/b.png</file>"
                + "<file>../shared/c.png</file></qresource></RCC>", QrcPath);
            var files = document.Prefixes[0].Files;

            Assert.AreEqual(@"C:\project\img\a.png", files[0].Path);
            Assert.AreEqual(":/C:/project/img/a.png", files[0].ResourcePath);
            Assert.AreEqual(":/img/b.png", files[1].ResourcePath);
            Assert.AreEqual(":/shared/c.png", files[2].ResourcePath);

            var xml = document.ToXml();
            StringAssert.Contains(xml, "<file>C:/project/img/a.png</file>");
            StringAssert.Contains(xml, "<file>sub/../img/b.png</file>");
            StringAssert.Contains(xml, "<file>../shared/c.png</file>");
        }

        [TestMethod]
        public void SaveAs_RewritesLoadedPathsForNewLocation()
        {
            using var temp = new TempDirectory();
            var qrcFile = Path.Combine(temp.Path, "app.qrc");
            File.WriteAllText(qrcFile,
                "<RCC><qresource prefix=\"/\"><file>./img/a.png</file></qresource></RCC>");
            var document = QrcDocument.Load(qrcFile);
            var file = document.Prefixes[0].Files[0];
            Assert.AreEqual("./img/a.png", file.RelativePath);

            var subDirectory = Directory.CreateDirectory(Path.Combine(temp.Path, "sub"));
            document.SaveAs(Path.Combine(subDirectory.FullName, "app.qrc"));
            Assert.AreEqual("../img/a.png", file.RelativePath);
        }

        [TestMethod]
        public void SaveAs_WritesPathsRelativeToNewLocation()
        {
            using var temp = new TempDirectory();
            var document = new QrcDocument(Path.Combine(temp.Path, "app.qrc"));
            var prefix = document.AddPrefix();
            var file = document.AddFiles(prefix,
                new[] { Path.Combine(temp.Path, "img", "a.png") }).Single();
            Assert.AreEqual("img/a.png", file.RelativePath);

            var subDirectory = Directory.CreateDirectory(Path.Combine(temp.Path, "sub"));
            document.SaveAs(Path.Combine(subDirectory.FullName, "app.qrc"));

            Assert.AreEqual("../img/a.png", file.RelativePath);
            StringAssert.Contains(File.ReadAllText(document.FilePath),
                "<file>../img/a.png</file>");
        }

        [TestMethod]
        public void Load_MissingFile_Throws()
        {
            using var temp = new TempDirectory();
            Assert.ThrowsExactly<QrcException>(
                () => QrcDocument.Load(Path.Combine(temp.Path, "missing.qrc")));
        }

        private sealed class TempDirectory : IDisposable
        {
            public string Path { get; } = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "qrc_" + Guid.NewGuid().ToString("N"));

            public TempDirectory() => Directory.CreateDirectory(Path);

            public void Dispose()
            {
                try {
                    Directory.Delete(Path, true);
                } catch (IOException) {
                }
            }
        }
    }
}
