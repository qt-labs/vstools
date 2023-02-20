/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace QtVsTest
{
    using Macros;

    [Guid(PackageGuidString)]
    [InstalledProductRegistration(
        productName: "Qt Visual Studio Test",
        productDetails: "Auto-test framework for Qt Visual Studio Tools.",
        productId: "1.0",
        IconResourceID = 400)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]

    public sealed class QtVsTest : AsyncPackage
    {
        public const string PackageGuidString = "0e258dce-fc8a-49a2-81c5-c9e138bfe500";
        MacroServer MacroServer { get; }

        public QtVsTest()
        {
            MacroServer = new MacroServer(this, JoinableTaskFactory);
        }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            // Get package install path
            var uri = new Uri(System.Reflection.Assembly
                .GetExecutingAssembly().EscapedCodeBase);
            var pkgInstallPath = Path.GetDirectoryName(
                Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";

            // Install client interface
            var qtVsTestFiles = Environment.
                ExpandEnvironmentVariables(@"%LOCALAPPDATA%\qtvstest");
            Directory.CreateDirectory(qtVsTestFiles);
            File.Copy(
                Path.Combine(pkgInstallPath, "MacroClient.h"),
                Path.Combine(qtVsTestFiles, "MacroClient.h"),
                overwrite: true);

            // Install .csmacro syntax highlighting
            var grammarFilesPath = Environment.
                ExpandEnvironmentVariables(@"%USERPROFILE%\.vs\Extensions\qtcsmacro");
            Directory.CreateDirectory(grammarFilesPath);
            File.Copy(
                Path.Combine(pkgInstallPath, "csmacro.tmLanguage"),
                Path.Combine(grammarFilesPath, "csmacro.tmLanguage"),
                overwrite: true);
            File.Copy(
                Path.Combine(pkgInstallPath, "csmacro.tmTheme"),
                Path.Combine(grammarFilesPath, "csmacro.tmTheme"),
                overwrite: true);

            // Start macro server loop as background task
            await Task.Run(() => MacroServer.LoopAsync().Forget());
        }

        protected override int QueryClose(out bool canClose)
        {
            // Shutdown macro server when closing Visual Studio
            MacroServer.Loop.Cancel();

            return base.QueryClose(out canClose);
        }
    }
}
