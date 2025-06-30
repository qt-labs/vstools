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
        public static QmlLanguageServerUpdateInstalled NotifyQmlLanguageServerUpdateInstalled
            => StaticLazy.Get(() => NotifyQmlLanguageServerUpdateInstalled,
                () => new QmlLanguageServerUpdateInstalled());
    }

    public class QmlLanguageServerUpdateInstalled : InfoBarMessage
    {
        protected override ImageMoniker Icon => KnownMonikers.StatusInformation;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new() { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            Utils.EmDash,
            new TextSpacer(2),
            "A new version of the QML Language Server was installed."
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

                        var asset = JsonConvert.DeserializeObject<QmlLanguageServerManager.AssetWithTag>(
                            File.ReadAllText(QmlLanguageServerManager.ReleaseJsonPath), JsonSerializer.Settings);

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
                        QtOptionsPage.NotifyQmlLanguageServerUpdateInstalled = false;
                        QtOptionsPage.SaveSettingsToStorageStatic();
                    } catch (Exception ex) {
                        ex.Log();
                    }
                }
            }
        };
    }

    public class QmlLanguageServerMonitorTask : IIdleTask
    {
        public async Tasks.Task RunAsync(CancellationToken cancellationToken)
        {
#pragma warning disable VSTHRD010
            try {
                var asset = await QmlLanguageServerManager.FetchAssetAsync(cancellationToken);
                var checkResult = await QmlLanguageServerManager
                    .CheckForInstallationUpdateAsync(asset, cancellationToken);
                if (checkResult is { ShouldInstall: false })
                    return;

                await StatusBar.SetTextAsync("Updating QML Language Server...");

                var downloadDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(downloadDir);

                try {
                    await StatusBar.ResetProgressAsync();

                    var tmpPath = Path.Combine(downloadDir, asset.Name);
                    await FileDownloader.DownloadAsync(asset.BrowserDownloadUrl,
                        tmpPath, cancellationToken,
                        async download =>
                        {
                            await StatusBar.ProgressAsync(
                                "Updating QML Language Server... Downloading "
                                + $"{BytesToKilobytes(download.CurrentBytes)} / "
                                + $"{BytesToKilobytes(download.MaxBytes)}",
                                (uint)download.MaxBytes, (uint)download.CurrentBytes);
                        }
                    );

                    await StatusBar.ResetProgressAsync();

                    // Do not use the idle managers cancellation token here, we do not want the
                    // unzip process to stop and leave a corrupted QML Language Server behind.
                    await Utils.ExtractArchiveAsync(tmpPath, QmlLanguageServerManager.ExtractDir,
                        CancellationToken.None,
                        async progress =>
                        {
                            await StatusBar.ProgressAsync(
                                $"Updating QML Language Server... Extracting '{progress.FullName}'"
                                + $": {progress.CurrentEntry} of {progress.TotalEntries} files",
                                (uint)progress.TotalEntries,
                                (uint)progress.CurrentEntry);
                        }
                    );

                    await Utils.WriteAllTextAsync(QmlLanguageServerManager.ReleaseJsonPath,
                        JsonConvert.SerializeObject(new { asset.TagName, asset.Body },
                            JsonSerializer.Settings));

                    if (QtOptionsPage.NotifyQmlLanguageServerUpdateInstalled) {
                        await VsShell.UiThreadAsync(
                            () => Notifications.NotifyQmlLanguageServerUpdateInstalled.Show());
                    }

                    await StatusBar.SetTextAsync("Updated QML Language Server...");
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
