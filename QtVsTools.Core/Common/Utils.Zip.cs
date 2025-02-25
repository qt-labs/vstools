// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Microsoft.VisualStudio.Shell;

using Tasks = System.Threading.Tasks;

namespace QtVsTools.Core.Common
{
    public static partial class Utils
    {
        private const int BufferSize = 81920; // buffer size 80 KB

        /// <summary>
        /// Extracts all files from a ZIP archive to the specified target directory synchronously.
        /// </summary>
        /// <param name="sourceArchive">The path to the ZIP archive to extract.</param>
        /// <param name="targetDirectory">The directory where the files will be extracted.</param>
        /// <param name="callback">
        /// An optional callback invoked during extraction, reporting progress as a tuple:
        /// (TotalEntries, CurrentEntry, FullName).
        /// </param>
        public static void ExtractArchive(string sourceArchive, string targetDirectory,
            Func<(long TotalEntries, long CurrentEntry, string FullName), Tasks.Task> callback = null)
        {
            ThreadHelper.JoinableTaskFactory.Run(() => ExtractArchiveAsync(sourceArchive,
                targetDirectory, CancellationToken.None, callback));
        }

        /// <summary>
        /// Extracts all files from a ZIP archive to the specified target directory asynchronously.
        /// </summary>
        /// <param name="sourceArchive">The path to the ZIP archive to extract.</param>
        /// <param name="targetDir">The directory where the files will be extracted.</param>
        /// <param name="token">A cancellation token to monitor for cancellation requests.</param>
        /// <param name="callback">
        /// An optional callback invoked during extraction, reporting progress as a tuple:
        /// (TotalEntries, CurrentEntry, FullName).
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Tasks.Task ExtractArchiveAsync(string sourceArchive, string targetDir,
            CancellationToken token,
            Func<(long TotalEntries, long CurrentEntry, string FullName), Tasks.Task> callback = null)
        {
            if (sourceArchive == null || targetDir == null)
                return;
            targetDir = Directory.CreateDirectory(targetDir).FullName;

            using var srcStream = new FileStream(sourceArchive, FileMode.Open, FileAccess.Read,
                FileShare.Read, BufferSize);
            using var archive = new ZipArchive(srcStream, ZipArchiveMode.Read);

            var currentEntry = 0;
            var totalEntries = archive.Entries.Count;
            foreach (var entry in archive.Entries) {
                token.ThrowIfCancellationRequested();
                await ExtractEntryAsync(entry, targetDir, token);
                if (callback != null)
                    await callback.Invoke((totalEntries, ++currentEntry, entry.FullName));
            }
        }

        /// <summary>
        /// Extracts a single ZIP archive entry to the specified target directory asynchronously.
        /// </summary>
        /// <param name="entry">The ZIP archive entry to extract.</param>
        /// <param name="targetDir">The directory where the entry will be extracted.</param>
        /// <param name="token">A cancellation token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Tasks.Task ExtractEntryAsync(ZipArchiveEntry entry, string targetDir,
            CancellationToken token)
        {
            var targetFile = Path.Combine(targetDir, entry.FullName);
            if (!Directory.Exists(targetDir = Path.GetDirectoryName(targetFile)))
                Directory.CreateDirectory(targetDir!);

            {   // scoped to close both source and target stream
                using var source = entry.Open();
                using var stream = new FileStream(targetFile, FileMode.Create, FileAccess.Write,
                    FileShare.None, BufferSize);

                int count;
                var buffer = new byte[BufferSize];
                while ((count = await source.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
                    token.ThrowIfCancellationRequested();
                    await stream.WriteAsync(buffer, 0, count, token);
                }
            }
            File.SetLastWriteTime(targetFile, entry.LastWriteTime.DateTime);
        }
    }
}
