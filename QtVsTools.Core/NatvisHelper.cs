// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

using Tasks = System.Threading.Tasks;

namespace QtVsTools.Core
{
    using Common;
    using VisualStudio;

    public static class NatvisHelper
    {
        private static Tasks.Task _initTask;
        private static string _visualizersPath;

        public static void CopyVisualizersFiles(string qtNamespace = null)
        {
            if (string.IsNullOrEmpty(qtNamespace))
                return;

            ThreadHelper.JoinableTaskFactory.Run(async () =>
                await CopyVisualizersFilesAsync(qtNamespace));
        }

        public static async Tasks.Task CopyVisualizersFilesAsync(string qtNamespace = null)
        {
            string[] files = { "qt5.natvis.xml", "qt6.natvis.xml" };
            foreach (var file in files)
                await CopyVisualizersFileAsync(file, qtNamespace);
        }

        private static async Tasks.Task CopyVisualizersFileAsync(string filename, string qtNamespace)
        {
            await EnsureVisualizersPathInitializedAsync();

            try {
                string visualizerFile;
                var text = await Utils.ReadAllTextAsync(Path.Combine(Utils.PackageInstallPath,
                    filename));

                if (string.IsNullOrEmpty(qtNamespace)) {
                    text = text.Replace("##NAMESPACE##::", string.Empty);
                    visualizerFile = Path.GetFileNameWithoutExtension(filename);
                } else {
                    text = text.Replace("##NAMESPACE##", qtNamespace);
                    visualizerFile = filename.Substring(0, filename.IndexOf('.'))
                        + $"_{qtNamespace.Replace("::", "_")}.natvis";
                }

                if (!Directory.Exists(_visualizersPath))
                    Directory.CreateDirectory(_visualizersPath);

                await Utils.WriteAllTextAsync(Path.Combine(_visualizersPath, visualizerFile), text);
            } catch (Exception exception) {
                exception.Log();
            }
        }

        private static async Tasks.Task EnsureVisualizersPathInitializedAsync()
        {
            if (!string.IsNullOrEmpty(_visualizersPath))
                return;

            var initTask = _initTask;
            if (initTask == null) {
                var task = InitializeVisualizersPathAsync();
                initTask = Interlocked.CompareExchange(ref _initTask, task, null) ?? task;
            }
            await initTask;
        }

        private static async Tasks.Task InitializeVisualizersPathAsync()
        {
            await VsShell.UiThreadAsync(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                try {
                    if (VsServiceProvider.GetService<DTE>() is not { } dte)
                        throw new InvalidOperationException("Unable to get service: DTE");

                    using var vsRootKey = Registry.CurrentUser.OpenSubKey(dte.RegistryRoot);
                    if (vsRootKey?.GetValue("VisualStudioLocation") is string vsLocation)
                        _visualizersPath = Path.Combine(vsLocation, "Visualizers");
                } catch (Exception exception) {
                    exception.Log();
                }

                if (string.IsNullOrEmpty(_visualizersPath)) {
                    _visualizersPath = Path.Combine(Environment.GetFolderPath(Environment
                        .SpecialFolder.MyDocuments),
#if VS2022
                        @"Visual Studio 2022\Visualizers\");
#elif VS2019
                        @"Visual Studio 2019\Visualizers\");
#endif
                }
            });
        }
    }
}
