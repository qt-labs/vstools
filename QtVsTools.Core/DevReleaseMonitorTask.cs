// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

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
                    + @"\.vsix"">");

                devVersion = matches.Cast<Match>()
                    .Select(match =>
                    {
                        var mainVersion = match.Groups["version"].Value;
                        var revision = match.Groups["revision"].Success
                            ? match.Groups["revision"].Value.Replace("rev.", "") : "0";
                        return new System.Version(mainVersion + "." + revision);
                    })
                    .Where(v => currentVersion < v)
                    .Max();

                if (devVersion == null)
                    return;

                Messages.Print(trim: false, text: $@"

    ################################################################
      Qt Visual Studio Tools version {devVersion} PREVIEW available at:
      {requestUri}
    ################################################################");
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
