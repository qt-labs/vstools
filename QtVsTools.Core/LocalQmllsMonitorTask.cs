// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

using Tasks = System.Threading.Tasks;

namespace QtVsTools.Core
{
    using Common;
    using Options;
    using VisualStudio;

    public static partial class Notifications
    {
        public static QmllsUpdateInstalled NotifyQmllsUpdateInstalled
            => StaticLazy.Get(() => NotifyQmllsUpdateInstalled, () => new QmllsUpdateInstalled());
    }

    public class QmllsUpdateInstalled : InfoBarMessage
    {
        protected override ImageMoniker Icon => KnownMonikers.StatusInformation;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new() { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            Utils.EmDash,
            new TextSpacer(2),
            "A new version of the QML language server was installed."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new()
            {
                Text = "Show Changelog",
                CloseInfoBar = false,
                OnClicked = () =>
                {
                    try {
                        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),
                            Path.GetRandomFileName()));

                        var asset = JsonConvert.DeserializeObject<LocalQmllsManager.AssetWithTag>(
                            File.ReadAllText(LocalQmllsManager.ReleaseJsonPath), JsonSerializer.Settings);

                        var changelog = Path.Combine(dir.FullName, "Changelog");
                        File.WriteAllText(changelog, asset.Body);

                        ThreadHelper.ThrowIfNotOnUIThread();
                        VsEditor.Open(changelog, VsEditor.OpenWith.TextEditor);
                    } catch (Exception ex) {
                        ex.Log();
                    }
                }
            },
            new()
            {
                Text = "Don't show again",
                CloseInfoBar = true,
                OnClicked = () =>
                {
                    try {
                        QtOptionsPage.NotifyQmllsUpdateInstalled = false;
                        QtOptionsPage.SaveSettingsToStorageStatic();
                    } catch (Exception ex) {
                        ex.Log();
                    }
                }
            }
        };
    }

    public class LocalQmllsMonitorTask : IIdleTask
    {
        public async Tasks.Task RunAsync(CancellationToken cancellationToken)
        {
#pragma warning disable VSTHRD010
            try {
                var asset = await LocalQmllsManager.FetchAssetAsync(cancellationToken);
                var checkResult = await LocalQmllsManager.CheckForInstallationUpdateAsync(asset,
                    cancellationToken);
                if (checkResult is { ShouldInstall: false })
                    return;

                await StatusBar.SetTextAsync("Updating QML language server...");

                var downloadDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(downloadDir);

                try {
                    await StatusBar.ResetProgressAsync();

                    var tmpPath = Path.Combine(downloadDir, asset.Name);
                    await LocalQmllsDownloader.DownloadAsync(asset.BrowserDownloadUrl, tmpPath,
                        cancellationToken,
                        async download =>
                        {
                            await StatusBar.ProgressAsync(
                                "Updating QML language server... Downloading "
                                + $"{BytesToKilobytes(download.CurrentBytes)} / "
                                + $"{BytesToKilobytes(download.MaxBytyes)}",
                                (uint)download.MaxBytyes, (uint)download.CurrentBytes);
                        }
                    );

                    await StatusBar.ResetProgressAsync();

                    // Do not use the idle managers cancellation token here, we do not want
                    // the unzip process to stop and leave a corrupted qmlls binary behind.
                    await Utils.ExtractArchiveAsync(tmpPath, LocalQmllsManager.ExtractDir,
                        CancellationToken.None,
                        async progress =>
                        {
                            await StatusBar.ProgressAsync(
                                $"Updating QML language server... Extracting '{progress.FullName}'"
                                + $": {progress.CurrentEntry} of {progress.TotalEntries} files",
                                (uint)progress.TotalEntries,
                                (uint)progress.CurrentEntry);
                        }
                    );

                    await Utils.WriteAllTextAsync(LocalQmllsManager. ReleaseJsonPath, JsonConvert.
                        SerializeObject(new { asset.TagName, asset.Body }, JsonSerializer.Settings));

                    if (QtOptionsPage.NotifyQmllsUpdateInstalled) {
                        await VsShell.UiThreadAsync(() => Notifications.NotifyQmllsUpdateInstalled
                            .Show());
                    }
                } finally {
                    await StatusBar.ResetProgressAsync();
                    await StatusBar.ClearAsync();

                    Utils.DeleteDirectory(downloadDir, Utils.Option.Recursive);
                }
            } catch (Tasks.TaskCanceledException) {
                // ignore
            } catch (Exception exception) {
                exception.Log();
            }
#pragma warning restore VSTHRD010
        }

        private static string BytesToKilobytes(long bytes)
        {
            return (bytes / 1024.0).ToString("0.00") + " KB";
        }
    }
}
