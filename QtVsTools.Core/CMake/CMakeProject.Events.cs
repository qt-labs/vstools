// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System.IO;
using System.Threading.Tasks;

namespace QtVsTools.Core.CMake
{
    using static Common.Utils;

    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        public delegate void ProjectConfigurationDelegate(ProjectConfigurationEventArgs args);
        public event ProjectConfigurationDelegate ProjectConfigurationChanged;

        private void SubscribeEvents()
        {
            FileWatcher.OnFileSystemChanged += OnFileSystemChangedAsync;
        }

        private void UnsubscribeEvents()
        {
            FileWatcher.OnFileSystemChanged -= OnFileSystemChangedAsync;
        }

        private async Task OnFileSystemChangedAsync(object sender, FileSystemEventArgs args)
        {
            var fullPath = HelperFunctions.ToNativeSeparator(args.FullPath);
            if (IsProjectFile(fullPath))
                await CheckQtStatusAsync();

            if (!string.Equals(fullPath, GetConfigurationFileName(), IgnoreCase))
                return;

            var configurationName = GetActiveConfigurationName();
            if (string.IsNullOrEmpty(ConfigurationName))
                ConfigurationName = configurationName;

            if (ConfigurationName == configurationName)
                return;

            ConfigurationName = configurationName;
            ProjectConfigurationChanged?.Invoke(new ProjectConfigurationEventArgs
            {
                ProjectPath = RootPath,
                IsCMakeProject = true,
                ConfigurationName = configurationName
            });
        }
    }
}
