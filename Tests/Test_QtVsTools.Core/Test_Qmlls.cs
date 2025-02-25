// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QtVsTools.Test.Core
{
    using QtVsTools.Core;
    using QtVsTools.Core.Common;

    [TestClass]
    public class Test_LocalQmllsManager
    {
        private CancellationTokenSource cts;

        [TestInitialize]
        public void TestSetup()
        {
            cts = new CancellationTokenSource();
            Utils.DeleteDirectory(LocalQmllsManager.InstallDir, Utils.Option.Recursive);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cts?.Dispose();
            Utils.DeleteDirectory(LocalQmllsManager.InstallDir, Utils.Option.Recursive);
        }

        [TestMethod]
        public async Task TestFetchAssetToInstallAsync()
        {
            var asset = await LocalQmllsManager.FetchAssetAsync(cts.Token);

            Assert.IsNotNull(asset, "asset != null");
            Assert.IsFalse(string.IsNullOrEmpty(asset.BrowserDownloadUrl),
                "string.IsNullOrEmpty(asset.BrowserDownloadUrl)");
        }

        [TestMethod]
        public async Task TestCheckStatusAgainstAsync()
        {
            var asset = await LocalQmllsManager.FetchAssetAsync(cts.Token);
            var checkResult = await LocalQmllsManager
                .CheckForInstallationUpdateAsync(asset, cts.Token);

            Assert.IsNotNull(checkResult, "checkResult != null");
            Assert.IsTrue(checkResult.ShouldInstall, "checkResult.ShouldInstall");
        }

        [TestMethod]
        public async Task TestInstallAsync()
        {
            var asset = await LocalQmllsManager.FetchAssetAsync(cts.Token);
            Assert.IsNotNull(asset, "asset != null");

            var checkResult = await LocalQmllsManager
                .CheckForInstallationUpdateAsync(asset, cts.Token);
            Assert.IsNotNull(checkResult, "checkResult != null");
            Assert.IsTrue(checkResult.ShouldInstall, "checkResult.ShouldInstall");

            if (checkResult.ShouldInstall) {
                await LocalQmllsManager.InstallAssetAsync(asset, cts.Token);
                Assert.IsTrue(File.Exists(LocalQmllsManager.QmlLspServerExePath),
                    "File.Exists(QmlLspServerInstaller.QmlLspServerExePath)");

                checkResult = await LocalQmllsManager
                    .CheckForInstallationUpdateAsync(asset, cts.Token);
                Assert.IsNotNull(checkResult);
                Assert.IsTrue(checkResult.Message.Contains("Already Up-to-date"),
                    "checkResult.Message.Contains('Already Up-to-date')");
            }
        }
    }
}
