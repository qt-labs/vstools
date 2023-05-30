/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Workspace.Indexing;

namespace QtVsTools.Core.CMake
{
    public partial class CMakeProject : Concurrent<CMakeProject>
    {
        private void SubscribeEvents()
        {
            FileWatcher.OnFileSystemChanged += OnFileSystemChangedAsync;
            Index.OnFileScannerCompleted += OnFileScannerCompletedAsync;
        }

        private void UnsubscribeEvents()
        {
            FileWatcher.OnFileSystemChanged -= OnFileSystemChangedAsync;
            Index.OnFileScannerCompleted -= OnFileScannerCompletedAsync;
        }

        private async Task OnFileSystemChangedAsync(object sender, FileSystemEventArgs args)
        {
            if (IsProjectFile(args.FullPath))
                await CheckQtStatusAsync();
        }

        private async Task OnFileScannerCompletedAsync(object sender, FileScannerEventArgs args)
        {
            await Task.Yield();
            if (Status != QtStatus.True)
                return;
            if (!args.TryGetContent<IReadOnlyCollection<FileDataValue>>(out var values))
                return;
            if (!values.Any())
                return;

            RefreshVariables(values);
        }
    }
}
