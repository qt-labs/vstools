/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.IO;
using System.Threading.Tasks;

namespace QtVsTools.Core.CMake
{
    public partial class CMakeProject : Concurrent<CMakeProject>
    {
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
            if (IsProjectFile(args.FullPath))
                await CheckQtStatusAsync();
        }
    }
}
