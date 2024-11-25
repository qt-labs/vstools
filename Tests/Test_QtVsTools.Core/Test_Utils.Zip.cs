/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Core
{
    using QtVsTools.Core.Common;

    public partial class Test_Utils
    {
        private static void CreateTestArchive(string archivePath)
        {
            using var archiveStream = new FileStream(archivePath, FileMode.Create);
            using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create);

            var item1 = archive.CreateEntry("item1.txt");
            using (var writer = new StreamWriter(item1.Open())) {
                writer.Write("Content of item1");
            }

            var item2 = archive.CreateEntry("subdir/item2.txt");
            using (var writer = new StreamWriter(item2.Open())) {
                writer.Write("Content of item2");
            }

            var item3 = archive.CreateEntry("subdir/item3.txt");
            using (var writer = new StreamWriter(item3.Open())) {
                writer.Write("Content of item3");
            }
        }
        private readonly string testDirectory = Path.Combine(Path.GetTempPath(), "UtilsZipTests");

        [TestInitialize]
        public void TestInitialize()
        {
            Directory.CreateDirectory(testDirectory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Utils.DeleteDirectory(testDirectory, Utils.Option.Recursive);
        }

        [TestMethod]
        public async Task Test_CreateCorrectFileStructureAsync()
        {
            var archivePath = Path.Combine(testDirectory, "test.zip");
            var targetDirectory = Path.Combine(testDirectory, "output");

            CreateTestArchive(archivePath);
            await Utils.ExtractArchiveAsync(archivePath, targetDirectory, default);

            Assert.IsTrue(File.Exists(Path.Combine(targetDirectory, "item1.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDirectory, "subdir", "item2.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDirectory, "subdir", "item3.txt")));
        }

        [TestMethod]
        public async Task Test_CallbackReportProgressAsync()
        {
            var archivePath = Path.Combine(testDirectory, "test.zip");
            var targetDirectory = Path.Combine(testDirectory, "output");

            CreateTestArchive(archivePath);

            long totalEntries = 0, reportedEntries = 0;
            await Utils.ExtractArchiveAsync(archivePath, targetDirectory, default,
                async progress =>
                {
                    await Task.Run(() =>
                    {
                        reportedEntries = progress.CurrentEntry;
                        totalEntries = progress.TotalEntries;
                    });
                });

            Assert.AreEqual(3, totalEntries, "Unexpected total entry count.");
            Assert.AreEqual(3, reportedEntries, "Unexpected reported entry count.");
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Test_ThrowWhenCanceledAsync()
        {
            var archivePath = Path.Combine(testDirectory, "test.zip");
            var targetDirectory = Path.Combine(testDirectory, "output");

            CreateTestArchive(archivePath);
            using var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel(); // immediately cancel the token

            await Utils.ExtractArchiveAsync(archivePath, targetDirectory, cts.Token);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task Test_ThrowOnInvalidFileAsync()
        {
            var invalidFilePath = Path.Combine(testDirectory, "random.txt");
            var targetDirectory = Path.Combine(testDirectory, "output");

            File.WriteAllText(invalidFilePath, "This is just a random text file.");
            await Utils.ExtractArchiveAsync(invalidFilePath, targetDirectory, default);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task Test_ThrowOnCorruptedArchiveAsync()
        {
            var archivePath = Path.Combine(testDirectory, "test.zip");
            var targetDirectory = Path.Combine(testDirectory, "output");

            CreateTestArchive(archivePath);
            using (var stream = new FileStream(archivePath, FileMode.Open)) {
                stream.SetLength(stream.Length - 10); // truncate, remove the last 10 bytes
            }

            await Utils.ExtractArchiveAsync(archivePath, targetDirectory, default);
        }
    }
}
