// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QtVsTools.Core
{
    internal static class QmlLanguageServerDownloader
    {
        private const int MaxRetries = 3;
        private const int MaxRedirects = 10;
        private const int BufferSize = 81920; // 80 KB
        private static readonly HttpClient HttpClient = new();

        internal static async Task DownloadAsync(string url, string destPath,
            CancellationToken token,
            Func<(long CurrentBytes, long MaxBytyes), Task> callback = null)
        {
            var downloadUrl = url;
            for (var i = 0; i < MaxRedirects; ++i) {
                if (string.IsNullOrEmpty(downloadUrl))
                    throw new InvalidOperationException("Invalid download URL.");

                using var response = await RetryPolicyAsync(() => HttpClient.GetAsync(downloadUrl,
                    HttpCompletionOption.ResponseHeadersRead, token), MaxRetries);

                if (IsRedirect(response.StatusCode) && response.Headers.Location != null) {
                    var redirectUrl = response.Headers.Location;
                    if (!redirectUrl.IsAbsoluteUri)
                        redirectUrl = new Uri(new Uri(downloadUrl), redirectUrl);

                    downloadUrl = redirectUrl.ToString();
                    continue;
                }

                await DownloadOctetStreamAsync(response, destPath, token, callback);
                return;
            }

            throw new InvalidOperationException("Too many redirects.");
        }

        private static async Task DownloadOctetStreamAsync(HttpResponseMessage response,
            string destPath, CancellationToken token,
            Func<(long CurrentBytes, long MaxBytyes), Task> callback = null)
        {
            if (!response.IsSuccessStatusCode) {
                throw new InvalidOperationException($"Unexpected status: {response.StatusCode} "
                    + $"for URL: '{response.RequestMessage?.RequestUri}'.");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != "application/octet-stream")
                Messages.Print("Warning: Content type is not 'application/octet-stream'.");

            var contentLength = response.Content.Headers.ContentLength;
            long downloadedBytes = 0;
            var buffer = new byte[BufferSize];

            try {
                using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write,
                    FileShare.None, BufferSize, useAsync: true);
                using var contentStream = await response.Content.ReadAsStreamAsync();

                while (true) {
                    var read = await contentStream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (read == 0)
                        break;

                    await fileStream.WriteAsync(buffer, 0, read, token);
                    if (callback != null)
                        await callback((downloadedBytes += read, contentLength ?? -1));
                }
            } catch (IOException ex) {
                throw new IOException($"Error writing to file '{destPath}'.", ex);
            }
        }

        private static async Task<HttpResponseMessage>
            RetryPolicyAsync(Func<Task<HttpResponseMessage>> operation, int maxRetries)
        {
            for (var retry = 0; retry < maxRetries; retry++) {
                try {
                    return await operation();
                } catch (HttpRequestException) when (retry < maxRetries - 1) {
                    await Task.Delay(TimeSpan.FromSeconds(2 * (retry + 1))); // Exponential backoff
                }
            }
            throw new HttpRequestException("Failed after maximum retry attempts.");
        }

        private static bool IsRedirect(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.Moved or HttpStatusCode.MovedPermanently => true,
                HttpStatusCode.Found or HttpStatusCode.Redirect => true,
                HttpStatusCode.RedirectMethod or HttpStatusCode.SeeOther => true,
                HttpStatusCode.NotModified
                    or HttpStatusCode.UseProxy
                    or HttpStatusCode.Unused => true,
                HttpStatusCode.RedirectKeepVerb or HttpStatusCode.TemporaryRedirect => true,
                _ => false
            };
        }
    }
}
