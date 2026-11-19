// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Qrc
{
    using QtVsTools.Qrc;

    [TestClass]
    public class Test_QrcEditing
    {
        private const string QrcPath = @"C:\project\resources.qrc";

        private static QrcDocument CreateDocument()
        {
            return QrcDocument.Parse("<RCC>"
                + "<qresource prefix=\"/images\">"
                + "<file>a.png</file><file alias=\"logo\">b.png</file></qresource>"
                + "<qresource prefix=\"/qml\"><file>main.qml</file></qresource>"
                + "</RCC>", QrcPath);
        }

        private static string[] Names(QrcDocument document)
        {
            return document.Prefixes.Select(x => x.Name).ToArray();
        }

        [TestMethod]
        public void AddPrefix_UsesUniqueNames()
        {
            var document = CreateDocument();

            Assert.AreEqual("/new/prefix1", document.AddPrefix().Name);
            Assert.AreEqual("/new/prefix2", document.AddPrefix().Name);
            CollectionAssert.AreEqual(
                new[] { "/images", "/qml", "/new/prefix1", "/new/prefix2" }, Names(document));

            document.UndoStack.Undo();
            document.UndoStack.Undo();
            CollectionAssert.AreEqual(new[] { "/images", "/qml" }, Names(document));
            Assert.IsFalse(document.IsDirty);
        }

        [TestMethod]
        public void AddFiles_SkipsDuplicatesAndExistingFiles()
        {
            var document = CreateDocument();
            var images = document.Prefixes[0];

            var added = document.AddFiles(images, new[] {
                @"C:\project\A.PNG",      // already contained (case-insensitive)
                @"C:\project\c.png",
                "c.png",                  // duplicate of the previous file
                @"C:\other\d.png"
            });

            CollectionAssert.AreEqual(new[] { "c.png", "../other/d.png" },
                added.Select(x => x.RelativePath).ToArray());
            Assert.HasCount(4, images.Files);

            document.UndoStack.Undo();
            Assert.HasCount(2, images.Files);
            document.UndoStack.Redo();
            CollectionAssert.AreEqual(added.ToArray(), images.Files.Skip(2).ToArray());
        }

        [TestMethod]
        public void AddFiles_NothingNew_DoesNotModify()
        {
            var document = CreateDocument();

            var added = document.AddFiles(document.Prefixes[0], new[] { "a.png" });

            Assert.IsEmpty(added);
            Assert.IsFalse(document.IsDirty);
            Assert.IsFalse(document.UndoStack.CanUndo);
        }

        [TestMethod]
        public void RemovePrefix_UndoRestoresPositionAndFiles()
        {
            var document = CreateDocument();
            var images = document.Prefixes[0];
            var files = images.Files.ToArray();

            document.RemovePrefix(images);
            CollectionAssert.AreEqual(new[] { "/qml" }, Names(document));
            Assert.IsNull(images.Document);

            document.UndoStack.Undo();
            Assert.AreSame(images, document.Prefixes[0]);
            Assert.AreSame(document, images.Document);
            CollectionAssert.AreEqual(files, images.Files.ToArray());
            Assert.IsFalse(document.IsDirty);
        }

        [TestMethod]
        public void RemoveFile_UndoRestoresPosition()
        {
            var document = CreateDocument();
            var images = document.Prefixes[0];
            var first = images.Files[0];

            document.RemoveFile(first);
            Assert.HasCount(1, images.Files);
            Assert.IsNull(first.Prefix);

            document.UndoStack.Undo();
            Assert.AreSame(first, images.Files[0]);
            Assert.AreSame(images, first.Prefix);

            document.UndoStack.Redo();
            Assert.AreEqual("logo", images.Files.Single().Alias);
        }

        [TestMethod]
        public void RenamePrefix_RejectsClashWithSameLanguage()
        {
            var document = CreateDocument();
            var images = document.Prefixes[0];

            Assert.IsFalse(document.RenamePrefix(images, "qml/"));
            Assert.AreEqual("/images", images.Name);
            Assert.IsFalse(document.IsDirty);

            document.SetLanguage(images, "de");
            Assert.IsTrue(document.RenamePrefix(images, "qml/"));
            Assert.AreEqual("/qml", images.Name);
        }

        [TestMethod]
        public void RenamePrefix_AllowsReturnToLoadedDuplicate()
        {
            // Several prefixes with the same name and language are valid in a .qrc file.
            var document = QrcDocument.Parse("<RCC><qresource prefix=\"/\"/>"
                + "<qresource prefix=\"/\"/></RCC>", @"C:\project\resources.qrc");
            var first = document.Prefixes[0];

            Assert.IsTrue(document.RenamePrefix(first, "/x"));
            Assert.IsTrue(document.RenamePrefix(first, "/"));
            Assert.AreEqual("/", first.Name);

            Assert.IsTrue(document.SetLanguage(first, "de"));
            Assert.IsTrue(document.SetLanguage(first, ""));
            Assert.AreEqual("", first.Language);

            // A new clash is still rejected.
            var added = document.AddPrefix();
            Assert.IsFalse(document.RenamePrefix(added, "/"));
        }

        [TestMethod]
        public void SetLanguage_RejectsClashWithSameName()
        {
            var document = CreateDocument();
            var images = document.Prefixes[0];
            Assert.IsTrue(document.SetLanguage(images, "de"));
            Assert.IsTrue(document.RenamePrefix(images, "/qml"));
            document.UndoStack.SetClean();

            Assert.IsFalse(document.SetLanguage(images, ""));
            Assert.AreEqual("de", images.Language);
            Assert.IsFalse(document.IsDirty);

            Assert.IsTrue(document.SetLanguage(images, "fr"));
            Assert.AreEqual("fr", images.Language);
        }

        [TestMethod]
        public void RenamePrefix_UpdatesResourcePaths()
        {
            var document = CreateDocument();
            var images = document.Prefixes[0];
            var changed = new List<string>();
            images.Files[1].PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            Assert.IsTrue(document.RenamePrefix(images, "/icons"));

            Assert.AreEqual(":/icons/logo", images.Files[1].ResourcePath);
            CollectionAssert.Contains(changed, nameof(QrcFile.ResourcePath));
        }

        [TestMethod]
        public void SetAlias_ConsecutiveEditsAreOneUndoStep()
        {
            var document = CreateDocument();
            var file = document.Prefixes[0].Files[0];

            document.SetAlias(file, "i", mergeId: 1);
            document.SetAlias(file, "ic", mergeId: 1);
            document.SetAlias(file, "icon", mergeId: 1);
            Assert.AreEqual("a.png (icon)", file.DisplayName);

            document.UndoStack.Undo();
            Assert.AreEqual("", file.Alias);
            Assert.IsFalse(document.UndoStack.CanUndo);

            document.UndoStack.Redo();
            Assert.AreEqual("icon", file.Alias);
        }

        [TestMethod]
        public void SetAlias_DifferentMergeIdsAreSeparateUndoSteps()
        {
            var document = CreateDocument();
            var file = document.Prefixes[0].Files[0];

            document.SetAlias(file, "first", mergeId: 1);
            document.SetAlias(file, "second", mergeId: 2);

            document.UndoStack.Undo();
            Assert.AreEqual("first", file.Alias);
        }

        [TestMethod]
        public void SetAlias_RevertingEditMakesDocumentClean()
        {
            var document = CreateDocument();
            var file = document.Prefixes[0].Files[1];

            document.SetAlias(file, "logo2", mergeId: 7);
            Assert.IsTrue(document.IsDirty);
            document.SetAlias(file, "logo", mergeId: 7);

            Assert.IsFalse(document.IsDirty);
            Assert.IsFalse(document.UndoStack.CanUndo);
        }

        [TestMethod]
        public void SetLanguage_DoesNotMergeAcrossSave()
        {
            var document = CreateDocument();
            var qml = document.Prefixes[1];

            document.SetLanguage(qml, "d", mergeId: 3);
            document.UndoStack.SetClean();
            document.SetLanguage(qml, "de", mergeId: 3);
            Assert.IsTrue(document.IsDirty);

            document.UndoStack.Undo();
            Assert.AreEqual("d", qml.Language);
            Assert.IsFalse(document.IsDirty);
        }

        [TestMethod]
        public void NewEditAfterUndo_DiscardsRedo()
        {
            var document = CreateDocument();

            document.AddPrefix();
            document.UndoStack.Undo();
            Assert.IsTrue(document.UndoStack.CanRedo);

            document.SetLanguage(document.Prefixes[0], "fr");
            Assert.IsFalse(document.UndoStack.CanRedo);
            Assert.HasCount(2, document.Prefixes);
        }

        [TestMethod]
        public void UndoPastSavedState_StaysDirty()
        {
            var document = CreateDocument();

            document.AddPrefix();
            document.UndoStack.SetClean();
            document.UndoStack.Undo();
            Assert.IsTrue(document.IsDirty);

            document.SetLanguage(document.Prefixes[0], "fr");
            document.UndoStack.Undo();
            Assert.IsTrue(document.IsDirty, "the saved state is no longer reachable");
        }
    }
}
