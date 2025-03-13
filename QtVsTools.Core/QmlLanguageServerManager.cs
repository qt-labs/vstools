// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

namespace QtVsTools.Core
{
    using Common;

    internal static class JsonSerializer
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        };
    }

    public static class QmlLanguageServerManager
    {
        private const int ReleaseInfoTimeoutMs = 10000; // 10 seconds
        private const string ReleaseInfoUrl = "https://qtccache.qt.io/QMLLS/LatestRelease";

        public static string ExtractDir => Path.Combine(InstallDir, "files");
        public static string ReleaseJsonPath => Path.Combine(InstallDir, "release.json");

        public static string QmlLanguageServerExePath => Path.Combine(ExtractDir, "qmlls.exe");
        public static string InstallDir => Path.Combine(Utils.PackageInstallPath, "qmlls");

        public class Asset
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public long Size { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public string BrowserDownloadUrl { get; set; }
        }

        public class AssetWithTag : Asset
        {
            public string TagName { get; set; }
            public Asset[] Assets { get; set; }
            public string Body { get; set; }
        }

        public class CheckResult
        {
            public string Message { get; set; }
            public bool ShouldInstall { get; set; }
        }

        public static async Task<CheckResult>
            CheckForInstallationUpdateAsync(AssetWithTag asset, CancellationToken token)
        {
            if (!File.Exists(ReleaseJsonPath) || !File.Exists(QmlLanguageServerExePath))
                return new CheckResult { Message = "Not Installed", ShouldInstall = true };

            var local = JsonConvert.DeserializeObject<AssetWithTag>(
                await Utils.ReadAllTextAsync(ReleaseJsonPath), JsonSerializer.Settings);

            if (local.TagName != asset.TagName) {
                return new CheckResult
                {
                    Message = $"Tag mismatch, local = {local.TagName}, recent = {asset.TagName}",
                    ShouldInstall = true
                };
            }

            if (await IsExecutableAsync(QmlLanguageServerExePath, token)) {
                return new CheckResult
                {
                    Message = $"Already Up-to-date, tag = {asset.TagName}", ShouldInstall = false
                };
            }

            return new CheckResult { Message = "Found, but not executable", ShouldInstall = true };
        }

        public static async Task InstallAssetAsync(AssetWithTag asset, CancellationToken token,
            Func<(long CurrentBytes, long MaxBytyes), Task> downloadCallback = null,
            Func<(long TotalEntries, long CurrentEntry, string FullName), Task> extractCallback = null)
        {
            var downloadDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(downloadDir);

            try {
                var tmpPath = Path.Combine(downloadDir, asset.Name);

                await QmlLanguageServerDownloader.DownloadAsync(asset.BrowserDownloadUrl, tmpPath,
                    token, downloadCallback);
                await Utils.ExtractArchiveAsync(tmpPath, ExtractDir, token, extractCallback);

                await Utils.WriteAllTextAsync(ReleaseJsonPath, JsonConvert.SerializeObject(
                    new { asset.TagName, asset.Body }, JsonSerializer.Settings));
            } finally {
                Utils.DeleteDirectory(downloadDir, Utils.Option.Recursive);
            }
        }

        public static async Task<AssetWithTag> FetchAssetAsync(CancellationToken token)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(ReleaseInfoTimeoutMs);

            var response = await httpClient.GetAsync(ReleaseInfoUrl, token);
            response.EnsureSuccessStatusCode();

            var json = JsonConvert.DeserializeObject<AssetWithTag>(
                await response.Content.ReadAsStringAsync(), JsonSerializer.Settings);

            var filteredAssets = json.Assets.Where(a => a.Name.StartsWith("qmlls-windows")).ToList();
            if (!filteredAssets.Any())
                throw new Exception("No suitable package found for platform 'windows'.");

            var latestAsset = filteredAssets.OrderByDescending(a => a.UpdatedAt).First();
            return new AssetWithTag
            {
                TagName = json.TagName,
                Body = json.Body,

                Id = latestAsset.Id,
                Name = latestAsset.Name,
                Size = latestAsset.Size,
                BrowserDownloadUrl = latestAsset.BrowserDownloadUrl,
                CreatedAt = latestAsset.CreatedAt,
                UpdatedAt = latestAsset.UpdatedAt
            };
        }

        private static async Task<bool> IsExecutableAsync(string path, CancellationToken token)
        {
            try {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--help",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync(token);
                return process.ExitCode == 0;
            } catch {
                return false;
            }
        }
    }
}
