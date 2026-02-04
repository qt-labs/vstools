// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace QtVsTools.Core.Common
{
    using QtVsTools.Common;

    public static partial class Utils
    {
        private static LazyFactory Lazy { get; } = new();

        /// <summary>
        /// Gets the absolute directory path where the currently executing assembly is installed.
        /// </summary>
        public static string PackageInstallPath => Lazy.Get(() => PackageInstallPath, () =>
        {
            var uri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.AbsolutePath)) + @"\";
        });

        /// <summary>
        /// Gets the extension architecture of Visual Studio. Defaults to x86 for VS2019 and x64
        /// for VS2022/VS2026.
        /// </summary>
        public static string Architecture => ResolvedArchitecture.Value;
        private static readonly Lazy<string> ResolvedArchitecture = new(() =>
        {
#if VS2019
            return "x86";
#elif VS2022 || VS2026
            var manifestPath = Path.Combine(PackageInstallPath, "extension.vsixmanifest");
            if (!File.Exists(manifestPath))
                return "x64";
            try {
                var doc = XDocument.Load(manifestPath);
                var arch = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "ProductArchitecture")?.Value;
                return string.Equals(arch, "arm64", IgnoreCase) ? "arm64" : "x64";
            } catch (Exception exception) {
                System.Diagnostics.Debug.WriteLine(exception);
                return "x64";
            }
#endif
        });

        /// <summary>
        /// /// Gets the combined version and extension architecture of Visual Studio.
        /// </summary>
        public static string VersionAndArchitecture => ResolvedVersionAndArchitecture.Value;
        private static readonly Lazy<string> ResolvedVersionAndArchitecture = new(() =>
        {
#if VS2019
            return $"2019-{Architecture}";
#elif VS2022 || VS2026
# if VS2022
            return $"2024-{Architecture}";
# elif VS2026
# error Remove this line if you want to build a special VS2026 version of the VsTools extension.
            return $"2026-{Architecture}";
# endif
#endif
        });

        public static string Unquote(string path)
        {
            path = path.Trim();
            if (!string.IsNullOrEmpty(path) && path.StartsWith("\"") && path.EndsWith("\""))
                return path.Substring(1, path.Length - 2);
            return path;
        }

        public static string SafeQuote(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            path = Unquote(path);
            if (!path.Contains(" "))
                return path;
            if (path.EndsWith("\\"))
                path += Path.DirectorySeparatorChar;
            return $"\"{path}\"";
        }

        /// <summary>
        /// Recursively copies the contents of the given directory and all its children to the
        /// specified target path. If the target path does not exist, it will be created.
        /// </summary>
        /// <param name="directory">The directory to copy.</param>
        /// <param name="targetPath">The path where the contents will be copied to.</param>
        public static void CopyDirectory(string directory, string targetPath)
        {
            var sourceDir = new DirectoryInfo(directory);
            if (!sourceDir.Exists)
                return;

            try {
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);

                var files = sourceDir.GetFiles();
                foreach (var file in files) {
                    try {
                        file.CopyTo(Path.Combine(targetPath, file.Name), true);
                    } catch (Exception exception) {
                        exception.Log();
                    }
                }
            } catch (Exception exception) {
                exception.Log();
            }

            var subDirs = sourceDir.GetDirectories();
            foreach (var subDir in subDirs)
                CopyDirectory(subDir.FullName, Path.Combine(targetPath, subDir.Name));
        }

        /// <summary>
        /// Asynchronously reads the contents of a text file and returns the content as a string.
        /// </summary>
        /// <param name="filePath">The path to the file to read.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the
        /// content of the file as a string.</returns>
        public static async Task<string> ReadAllTextAsync(string filePath)
        {
            using var reader = File.OpenText(filePath);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Asynchronously writes the specified string to a file, creating the file if it does
        /// not exist, and overwriting the content if it does.
        /// </summary>
        /// <param name="filePath">The path to the file to write.</param>
        /// <param name="text">The string to write to the file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteAllTextAsync(string filePath, string text)
        {
            using var writer = File.CreateText(filePath);
            await writer.WriteAsync(text);
        }

        /// <summary>
        /// Snapshot of a directory's files and subdirectories at a point in time. Used to detect
        /// and clean up new entries created by a tool run.
        /// </summary>
        public sealed class DirectorySnapshot
        {
            public string Root { get; }
            public bool Recursive { get; }
            public HashSet<string> Files { get; } = new(CaseIgnorer);
            public HashSet<string> Dirs { get; } = new(CaseIgnorer);

            public DirectorySnapshot(string root, bool recursive)
            {
                Root = root ?? "";
                Recursive = recursive;
            }
        }

        /// <summary>
        /// Creates a snapshot of the current files and directories in <paramref name="dir"/>.
        /// </summary>
        public static DirectorySnapshot TakeSnapshot(string dir, bool recursive = false)
        {
            var snapshot = new DirectorySnapshot(Path.GetFullPath(dir ?? ""), recursive);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return snapshot;

            var searchOption = recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            foreach (var file in Directory.EnumerateFiles(dir, "*", searchOption))
                snapshot.Files.Add(Path.GetFullPath(file));
            foreach (var subDir in Directory.EnumerateDirectories(dir, "*", searchOption))
                snapshot.Dirs.Add(Path.GetFullPath(subDir));

            return snapshot;
        }

        /// <summary>
        /// Deletes files and directories created after <paramref name="snapshot"/> was taken.
        /// Cleanup is best-effort; missing paths or IO errors are ignored.
        /// </summary>
        public static void CleanupNewEntries(string directory, DirectorySnapshot snapshot,
            ISet<string> keepFiles = null, ISet<string> keepDirs = null,
            Option deleteOption = Option.Recursive)
        {
            if (snapshot == null || string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            var keepFilesFull = new HashSet<string>(CaseIgnorer);
            if (keepFiles != null) {
                foreach (var file in keepFiles.Where(x => !string.IsNullOrEmpty(x)))
                    keepFilesFull.Add(Path.GetFullPath(file));
            }

            var keepDirsFull = new HashSet<string>(CaseIgnorer);
            if (keepDirs != null) {
                foreach (var dir in keepDirs.Where(x => !string.IsNullOrEmpty(x)))
                    keepDirsFull.Add(Path.GetFullPath(dir));
            }

            var searchOption = snapshot.Recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            foreach (var file in Directory.EnumerateFiles(directory, "*", searchOption)) {
                var fullPath = Path.GetFullPath(file);
                if (snapshot.Files.Contains(fullPath) || keepFilesFull.Contains(fullPath))
                    continue;
                DeleteFile(fullPath);
            }

            var dirs = Directory.EnumerateDirectories(directory, "*", searchOption)
                .Select(Path.GetFullPath);
            if (snapshot.Recursive)
                dirs = dirs.OrderByDescending(x => x.Length);

            foreach (var dir in dirs) {
                if (snapshot.Dirs.Contains(dir) || keepDirsFull.Contains(dir))
                    continue;
                DeleteDirectory(dir, deleteOption);
            }
        }

        /// <summary>
        /// Deletes a file specified by its path. Does not throw any exceptions.
        /// </summary>
        /// <param name="file">The path to the file to delete.</param>
        public static void DeleteFile(string file)
        {
            try {
                DeleteFile(new FileInfo(file));
            } catch (Exception exception) {
                exception.Log();
            }
        }

        /// <summary>
        /// Deletes a file if it exists. Does not throw any exceptions.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> object representing the file to delete.</param>
        public static void DeleteFile(FileInfo file)
        {
            if (!file?.Exists ?? true)
                return;

            try {
                file.Delete();
            } catch (Exception exception) {
                exception.Log();
            }
        }

        /// <summary>
        /// Specifies the options for directory deletion.
        /// </summary>
        public enum Option
        {
            /// <summary>
            /// Only the specified directory itself is deleted, without deleting its contents.
            /// </summary>
            Shallow,

            /// <summary>
            /// The specified directory and all its contents, including subdirectories, are deleted.
            /// </summary>
            Recursive
        }

        /// <summary>
        /// Recursively deletes the given directory and all its contents. Does not throw any
        /// exceptions.
        /// </summary>
        /// <param name="path">The path of the directory to delete.</param>
        /// <param name="option">Specifies the options for directory deletion.</param>
        public static void DeleteDirectory(string path, Option option = Option.Shallow)
        {
            try {
                DeleteDirectory(new DirectoryInfo(path), option);
            } catch (Exception exception) {
                exception.Log();
            }
        }

        /// <summary>
        /// Recursively deletes the given directory and all its contents. Does not throw any
        /// exceptions.
        /// </summary>
        /// <param name="directory">The directory to delete.</param>
        /// <param name="option">Specifies the options for directory deletion.</param>
        public static void DeleteDirectory(DirectoryInfo directory, Option option = Option.Shallow)
        {
            if (!directory?.Exists ?? true)
                return;

            if (option == Option.Recursive) {
                foreach (var file in directory.GetFiles())
                    DeleteFile(file);

                try {
                    foreach (var subDir in directory.GetDirectories())
                        DeleteDirectory(subDir, option);
                } catch (Exception exception) {
                    exception.Log();
                }
            }

            try {
                directory.Delete();
            } catch (Exception exception) {
                exception.Log();
            }
        }
    }
}
