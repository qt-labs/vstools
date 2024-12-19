/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;

using Tasks = System.Threading.Tasks;

namespace QtVsTools.Core
{
    using Options;
    using VisualStudio;

    using static SyntaxAnalysis.RegExpr;

    public class DevReleaseMonitorTask : IIdleTask
    {
        private const string UrlDownloadQtIo = "https://download.qt.io/development_releases/vsaddin/";

        public async Tasks.Task RunAsync(CancellationToken cancellationToken)
        {
            if (!QtOptionsPage.SearchDevRelease)
                return;

            var currentVersion = new System.Version(Version.PRODUCT_VERSION);
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
                    .Where(v => currentVersion < v)
                    .Max();
                if (devVersion == null)
                    return;

                var requestUri = $"{UrlDownloadQtIo}{devVersion}/";
                response = await http.GetAsync(requestUri, cancellationToken);
                if (!response.IsSuccessStatusCode)
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
}
