// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

using Tasks = System.Threading.Tasks;

namespace QtVsTools.Core
{
    using Common;
    using Options;
    using VisualStudio;

    using static SyntaxAnalysis.RegExpr;

    public static partial class Notifications
    {
        public static SearchDevRelease NotifySearchDevRelease
            => StaticLazy.Get(() => NotifySearchDevRelease, () => new SearchDevRelease());

        public static DownloadDevRelease NotifyShowDevReleaseDownload
            => StaticLazy.Get(() => NotifyShowDevReleaseDownload, () => new DownloadDevRelease());
    }

    public class SearchDevRelease : InfoBarMessage
    {
        protected override ImageMoniker Icon => KnownMonikers.StatusInformation;

        protected override TextSpan[] Text => new TextSpan[]
        {
            new() { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            Utils.EmDash,
            new TextSpacer(2),
            "can auto-search for development releases every 24 hours if Visual Studio has been "
                + "idle for at least 60 seconds."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new()
            {
                Text = "Enable",
                CloseInfoBar = false,
                OnClicked= () =>
                {
                    try {
                        QtOptionsPage.SearchDevRelease = true;
                        QtOptionsPageSettings.Instance.SaveSettings();
                    } catch (Exception ex) {
                        ex.Log();
                    }
                }
            },
            new()
            {
                Text = "Disable",
                CloseInfoBar = false,
                OnClicked= () =>
                {
                    try {
                        QtOptionsPage.SearchDevRelease = false;
                        QtOptionsPageSettings.Instance.SaveSettings();
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
                        QtOptionsPage.NotifySearchDevRelease = false;
                        QtOptionsPage.SaveSettingsToStorageStatic();
                    } catch (Exception ex) {
                        ex.Log();
                    }
                }
            }
        };
    }

    public class DownloadDevRelease : InfoBarMessage
    {
        private string devVersion;
        private string downloadUri;

        private static readonly Lazy<string> ResolvedPlatform = new(() =>
        {
#if VS2019
    return "2019-x86";
#elif VS2022 || VS2026
            var manifestPath = Path.Combine(Utils.PackageInstallPath, "extension.vsixmanifest");
            var doc = XDocument.Load(manifestPath);
            var arch = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "ProductArchitecture")?.Value;
            return string.Equals(arch, "arm64", Utils.IgnoreCase)
# if VS2022
                ? "2022-arm64" : "2022-x64";
# elif VS2026
                ? "2026-arm64" : "2026-x64";
# endif
#endif
        });

        private static string Platform => ResolvedPlatform.Value;

        public void Show(string requestUri, string version)
        {
            devVersion = version;
            downloadUri = requestUri;

            ThreadHelper.ThrowIfNotOnUIThread();
            base.Show();
        }

        public new void Show() => throw new NotSupportedException();

        protected override ImageMoniker Icon => KnownMonikers.StatusInformation;
        protected override TextSpan[] Text => new TextSpan[]
        {
            new() { Bold = true, Text = "Qt Visual Studio Tools" },
            new TextSpacer(2),
            Utils.EmDash,
            new TextSpacer(2),
            "A development release is available for download."
        };

        protected override Hyperlink[] Hyperlinks => new Hyperlink[]
        {
            new()
            {
                Text = "Download",
                CloseInfoBar = true,
                OnClicked = () =>
                {
                    try {
                        ThreadHelper.JoinableTaskFactory.Run(() => DownloadVsixAsync(open: false));
                    } catch (Exception ex) {
                        ex.Log();
                    }
                }
            },
            new()
            {
                Text = "Download & Open Folder",
                CloseInfoBar = true,
                OnClicked = () =>
                {
                    try {
                        ThreadHelper.JoinableTaskFactory.Run(() => DownloadVsixAsync(open: true));
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
                        QtOptionsPage.NotifyShowDevReleaseDownload = false;
                        QtOptionsPage.SaveSettingsToStorageStatic();
                    } catch (Exception ex) {
                        ex.Log();
                    }
                }
            }
        };

        private async Tasks.Task DownloadVsixAsync(bool open)
        {
            await StatusBar.SetTextAsync($"Downloading Qt VS Tools {devVersion}...");

            var package = $"qt-vsaddin-msvc{Platform}-{devVersion}.vsix";
            var targetPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", package);

            await StatusBar.ResetProgressAsync();

            await FileDownloader.DownloadAsync(downloadUri + package,
                targetPath, CancellationToken.None,
                async download =>
                {
                    await StatusBar.ProgressAsync(
                        $"Downloading Qt VS Tools {devVersion}... Progress "
                            + $"{BytesToKilobytes(download.CurrentBytes)} / "
                            + $"{BytesToKilobytes(download.MaxBytes)}",
                        (uint)download.MaxBytes, (uint)download.CurrentBytes);
                });

            await StatusBar.ResetProgressAsync();
            await StatusBar.ClearAsync();

            if (open) {
                var folder = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(folder))
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{targetPath}\"");
            } else {
                await StatusBar.SetTextAsync($"Downloaded Qt VS Tools to: {targetPath}");
            }
        }

        private static string BytesToKilobytes(long bytes)
        {
            return (bytes / 1024.0).ToString("0.00") + " KB";
        }
    }

    public class DevReleaseMonitorTask : IIdleTask
    {
        private const string UrlDownloadQtIo = "https://download.qt.io/development_releases/vsaddin/";

        public async Tasks.Task RunAsync(CancellationToken cancellationToken)
        {
            if (!QtOptionsPage.SearchDevRelease)
                return;

            var currentVersion = new System.Version(Version.PRODUCT_VERSION);
            var currentVersionNoRevision = currentVersion.ToVersionWithoutRevision();
            try {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(QtOptionsPage.SearchDevReleaseTimeout);
                var response = await http.GetAsync(UrlDownloadQtIo, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return;

                var tokenVersion = new Token("VERSION", Number & "." & Number & "." & Number)
                {
                    new Rule<System.Version> { Capture(value => new System.Version(value)) }
                };
                var regexHrefVersion = "href=\"" & tokenVersion & Chars["/"].Optional() & "\"";
                var regexResponse = (regexHrefVersion | AnyChar | VertSpace).Repeat();
                var parserResponse = regexResponse.Render();

                var responseData = await response.Content.ReadAsStringAsync();
                var devVersion = parserResponse.Parse(responseData)
                    .GetValues<System.Version>("VERSION")
                    .Where(v => v >= currentVersionNoRevision)
                    .Max();
                if (devVersion == null)
                    return;

                var requestUri = $"{UrlDownloadQtIo}{devVersion}/";
                response = await http.GetAsync(requestUri, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return;

                responseData = await response.Content.ReadAsStringAsync();
                var matches = Regex.Matches(responseData, @"<a href=""qt-vsaddin-[a-zA-Z0-9-]+-"
                    + @"(?<version>\d+\.\d+\.\d+)" // capture the main version
                    + @"(?:-(?<revision>rev\.\d+))?" // optionally capture the revision part
                    + @"\.vsix(?:[\""?])");

                var versionMatches = matches.Cast<Match>()
                    .Select(match =>
                    {
                        var mainVersion = match.Groups["version"].Value;
                        var revision = match.Groups["revision"].Success
                            ? match.Groups["revision"].Value
                            : null;

                        var parsedVersion = revision != null
                            ? new System.Version(mainVersion + "." + revision.Replace("rev.", ""))
                            : new System.Version(mainVersion);

                        var originalString = revision != null
                            ? $"{mainVersion}-{revision}"
                            : mainVersion;

                        return (Parsed: parsedVersion, Original: originalString);
                    });

                var newerVersions = versionMatches
                    .Where(v => currentVersion < v.Parsed);

                var devVersionInfo = newerVersions
                    .OrderByDescending(v => v.Parsed)
                    .FirstOrDefault();

                if (devVersionInfo.Parsed == null)
                    return;

                Messages.Print(trim: false, text: $@"

    ################################################################
      Qt Visual Studio Tools version {devVersionInfo.Original} PREVIEW available at:
      {requestUri}
    ################################################################");

                if (QtOptionsPage.NotifyShowDevReleaseDownload) {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    Notifications.NotifyShowDevReleaseDownload.Show(requestUri, devVersionInfo.Original);
                }
            } catch (Tasks.TaskCanceledException) {
                // ignore
            } catch (Exception exception) {
                exception.Log();
            }
        }
    }

    internal static class VersionExtension
    {
        public static System.Version ToVersionWithoutRevision(this System.Version version)
        {
            return new System.Version(version.Major, version.Minor, version.Build);
        }
    }
}
