// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QtVsTools.Qrc
{
    /// <summary>
    /// Path and prefix helpers for .qrc files. File paths inside a .qrc file are stored relative
    /// to the directory of the .qrc file, using forward slashes.
    /// </summary>
    public static class QrcPath
    {
        /// <summary>
        /// File paths are compared case-insensitively, as on Windows.
        /// </summary>
        public static StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;

        private static readonly char[] Separators = { '/', '\\' };

        /// <summary>
        /// Normalizes a resource prefix: ensures a leading slash, collapses repeated slashes and
        /// removes a trailing slash. An empty prefix becomes "/".
        /// </summary>
        public static string FixPrefix(string prefix)
        {
            var result = new StringBuilder("/");
            foreach (var c in prefix ?? string.Empty) {
                if (c == '/' && result[result.Length - 1] == '/')
                    continue;
                result.Append(c);
            }
            if (result.Length > 1 && result[result.Length - 1] == '/')
                result.Length--;
            return result.ToString();
        }

        // Characters Windows does not allow in paths. Checked explicitly, because .NET Framework
        // and .NET reject different characters in Path.Combine and Path.GetFullPath.
        private static readonly char[] InvalidPathChars = "\"<>|?*".ToCharArray()
            .Concat(Enumerable.Range(0, 32).Select(x => (char)x)).ToArray();

        /// <summary>
        /// Returns the absolute, normalized path of <paramref name="path"/>. Relative paths are
        /// resolved against <paramref name="baseDirectory"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The path is not a valid Windows path, e.g. it
        /// contains '|' or a ':' other than after the drive letter.</exception>
        public static string ToAbsolute(string baseDirectory, string path)
        {
            path = (path ?? string.Empty).Replace('/', Path.DirectorySeparatorChar);
            var combined = string.IsNullOrEmpty(baseDirectory) ? path
                : Path.Combine(baseDirectory, path);
            if (combined.IndexOfAny(InvalidPathChars) >= 0
                || combined.IndexOf(':', Math.Min(2, combined.Length)) >= 0) {
                throw new ArgumentException($"Invalid path '{path}'.", nameof(path));
            }
            try {
                return Path.GetFullPath(combined);
            } catch (Exception exception) when (exception is NotSupportedException
                or PathTooLongException) {
                throw new ArgumentException($"Invalid path '{path}'.", nameof(path), exception);
            }
        }

        /// <summary>
        /// Returns the name under which rcc stores a file: <paramref name="name"/> (the alias or
        /// the relative file path) cleaned like QDir::cleanPath, without leading "../".
        /// </summary>
        public static string ToResourceName(string name)
        {
            var parts = new List<string>();
            foreach (var part in (name ?? string.Empty).Replace('\\', '/').Split('/')) {
                if (part.Length == 0 || part == ".")
                    continue;
                if (part == ".." && parts.Count > 0 && parts[parts.Count - 1] != "..")
                    parts.RemoveAt(parts.Count - 1);
                else
                    parts.Add(part);
            }
            while (parts.Count > 0 && parts[0] == "..")
                parts.RemoveAt(0);
            return string.Join("/", parts);
        }

        /// <summary>
        /// Returns <paramref name="path"/> relative to <paramref name="baseDirectory"/>, using
        /// forward slashes. If both are on different roots, the absolute path is returned.
        /// </summary>
        public static string ToRelative(string baseDirectory, string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            if (string.IsNullOrEmpty(baseDirectory) || !Path.IsPathRooted(path))
                return path.Replace('\\', '/');

            var fullPath = ToAbsolute(null, path);
            var fullBase = ToAbsolute(null, baseDirectory);
            if (!PathComparer.Equals(Path.GetPathRoot(fullPath), Path.GetPathRoot(fullBase)))
                return fullPath.Replace('\\', '/');

            var pathParts = fullPath.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var baseParts = fullBase.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

            var common = 0;
            while (common < pathParts.Length && common < baseParts.Length
                && PathComparer.Equals(pathParts[common], baseParts[common])) {
                common++;
            }

            var parts = Enumerable.Repeat("..", baseParts.Length - common)
                .Concat(pathParts.Skip(common));
            var relative = string.Join("/", parts);
            return relative.Length == 0 ? "." : relative;
        }

        /// <summary>
        /// Returns true if <paramref name="path"/> is not located in
        /// <paramref name="baseDirectory"/> or one of its subdirectories.
        /// </summary>
        public static bool IsOutside(string baseDirectory, string path)
        {
            var relative = ToRelative(baseDirectory, path);
            return relative == ".." || relative.StartsWith("../", StringComparison.Ordinal)
                || Path.IsPathRooted(relative);
        }
    }
}
